using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Color = Nucleus.Common.Types.Color;
using Texture = Nucleus.ManagedMemory.Texture;
using Texture2D = AssetStudio.Texture2D;
using Transform = AssetStudio.Transform;

namespace CloneDash.Scenes;

public record class MuseDashSceneInfo
{
	readonly static Dictionary<ulong, MuseDashSceneInfo> scenes = [];

	public readonly string MapName;
	public readonly int MapIdx;

	public static IEnumerable<MuseDashSceneInfo> GetScenes() => scenes.Values;

	public MuseDashSceneInfo(int idx, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = "scene_{0:00}") {
		MapIdx = idx;
		MapName = string.Format(format, idx);

		scenes[MapName.Hash()] = this;
	}

	public static MuseDashSceneInfo? GetSceneInfo(ReadOnlySpan<char> name){
		ulong hash = name.Hash();
		if (scenes.TryGetValue(hash, out var ret))
			return ret;
		return null;
	}

	public static readonly MuseDashSceneInfo SpaceStation = new(1);
	public static readonly MuseDashSceneInfo RetroCity = new(2);
	public static readonly MuseDashSceneInfo HauntedMansion = new(3);
	public static readonly MuseDashSceneInfo RainyNight = new(4);
	public static readonly MuseDashSceneInfo Candyland = new(5);
	public static readonly MuseDashSceneInfo Oriental = new(6);
	public static readonly MuseDashSceneInfo GrooveCoaster = new(7);
	public static readonly MuseDashSceneInfo Gensokyo = new(8);
	public static readonly MuseDashSceneInfo GameGraveyard = new(9);
	public static readonly MuseDashSceneInfo Museland = new(10, "scene_{0:00}_miku");
	public static readonly MuseDashSceneInfo Mirrorland = new(10, "scene_{0:00}_rin_len");
	public static readonly MuseDashSceneInfo Warriorland = new(11);
	public static readonly MuseDashSceneInfo JadeTemple = new(12);
}

static class UnityCRC32
{
	static readonly uint[] table = new uint[256];
	static UnityCRC32() {
		for (uint i = 0; i < 256; i++) {
			uint crc = i;
			for (int j = 0; j < 8; j++)
				crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
			table[i] = crc;
		}
	}
	public static uint Hash(string s) {
		uint crc = 0xFFFFFFFF;
		foreach (char c in s)
			crc = (crc >> 8) ^ table[(byte)(crc ^ c)];
		return crc ^ 0xFFFFFFFF;
	}
}

class DecodedClip
{
	public struct CurveData
	{
		public GenericBinding Binding;
		public List<(float time, float value, float inSlope, float outSlope)> Keys;
	}

	public CurveData[] Curves;
	public float StartTime, StopTime;
	public bool LoopTime;

