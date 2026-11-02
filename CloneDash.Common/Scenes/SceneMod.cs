using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;

namespace CloneDash.Common.Scenes;

[MarkForStaticConstruction]
public sealed class SceneMod : ActiveModStore<ISceneProvider, ISceneDescriptor>
{
	public static SceneMod Instance => ModStores.Get<SceneMod>();

	public static ConVar scene = new(nameof(scene), "", FCvar.Saved | FCvar.NotInGame, "Allows overriding the scene throughout the entire song instead of letting the chart decide the scene(s) used. If left blank, this does nothing.");
	public static ConCommand scenes = new(nameof(scenes), (_, in _) => Instance.PrintAll(), "Prints all available scenes");

	static SceneMod() {
		_ = Instance;
	}

	public SceneMod() : base("Scenes", "scene") {
		BindConVar(scene);
	}
}
