using Nucleus.ModelEditor.UI;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class AnimationsList() : EditorList<EditorAnimation>("animation", "animations")
	{
		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Animation", () => PropertiesPanel.NewAnimationDialog(ModelEditor.Active.File, this)),
				new("Folder", () => AnimationFolder.NewFolderDialog(Model, "")),
			]);
		}
	}

	public class AnimationFolder : IEditorType
	{
		public EditorModel Model { get; set; }
		public EditorModel GetModel() => Model;
		public string SingleName => "folder";
		public string PluralName => "folders";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		/// <summary>
		/// Full path, like "emotes" or "emotes/happy"
		/// </summary>
		public string Path { get; set; } = "";
		public string GetName() => Leaf(Path);
		public bool CanHide() => false;
		public bool CanRename() => true;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameAnimationFolder(Model, Path, newName);
		public bool IsNameTaken(string name) => ModelEditor.Active.File.AnimationFolderPaths(Model).Contains(Join(Parent(Path), name));

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Animation", () => NewAnimationDialog(Model, Path)),
				new("Folder", () => NewFolderDialog(Model, Path)),
			]);
		}

		public static string Leaf(string path) => path.Contains('/') ? path.Substring(path.LastIndexOf('/') + 1) : path;
		public static string Parent(string path) => path.Contains('/') ? path.Substring(0, path.LastIndexOf('/')) : "";
		public static string Join(string folder, string name) => folder.Length == 0 ? name : $"{folder}/{name}";

		public static void NewFolderDialog(EditorModel model, string parent) {
			EditorDialogs.TextInput("New Folder", "Enter the name for the new folder.", "", true, (name) => {
				var result = ModelEditor.Active.File.AddAnimationFolder(model, Join(parent, name.ToString()));
				if (!result.Succeeded)
					EditorDialogs.ConfirmAction("Folder creation error", result.Reason, true,
						() => NewFolderDialog(model, parent));
			}, null);
		}

		public static void NewAnimationDialog(EditorModel model, string folder) {
			EditorDialogs.TextInput("New Animation", "Enter the name for the new animation.", "", true, (name) => {
				var result = ModelEditor.Active.File.AddAnimation(model, Join(folder, name.ToString()));
				if (result.Failed)
					EditorDialogs.ConfirmAction("Animation creation error", result.Reason, true,
						() => NewAnimationDialog(model, folder));
			}, null);
		}
	}
}
