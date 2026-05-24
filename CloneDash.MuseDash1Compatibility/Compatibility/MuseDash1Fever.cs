using AssetStudio;
using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Types;
using Nucleus.Util;

namespace CloneDash.MD1_Compat.Compatibility;

public interface IMuseDash1FeverProvider
{
	IMuseDash1FeverDescriptor? FindByName(ReadOnlySpan<char> name);
	IEnumerable<string> GetAvailable();
}

public interface IMuseDash1FeverDescriptor
{
	IMuseDash1FeverRuntime? Instantiate(IGame game);
}

public interface IMuseDash1FeverRuntime
{
	void Initialize();
	void Activate();
	void Think();
	void Render();
}

public static class FeverMod
{
	static IMuseDash1FeverDescriptor? activeDescriptor;
	static IMuseDash1FeverProvider[]? providers;

	public static ConVar fever = new(nameof(fever), "fever/musedash1/battle_fever", FCvar.Saved | FCvar.NotInGame, "Your fever.", null, null, (cv, o, n) => {
		var lastDescriptor = activeDescriptor;
		activeDescriptor = GetFeverData();
	}, autocomplete: clonedash_fever_autocomplete);

	private static IMuseDash1FeverDescriptor? GetFeverData(string? name = null) {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IMuseDash1FeverProvider>();
		name ??= fever == null ? default : new(fever.GetString());

		if (string.IsNullOrWhiteSpace(name))
			return null;

		foreach (var retriever in providers) {
			IMuseDash1FeverDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The fever '{name}' could not be found!");
		return null;
	}

	private static void clonedash_fever_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		var availableFevers = GetAvailableFevers().Where(x => x.StartsWith(args.ArgS(curArgPos))).ToArray();
		returns = availableFevers;
	}

	public static IEnumerable<string> GetAvailableFevers() {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IMuseDash1FeverProvider>();
		foreach (var retriever in providers)
			foreach (var feverName in retriever.GetAvailable())
				yield return feverName;
	}

	internal static IMuseDash1FeverRuntime? InstantiateCurrentFever(IGame game) {
		var descriptor = GetFeverData();
		if (descriptor == null) return null;
		return descriptor.Instantiate(game);
	}
}

public class MuseDash1FeverProvider : IMuseDash1FeverProvider
{
	public const string UUID_PREFIX = "fever/musedash1";
	readonly MuseDash1FeverDescriptor[] descriptors = [
		new("battle_fever"),
	];

	public IMuseDash1FeverDescriptor? FindByName(ReadOnlySpan<char> name) {
		if (!name.StartsWith(UUID_PREFIX, StringComparison.InvariantCultureIgnoreCase))
			return null;
		var splits = name.Split('/');
		splits.MoveNext();
		splits.MoveNext();
		var piece = name[splits.Current];
		foreach (var descriptor in descriptors) {
			if (descriptor.Name.Equals(piece, StringComparison.InvariantCultureIgnoreCase))
				return descriptor;
		}
		return null;
	}

	public IEnumerable<string> GetAvailable() {
		foreach (var descriptor in descriptors)
			yield return descriptor.GlobalName;
	}
}

public class MuseDash1FeverDescriptor(string name) : IMuseDash1FeverDescriptor
{
	public readonly string Name = name;
	public readonly string GlobalName = MuseDash1FeverProvider.UUID_PREFIX + "/" + name;
	public IMuseDash1FeverRuntime? Instantiate(IGame game) {
		return new MuseDash1FeverRuntime(this, (MuseDash1Game)game);
	}
}

public class MuseDash1FeverRuntime(MuseDash1FeverDescriptor descriptor, MuseDash1Game game) : BaseMuseDash1UnitySimScene, IMuseDash1FeverRuntime
{
	SceneTransform rootTransform = new();
	SceneObject background = null!;
	SceneAnimator backgroundAnimator = null!;
	SceneObject whitBoard = null!;
	SceneSpriteRenderer whitBoardRenderer = null!;
	Vector2F outScenePosition = new(15, 0.8f);
	SceneObject?[] particles = new SceneObject[7];
	private const float COME_OUT_DURING_TIME = 0.3f;
	private bool isActivatedComeOut;

	public void Initialize() {
		var gameobject = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(descriptor.Name);
		if (gameobject == null) return;

		var feverEffectManagerMb = gameobject.GetMonoBehaviorByScriptName("FeverEffectManager");
		if (feverEffectManagerMb == null) return;
		
		var feverEffectManager = new MonoBehaviourReader(feverEffectManagerMb);
		background = ImportGameObject(feverEffectManager.Get<GameObject>("m_Background"), rootTransform)!;
		whitBoard = ImportGameObject(feverEffectManager.Get<SpriteRenderer>("m_WhitBoardRender")!.GetGameObject(), rootTransform)!;
		whitBoardRenderer = whitBoard.GetComponent<SceneSpriteRenderer>()!;
		var outScenePosition = feverEffectManager.GetVector3("outScenePosition");
		this.outScenePosition = new(outScenePosition!.Value.X, outScenePosition!.Value.Y);
		var m_Particles = feverEffectManager.GetList<GameObject>("m_Particles");
		for (int i = 0; i < m_Particles.Count; i++) 
			particles[i] = ImportGameObject(m_Particles[i], rootTransform);
	}

	public void Activate() {

	}

	public void Think() {

	}

	public void Render() {

	}
}
