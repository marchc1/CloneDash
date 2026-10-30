using Newtonsoft.Json;
using Nucleus.Models.Runtime;

namespace Nucleus.ModelEditor
{
	public class EditorSkin : IEditorType, IModel4Nameable
	{
		public EditorModel GetModel() => Model;
		public EditorModel Model { get; set; }
		public string Name { get; set; } = "";

		// only active while this skin is
		public List<EditorBone> Bones { get; set; } = [];
		public List<EditorConstraint> Constraints { get; set; } = [];

		public IEditorType? DeferTransformationsTo() => null;

		[JsonIgnore] public string SingleName => "skin";

		[JsonIgnore] public string PluralName => "skins";

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore] public bool Hidden { get; set; }

		public bool CanHide() => true;
		public bool CanDelete() => true;
		public string GetName() => Name;

		public EditorResult Remove() => ModelEditor.Active.File.RemoveSkin(Model, this);
		public bool IsNameTaken(string name) => Model.Skins.Any(x => x.Name == name);
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameSkin(Model, this, newName);

		public void BuildTopOperators(Nucleus.UI.Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}

		public IEnumerable<EditorAttachment> GetAttachments() {
			var attachments = new List<EditorAttachment>();
			foreach (var slot in Model.Slots) {
				foreach (var attachment in slot.Attachments) {
					if (attachment.Skin == this)
						attachments.Add(attachment);
				}
			}
			return attachments;
		}
		public bool GetVisible() => Model.ActiveSkin == this;
		public void OnShown() => ModelEditor.Active.File.SetActiveSkin(this);
		public void OnHidden() => ModelEditor.Active.File.UnsetActiveSkin(this);
	}
}
