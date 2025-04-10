using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.Types;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Reflection;

namespace Nucleus.ModelEditor;

public interface IBoneEditorTimeline
{
	public EditorBone Bone { get; set; }
}
public interface ISlotEditorTimeline
{
	public EditorSlot Slot { get; set; }
}

public abstract class EditorTimeline
{
	public EditorBone AssociatedBone { get; set; }
	public abstract void Apply(EditorModel model, float time);
}

public abstract class CurveEditorTimeline : EditorTimeline;

public abstract class CurveEditorTimeline1 : CurveEditorTimeline
{
	public FCurve<float> Curve = new();
}
public abstract class CurveEditorTimeline2 : CurveEditorTimeline
{
	public FCurve<float> Curve1 = new();
	public FCurve<float> Curve2 = new();
	
	public void Split<T>(out T t1, out T t2) where T : CurveEditorTimeline1, new() {
		t1 = new();
		t2 = new();

		t1.Curve = Curve1;
		t2.Curve = Curve2;

		return;
	}
}

public class TranslateTimeline : CurveEditorTimeline2, IBoneEditorTimeline
{
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, float time) {
		Bone.PositionX = Curve1.DetermineValueAtTime(time);
		Bone.PositionY = Curve2.DetermineValueAtTime(time);
	}
}

public class TranslateXTimeline : CurveEditorTimeline1, IBoneEditorTimeline
{
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, float time) {
		Bone.PositionX = Curve.DetermineValueAtTime(time);
	}
}

public class TranslateYTimeline : CurveEditorTimeline1, IBoneEditorTimeline
{
	public EditorBone Bone { get; set; }
	public override void Apply(EditorModel model, float time) {
		Bone.PositionY = Curve.DetermineValueAtTime(time);
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