	public static DecodedClip Decode(AnimationClip clip) {
		var result = new DecodedClip();
		result.StartTime = clip.m_MuscleClip?.m_StartTime ?? 0;
		result.StopTime = clip.m_MuscleClip?.m_StopTime ?? 0;
		result.LoopTime = clip.m_MuscleClip?.m_LoopTime ?? false;

		var bindings = clip.m_ClipBindingConstant;
		if (bindings?.genericBindings == null || clip.m_MuscleClip?.m_Clip == null) {
			result.Curves = [];
			return result;
		}

		var muscleClip = clip.m_MuscleClip.m_Clip;

		int totalCurves = 0;
		foreach (var b in bindings.genericBindings) {
			if (b.typeID == ClassIDType.Transform) {
				totalCurves += b.attribute switch {
					1 => 3,
					2 => 4,
					3 => 3,
					4 => 3,
					_ => 1
				};
			}
			else {
				totalCurves++;
			}
		}

		var curveLists = new List<(float time, float value, float inSlope, float outSlope)>[totalCurves];
		for (int i = 0; i < totalCurves; i++)
			curveLists[i] = [];

		if (muscleClip.m_StreamedClip?.data != null && muscleClip.m_StreamedClip.data.Length > 0) {
			var frames = muscleClip.m_StreamedClip.ReadData();
			foreach (var frame in frames) {
				foreach (var key in frame.keyList) {
					if (key.index >= 0 && key.index < totalCurves)
						curveLists[key.index].Add((frame.time, key.value, key.inSlope, key.outSlope));
				}
			}
		}

		if (muscleClip.m_DenseClip != null && muscleClip.m_DenseClip.m_SampleArray?.Length > 0) {
			var dense = muscleClip.m_DenseClip;
			int curveCount = (int)dense.m_CurveCount;
			int streamedCurveCount = (int)(muscleClip.m_StreamedClip?.curveCount ?? 0);

			for (int frameIdx = 0; frameIdx < dense.m_FrameCount; frameIdx++) {
				float t = dense.m_BeginTime + frameIdx / dense.m_SampleRate;
				for (int c = 0; c < curveCount; c++) {
					int sampleIdx = frameIdx * curveCount + c;
					if (sampleIdx >= dense.m_SampleArray.Length) break;
					int curveIdx = streamedCurveCount + c;
					if (curveIdx < totalCurves) {
						float val = dense.m_SampleArray[sampleIdx];
						curveLists[curveIdx].Add((t, val, float.PositiveInfinity, float.PositiveInfinity));
					}
				}
			}
		}

		if (muscleClip.m_ConstantClip?.data != null) {
			int streamedCount = (int)(muscleClip.m_StreamedClip?.curveCount ?? 0);
			int denseCount = (int)(muscleClip.m_DenseClip?.m_CurveCount ?? 0);
			int constantOffset = streamedCount + denseCount;

			for (int c = 0; c < muscleClip.m_ConstantClip.data.Length; c++) {
				int curveIdx = constantOffset + c;
				if (curveIdx < totalCurves) {
					float val = muscleClip.m_ConstantClip.data[c];
					curveLists[curveIdx].Add((result.StartTime, val, 0, 0));
					curveLists[curveIdx].Add((result.StopTime, val, 0, 0));
				}
			}
		}

		var curveDataList = new List<CurveData>();
		int ci = 0;
		foreach (var b in bindings.genericBindings) {
			int numFloats;
			if (b.typeID == ClassIDType.Transform) {
				numFloats = b.attribute switch { 1 => 3, 2 => 4, 3 => 3, 4 => 3, _ => 1 };
			}
			else {
				numFloats = 1;
			}

			for (int f = 0; f < numFloats && ci < totalCurves; f++, ci++) {
				curveDataList.Add(new CurveData {
					Binding = b,
					Keys = curveLists[ci]
				});
			}
		}

		result.Curves = curveDataList.ToArray();
		return result;
	}

