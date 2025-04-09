using Newtonsoft.Json;
using Nucleus.Models;
using System.Diagnostics;
using System.Reflection;

namespace Nucleus.ModelEditor;

public interface IBoneEditorTimeline {
	public EditorBone Bone { get; set; }
}
public interface ISlotEditorTimeline {
	public EditorSlot Slot { get; set; }
}

public abstract class EditorTimeline
{
	public abstract void Apply(EditorModel model, float time);
}

public class AttachmentTimeline : EditorTimeline, ISlotEditorTimeline
{
	public EditorSlot Slot { get; set; }
	public FCurve<EditorAttachment?> Attachments = new();
	public override void Apply(EditorModel model, float time) {
		var currentAttachment = Attachments.DetermineValueAtTime(time);

		Slot.SetActiveAttachment(currentAttachment);
	}
}


public class EditorAnimation : IEditorType
{
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

	public List<EditorTimeline> Timelines = [];
	public bool Export { get; set; } = true;

	public string GetName() => Name;

	public void Apply(float time) {
		foreach(var timeline in Timelines) {
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
