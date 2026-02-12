using CloneDash.Modding.Descriptors;
using CloneDash.Scenes;

using Nucleus.Files;

namespace CloneDash.Characters;

public class SceneModProvider : ISceneProvider
{
	int ISceneProvider.Priority => 10000000;

	IEnumerable<string> ISceneProvider.GetAvailable() {
		var dirs = filesystem.FindDirectories("scenes", "");
		return dirs;
	}

	ISceneDescriptor? ISceneProvider.FindByName(ReadOnlySpan<char> name) {
		var descriptor = CloneDashScene.ParseScene(Path.Combine(new(name), "scene.cdd"));
		if (descriptor == null) return null;

		descriptor.Filename = new(name);
		descriptor.MountToFilesystem();
		return descriptor;
	}
}