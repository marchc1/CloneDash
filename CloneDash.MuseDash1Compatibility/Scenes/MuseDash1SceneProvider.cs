using CloneDash.Common.Compatibility;
using CloneDash.Common.Scenes;
using CloneDash.Scenes;

namespace CloneDash.Characters;

public class MuseDash1SceneProvider() : ModProvider<ISceneDescriptor>("Muse Dash 1", "musedash1"), ISceneProvider
{
	public override int GetSortIndex() => 10000000;

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var scene in MuseDash1SceneInfo.GetScenes())
			yield return scene.MapName;
	}

	public override ISceneDescriptor? FindByID(ReadOnlySpan<char> id) => MuseDash1SceneDescriptor.GetScene(this, id);
}