	public static float Eval(List<(float time, float value, float inSlope, float outSlope)> keys, float t) {
		if (keys.Count == 0) return 0;
		if (keys.Count == 1) return keys[0].value;
		if (t <= keys[0].time) return keys[0].value;
		if (t >= keys[^1].time) return keys[^1].value;

		int i = 0;
		for (; i < keys.Count - 2; i++)
			if (t < keys[i + 1].time) break;

		var k0 = keys[i];
		var k1 = keys[i + 1];
		float dt = k1.time - k0.time;
		if (dt <= 0) return k0.value;

		if (float.IsPositiveInfinity(k0.outSlope) || float.IsPositiveInfinity(k1.inSlope)) {
			float u = (t - k0.time) / dt;
			return k0.value + (k1.value - k0.value) * u;
		}

		float uu = (t - k0.time) / dt;
		float uu2 = uu * uu;
		float uu3 = uu2 * uu;
		float a = 2 * uu3 - 3 * uu2 + 1;
		float b = uu3 - 2 * uu2 + uu;
		float c = uu3 - uu2;
		float d = -2 * uu3 + 3 * uu2;
		return a * k0.value + b * (k0.outSlope * dt) + c * (k1.inSlope * dt) + d * k1.value;
	}
}

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
			wx *= p.LocalScaleX; wy *= p.LocalScaleY; wz *= p.LocalScaleZ;
			RotateVector(p.LocalRotationX, p.LocalRotationY, p.LocalRotationZ, p.LocalRotationW,
				wx, wy, wz, out wx, out wy, out wz);
			wx += p.LocalX; wy += p.LocalY; wz += p.LocalZ;
			p = p.Parent;
		}
	}

	public void GetWorldScale(out float sx, out float sy) {
		sx = LocalScaleX; sy = LocalScaleY;
		var p = Parent;
		while (p != null) { sx *= p.LocalScaleX; sy *= p.LocalScaleY; p = p.Parent; }
	}

	public float GetWorldRotationZ() {
		float totalRad = LocalRotationZRadians();
		var p = Parent;
		while (p != null) { totalRad += p.LocalRotationZRadians(); p = p.Parent; }
		return totalRad * (180f / MathF.PI);
	}

	float LocalRotationZRadians() =>
		MathF.Atan2(2f * (LocalRotationW * LocalRotationZ + LocalRotationX * LocalRotationY),
					1f - 2f * (LocalRotationY * LocalRotationY + LocalRotationZ * LocalRotationZ));

	static void RotateVector(float qx, float qy, float qz, float qw,
		float vx, float vy, float vz, out float ox, out float oy, out float oz) {
		float dot = qx * vx + qy * vy + qz * vz;
		float qsq = qx * qx + qy * qy + qz * qz;
		float cx = qy * vz - qz * vy, cy = qz * vx - qx * vz, cz = qx * vy - qy * vx;
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
			position = new Vector3(position.X * parent.LocalScaleX, position.Y * parent.LocalScaleY, position.Z * parent.LocalScaleZ);
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
		float cx = uy * v.Z - uz * v.Y, cy = uz * v.X - ux * v.Z, cz = ux * v.Y - uy * v.X;
		return new Vector3(
			2f * dotUV * ux + (s * s - dotUU) * v.X + 2f * s * cx,
			2f * dotUV * uy + (s * s - dotUU) * v.Y + 2f * s * cy,
			2f * dotUV * uz + (s * s - dotUU) * v.Z + 2f * s * cz);
	}

	private static Quaternion MultiplyQuaternion(Quaternion a, Quaternion b) {
		return new Quaternion(
			a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
			a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
			a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
			a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
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
	System.Numerics.Vector4 color;
	private SpriteMaskInteraction MaskInteraction;

	public SceneSpriteRenderer(SpriteRenderer sr) { UnitySpriteRenderer = sr; }

	public override void Awake() {
		if (UnitySpriteRenderer.m_MaskInteraction != SpriteMaskInteraction.None) {
			// TODO: masks
			return;
		}

		var sprite = UnitySpriteRenderer.GetSprite();
		if (sprite == null) return;
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;

		texture = ((MuseDashScene)Object.Scene).LoadTexture(tex2d);
		atlasW = (int)texture.Width; atlasH = (int)texture.Height;
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;

		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
		flipX = UnitySpriteRenderer.m_FlipX; flipY = UnitySpriteRenderer.m_FlipY;

		var c = UnitySpriteRenderer.m_Color;
		color = new(c.R, c.G, c.B, c.A);
		SortingOrder = UnitySpriteRenderer.m_SortingOrder;
		SortingLayerID = (int)UnitySpriteRenderer.m_SortingLayerID;

		MaskInteraction = UnitySpriteRenderer.m_MaskInteraction;
	}

	public void SetSprite(Sprite sprite, MuseDashScene scene) {
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;
		texture = scene.LoadTexture(tex2d);
		atlasW = (int)texture.Width; atlasH = (int)texture.Height;
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;
		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
	}

	public override void Render(MuseDashScene scene) {
		if (texture == null || texRectW <= 0 || texRectH <= 0) return;
		if (!MuseDashScene.IsActiveInHierarchy(Object)) return;

		Transform.GetWorldPosition(out float wx, out float wy, out _);
		Transform.GetWorldScale(out float sx, out float sy);

		float w = unitW * MathF.Abs(sx), h = unitH * MathF.Abs(sy);
		float offX = -pivotX * w, offY = -(1f - pivotY) * h;

		float flippedY = atlasH - texRectY - texRectH;
		float u0 = texRectX / atlasW, v0 = flippedY / atlasH;
		float u1 = (texRectX + texRectW) / atlasW, v1 = (flippedY + texRectH) / atlasH;

		if (flipX ^ (sx < 0)) (u0, u1) = (u1, u0);
		if (flipY ^ (sy < 0)) (v0, v1) = (v1, v0);

		float screenX = wx + offX, screenY = -wy + offY;
		float rotDeg = Transform.GetWorldRotationZ();
		uint texId = texture.HardwareID;
		var fcolor = this.color * this.Object.GetColor();

		if (MathF.Abs(rotDeg) > 0.01f) {
			float cx = wx, cy = -wy;
			float rad = -rotDeg * (MathF.PI / 180f);
			float cos = MathF.Cos(rad), sin = MathF.Sin(rad);
			float x0 = offX, y0 = offY, x1 = offX, y1 = offY + h;
			float x2 = offX + w, y2 = offY + h, x3 = offX + w, y3 = offY;

			Rlgl.SetTexture(texId); Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4f(fcolor.X, fcolor.Y, fcolor.Z, fcolor.W);
			Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex2f(cx + x0 * cos - y0 * sin, cy + x0 * sin + y0 * cos);
			Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex2f(cx + x1 * cos - y1 * sin, cy + x1 * sin + y1 * cos);
			Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex2f(cx + x2 * cos - y2 * sin, cy + x2 * sin + y2 * cos);
			Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex2f(cx + x3 * cos - y3 * sin, cy + x3 * sin + y3 * cos);
			Rlgl.End(); Rlgl.SetTexture(0);
		}
		else {
			Rlgl.SetTexture(texId); Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4f(fcolor.X, fcolor.Y, fcolor.Z, fcolor.W);
			Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex2f(screenX, screenY);
			Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex2f(screenX, screenY + h);
			Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex2f(screenX + w, screenY + h);
			Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex2f(screenX + w, screenY);
			Rlgl.End(); Rlgl.SetTexture(0);
		}
	}
}

class RuntimeClip
{
	public record struct TargetCurve(
		SceneTransform Target,
		int PosX, int PosY, int PosZ,
		int RotX, int RotY, int RotZ, int RotW,
		int ScX, int ScY, int ScZ
	);

	public DecodedClip Decoded;
	public List<TargetCurve> TransformTargets = [];
	public float Duration;
	public bool Loop;
}

public class SceneAnimator : SceneComponent
{
	public Animator? UnityAnimator { get; set; }

	readonly List<RuntimeClip> runtimeClips = [];
	float time;
	public int ResolvedClipCount => runtimeClips.Count;

	public override void Awake() {
		if (UnityAnimator == null) return;

		var controller = UnityAnimator.GetController();
		if (controller == null) return;

		var scene = (MuseDashScene)Object.Scene;
		List<AnimationClip> animClips = [];

		switch (controller) {
			case AnimatorController ac:
				foreach (var pptr in ac.m_AnimationClips)
					if (pptr.TryGet(out var clip)) animClips.Add(clip);
				break;
			case AnimatorOverrideController aoc:
				if (aoc.m_Controller.TryGet(out var baseCtrl) && baseCtrl is AnimatorController baseAc)
					foreach (var pptr in baseAc.m_AnimationClips)
						if (pptr.TryGet(out var clip)) animClips.Add(clip);
				if (aoc.m_Clips != null)
					foreach (var ov in aoc.m_Clips)
						if (ov.m_OverrideClip.TryGet(out var overrideClip)) animClips.Add(overrideClip);
				break;
		}

		if (animClips.Count == 0) return;

		var hashToTransform = new Dictionary<uint, SceneTransform>();
		BuildPathHashes(Object.Transform, "", hashToTransform);

		foreach (var clip in animClips) {
			var decoded = DecodedClip.Decode(clip);
			if (decoded.Curves.Length == 0) continue;

			var runtime = new RuntimeClip {
				Decoded = decoded,
				Duration = decoded.StopTime - decoded.StartTime,
				Loop = decoded.LoopTime || clip.m_WrapMode == 2 || clip.m_WrapMode == 0
			};

			var grouped = new Dictionary<uint, (int posX, int posY, int posZ, int rotX, int rotY, int rotZ, int rotW, int scX, int scY, int scZ)>();

			for (int i = 0; i < decoded.Curves.Length; i++) {
				ref var curve = ref decoded.Curves[i];
				if (curve.Binding.typeID != ClassIDType.Transform) continue;
				if (curve.Keys.Count == 0) continue;

				uint pathHash = curve.Binding.path;
				if (!grouped.TryGetValue(pathHash, out var g))
					g = (-1, -1, -1, -1, -1, -1, -1, -1, -1, -1);

				uint attr = curve.Binding.attribute;
				int componentIdx = GetComponentIndex(decoded.Curves, i, curve.Binding);

				switch (attr) {
					case 1: // position
						if (componentIdx == 0) g.posX = i;
						else if (componentIdx == 1) g.posY = i;
						else if (componentIdx == 2) g.posZ = i;
						break;
					case 2: // rotation
						if (componentIdx == 0) g.rotX = i;
						else if (componentIdx == 1) g.rotY = i;
						else if (componentIdx == 2) g.rotZ = i;
						else if (componentIdx == 3) g.rotW = i;
						break;
					case 3: // scale
						if (componentIdx == 0) g.scX = i;
						else if (componentIdx == 1) g.scY = i;
						else if (componentIdx == 2) g.scZ = i;
						break;
					case 4: // euler (treat as rotation via euler angles — simplified) TODO
						break;
				}

				grouped[pathHash] = g;
			}

			foreach (var (pathHash, g) in grouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				runtime.TransformTargets.Add(new(target, g.posX, g.posY, g.posZ,
					g.rotX, g.rotY, g.rotZ, g.rotW, g.scX, g.scY, g.scZ));
			}

			if (runtime.TransformTargets.Count > 0)
				runtimeClips.Add(runtime);
		}

	}

	static int GetComponentIndex(DecodedClip.CurveData[] curves, int curveIdx, GenericBinding binding) {
		int count = 0;
		for (int i = curveIdx - 1; i >= 0; i--) {
			if (curves[i].Binding == binding) count++;
			else break;
		}
		return count;
	}

	void BuildPathHashes(SceneTransform transform, string path, Dictionary<uint, SceneTransform> map) {
		uint hash = UnityCRC32.Hash(path);
		map.TryAdd(hash, transform);

		foreach (var child in transform.Children) {
			string childPath = string.IsNullOrEmpty(path) ? child.Object.Name : path + "/" + child.Object.Name;
			BuildPathHashes(child, childPath, map);
		}
	}

	public void Evaluate(float deltaTime) {
		if (runtimeClips.Count == 0) return;
		time += deltaTime;

		foreach (var clip in runtimeClips) {
			float t;
			if (clip.Loop && clip.Duration > 0) {
				t = clip.Decoded.StartTime + ((time - clip.Decoded.StartTime) % clip.Duration);
				if (t < clip.Decoded.StartTime) t += clip.Duration;
			}
			else {
				t = Math.Clamp(time, clip.Decoded.StartTime, clip.Decoded.StopTime);
			}

			foreach (var target in clip.TransformTargets) {
				var tr = target.Target;
				if (target.PosX >= 0) tr.LocalX = DecodedClip.Eval(clip.Decoded.Curves[target.PosX].Keys, t);
				if (target.PosY >= 0) tr.LocalY = DecodedClip.Eval(clip.Decoded.Curves[target.PosY].Keys, t);
				if (target.PosZ >= 0) tr.LocalZ = DecodedClip.Eval(clip.Decoded.Curves[target.PosZ].Keys, t);
				if (target.RotX >= 0) tr.LocalRotationX = DecodedClip.Eval(clip.Decoded.Curves[target.RotX].Keys, t);
				if (target.RotY >= 0) tr.LocalRotationY = DecodedClip.Eval(clip.Decoded.Curves[target.RotY].Keys, t);
				if (target.RotZ >= 0) tr.LocalRotationZ = DecodedClip.Eval(clip.Decoded.Curves[target.RotZ].Keys, t);
				if (target.RotW >= 0) tr.LocalRotationW = DecodedClip.Eval(clip.Decoded.Curves[target.RotW].Keys, t);
				if (target.ScX >= 0) tr.LocalScaleX = DecodedClip.Eval(clip.Decoded.Curves[target.ScX].Keys, t);
				if (target.ScY >= 0) tr.LocalScaleY = DecodedClip.Eval(clip.Decoded.Curves[target.ScY].Keys, t);
				if (target.ScZ >= 0) tr.LocalScaleZ = DecodedClip.Eval(clip.Decoded.Curves[target.ScZ].Keys, t);
			}
		}
	}
}
public class SceneObject
{
	public string Name = "";
	public bool Active = true;
	public SceneTransform Transform { get; } = new();
	public MuseDashScene Scene { get; internal set; } = null!;
	readonly List<SceneComponent> components = [];
	public IReadOnlyList<SceneComponent> Components => components;
	public System.Numerics.Vector4 Color = new(1, 1, 1, 1);

	public SceneObject() { Transform.Object = this; components.Add(Transform); }

	public T AddComponent<T>(T component) where T : SceneComponent {
		component.Object = this; components.Add(component); return component;
	}
	public T? GetComponent<T>() where T : SceneComponent {
		foreach (var c in components) if (c is T t) return t; return null;
	}
	public IEnumerable<T> GetComponents<T>() where T : SceneComponent {
		foreach (var c in components) if (c is T t) yield return t;
	}
	public void Awake() { foreach (var c in components) c.Awake(); }

	public string Dump(StringBuilder? sb = null, int depth = 0) {
		sb ??= new();
		string indent = new(' ', depth * 2);
		string compInfo = components.Count > 1 ? $" [{string.Join(", ", components.Skip(1).Select(c => c.GetType().Name))}]" : "";
		sb.AppendLine($"{indent}{Name} ({Transform.LocalX:F2}, {Transform.LocalY:F2}){compInfo}");
		foreach (var child in Transform.Children) child.Object.Dump(sb, depth + 1);
		return sb.ToString();
	}

	public System.Numerics.Vector4 GetColor() {
		var c = new System.Numerics.Vector4(1, 1, 1, 1);
		var p = this;
		while (p != null) { c *= p.Color; p = p.Transform.Parent?.Object; }
		return c;
	}
}

public class MuseDashScene : ISceneDescriptor
{
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 1;
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	readonly List<SceneObject> allObjects = [];
	readonly List<SceneRenderer> sortedRenderers = [];
	readonly List<SceneAnimator> animators = [];
	SceneObject? root;
	readonly Dictionary<long, ITexture> textureCache = [];
	readonly Dictionary<long, SceneObject> pathIdToObject = [];

	internal ITexture LoadTexture(Texture2D? texture2D) {
		if (textureCache.TryGetValue(texture2D!.m_PathID, out var tex)) return tex;
		textureCache[texture2D.m_PathID] = tex = MuseDashCompatibility.ConvertTexture(EngineCore.Level, texture2D!);
		return tex;
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		var sceneInfo = MuseDashSceneInfo.GetSceneInfo(name);
		if (sceneInfo == null)
			return null;

		var sceneGameObject = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>(sceneInfo.MapName)!;
		var sceneSubControl = new MonoBehaviourReader(
			sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl")
			?? throw new NullReferenceException("No scene control?"));

		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;

		var scene = new MuseDashScene();
		var pathwaysObject = scene.ImportGameObject(scenePoint, null);

		var pathwayChildren = new List<(SceneObject obj, Vector3 pos)>();
		foreach (var child in pathwaysObject.Transform.Children) {
			child.Object.Transform.ComputeGlobalTransform(out var pos, out _);
			pathwayChildren.Add((child.Object, pos));
		}

		if (pathwayChildren.Count >= 2) {
			pathwayChildren.Sort((a, b) => b.pos.Y.CompareTo(a.pos.Y));
			scene.AssignPathway(PathwaySide.Top, pathwayChildren[0].obj, pathwayChildren[0].pos);
			scene.AssignPathway(PathwaySide.Bottom, pathwayChildren[1].obj, pathwayChildren[1].pos);
		}
		else if (pathwayChildren.Count == 1) {
			scene.AssignPathway(PathwaySide.Bottom, pathwayChildren[0].obj, pathwayChildren[0].pos);
		}

		var rootTransform = sceneGameObject.GetFirstComponent<Transform>()!;
		scene.root = scene.ImportGameObject(rootTransform.GetGameObject()!, null);

		foreach (var obj in scene.allObjects) obj.Awake();

		foreach (var obj in scene.allObjects)
			foreach (var anim in obj.GetComponents<SceneAnimator>())
				scene.animators.Add(anim);

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

	private void AssignPathway(PathwaySide side, SceneObject obj, Vector3 pos) {
		pathwayInfo[(int)side] = new(pos.X * MUSEDASH_MULTIPLIER_POSITIONS, pos.Y * MUSEDASH_MULTIPLIER_POSITIONS, obj);
	}

	SceneObject ImportGameObject(GameObject unityGO, SceneTransform? parent) {
		if (pathIdToObject.TryGetValue(unityGO.m_PathID, out var existing)) return existing;

		var obj = new SceneObject { Name = unityGO.m_Name ?? "", Active = unityGO.m_IsActive, Scene = this };
		allObjects.Add(obj);
		pathIdToObject[unityGO.m_PathID] = obj;

		var unityTransform = unityGO.GetFirstComponent<Transform>();
		if (unityTransform != null) obj.Transform.ReadFrom(unityTransform);
		if (parent != null) obj.Transform.SetParent(parent);

		foreach (var comp in unityGO.Components) {
			switch (comp) {
				case SpriteRenderer sr: obj.AddComponent(new SceneSpriteRenderer(sr)); break;
				case Animator animator: obj.AddComponent(new SceneAnimator { UnityAnimator = animator }); break;
			}
		}

		if (unityTransform != null)
			foreach (var childTransform in unityTransform.GetChildren()) {
				var childGO = childTransform.GetGameObject();
				if (childGO != null) ImportGameObject(childGO, obj.Transform);
			}
		return obj;
	}

	void BuildRenderOrder() {
		sortedRenderers.Clear();
		foreach (var obj in allObjects) {
			if (!IsActiveInHierarchy(obj)) continue;
			foreach (var renderer in obj.GetComponents<SceneRenderer>()) sortedRenderers.Add(renderer);
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

	internal static bool IsActiveInHierarchy(SceneObject obj) {
		var current = obj;
		while (current != null) {
			if (!current.Active) return false;
			current = current.Transform.Parent?.Object;
		}
		return true;
	}

	public void Initialize(DashGameLevel game) { }

	public void RenderBackground(DashGameLevel game) {
		Rlgl.PushMatrix();
		Rlgl.Scalef(MUSEDASH_MULTIPLIER_POSITIONS, MUSEDASH_MULTIPLIER_POSITIONS, 1);
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void RenderPathway(DashGameLevel game, PathwaySide side, float alpha, float size, float rotation) {
		var obj = ((SceneObject)pathwayInfo[(int)side].UserData!);
		var transform = obj.Transform;
		transform.LocalRotationX = 0; transform.LocalRotationY = 0;
		transform.LocalRotationZ = NMath.Remap(rotation, 0, 1, -1, 1);
		transform.LocalRotationW = 1;
		transform.LocalScaleX = size; transform.LocalScaleY = size;
		obj.Color.W = alpha / 255f;
	}

	public void Think(DashGameLevel game) {
		float dt = (float)globals.CurTimeDelta;
		foreach (var anim in animators) anim.Evaluate(dt);
	}

	public void Refresh(DashGameLevel game) { }
	public void PlaySound(SceneSound sound, int hits) {
		switch (sound) {
			case SceneSound.Begin:
				break;
		}
	}
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
