using Nucleus.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class SkinsList() : EditorList<EditorSkin>("skins", "skins")
	{
		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Skin", () => PropertiesPanel.NewSkinDialog(ModelEditor.Active.File, Model))
			]);
		}
	}
}
