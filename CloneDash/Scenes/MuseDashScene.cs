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
	public int SortingOrder => 0;
	public int SortingLayerID => 0;

	public MuseDashScenePieceRenderer(Renderer unityRenderer) {
		UnityRenderer = unityRenderer;
	}
	public abstract void Build(MuseDashScene scene);
	public abstract void Render(MuseDashScene scene, float offsetX, float offsetY);
}

public class MuseDashScenePieceSpriteRenderer : MuseDashScenePieceRenderer
{
	public readonly SpriteRenderer SpriteRenderer;

	// Extracted data
	public Texture? LoadedTexture;
	public float Width;
	public float Height;
	public float PivotX;
	public float PivotY;
	public bool FlipX;
	public bool FlipY;
	public Color TintColor = Color.White;

	public MuseDashScenePieceSpriteRenderer(SpriteRenderer unityRenderer) : base(unityRenderer) {
		SpriteRenderer = unityRenderer;
	}

	public override void Build(MuseDashScene scene) {
		// Extract sprite data from the SpriteRenderer
		// The SpriteRenderer has m_Sprite (PPtr<Sprite>), which contains texture + rect info
		// It also has m_Color for tinting, and m_FlipX / m_FlipY

		FlipX = SpriteRenderer.m_FlipX;
		FlipY = SpriteRenderer.m_FlipY;

		// Extract tint color from the renderer
		var col = SpriteRenderer.m_Color;
		TintColor = new Color(
			(byte)(col.R * 255),
			(byte)(col.G * 255),
			(byte)(col.B * 255),
			(byte)(col.A * 255)
		);

		// Resolve the Sprite reference
		if (SpriteRenderer.m_Sprite.TryGet(out var sprite)) {
			// Sprite contains:
			//   m_Rect (the region within the texture atlas)
			//   m_Pivot (normalized pivot point)
			//   m_PixelsToUnits (conversion factor)
			//   m_RD.texture (PPtr to the actual Texture2D)

			Width = sprite.m_Rect.width;
			Height = sprite.m_Rect.height;
			PivotX = sprite.m_Pivot.X;
			PivotY = sprite.m_Pivot.Y;

			// Get the actual Texture2D and convert it
			if (sprite.m_RD.texture.TryGet(out var texture2D)) {
				// TODO: Convert texture2D to your engine's texture format
				// LoadedTexture = MuseDashCompatibility.ConvertTexture(level, texture2D);
				//
				// For sprites that are part of an atlas, you'll also need:
				//   sprite.m_Rect (x, y, width, height within the atlas)
				//   sprite.m_TextureRect (actual texture rect after trimming)
				//   sprite.m_Offset (offset for trimmed sprites)
				Logs.Info($"  Sprite: {sprite.m_Name} -> Texture: {texture2D.m_Name} ({Width}x{Height}, pivot: {PivotX},{PivotY})");
			}
		}
	}

