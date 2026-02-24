using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using System.Text;
using Texture = Nucleus.ManagedMemory.Texture;
using Color = Nucleus.Common.Types.Color;
using Nucleus;
using System.Diagnostics;

namespace CloneDash.Scenes;

public abstract class MuseDashScenePieceRenderer
{
	public readonly Renderer UnityRenderer;
	public MuseDashScenePieceRenderer(Renderer unityRenderer) {
		UnityRenderer = unityRenderer;
	}
	public abstract void Build(MuseDashScene scene);
}

public class MuseDashScenePieceSpriteRenderer : MuseDashScenePieceRenderer
{
	public readonly SpriteRenderer SpriteRenderer;
	public MuseDashScenePieceSpriteRenderer(SpriteRenderer unityRenderer) : base(unityRenderer) {
		SpriteRenderer = unityRenderer;
	}

	public override void Build(MuseDashScene scene) {
		
	}
}

public class MuseDashScenePiece
{
	public Transform Transform = null!;
	public readonly List<MuseDashScenePieceRenderer> Renderers = [];

	public void AddRenderer(MuseDashScenePieceRenderer renderer) {
		Renderers.Add(renderer);
	}

	public bool IsRenderable() => Renderers.Count > 0;

	public string Dump(StringBuilder? sb = null, int recursiveIdx = 0) {
		sb ??= new();
		sb.AppendLine(new string(' ', recursiveIdx * 2) + Name);
		foreach (var child in Children)
			child.Dump(sb, recursiveIdx + 1);
		return sb.ToString();
	}
	public string Name = "";
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 200; // seems to be the right base

	internal MuseDashScene Scene = null!;
	MuseDashScenePiece? Parent;
	readonly List<MuseDashScenePiece> Children = [];

	public MuseDashScene GetScene() => Scene;
	public IReadOnlyList<MuseDashScenePiece> GetChildren() => Children;
	public MuseDashScenePiece? GetParent() => Parent;
	public void AddChild(MuseDashScenePiece child) => child.SetParent(this);
	public void SetParent(MuseDashScenePiece? parent) {
		if (Parent == parent) return;

		Parent?.Children.Remove(this);
		Parent = parent;
		Parent?.Children.Add(this);
	}

	public void BuildRenderables() {
		foreach (var renderer in Renderers) 
			renderer.Build(Scene);
	}
}


public class MuseDashScene : MuseDashScenePiece, ISceneDescriptor
{
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	readonly List<MuseDashScenePiece> AllPieces = [];
	readonly List<List<MuseDashScenePiece>> RenderOrder = [];
	readonly Dictionary<long, MuseDashScenePiece> UnityPathIDToScenePiece = [];

	public T GetOrCreateScenePiece<T>(GameObject? obj) where T : MuseDashScenePiece, new() {
		ArgumentNullException.ThrowIfNull(obj);
		if (UnityPathIDToScenePiece.TryGetValue(obj.m_PathID, out var piece))
			return (T)piece;
		piece = new T {
			Scene = this,
			Name = obj.m_Name ?? ""
		};
		AllPieces.Add(piece);
		UnityPathIDToScenePiece[obj.m_PathID] = piece;
		return (T)piece;
	}

