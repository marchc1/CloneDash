using AssetStudio;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;

namespace CloneDash.MD1_Compat.Compatibility;

public interface IMuseDash1FeverProvider : ICloneDashModProvider<IMuseDash1FeverDescriptor>;

public interface IMuseDash1FeverDescriptor : IContentSource;

public interface IMuseDash1FeverRuntime
{
	void Initialize();
	void Activate();
	void Cancel();
	void Reset();
	void Think();
	void Render();
}

[MarkForStaticConstruction]
public sealed class FeverMod : ActiveModStore<IMuseDash1FeverProvider, IMuseDash1FeverDescriptor>
{
	public static FeverMod Instance => ModStores.Get<FeverMod>();

	public static ConVar fever = new(nameof(fever), "fever/musedash1/battle_fever", FCvar.Saved | FCvar.NotInGame, "Your fever.");
	public static ConCommand fevers = new(nameof(fevers), (_, in _) => Instance.PrintAll(), "Prints all available fevers");

	static FeverMod() {
		_ = Instance;
	}

	public FeverMod() : base("Fevers", "fever") {
		BindConVar(fever);
	}
}

public class MuseDash1FeverProvider : ModProvider<IMuseDash1FeverDescriptor>, IMuseDash1FeverProvider
{
	readonly MuseDash1FeverDescriptor[] descriptors;

	public MuseDash1FeverProvider() : base("Muse Dash 1", "musedash1") {
		descriptors = [
			new("battle_fever", this),
			new("battle_fever_miku", this),
		];
	}

	public override IMuseDash1FeverDescriptor? FindByID(ReadOnlySpan<char> id) {
		foreach (var descriptor in descriptors)
			if (id.Equals(descriptor.Name, StringComparison.InvariantCultureIgnoreCase))
				return descriptor;
		return null;
	}

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var descriptor in descriptors)
			yield return descriptor.Name;
	}
}

public class MuseDash1FeverDescriptor(string name, ICloneDashModProvider provider) : SimpleModDescriptor(name, name, provider), IMuseDash1FeverDescriptor
{
	public readonly string Name = name;
	public IGamemodeDescriptor GetGamemode() => ModStores.Get<GamemodeMod>().GetDescriptorByUID(MuseDash1Gamemode.UUID)!;

	public Type GetRuntimeType() => typeof(MuseDash1FeverRuntime);
}

public class MuseDash1FeverRuntime(MuseDash1FeverDescriptor descriptor, RhythmGameLevel game) : UnitySimScene, IMuseDash1FeverRuntime
{
	readonly RhythmGameLevel game = game;
	SceneTransform rootTransform = new();
	SceneObject? background;
	SceneAnimator? backgroundAnimator;
	SceneObject? whitBoard;
	SceneSpriteRenderer? whitBoardRenderer;
	Vector2F outScenePosition = new(15, 0.8f);
	SceneObject?[] particles = new SceneObject[7];
	bool isActivatedComeOut;
	bool ifShow = true;

