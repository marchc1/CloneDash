using Newtonsoft.Json;
using Nucleus.Models;
using System.Diagnostics;
using System.Reflection;

namespace Nucleus.ModelEditor;

public abstract class EditorTimeline
{
	public static Type[] GetTimelineTypes()
							=> Assembly.GetAssembly(typeof(EditorTimeline)).GetTypes()
							.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(EditorTimeline)))
							.ToArray();
	public abstract void Apply(EditorModel model, float time);
}

public class AttachmentTimeline : EditorTimeline
{
	public FCurve<EditorAttachment?> Attachments = new();
	public EditorSlot TargetSlot;
	public override void Apply(EditorModel model, float time) {
		var currentAttachment = Attachments.DetermineValueAtTime(time);

		TargetSlot?.SetActiveAttachment(currentAttachment);
	}
}


public class EditorAnimation : IEditorType
{
	public EditorModel Model { get; set; }
	[JsonIgnore] public string SingleName => "animation";
	[JsonIgnore] public string PluralName => "animations";

	[JsonIgnore] public bool Hovered { get; set; }
	[JsonIgnore] public bool Selected { get; set; }
	[JsonIgnore] public bool Hidden { get; set; }

	public string Name { get; set; }
	public bool Export { get; set; } = true;

	public string GetName() => Name;
}
