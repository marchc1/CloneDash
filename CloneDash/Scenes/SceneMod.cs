using CloneDash.Data;
using CloneDash.Modding.Descriptors;

using Nucleus;
using Nucleus.Commands;
using Nucleus.Util;

namespace CloneDash.Scenes;

[MarkForStaticConstruction]
public static class SceneMod
{
	public static ConVar scene = new(nameof(scene), "clonedash", FCvar.Saved, "Your scene.");
	public static ConVar allowsceneoverride = new(nameof(allowsceneoverride), "1", FCvar.Saved, $"If true (and the scene specified exists on-disk), allows charts to specify the scene used during gameplay. If false, will always use ConVar \"{nameof(scene)}\"'s value.", 0, 1);
	public static ConCommand scenes = new(nameof(scenes), (_, in _) => {
		var scenes = GetAvailableScenes();
		foreach (var scene in scenes)
			Logs.Print($"    {scene}");
	}, "Prints all available scenes");

	public static IEnumerable<string> GetAvailableScenes() {
		ISceneProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ISceneProvider>();
		foreach (var retriever in retrievers)
			foreach (var characterName in retriever.GetAvailable())
				yield return characterName;
	}

	public static ISceneDescriptor? GetSceneData(ChartSong? song = null) {
		ReadOnlySpan<char> name = scene.GetString();

		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		ISceneProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ISceneProvider>();
		foreach (var retriever in retrievers) {
			ISceneDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The scene '{name}' could not be found!");
		return null;
	}
}

