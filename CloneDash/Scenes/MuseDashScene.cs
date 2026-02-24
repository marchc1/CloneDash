using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;
using System.Text;
using Color = Nucleus.Common.Types.Color;
using Texture = Nucleus.ManagedMemory.Texture;
using Texture2D = AssetStudio.Texture2D;
using Transform = AssetStudio.Transform;

namespace CloneDash.Scenes;

public abstract class SceneComponent
{
	public SceneObject Object { get; internal set; } = null!;
	public SceneTransform Transform => Object.Transform;
	public virtual void Awake() { }
}

public class SceneTransform : SceneComponent
{
	public float LocalX, LocalY, LocalZ;
	public float LocalScaleX = 1, LocalScaleY = 1, LocalScaleZ = 1;
	public float LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW = 1;

	public SceneTransform? Parent { get; private set; }
	readonly List<SceneTransform> children = [];
	public IReadOnlyList<SceneTransform> Children => children;

	public void SetParent(SceneTransform? parent) {
		Parent?.children.Remove(this);
		Parent = parent;
		Parent?.children.Add(this);
	}

	public void GetWorldPosition(out float wx, out float wy, out float wz) {
		wx = LocalX; wy = LocalY; wz = LocalZ;
		var p = Parent;
		while (p != null) {
			wx *= p.LocalScaleX;
			wy *= p.LocalScaleY;
			wz *= p.LocalScaleZ;
			RotateVector(p.LocalRotationX, p.LocalRotationY, p.LocalRotationZ, p.LocalRotationW,
				wx, wy, wz, out wx, out wy, out wz);
			wx += p.LocalX;
			wy += p.LocalY;
			wz += p.LocalZ;
			p = p.Parent;
		}
	}

	public void GetWorldScale(out float sx, out float sy) {
		sx = LocalScaleX; sy = LocalScaleY;
		var p = Parent;
		while (p != null) {
			sx *= p.LocalScaleX;
			sy *= p.LocalScaleY;
			p = p.Parent;
		}
	}

	public float GetWorldRotationZ() {
		float totalRad = LocalRotationZRadians();
		var p = Parent;
		while (p != null) {
			totalRad += p.LocalRotationZRadians();
			p = p.Parent;
		}
		return totalRad * (180f / MathF.PI);
	}

	float LocalRotationZRadians() =>
		MathF.Atan2(2f * (LocalRotationW * LocalRotationZ + LocalRotationX * LocalRotationY),
					1f - 2f * (LocalRotationY * LocalRotationY + LocalRotationZ * LocalRotationZ));

	static void RotateVector(float qx, float qy, float qz, float qw,
		float vx, float vy, float vz, out float ox, out float oy, out float oz) {
		float dot = qx * vx + qy * vy + qz * vz;
		float qsq = qx * qx + qy * qy + qz * qz;
		float cx = qy * vz - qz * vy;
		float cy = qz * vx - qx * vz;
		float cz = qx * vy - qy * vx;
		ox = 2f * dot * qx + (qw * qw - qsq) * vx + 2f * qw * cx;
		oy = 2f * dot * qy + (qw * qw - qsq) * vy + 2f * qw * cy;
		oz = 2f * dot * qz + (qw * qw - qsq) * vz + 2f * qw * cz;
	}

	public void ReadFrom(Transform unityTransform) {
		LocalX = unityTransform.m_LocalPosition.X;
		LocalY = unityTransform.m_LocalPosition.Y;
		LocalZ = unityTransform.m_LocalPosition.Z;
		LocalScaleX = unityTransform.m_LocalScale.X;
		LocalScaleY = unityTransform.m_LocalScale.Y;
		LocalScaleZ = unityTransform.m_LocalScale.Z;
		LocalRotationX = unityTransform.m_LocalRotation.X;
		LocalRotationY = unityTransform.m_LocalRotation.Y;
		LocalRotationZ = unityTransform.m_LocalRotation.Z;
		LocalRotationW = unityTransform.m_LocalRotation.W;
	}

