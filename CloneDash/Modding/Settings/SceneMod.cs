using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class SceneMod
	{
		public static ConVar clonedash_scene = ConVar.Register("clonedash_scene", "clonedash", ConsoleFlags.Saved, "Your scene.");
		public static ConCommand clonedash_allscenes = ConCommand.Register("clonedash_allscenes", (_, _) => {
			var scenes = GetAvailableScenes();
			foreach (var scene in scenes)
				Logs.Print($"    {scene}");
		}, "Prints all available scenes");

		public static string[] GetAvailableScenes() {
			var files = Filesystem.FindDirectories("scenes", "*.cdd", absolutePaths: false);
			return files.ToArray();
		}
		public static SceneDescriptor[] GetAvailableSceneDescriptors() {
			var files = Filesystem.FindDirectories("scenes", "").ToArray();
			var descriptors = new SceneDescriptor[files.Length];
			for (int i = 0; i < files.Length; i++) {
				descriptors[i] = SceneDescriptor.ParseFile(Path.Combine(files[i], "scene.cdd"));
			}
			return descriptors;
		}

		public static SceneDescriptor? GetCharacterData() {
			string name = clonedash_scene.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
			}

			var path = Filesystem.Resolve($"{name}/scene.cdd", "scenes", false);
			if (path == null || !File.Exists(path)) {
				Logs.Warn($"WARNING: Bad scene name '{name}'! Refusing to load SceneDescriptor!");
				return null;
			}

			SceneDescriptor descriptor = SceneDescriptor.ParseFile(path);
			descriptor.Filepath = path;
			return descriptor;
		}
	}
}

