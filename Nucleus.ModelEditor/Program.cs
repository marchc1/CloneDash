using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor
{
	public class ModelEditor : Level
	{

		public EditorPanel Editor;
		public OutlinerPanel Outliner;
		public PropertiesPanel Properties;

		public override void Initialize(params object[] args) {
			Menubar menubar = UI.Add<Menubar>();
			Keybinds.AddKeybind([KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new ModelEditor()));

			UI.Add(out Outliner);
		}
	}

	internal class Program
	{
		static void Main(string[] args) {
			EngineCore.Initialize(1600, 900, "Model v4 Editor", args);
			EngineCore.GameInfo = new() {
				GameName = "Model v4 Editor"
			};

			EngineCore.LoadLevel(new ModelEditor());
			EngineCore.Start();
		}
	}
}
