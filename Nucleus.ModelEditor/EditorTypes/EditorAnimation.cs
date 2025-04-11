using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;

namespace Nucleus.ModelEditor;

public enum KeyframeState
{
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

public interface IProperty<T> {
	public T GetValue();
}

public interface IBoneProperty<T> : IBoneTimeline, IProperty<T>
{

}
public interface ISlotProperty<T> : ISlotTimeline, IProperty<T>
{

}

public abstract class EditorTimeline
{
	public static readonly Color TIMELINE_COLOR_ROTATION = new(50, 255, 50);
	public static readonly Color TIMELINE_COLOR_TRANSLATE = new(50, 50, 255);
	public static readonly Color TIMELINE_COLOR_SCALE = new(255, 50, 50);
	public static readonly Color TIMELINE_COLOR_SHEAR = new(255, 255, 70);
	/// <summary>
	/// Optional <see cref="Color"/>, used in the dope sheet
	/// </summary>
	public virtual Color Color => Color.White;
	public abstract void Apply(EditorModel model, double time);
	public abstract KeyframeState KeyframedAt(double time);
	public abstract double CalculateMaxTime();

	public abstract IEnumerable<double> GetKeyframeTimes();
}

public interface IKeyframeQueryable<T>
{
	public abstract bool KeyframedAtTime(double time);
	public abstract bool TryGetValueAtTime(double time, out T? value);
	public abstract void InsertKeyframe(double time, T value);
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

	public void InsertKeyframe(double time, float value) {
		Curve.AddKeyframe(new(time, value));
	}

	public override double CalculateMaxTime() => Curve.Last.Time;

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach(var keyframe in Curve.GetKeyframes())
			yield return keyframe.Time;
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

	public void InsertKeyframe(double time, Vector2F value) {
		CurveX.AddKeyframe(new(time, value.X));
		CurveY.AddKeyframe(new(time, value.Y));
	}

	public override double CalculateMaxTime() => CurveX.Last.Time;

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach (var keyframe in CurveX.GetKeyframes())
			yield return keyframe.Time;
	}
}

public class RotationTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_ROTATION;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.Rotation = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.Rotation;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}


public class TranslateTimeline : CurveTimeline2, IBoneProperty<Vector2F>
{
	public override Color Color => TIMELINE_COLOR_TRANSLATE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.Position = new(
			CurveX.DetermineValueAtTime(time),
			CurveY.DetermineValueAtTime(time)
		);
	}
	public Vector2F GetValue() => Bone.Position;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class TranslateXTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_TRANSLATE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.PositionX = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.PositionX;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class TranslateYTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_TRANSLATE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.PositionY = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.PositionY;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}





public class ScaleTimeline : CurveTimeline2, IBoneProperty<Vector2F>
{
	public override Color Color => TIMELINE_COLOR_SCALE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.Scale = new(
			CurveX.DetermineValueAtTime(time),
			CurveY.DetermineValueAtTime(time)
		);
	}
	public Vector2F GetValue() => Bone.Scale;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class ScaleXTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_SCALE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.ScaleX = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.ScaleX;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class ScaleYTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_SCALE;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.ScaleY = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.ScaleY;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}






public class ShearTimeline : CurveTimeline2, IBoneProperty<Vector2F>
{
	public override Color Color => TIMELINE_COLOR_SHEAR;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.Shear = new(
			CurveX.DetermineValueAtTime(time),
			CurveY.DetermineValueAtTime(time)
		);
	}
	public Vector2F GetValue() => Bone.Shear;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class ShearXTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_SHEAR;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.ShearX = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.ShearX;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
}

