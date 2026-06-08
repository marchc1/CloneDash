using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;

namespace CloneDash.Common.Scenes;

[MarkForStaticConstruction]
public static class SceneMod
{
	public static ConVar scene = new(nameof(scene), "", FCvar.Saved | FCvar.NotInGame, "Allows overriding the scene throughout the entire song instead of letting the chart decide the scene(s) used. If left blank, this does nothing.", autocomplete: clonedash_scene_autocomplete);
	public static ConCommand scenes = new(nameof(scenes), (_, in _) => {
		var scenes = GetAvailableScenes();
		foreach (var scene in scenes)
			Logs.Print($"    {scene}");
	}, "Prints all available scenes");
	static ISceneProvider[]? providers;

	private static void clonedash_scene_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		var availableCharacters = GetAvailableScenes().Where(x => x.StartsWith(args.ArgS(curArgPos))).ToArray();
		returns = availableCharacters;
	}


	public static IEnumerable<string> GetAvailableScenes() {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<ISceneProvider>();
		foreach (var retriever in providers)
			foreach (var characterName in retriever.GetAvailable())
				yield return characterName;
	}

	public static ISceneDescriptor? GetSceneData(ReadOnlySpan<char> name = default) {
		if (name.IsEmpty || name.IsWhiteSpace())
			name = scene.GetString();

		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<ISceneProvider>();
		foreach (var retriever in providers) {
			ISceneDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The scene '{name}' could not be found!");
		return null;
	}
}

