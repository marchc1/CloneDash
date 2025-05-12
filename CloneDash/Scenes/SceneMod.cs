using CloneDash.Data;
using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Util;

namespace CloneDash.Scenes;

[MarkForStaticConstruction]
public static class SceneMod
{
	public static ConVar clonedash_scene = ConVar.Register("clonedash_scene", "clonedash", ConsoleFlags.Saved, "Your scene.");
	public static ConVar clonedash_allowsceneoverride = ConVar.Register("clonedash_allowsceneoverride", "1", ConsoleFlags.Saved, "If true (and the scene specified exists on-disk), allows charts to specify the scene used during gameplay. If false, will always use clonedash_scene.", 0, 1);
	public static ConCommand clonedash_allscenes = ConCommand.Register("clonedash_allscenes", (_, _) => {
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
		ISceneProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ISceneProvider>();
		string? name = clonedash_scene?.GetString();

		if (string.IsNullOrWhiteSpace(name))
			return null;

		foreach (var retriever in retrievers) {
			ISceneDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The scene '{name}' could not be found!");
		return null;
	}
}