	public void ComputeGlobalTransform(out Vector3 position, out Quaternion rotation) {
		position = new(LocalX, LocalY, LocalZ);
		rotation = new(LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW);

		var parent = Parent;
		while (parent != null) {
			position = new Vector3(
				position.X * parent.LocalScaleX,
				position.Y * parent.LocalScaleY,
				position.Z * parent.LocalScaleZ
			);

			position = RotateVector(new(parent.LocalRotationX, parent.LocalRotationY, parent.LocalRotationZ, parent.LocalRotationW), position);
			position += new Vector3(parent.LocalX, parent.LocalY, parent.LocalZ);
			rotation = MultiplyQuaternion(new(parent.LocalRotationX, parent.LocalRotationY, parent.LocalRotationZ, parent.LocalRotationW), rotation);
			parent = parent.Parent;
		}
	}

	private static Vector3 RotateVector(Quaternion q, Vector3 v) {
		float ux = q.X, uy = q.Y, uz = q.Z, s = q.W;

		float dotUV = ux * v.X + uy * v.Y + uz * v.Z;
		float dotUU = ux * ux + uy * uy + uz * uz;

		float cx = uy * v.Z - uz * v.Y;
		float cy = uz * v.X - ux * v.Z;
		float cz = ux * v.Y - uy * v.X;

		return new Vector3(
			2f * dotUV * ux + (s * s - dotUU) * v.X + 2f * s * cx,
			2f * dotUV * uy + (s * s - dotUU) * v.Y + 2f * s * cy,
			2f * dotUV * uz + (s * s - dotUU) * v.Z + 2f * s * cz
		);
	}

	private static Quaternion MultiplyQuaternion(Quaternion a, Quaternion b) {
		return new Quaternion(
			a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
			a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
			a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
			a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
		);
	}
}

public abstract class SceneRenderer : SceneComponent
{
	public int SortingLayerID;
	public int SortingOrder;
	public abstract void Render(MuseDashScene scene);
}

public class SceneSpriteRenderer : SceneRenderer
{
	public SpriteRenderer UnitySpriteRenderer { get; }

	ITexture? texture;

	float texRectX, texRectY, texRectW, texRectH;
	int atlasW, atlasH;
	float unitW, unitH;
	float pivotX, pivotY;

	bool flipX, flipY;
	byte colR = 255, colG = 255, colB = 255, colA = 255;

	public SceneSpriteRenderer(SpriteRenderer sr) {
		UnitySpriteRenderer = sr;
	}

	public override void Awake() {
		var sprite = UnitySpriteRenderer.GetSprite();
		if (sprite == null) return;

		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;

		texture = ((MuseDashScene)Object.Scene).LoadTexture(tex2d);
		atlasW = (int)texture.Width;
		atlasH = (int)texture.Height;

		texRectX = sprite.m_RD.textureRect.x;
		texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width;
		texRectH = sprite.m_RD.textureRect.height;

		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu;
		unitH = sprite.m_Rect.height / ppu;

		pivotX = sprite.m_Pivot.X;
		pivotY = sprite.m_Pivot.Y;

		flipX = UnitySpriteRenderer.m_FlipX;
		flipY = UnitySpriteRenderer.m_FlipY;

		var c = UnitySpriteRenderer.m_Color;
		colR = (byte)(c.R * 255);
		colG = (byte)(c.G * 255);
		colB = (byte)(c.B * 255);
		colA = (byte)(c.A * 255);

		SortingOrder = UnitySpriteRenderer.m_SortingOrder;
		SortingLayerID = (int)UnitySpriteRenderer.m_SortingLayerID;
	}

