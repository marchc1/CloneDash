using CloneDash.Common.Scenes;
using CloneDash.Scenes;

using Nucleus.Files;

namespace CloneDash.Characters;

public class MuseDash1SceneProvider : ISceneProvider
{
	int ISceneProvider.Priority => 10000000;

	IEnumerable<string> ISceneProvider.GetAvailable() {
		foreach (var scene in MuseDash1SceneInfo.GetScenes())
			yield return "scene/musedash1/" + scene.MapName;
	}

	ISceneDescriptor? ISceneProvider.FindByName(ReadOnlySpan<char> name) {
		if (!name.StartsWith("scene/musedash1/"))
			return null;
		Span<Range> pieces = stackalloc Range[10];
		var parts = name.Split(pieces, '/');
		MuseDash1SceneDescriptor? descriptor = MuseDash1SceneDescriptor.GetScene(name[pieces[2]]);
		if (descriptor == null) return null;

		return descriptor;
	}
}