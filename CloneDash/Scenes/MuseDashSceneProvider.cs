using CloneDash.Modding.Descriptors;
using CloneDash.Scenes;

using Nucleus.Files;

namespace CloneDash.Characters;

public class MuseDashSceneProvider : ISceneProvider
{
	int ISceneProvider.Priority => 10000000;

	IEnumerable<string> ISceneProvider.GetAvailable() {
		foreach (var scene in MuseDashSceneInfo.GetScenes())
			yield return scene.MapName;
	}

	ISceneDescriptor? ISceneProvider.FindByName(ReadOnlySpan<char> name) {
		MuseDashScene? descriptor = MuseDashScene.GetScene(name);
		if (descriptor == null) return null;

		descriptor.MountToFilesystem();
		return descriptor;
	}
}