	public override void Render(MuseDashScene scene) {
		if (texture == null || texRectW <= 0 || texRectH <= 0) return;

		Transform.GetWorldPosition(out float wx, out float wy, out _);
		Transform.GetWorldScale(out float sx, out float sy);

		float w = unitW * MathF.Abs(sx);
		float h = unitH * MathF.Abs(sy);

		float offX = -pivotX * w;
		float offY = -(1f - pivotY) * h;

		float flippedY = atlasH - texRectY - texRectH;

		float u0 = texRectX / atlasW;
		float v0 = flippedY / atlasH;
		float u1 = (texRectX + texRectW) / atlasW;
		float v1 = (flippedY + texRectH) / atlasH;

		bool effFlipX = flipX ^ (sx < 0);
		bool effFlipY = flipY ^ (sy < 0);
		if (effFlipX) (u0, u1) = (u1, u0);
		if (effFlipY) (v0, v1) = (v1, v0);

		float screenX = wx + offX;
		float screenY = -wy + offY;

		float rotDeg = Transform.GetWorldRotationZ();
		uint texId = texture.HardwareID;

		if (MathF.Abs(rotDeg) > 0.01f) {
			float cx = wx;
			float cy = -wy;
			float rad = -rotDeg * (MathF.PI / 180f);
			float cos = MathF.Cos(rad);
			float sin = MathF.Sin(rad);

			float x0 = offX, y0 = offY;
			float x1 = offX, y1 = offY + h;
			float x2 = offX + w, y2 = offY + h;
			float x3 = offX + w, y3 = offY;

			Rlgl.SetTexture(texId);
			Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4ub(colR, colG, colB, colA);

			Rlgl.TexCoord2f(u0, v0);
			Rlgl.Vertex2f(cx + x0 * cos - y0 * sin, cy + x0 * sin + y0 * cos);
			Rlgl.TexCoord2f(u0, v1);
			Rlgl.Vertex2f(cx + x1 * cos - y1 * sin, cy + x1 * sin + y1 * cos);
			Rlgl.TexCoord2f(u1, v1);
			Rlgl.Vertex2f(cx + x2 * cos - y2 * sin, cy + x2 * sin + y2 * cos);
			Rlgl.TexCoord2f(u1, v0);
			Rlgl.Vertex2f(cx + x3 * cos - y3 * sin, cy + x3 * sin + y3 * cos);

			Rlgl.End();
			Rlgl.SetTexture(0);
		}
		else {
			Rlgl.SetTexture(texId);
			Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4ub(colR, colG, colB, colA);

			Rlgl.TexCoord2f(u0, v0);
			Rlgl.Vertex2f(screenX, screenY);
			Rlgl.TexCoord2f(u0, v1);
			Rlgl.Vertex2f(screenX, screenY + h);
			Rlgl.TexCoord2f(u1, v1);
			Rlgl.Vertex2f(screenX + w, screenY + h);
			Rlgl.TexCoord2f(u1, v0);
			Rlgl.Vertex2f(screenX + w, screenY);

			Rlgl.End();
			Rlgl.SetTexture(0);
		}
	}
}

public class SceneAnimator : SceneComponent
{
	public Animator? UnityAnimator { get; set; }
}

public class SceneObject
{
	public string Name = "";
	public bool Active = true;
	public SceneTransform Transform { get; } = new();
	public MuseDashScene Scene { get; internal set; } = null!;

	readonly List<SceneComponent> components = [];
	public IReadOnlyList<SceneComponent> Components => components;

	public SceneObject() {
		Transform.Object = this;
		components.Add(Transform);
	}

	public T AddComponent<T>(T component) where T : SceneComponent {
		component.Object = this;
		components.Add(component);
		return component;
	}

	public T? GetComponent<T>() where T : SceneComponent {
		foreach (var c in components)
			if (c is T t) return t;
		return null;
	}

	public IEnumerable<T> GetComponents<T>() where T : SceneComponent {
		foreach (var c in components)
			if (c is T t) yield return t;
	}

