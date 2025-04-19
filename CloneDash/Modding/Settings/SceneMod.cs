using CloneDash.Data;
using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class SceneMod
	{
		public static ConVar clonedash_scene = ConVar.Register("clonedash_scene", "clonedash", ConsoleFlags.Saved, "Your scene.");
		public static ConVar clonedash_allowsceneoverride = ConVar.Register("clonedash_allowsceneoverride", "1", ConsoleFlags.Saved, "If true (and the scene specified exists on-disk), allows charts to specify the scene used during gameplay. If false, will always use clonedash_scene.", 0, 1);
		public static ConCommand clonedash_allscenes = ConCommand.Register("clonedash_allscenes", (_, _) => {
			var scenes = GetAvailableScenes();
			foreach (var scene in scenes)
				Logs.Print($"    {scene}");
		}, "Prints all available scenes");

		public static string[] GetAvailableScenes() {
			var files = Filesystem.FindDirectories("scenes", "");
			return files.ToArray();
		}
		public static SceneDescriptor[] GetAvailableSceneDescriptors() {
			var dirs = Filesystem.FindDirectories("scenes", "").ToArray();
			var descriptors = new SceneDescriptor?[dirs.Length];

			for (int i = 0; i < dirs.Length; i++)
				descriptors[i] = SceneDescriptor.ParseScene(Path.Combine(dirs[i], "scene.cdd"));

			var notNull = 0;
			for (int i = 0; i < dirs.Length; i++)
				if (descriptors[i] != null) notNull++;

			var notNullReturn = new SceneDescriptor[notNull];
			var notNullPtr = 0;
			for (int i = 0; i < dirs.Length; i++) {
				var descriptor = descriptors[i];
				if (descriptor != null) {
					notNullReturn[notNullPtr] = descriptor;
					notNullPtr++;
				}
			}

			return notNullReturn;
		}

		public static SceneDescriptor? GetSceneData(ChartSong? song = null) {
			SceneDescriptor? descriptor;

			if (song != null && clonedash_allowsceneoverride.GetBool()) {
				var sceneName = song.GetInfo().Scene;
				descriptor = SceneDescriptor.ParseScene(sceneName);
				if (descriptor != null) return descriptor;
				Logs.Warn($"WARNING: Song scene override is enabled, but the scene '{sceneName}' doesn't exist in Clone Dash! Falling back to clonedash_scene...");
			}

			string name = clonedash_scene.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
			}

			descriptor = SceneDescriptor.ParseFile(name);
			if(descriptor == null) {
				Logs.Warn($"WARNING: The scene '{name}' could not be found by the file system!");
				return null;
			}
			descriptor.Filename = name;
			return descriptor;
		}
	}
}

