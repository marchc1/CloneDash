using CloneDash.Modding.Descriptors;
using CloneDash.Scenes;

using Nucleus.Files;

namespace CloneDash.Characters;

public class MuseDashSceneProvider : ISceneProvider
{
	int ISceneProvider.Priority => 10000000;

	// todo: find out where this these are defined instead
	public const int MAX_SCENES = 12;

	IEnumerable<string> ISceneProvider.GetAvailable() {
		for (int i = 1; i <= MAX_SCENES; i++) {
			yield return $"scene_{i.ToString().PadLeft(2, '0')}";
		}
	}

	ISceneDescriptor? ISceneProvider.FindByName(ReadOnlySpan<char> name) {
		MuseDashScene descriptor = MuseDashScene.GetScene(name);
		if (descriptor == null) return null;

		descriptor.MountToFilesystem();
		return descriptor;
	}
}