	public void Awake() {
		foreach (var c in components) c.Awake();
	}

	public string Dump(StringBuilder? sb = null, int depth = 0) {
		sb ??= new();
		string indent = new(' ', depth * 2);
		string compInfo = components.Count > 1
			? $" [{string.Join(", ", components.Skip(1).Select(c => c.GetType().Name))}]"
			: "";
		sb.AppendLine($"{indent}{Name} ({Transform.LocalX:F2}, {Transform.LocalY:F2}){compInfo}");
		foreach (var child in Transform.Children)
			child.Object.Dump(sb, depth + 1);
		return sb.ToString();
	}
}
public class MuseDashScene : ISceneDescriptor
{
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 200;

	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	readonly List<SceneObject> allObjects = [];
	readonly List<SceneRenderer> sortedRenderers = [];
	SceneObject? root;
	readonly Dictionary<long, ITexture> textureCache = [];
	readonly Dictionary<long, SceneObject> pathIdToObject = [];

	internal ITexture LoadTexture(Texture2D? texture2D) {
		if (textureCache.TryGetValue(texture2D!.m_PathID, out var tex))
			return tex;
		textureCache[texture2D.m_PathID] = tex = MuseDashCompatibility.ConvertTexture(EngineCore.Level, texture2D!);
		return tex;
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		int sceneIDX = name.IndexOf('_');
		if (sceneIDX == -1) return null;
		sceneIDX = int.TryParse(name[(sceneIDX + 1)..], out int i) ? i : -1;
		if (sceneIDX == -1) return null;

		string strSceneIdx = sceneIDX.ToString().PadLeft(2, '0');
		var sceneGameObject = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>($"scene_{strSceneIdx}")!;

		var sceneSubControl = new MonoBehaviourReader(
			sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl")
			?? throw new NullReferenceException("No scene control?"));

		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;

		var scene = new MuseDashScene();

		var pathwaysObject = scene.ImportGameObject(scenePoint, null);
		foreach (var child in pathwaysObject.Transform.Children)
			scene.EvaluatePathway(child!.Object);

		var rootTransform = sceneGameObject.GetFirstComponent<Transform>()!;
		scene.root = scene.ImportGameObject(rootTransform.GetGameObject()!, null);

		foreach (var obj in scene.allObjects)
			obj.Awake();

		scene.BuildRenderOrder();

		scene.pathwayInfo[(int)PathwaySide.Both] = new() {
			Position = (scene.pathwayInfo[(int)PathwaySide.Top].Position +
						scene.pathwayInfo[(int)PathwaySide.Bottom].Position) / 2,
			Color = Pathway.PATHWAY_DUAL_COLOR
		};
		scene.pathwayInfo[(int)PathwaySide.Top].Color = Pathway.PATHWAY_TOP_COLOR;
		scene.pathwayInfo[(int)PathwaySide.Bottom].Color = Pathway.PATHWAY_BOTTOM_COLOR;

		return scene;
	}

	private void EvaluatePathway(SceneObject? obj) {
		ArgumentNullException.ThrowIfNull(obj);
		var gameTransform = obj.Transform!;

		PathwaySide side;
		if (obj.Name == "HitPointRoad")
			side = PathwaySide.Bottom;
		else
			side = PathwaySide.Top;

		gameTransform.ComputeGlobalTransform(out var hitpointPosition, out _);

		int tempOffset = -42;
		pathwayInfo[(int)side] = new(
			hitpointPosition.X * MUSEDASH_MULTIPLIER_POSITIONS,
			(hitpointPosition.Y * MUSEDASH_MULTIPLIER_POSITIONS) + tempOffset,
			obj
		);
	}