	/// <summary>
	/// Performs some final setup that can't be done until all data is retrieved
	/// </summary>
	public void FinalSetup() {
		pathwayInfo[(int)PathwaySide.Both] = new() {
			Position = (pathwayInfo[(int)PathwaySide.Top].Position + pathwayInfo[(int)PathwaySide.Bottom].Position) / 2,
			Color = Pathway.PATHWAY_DUAL_COLOR
		};
		pathwayInfo[(int)PathwaySide.Top].Color = Pathway.PATHWAY_TOP_COLOR;
		pathwayInfo[(int)PathwaySide.Bottom].Color = Pathway.PATHWAY_BOTTOM_COLOR;

		foreach (var item in AllPieces)
			item.BuildRenderables();

		var layer = new List<MuseDashScenePiece>();
		RenderOrder.Add(layer);
		foreach (var item in AllPieces) {
			if (item.IsRenderable())
				layer.Add(item);
		}
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		int sceneIDX = name.IndexOf('_');
		if (sceneIDX == -1)
			return null;

		sceneIDX = int.TryParse(name[(sceneIDX + 1)..], out int i) ? i : -1;
		if (sceneIDX == -1)
			return null;

		string strSceneIdx = sceneIDX.ToString().PadLeft(2, '0');
		var sceneGameObject = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>($"scene_{strSceneIdx}")!;

		var sceneSubControl = new MonoBehaviourReader(sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl") ?? throw new NullReferenceException("No scene control?"));
		if (sceneSubControl == null)
			return null;

		// HITPOINTS
		// This gets the hitpoints and their positions out of the scene data
		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;
		transform.m_Children[0].TryGet(out var child1);
		transform.m_Children[1].TryGet(out var child2);

		var mdScene = new MuseDashScene();
		mdScene.Name = strSceneIdx;
		child1!.ComputeGlobalTransform(out var v1, out _);
		child2!.ComputeGlobalTransform(out var v2, out _);
		int tempOffset = -42; // TODO: What is this? 
		mdScene.pathwayInfo[(int)PathwaySide.Top] = new(v1.X * MUSEDASH_MULTIPLIER_POSITIONS, (v1.Y * MUSEDASH_MULTIPLIER_POSITIONS) + tempOffset);
		mdScene.pathwayInfo[(int)PathwaySide.Bottom] = new(v2.X * MUSEDASH_MULTIPLIER_POSITIONS, (v2.Y * MUSEDASH_MULTIPLIER_POSITIONS) + tempOffset);

		mdScene.BuildScene(sceneGameObject.GetFirstComponent<Transform>()?.GetGameObject());

		mdScene.FinalSetup();
		return mdScene;
	}

	private void BuildScene(GameObject? gameObject) {
		Scene = new();
		BuildObject(gameObject, this);
	}

	private static void BuildObject(GameObject? gameObject, MuseDashScenePiece piece) {
		if (gameObject == null) return;

		var transform = gameObject.GetFirstComponent<Transform>()!;
		piece.Transform = transform;

		var mayBeRenderable = gameObject.Components.Any(x => x is not Animator && x is not AssetStudio.Transform);
		if (mayBeRenderable) {
			foreach (var comp in gameObject.Components)
				if (comp is Renderer r)
					switch (r) {
						case SpriteRenderer spriteRenderer:
							piece.AddRenderer(new MuseDashScenePieceSpriteRenderer(spriteRenderer));
							break;
					}

			Debugger.Break();
		}

		foreach (var child in transform.GetChildren()) {
			var childGameObject = child.GetGameObject()!;
			var childObj = piece.Scene.GetOrCreateScenePiece<MuseDashScenePiece>(childGameObject);
			childObj.SetParent(piece);
			BuildObject(childGameObject, childObj);
		}
	}

	public string? GetBossAnimation(BossAnimationType type, out double time) {
		time = 0;
		return null;
	}

	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time) {
		time = 0;
		return null;
	}

	public string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType) {
		return null;
	}

	public ModelData? GetEnemyModel(DashEnemy enemy) {
		return null;

	}

	public ModelData? GetHP(out string? mountAnimation) {
		mountAnimation = null;
		return null;
	}

	public BoneInstance? GetHPMount(DashEnemy enemy) {
		return null;
	}

	public string GetMasherHitAnimation() {
		return "";
	}

	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway) => ref pathwayInfo[(int)pathway];

	public Nucleus.Common.Types.Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
	public Vector2F GetPathwayPosition(PathwaySide side) => GetPathwayInformation(side).Position;

	public MusicTrack? GetPressIdleSound() {
		return null;
	}

	public void GetSustainResources(PathwaySide pathway, out ITexture start, out ITexture end, out ITexture body, out ITexture up, out ITexture down, out float rotationDegsPerSecond) {
		start = null!;
		end = null!;
		body = null!;
		up = null!;
		down = null!;
		rotationDegsPerSecond = 0;
		// todo: refactor this part of the API.
	}

	public void Initialize(DashGameLevel game) {

	}

	public void PlaySound(SceneSound sound, int hits) {

	}

	public void Refresh(DashGameLevel game) {

	}

	public void RenderBackground(DashGameLevel game) {

	}

	public void Think(DashGameLevel game) {

	}

	internal void MountToFilesystem() {

	}
}