public class ShearYTimeline : CurveTimeline1, IBoneProperty<float>
{
	public override Color Color => TIMELINE_COLOR_SHEAR;
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, double time) {
		Bone.ShearY = Curve.DetermineValueAtTime(time);
	}
	public float GetValue() => Bone.ShearY;
	public override KeyframeState KeyframedAt(double time) => !TryGetValueAtTime(time, out var key) ? KeyframeState.NotKeyframed : key == GetValue() ? KeyframeState.Keyframed : KeyframeState.PendingKeyframe;
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

	public double CalculateMaxTime() {
		double time = 0;

		foreach(var tl in Timelines) {
			var tlTime = tl.CalculateMaxTime();
			if (tlTime > time)
				time = tlTime;
		}

		return time;
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

	public (T Timeline, bool Created) GetTimeline<T>(EditorBone bone, bool createIfMissing = true) where T : EditorTimeline, IBoneTimeline, new() {
		T? timeline = Timelines.FirstOrDefault(x => x is T tTimeline && tTimeline.Bone == bone) as T;
		(T Timeline, bool Created) result = (null, false);
		if (timeline == null) {
			if (!createIfMissing) return result;

			result.Created = true;
			timeline = new();
			timeline.Bone = bone;
			Timelines.Add(timeline);
		}

		result.Timeline = timeline;
		return result;
	}

	public EditorTimeline? SearchTimelineByProperty(IEditorType? type, KeyframeProperty property, int arrayIndex, bool createIfMissing) {
		var tl = SearchTimelineByProperty(type, property, out var _, arrayIndex, createIfMissing);
		return tl;
	}
	public EditorTimeline? SearchTimelineByProperty(IEditorType? type, KeyframeProperty property, out bool created, int arrayIndex, bool createIfMissing) {
		(EditorTimeline Timeline, bool Created) info = type switch {
			EditorBone bone => property switch {
				KeyframeProperty.None => new(null, false),
				KeyframeProperty.Bone_Rotation => GetTimeline<RotationTimeline>(bone, createIfMissing),
				KeyframeProperty.Bone_Translation => arrayIndex switch {
					-1 => GetTimeline<TranslateTimeline>(bone, createIfMissing),
					0 => GetTimeline<TranslateXTimeline>(bone, createIfMissing),
					1 => GetTimeline<TranslateYTimeline>(bone, createIfMissing),
					_ => new(null, false)
				},
				KeyframeProperty.Bone_Scale => arrayIndex switch {
					-1 => GetTimeline<ScaleTimeline>(bone, createIfMissing),
					0 => GetTimeline<ScaleXTimeline>(bone, createIfMissing),
					1 => GetTimeline<ScaleYTimeline>(bone, createIfMissing),
					_ => new(null, false)
				},
				KeyframeProperty.Bone_Shear => arrayIndex switch {
					-1 => GetTimeline<ShearTimeline>(bone, createIfMissing),
					0 => GetTimeline<ShearXTimeline>(bone, createIfMissing),
					1 => GetTimeline<ShearYTimeline>(bone, createIfMissing),
					_ => new(null, false)
				},
				_ => throw new Exception("Missing property to search.")
			},
			_ => new(null, false)
		};

		created = info.Created;
		return info.Timeline;
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

	public void Apply(double time) {
		foreach (var timeline in Timelines) {
			timeline.Apply(Model, time);
		}
	}

	public virtual void OnHidden() {
		ModelEditor.Active.File.UnsetActiveAnimation(Model);
		// Force selection (pull out of any operators etc)
		ModelEditor.Active.File.DeactivateOperator(true);
	}
	public virtual void OnShown() {
		ModelEditor.Active.File.SetActiveAnimation(Model, this);
		ModelEditor.Active.File.DeactivateOperator(true);
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

	internal List<EditorBone> GetAffectedBones() {
		HashSet<EditorBone> bones = [];

		foreach(var timeline in Timelines) {
			switch (timeline) {
				case IBoneTimeline boneTimeline: bones.Add(boneTimeline.Bone); break;
				case ISlotTimeline slotTimeline: bones.Add(slotTimeline.Slot.Bone); break;
			}
		}

		return bones.ToList();
	}

	//public EditorResult Remove() => ModelEditor.Active.File
}
