using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class AnimationsList() : EditorList<EditorAnimation>("animation", "animations")
	{
		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Animation", () => {})
			]);
		}
	}
}
