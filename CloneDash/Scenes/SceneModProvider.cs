using CloneDash.Modding.Descriptors;
using CloneDash.Scenes;
using Nucleus.Files;

namespace CloneDash.Characters;

public class SceneModProvider : ISceneProvider
{
	int ISceneProvider.Priority => 10000000;

	IEnumerable<string> ISceneProvider.GetAvailable() {
		var dirs = Filesystem.FindDirectories("scenes", "");
		return dirs;
	}

	ISceneDescriptor? ISceneProvider.FindByName(string name) {
		var descriptor = CD_SceneDescriptor.ParseScene(Path.Combine(name, "scene.cdd"));
		if (descriptor == null) return null;

		descriptor.Filename = name;
		descriptor.MountToFilesystem();
		return descriptor;
	}
}