	SceneObject ImportGameObject(GameObject unityGO, SceneTransform? parent) {
		if (pathIdToObject.TryGetValue(unityGO.m_PathID, out var existing))
			return existing;

		var obj = new SceneObject {
			Name = unityGO.m_Name ?? "",
			Active = true,
			Scene = this
		};
		allObjects.Add(obj);
		pathIdToObject[unityGO.m_PathID] = obj;

		var unityTransform = unityGO.GetFirstComponent<Transform>();
		if (unityTransform != null)
			obj.Transform.ReadFrom(unityTransform);

		if (parent != null)
			obj.Transform.SetParent(parent);

		foreach (var comp in unityGO.Components) {
			switch (comp) {
				case SpriteRenderer sr:
					obj.AddComponent(new SceneSpriteRenderer(sr));
					break;
				case Animator animator:
					obj.AddComponent(new SceneAnimator { UnityAnimator = animator });
					break;
			}
		}

		if (unityTransform != null) {
			foreach (var childTransform in unityTransform.GetChildren()) {
				var childGO = childTransform.GetGameObject();
				if (childGO != null)
					ImportGameObject(childGO, obj.Transform);
			}
		}

		return obj;
	}

	void BuildRenderOrder() {
		sortedRenderers.Clear();
		foreach (var obj in allObjects) {
			if (!obj.Active) continue;
			foreach (var renderer in obj.GetComponents<SceneRenderer>())
				sortedRenderers.Add(renderer);
		}
		sortedRenderers.Sort((a, b) => {
			int cmp = a.SortingLayerID.CompareTo(b.SortingLayerID);
			if (cmp != 0) return cmp;
			cmp = a.SortingOrder.CompareTo(b.SortingOrder);
			if (cmp != 0) return cmp;
			a.Transform.GetWorldPosition(out _, out _, out float az);
			b.Transform.GetWorldPosition(out _, out _, out float bz);
			return bz.CompareTo(az);
		});
	}

	public void Initialize(DashGameLevel game) { }

	public void RenderBackground(DashGameLevel game) {
		Rlgl.PushMatrix();
		Rlgl.Scalef(MUSEDASH_MULTIPLIER_POSITIONS, MUSEDASH_MULTIPLIER_POSITIONS, 1);
		foreach (var renderer in sortedRenderers)
			renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void RenderPathway(DashGameLevel game, PathwaySide side, float alpha, float size, float rotation) {
		// This is misleading! :(
		// This just sets rotations/alphas at the moment...	
		var obj = ((SceneObject)pathwayInfo[(int)side].UserData!);
		var transform = obj.Transform;

		transform.LocalRotationX = 0;
		transform.LocalRotationY = 0;
		transform.LocalRotationZ = NMath.Remap(rotation, 0, 1, -1, 1);
		transform.LocalRotationW = 1;

		transform.LocalScaleX = size;
		transform.LocalScaleY = size;
	}

	public void Think(DashGameLevel game) { }
	public void Refresh(DashGameLevel game) { }
	public void PlaySound(SceneSound sound, int hits) { }
	public string? GetBossAnimation(BossAnimationType type, out double time) { time = 0; return null; }
	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time) { time = 0; return null; }
	public string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType) => null;
	public ModelData? GetEnemyModel(DashEnemy enemy) => null;
	public ModelData? GetHP(out string? mountAnimation) { mountAnimation = null; return null; }
	public BoneInstance? GetHPMount(DashEnemy enemy) => null;
	public string GetMasherHitAnimation() => "";
	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway) => ref pathwayInfo[(int)pathway];
	public Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
	public Vector2F GetPathwayPosition(PathwaySide side) => GetPathwayInformation(side).Position;
	public MusicTrack? GetPressIdleSound() => null;
	public void GetSustainResources(PathwaySide pathway, out ITexture start, out ITexture end,
		out ITexture body, out ITexture up, out ITexture down, out float rotationDegsPerSecond) {
		start = null!; end = null!; body = null!; up = null!; down = null!; rotationDegsPerSecond = 0;
	}
	internal void MountToFilesystem() { }
}