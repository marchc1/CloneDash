using Newtonsoft.Json;

namespace Nucleus.ModelEditor
{
	public class EditorSkin : IEditorType
	{
		public EditorModel Model { get; set; }
		public string Name { get; set; } = "";

		[JsonIgnore] public string SingleName => "skin";

		[JsonIgnore] public string PluralName => "skins";

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore] public bool Hidden { get; set; }

		public bool CanHide() => true;

		public bool GetVisible() => Model.ActiveSkin == this;
		public void OnShown() => ModelEditor.Active.File.SetActiveSkin(this);
		public void OnHidden() => ModelEditor.Active.File.UnsetActiveSkin(this);
	}
}
