using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.Types;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;

namespace Nucleus.ModelEditor;

public enum KeyframeState {
	NotKeyframed,
	PendingKeyframe,
	Keyframed
}

public interface IBoneTimeline
{
	public EditorBone Bone { get; set; }
}

public interface ISlotTimeline
{
	public EditorSlot Slot { get; set; }
}

public interface IBoneProperty<T> : IBoneTimeline
{
	public T GetValue();
	public KeyframeState KeyframedAt(double time);
}
public interface ISlotProperty<T> : ISlotTimeline
{
	public T GetValue();
	public KeyframeState KeyframedAt(double time);
}

public interface IKeyframableProperty<TargetType, ValueType> {
	public ValueType GetValue(TargetType target);
}

public abstract class EditorTimeline
{
	public abstract void Apply(EditorModel model, float time);
}

public interface IKeyframeQueryable<T> {
	public abstract bool KeyframedAtTime(double time);
	public abstract bool TryGetValueAtTime(double time, out T? value);
}

public abstract class CurveTimeline1 : EditorTimeline, IKeyframeQueryable<float>
{
	public FCurve<float> Curve = new();

	public bool KeyframedAtTime(double time) => Curve.TryFindKeyframe(time, out var _);
	public bool TryGetValueAtTime(double time, out float value) {
		var found = Curve.TryFindKeyframe(time, out var key);
		value = 0;
		if (!found) return false;

		value = key?.Value ?? 0;
		return true;
	}
}
public abstract class CurveTimeline2 : EditorTimeline, IKeyframeQueryable<Vector2F>
{
	public FCurve<float> CurveX = new();
	public FCurve<float> CurveY = new();

	public bool KeyframedAtTime(double time) => CurveX.TryFindKeyframe(time, out var _);
	public bool TryGetValueAtTime(double time, out Vector2F value) {
		var foundX = CurveX.TryFindKeyframe(time, out var keyX);
		var foundY = CurveY.TryFindKeyframe(time, out var keyY);
		Debug.Assert(foundX == foundY);

		value = Vector2F.Zero;
		if (!foundX) return false;

		value = new(keyX?.Value ?? 0, keyY?.Value ?? 0);
		return true;
	}
}

public class TranslateTimeline : CurveTimeline2, IBoneProperty<Vector2F>
{
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, float time) {
		Bone.Position = new(
			CurveX.DetermineValueAtTime(time),
			CurveY.DetermineValueAtTime(time)
		);
	}
	public Vector2F GetValue() => Bone.Position;
	public KeyframeState KeyframedAt(double time) {
		if (!TryGetValueAtTime(time, out var key))
			return KeyframeState.NotKeyframed;
		return key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
	}
}


public class EditorAnimation : IEditorType
{
	public EditorModel GetModel() => Model;
	public EditorModel Model { get; set; }
	[JsonIgnore] public string SingleName => "animation";
	[JsonIgnore] public string PluralName => "animations";

	[JsonIgnore] public bool Hovered { get; set; }
	[JsonIgnore] public bool Selected { get; set; }

	[JsonIgnore]
	public bool Hidden {
		get => Model.ActiveAnimation != this;
		set { }
	}

	public string Name { get; set; }

	public Dictionary<KeyframeProperty, HashSet<EditorBone>> SeparatedProperties = [];
	public bool DoesBoneHaveSeparatedProperty(EditorBone bone, KeyframeProperty property) {
		if (!SeparatedProperties.TryGetValue(property, out var hs)) {
			hs = [];
			SeparatedProperties[property] = hs;
		}

		return hs.Contains(bone);
	}
	public void SetDoesBoneHaveSeparatedProperty(EditorBone bone, KeyframeProperty property, bool state) {
		if (!SeparatedProperties.TryGetValue(property, out var hs)) {
			hs = [];
			SeparatedProperties[property] = hs;
		}

		if (state)
			hs.Add(bone);
		else
			hs.Remove(bone);
	}

	public List<EditorTimeline> Timelines = [];

	public T GetTimeline<T>(EditorBone bone, bool createIfMissing = true) where T : EditorTimeline, IBoneTimeline, new() {
		T? timeline = Timelines.FirstOrDefault(x => x is T tTimeline && tTimeline.Bone == bone) as T;
		if(timeline == null) {
			if (!createIfMissing) return null;

			timeline = new();
			timeline.Bone = bone;
		}

		return timeline;
	}

	public T GetTimeline<T>(EditorSlot slot, bool createIfMissing = true) where T : EditorTimeline, ISlotTimeline, new() {
		T? timeline = Timelines.FirstOrDefault(x => x is T tTimeline && tTimeline.Slot == slot) as T;
		if (timeline == null) {
			if (!createIfMissing) return null;

			timeline = new();
			timeline.Slot = slot;
		}

		return timeline;
	}


	public bool Export { get; set; } = true;

	public string GetName() => Name;

	public void Apply(float time) {
		foreach (var timeline in Timelines) {
			timeline.Apply(Model, time);
		}
	}

	public virtual void OnHidden() {
		ModelEditor.Active.File.UnsetActiveAnimation(Model);
		// Force selection (pull out of any operators etc)
		ModelEditor.Active.File.DeactivateOperator(true);
		ModelEditor.Active.SelectObject(this);
	}
	public virtual void OnShown() {
		ModelEditor.Active.File.SetActiveAnimation(Model, this);
		ModelEditor.Active.File.DeactivateOperator(true);
		ModelEditor.Active.SelectObject(this);
	}

	public virtual bool CanTranslate() => false;
	public virtual bool CanRotate() => false;
	public virtual bool CanScale() => false;
	public virtual bool CanShear() => false;
	public virtual bool CanHide() => true;

	public virtual bool GetVisible() => !Hidden;

	public virtual bool CanRename() => true;
	public virtual bool CanDelete() => true;
	public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameAnimation(Model, this, newName);
	public virtual void Render() { }
	public virtual void RenderOverlay() { }

	//public EditorResult Remove() => ModelEditor.Active.File
}
