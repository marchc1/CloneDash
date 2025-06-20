using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class SkinsList() : EditorList<EditorSkin>("skin", "skins")
	{
		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Skin", () => PropertiesPanel.NewSkinDialog(ModelEditor.Active.File, Model))
			]);
		}
	}
}