	public override void Render(MuseDashScene scene, float offsetX, float offsetY) {
		if (LoadedTexture == null) return;

		// TODO: Draw the sprite using your rendering backend
		// Position comes from the parent MuseDashScenePiece's computed global transform
		// Apply pivot, flip, tint, sorting order
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

	// Computed world-space values (filled during FinalSetup)
	public float WorldX;
	public float WorldY;
	public float WorldZ;
	public float WorldRotationDeg;
	public float ScaleX = 1;
	public float ScaleY = 1;

	public void ComputeWorldTransform() {
		if (Transform == null) return;

		Transform.ComputeGlobalTransform(out var pos, out var rot);

		WorldX = pos.X * MUSEDASH_MULTIPLIER_POSITIONS;
		WorldY = pos.Y * MUSEDASH_MULTIPLIER_POSITIONS;
		WorldZ = pos.Z; // Z is used for depth/layer ordering

		// Extract rotation angle from quaternion (2D case: rotation around Z axis)
		// For a Z-axis rotation quaternion: q = (0, 0, sin(θ/2), cos(θ/2))
		float sinHalf = rot.Z;
		float cosHalf = rot.W;
		WorldRotationDeg = MathF.Atan2(2f * sinHalf * cosHalf, 1f - 2f * sinHalf * sinHalf) * (180f / MathF.PI);

		// Compute cumulative scale by walking the transform chain
		ScaleX = Transform.m_LocalScale.X;
		ScaleY = Transform.m_LocalScale.Y;
		var parent = Transform.GetFather();
		while (parent != null) {
			ScaleX *= parent.m_LocalScale.X;
			ScaleY *= parent.m_LocalScale.Y;
			parent = parent.GetFather();
		}
	}

	public string Dump(StringBuilder? sb = null, int recursiveIdx = 0) {
		sb ??= new();
		var rendInfo = IsRenderable() ? $" [renderers: {Renderers.Count}]" : "";
		var posInfo = Transform != null ? $" ({Transform.m_LocalPosition.X:F2}, {Transform.m_LocalPosition.Y:F2}, {Transform.m_LocalPosition.Z:F2})" : "";
		sb.AppendLine(new string(' ', recursiveIdx * 2) + Name + posInfo + rendInfo);
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
	readonly List<MuseDashScenePiece> RenderList = [];
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

		// Build all renderable components (extract textures, sprites, etc.)
		foreach (var item in AllPieces)
			item.BuildRenderables();

		// Compute world transforms for all pieces
		foreach (var item in AllPieces)
			item.ComputeWorldTransform();

		// Build sorted render list based on sorting layer + order + Z depth
		RenderList.Clear();
		foreach (var item in AllPieces) {
			if (item.IsRenderable())
				RenderList.Add(item);
		}

		// Sort by: sorting layer first, then sorting order, then Z position as tiebreaker
		RenderList.Sort((a, b) => {
			// Get the "best" renderer from each piece for sorting purposes
			var ra = a.Renderers[0];
			var rb = b.Renderers[0];

			int layerCmp = ra.SortingLayerID.CompareTo(rb.SortingLayerID);
			if (layerCmp != 0) return layerCmp;

			int orderCmp = ra.SortingOrder.CompareTo(rb.SortingOrder);
			if (orderCmp != 0) return orderCmp;

			// Z depth as final tiebreaker (further = drawn first)
			return b.WorldZ.CompareTo(a.WorldZ);
		});

		Logs.Info($"Scene {Name}: {AllPieces.Count} total pieces, {RenderList.Count} renderable");
		Logs.Info(Dump());
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

		// Build the scene tree from the prefab root
		var rootTransform = sceneGameObject.GetFirstComponent<Transform>();
		var rootGameObject = rootTransform?.GetGameObject();
		mdScene.BuildScene(rootGameObject);

		mdScene.FinalSetup();
		return mdScene;
	}

	private void BuildScene(GameObject? gameObject) {
		Scene = this;
		Transform = gameObject?.GetFirstComponent<Transform>()!;
		BuildChildren(gameObject, this);
	}

	private static void BuildChildren(GameObject? gameObject, MuseDashScenePiece parentPiece) {
		if (gameObject == null) return;

		var transform = gameObject.GetFirstComponent<Transform>();
		if (transform == null) return;

		foreach (var childTransform in transform.GetChildren()) {
			var childGameObject = childTransform.GetGameObject();
			if (childGameObject == null) continue;

			var childPiece = parentPiece.Scene.GetOrCreateScenePiece<MuseDashScenePiece>(childGameObject);
			childPiece.Transform = childTransform;
			childPiece.SetParent(parentPiece);

			// Check all components for renderers
			foreach (var comp in childGameObject.Components) {
				if (comp is SpriteRenderer spriteRenderer) {
					childPiece.AddRenderer(new MuseDashScenePieceSpriteRenderer(spriteRenderer));
				}
				// TODO: Add MeshRenderer, ParticleSystemRenderer, etc. as needed
			}

			// Recurse into children
			BuildChildren(childGameObject, childPiece);
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
	public Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
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
	}

	public void Initialize(DashGameLevel game) {
	}

	public void PlaySound(SceneSound sound, int hits) {
	}

	public void Refresh(DashGameLevel game) {
	}

	public void RenderBackground(DashGameLevel game) {
		// Render all scene pieces in sorted order
		foreach (var piece in RenderList) {
			foreach (var renderer in piece.Renderers) {
				renderer.Render(this, piece.WorldX, piece.WorldY);
			}
		}
	}

	public void Think(DashGameLevel game) {
	}

	internal void MountToFilesystem() {
	}
}