	public void Initialize() {
		var gameobject = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(descriptor.Name);
		if (gameobject == null) return;

		AssetStudio.Transform? gameTransform;
		if ((gameTransform = gameobject.GetFirstComponent<AssetStudio.Transform>()) != null) {
			rootTransform.LocalX = gameTransform.m_LocalPosition.X;
			rootTransform.LocalY = gameTransform.m_LocalPosition.Y;
			rootTransform.LocalZ = gameTransform.m_LocalPosition.Z;
			rootTransform.LocalScaleX = gameTransform.m_LocalScale.X;
			rootTransform.LocalScaleY = gameTransform.m_LocalScale.Y;
			rootTransform.LocalScaleZ = gameTransform.m_LocalScale.Z;
		}

		var feverEffectManagerMb = gameobject.GetMonoBehaviorByScriptName("FeverEffectManager");
		if (feverEffectManagerMb == null) return;

		var feverEffectManager = new MonoBehaviourReader(feverEffectManagerMb);
		background = ImportGameObject(feverEffectManager.Get<GameObject>("m_Background"), rootTransform);
		var whitBoardSr = feverEffectManager.Get<SpriteRenderer>("m_WhitBoardRender");
		if (whitBoardSr != null)
			whitBoard = ImportGameObject(whitBoardSr.GetGameObject(), rootTransform);
		whitBoardRenderer = whitBoard?.GetComponent<SceneSpriteRenderer>();

		var outScenePositionVec = feverEffectManager.GetVector3("outScenePosition");
		if (outScenePositionVec.HasValue)
			outScenePosition = new(outScenePositionVec.Value.X, outScenePositionVec.Value.Y);

		var m_Particles = feverEffectManager.GetList<GameObject>("m_Particles");
		for (int i = 0; i < m_Particles.Count && i < particles.Length; i++)
			particles[i] = ImportGameObject(m_Particles[i], rootTransform);

		foreach (var obj in allObjects) obj.Awake();

		backgroundAnimator = background?.GetComponent<SceneAnimator>();
		animators.Add(backgroundAnimator!);

		// InitFeverEffect: position at outScenePosition, hide whitboard, disable particles
		if (background != null) {
			background.Transform.LocalX = outScenePosition.X;
			background.Transform.LocalY = outScenePosition.Y;
		}
		if (whitBoard != null)
			whitBoard.Color = new(1, 1, 1, 0);
		foreach (var p in particles)
			if (p != null) p.Active = false;

		backgroundAnimator?.Rebind();

		isActivatedComeOut = false;
		ifShow = true;
	}

	public void Activate() {
		// Mirrors InvokeNormalFever
		if (background != null) {
			background.Active = true;
			if (backgroundAnimator != null) {
				backgroundAnimator.Rebind();
				backgroundAnimator.Play("come_in");
			}
		}

		if (whitBoard != null) {
			whitBoard.Active = false;
			whitBoard.Color = new(1, 1, 1, 0);
		}

		foreach (var p in particles)
			if (p != null) p.Active = true;

		isActivatedComeOut = false;
		ifShow = true;

		BuildRenderOrder();
	}

	public void Cancel() {
		isActivatedComeOut = true;
	}

	public void Reset() {
		isActivatedComeOut = false;
		backgroundAnimator?.ResetTime();
		backgroundAnimator?.Rebind();
	}

	public void Think() {
		float dt = (float)globals.CurTimeDelta;

		RunThinkFuncs(dt);

		if (isActivatedComeOut) {
			if (ifShow) {
				if (whitBoard != null) {
					whitBoard.Active = true;
					float alpha = whitBoard.Color.W;
					float step = dt / 0.15f;
					alpha += step;
					if (alpha < 1f) {
						whitBoard.Color = new(1, 1, 1, alpha);
					}
					else {
						whitBoard.Color = new(1, 1, 1, 1);
						if (background != null) {
							background.Transform.LocalX = outScenePosition.X;
							background.Transform.LocalY = outScenePosition.Y;
							background.Active = false;
						}
						foreach (var p in particles)
							if (p != null) p.Active = false;
						ifShow = false;
					}
				}
				else {
					ifShow = false;
				}
			}
			else {
				if (whitBoard != null) {
					float alpha = whitBoard.Color.W;
					float step = dt / 0.15f;
					alpha -= step;
					if (alpha > 0f) {
						whitBoard.Color = new(1, 1, 1, alpha);
					}
					else {
						whitBoard.Color = new(1, 1, 1, 0);
						whitBoard.Active = false;
						isActivatedComeOut = false;
						if (backgroundAnimator != null) {
							if (background != null) background.Active = true;
							backgroundAnimator.Rebind();
							backgroundAnimator.Play("waiting_outside");
						}
						ifShow = true;
					}
				}
				else {
					isActivatedComeOut = false;
					ifShow = true;
				}
			}
		}

		BuildRenderOrder();
	}

	public void Render() {
		Rlgl.PushMatrix();
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}
}
