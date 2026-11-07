using AssetStudio;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Particles;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;
using Transform = AssetStudio.Transform;

namespace CloneDash.Compatibility.Unity;


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
	public List<PPtr<AssetStudio.Object>> PPtrMapping = [];
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

		var muscleClip = clip.m_MuscleClip.m_Clip.data;
		result.PPtrMapping = bindings.pptrCurveMapping ?? [];

		int totalCurves = 0;
		List<bool> pptrCurves = [];
		foreach (var b in bindings.genericBindings) {
			int bindingCurves = 1;
			if (b.typeID == ClassIDType.Transform) {
				bindingCurves = b.attribute switch {
					1 => 3,
					2 => 4,
					3 => 3,
					4 => 3,
					_ => 1
				};
			}
			totalCurves += bindingCurves;
			for (int f = 0; f < bindingCurves; f++)
				pptrCurves.Add(b.isPPtrCurve == 1);
		}

		var curveLists = new List<(float time, float value, float inSlope, float outSlope)>[totalCurves];
		for (int i = 0; i < totalCurves; i++)
			curveLists[i] = [];

		if (muscleClip.m_StreamedClip?.data != null && muscleClip.m_StreamedClip.data.Length > 0) {
			var frames = muscleClip.m_StreamedClip.ReadData();
			foreach (var frame in frames) {
				bool sentinel = frame.time < -1e30f;
				foreach (var key in frame.keyList) {
					if (key.index < 0 || key.index >= totalCurves) continue;
					if (sentinel && !pptrCurves[key.index]) continue; // skip sentinel frame
					curveLists[key.index].Add((sentinel ? result.StartTime : frame.time, key.value, key.inSlope, key.outSlope));
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

	public static float EvalStepped(List<(float time, float value, float inSlope, float outSlope)> keys, float t) {
		if (keys.Count == 0) return 0;
		float value = keys[0].value;
		foreach (var key in keys) {
			if (key.time > t) break;
			value = key.value;
		}
		return value;
	}

	public static float Eval(List<(float time, float value, float inSlope, float outSlope)> keys, float t) {
		if (keys.Count == 0) return 0;
		if (keys.Count == 1) return keys[0].value;
		if (t <= keys[0].time) return keys[0].value;
		if (t >= keys[^1].time) return keys[^1].value;

		int i = 0;
		for (; i < keys.Count - 1; i++)
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

	public System.Numerics.Matrix4x4 GetWorldMatrix() {
		var matrix = System.Numerics.Matrix4x4.CreateScale(LocalScaleX, LocalScaleY, LocalScaleZ)
			* System.Numerics.Matrix4x4.CreateFromQuaternion(new(LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW))
			* System.Numerics.Matrix4x4.CreateTranslation(LocalX, LocalY, LocalZ);
		return Parent == null ? matrix : matrix * Parent.GetWorldMatrix();
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
	public abstract void Render(UnitySimScene scene);
}

public class SceneSpriteRenderer : SceneRenderer
{
	public SpriteRenderer UnitySpriteRenderer { get; }
	ITexture? texture;
	Sprite? currentSprite;
	float texRectX, texRectY, texRectW, texRectH;
	int atlasW, atlasH;
	float unitW, unitH;
	float pivotX, pivotY;
	bool flipX, flipY;
	System.Numerics.Vector4 color;
	private SpriteMaskInteraction MaskInteraction;
	float pixelSize = 0.01f;
	float trimX, trimY;
	bool outlined;
	float outlineWidth;
	System.Numerics.Vector4 outlineColor;
	System.Numerics.Vector4 materialColor = new(1, 1, 1, 1);

	bool useMesh;
	float[] mPosX = null!, mPosY = null!, mU = null!, mV = null!;
	ushort[] mIdx = null!;

	public SceneSpriteRenderer(SpriteRenderer sr) { UnitySpriteRenderer = sr; }
	public bool HasTexture => texture != null;
	public System.Numerics.Vector4 Color { get => color; set => color = value; }
	public int SpriteDrawMode => UnitySpriteRenderer.m_DrawMode;
	public bool FlipX { get => flipX; set => flipX = value; }
	public bool FlipY { get => flipY; set => flipY = value; }
	public System.Numerics.Vector2 Size { get; set; }

	public override void Awake() {
		if (UnitySpriteRenderer.m_MaskInteraction != SpriteMaskInteraction.None) {
			return;
		}

		var sprite = UnitySpriteRenderer.GetSprite();
		if (sprite == null) return;
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;

		currentSprite = sprite;
		texture = ((UnitySimScene)Object.Scene).LoadTexture(tex2d);
		atlasW = (int)texture.GetWidth(); atlasH = (int)texture.GetHeight();
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;

		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
		pixelSize = 1 / ppu;
		trimX = sprite.m_RD.textureRectOffset.X; trimY = sprite.m_RD.textureRectOffset.Y;
		flipX = UnitySpriteRenderer.m_FlipX; flipY = UnitySpriteRenderer.m_FlipY;
		Size = new(UnitySpriteRenderer.m_Size.X, UnitySpriteRenderer.m_Size.Y);
		ReadMaterial();

		var c = UnitySpriteRenderer.m_Color;
		color = new(c.R, c.G, c.B, c.A);
		SortingOrder = UnitySpriteRenderer.m_SortingOrder;
		SortingLayerID = (int)UnitySpriteRenderer.m_SortingLayerID;
		MaskInteraction = UnitySpriteRenderer.m_MaskInteraction;

		useMesh = TryLoadMesh(sprite);
	}

	bool TryLoadMesh(Sprite sprite) {
		var rd = sprite.m_RD;
		var vd = rd.m_VertexData;
		if (vd == null || vd.m_VertexCount == 0) return false;
		if (vd.m_DataSize == null || vd.m_DataSize.Length == 0) return false;
		if (vd.m_Channels == null || vd.m_Channels.Count < 1) return false;
		if (vd.m_Streams == null || vd.m_Streams.Count == 0) return false;

		int vc = (int)vd.m_VertexCount;
		byte[] data = vd.m_DataSize;
		var posCh = vd.m_Channels[0];
		if (posCh.dimension < 2 || posCh.stream >= vd.m_Streams.Count) return false;

		var ps = vd.m_Streams[posCh.stream];
		int pStride = (int)ps.stride, pBase = (int)ps.offset;
		if (pStride <= 0) return false;
		int pfs = FmtSize(posCh.format);

		float uvSx = rd.uvTransform.X, uvOx = rd.uvTransform.Y;
		float uvSy = rd.uvTransform.Z, uvOy = rd.uvTransform.W;

		mPosX = new float[vc]; mPosY = new float[vc];
		mU = new float[vc]; mV = new float[vc];
		for (int i = 0; i < vc; i++) {
			int off = pBase + i * pStride + posCh.offset;
			if (off + posCh.dimension * pfs > data.Length) return false;
			float px = RdF(data, off, posCh.format);
			float py = RdF(data, off + pfs, posCh.format);
			mPosX[i] = px; mPosY[i] = py;
			mU[i] = atlasW > 0 ? (uvSx * px + uvOx) / atlasW : 0;
			mV[i] = atlasH > 0 ? 1f - (uvSy * py + uvOy) / atlasH : 0;
		}

		if (rd.m_IndexBuffer != null && rd.m_IndexBuffer.Length >= 2) {
			int fi = 0, ic = rd.m_IndexBuffer.Length / 2;
			if (rd.m_SubMeshes != null && rd.m_SubMeshes.Count > 0) {
				fi = (int)(rd.m_SubMeshes[0].firstByte / 2);
				ic = (int)rd.m_SubMeshes[0].indexCount;
			}
			int ta = rd.m_IndexBuffer.Length / 2;
			if (fi + ic > ta) ic = ta - fi;
			if (ic <= 0) return false;
			mIdx = new ushort[ic];
			for (int i = 0; i < ic; i++) {
				mIdx[i] = BitConverter.ToUInt16(rd.m_IndexBuffer, (fi + i) * 2);
				if (mIdx[i] >= vc) return false;
			}
		}
		else if (vc == 4) { mIdx = [0, 1, 2, 2, 3, 0]; }
		else return false;

		return true;
	}

	void ReadMaterial() {
		if (UnitySpriteRenderer.m_Materials == null || UnitySpriteRenderer.m_Materials.Count == 0) return;
		if (!UnitySpriteRenderer.m_Materials[0].TryGet(out var material)) return;

		var properties = material.m_SavedProperties;
		foreach (var property in properties.m_Floats) {
			if (property.Key != "_OutlineWidth") continue;
			outlined = true;
			outlineWidth = property.Value;
		}
		if (!outlined) return;

		foreach (var property in properties.m_Colors) {
			var c = property.Value;
			if (property.Key == "_OutlineColor") outlineColor = new(c.R, c.G, c.B, c.A);
			else if (property.Key == "_Color") materialColor = new(c.R, c.G, c.B, c.A);
		}
	}

	static int FmtSize(int f) => f switch {
		0 => 4,
		1 => 2,
		2 => 1,
		3 => 1,
		4 => 2,
		5 => 2,
		6 => 1,
		7 => 1,
		8 => 2,
		9 => 2,
		10 => 4,
		11 => 4,
		_ => 4
	};
	static float RdF(byte[] d, int o, int f) => f switch {
		0 => BitConverter.ToSingle(d, o),
		1 => (float)BitConverter.ToHalf(d, o),
		2 => d[o] / 255f,
		3 => Math.Max((sbyte)d[o] / 127f, -1f),
		4 => BitConverter.ToUInt16(d, o) / 65535f,
		5 => Math.Max(BitConverter.ToInt16(d, o) / 32767f, -1f),
		_ => BitConverter.ToSingle(d, o)
	};

	public void SetSprite(Sprite sprite, UnitySimScene scene) {
		if (sprite == currentSprite) return;
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;
		currentSprite = sprite;
		texture = scene.LoadTexture(tex2d);
		atlasW = (int)texture.GetWidth(); atlasH = (int)texture.GetHeight();
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;
		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
		pixelSize = 1 / ppu;
		trimX = sprite.m_RD.textureRectOffset.X; trimY = sprite.m_RD.textureRectOffset.Y;
		mPosX = null!; mPosY = null!; mU = null!; mV = null!; mIdx = null!;
		useMesh = TryLoadMesh(sprite);
	}

	public override void Render(UnitySimScene scene) {
		if (texture == null) return;
		if (!UnitySimScene.IsActiveInHierarchy(Object)) return;
		// TODO: mesh rendering. Very slow, probably incorrect right now...
		// the quads represent the main geometry and the main problems we're experiencing with geometry anyway
		// if (useMesh && mIdx != null)
		// 	RenderMesh();
		// else if (texRectW > 0 && texRectH > 0)
		if (scene.RenderSpriteMeshes && useMesh && mIdx != null) {
			RenderMesh();
			return;
		}
		RenderQuad();
	}

	void RenderMesh() {
		Transform.GetWorldPosition(out float wx, out float wy, out _);
		Transform.GetWorldScale(out float sx, out float sy);
		float rotDeg = Transform.GetWorldRotationZ();
		uint texId = texture!.GetTextureHandle();
		bool flipV = texture.HasPublicFlags(PublicTextureFlags.RequiresFlippedV);
		var fcolor = this.color * materialColor * this.Object.GetColor();

		if (outlined) {
			float distance = outlineWidth * pixelSize * MathF.Abs(sx);
			var ocolor = outlineColor;
			ocolor.W *= fcolor.W;
			for (int i = 0; i < 8; i++) {
				float angle = i * MathF.PI / 4;
				DrawMesh(wx + MathF.Cos(angle) * distance, wy + MathF.Sin(angle) * distance, sx, sy, rotDeg, texId, flipV, ocolor);
			}
		}

		DrawMesh(wx, wy, sx, sy, rotDeg, texId, flipV, fcolor);
	}

	void DrawMesh(float wx, float wy, float sx, float sy, float rotDeg, uint texId, bool flipV, System.Numerics.Vector4 fcolor) {
		float rad = -rotDeg * (MathF.PI / 180f);
		float cos = MathF.Cos(rad), sin = MathF.Sin(rad);

		Rlgl.DisableBackfaceCulling();
		Rlgl.SetTexture(texId);
		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.Color4f(fcolor.X, fcolor.Y, fcolor.Z, fcolor.W);
		for (int i = 0; i + 2 < mIdx.Length; i += 3) {
			for (int t = 0; t < 3; t++) {
				int vi = mIdx[i + t];
				float lx = mPosX[vi] * sx, ly = mPosY[vi] * sy;
				if (flipX) lx = -lx;
				if (flipY) ly = -ly;
				float rx = lx, ry = -ly;
				float v = mV[vi];
				if (flipV) v = 1f - v;
				Rlgl.TexCoord2f(mU[vi], v);
				Rlgl.Vertex2f(wx + rx * cos - ry * sin, -wy + rx * sin + ry * cos);
			}
		}
		Rlgl.End();
		Rlgl.SetTexture(0);
	}

	void RenderQuad() {
		if (texture == null || texRectW <= 0 || texRectH <= 0) return;
		if (!UnitySimScene.IsActiveInHierarchy(Object)) return;

		Transform.GetWorldPosition(out float wx, out float wy, out _);
		Transform.GetWorldScale(out float sx, out float sy);

		float w, h, left, bottom;
		if (SpriteDrawMode != 0) {
			w = Size.X * sx; h = Size.Y * sy;
			left = -pivotX * w; bottom = -pivotY * h;
		}
		else {
			w = texRectW * pixelSize * sx; h = texRectH * pixelSize * sy;
			left = (trimX * pixelSize - pivotX * unitW) * sx;
			bottom = (trimY * pixelSize - pivotY * unitH) * sy;
		}
		if (flipX) left = -left - w;
		if (flipY) bottom = -bottom - h;
		float offX = left, offY = -(bottom + h);


		float flippedY = atlasH - texRectY - texRectH;
		float u0 = texRectX / atlasW, v0 = flippedY / atlasH;
		float u1 = (texRectX + texRectW) / atlasW, v1 = (flippedY + texRectH) / atlasH;

		float halfTexelU = 0.5f / atlasW;
		float halfTexelV = 0.5f / atlasH;
		u0 += halfTexelU; u1 -= halfTexelU;
		v0 += halfTexelV; v1 -= halfTexelV;

		if (texture.HasPublicFlags(PublicTextureFlags.RequiresFlippedV)) {
			v0 = 1 - v0;
			v1 = 1 - v1;
		}

		if (flipX) (u0, u1) = (u1, u0);
		if (flipY) (v0, v1) = (v1, v0);

		float rotDeg = Transform.GetWorldRotationZ();
		uint texId = texture.GetTextureHandle();
		var fcolor = this.color * materialColor * this.Object.GetColor();

		if (outlined) {
			float distance = outlineWidth * pixelSize * MathF.Abs(sx);
			var ocolor = outlineColor;
			ocolor.W *= fcolor.W;
			for (int i = 0; i < 8; i++) {
				float angle = i * MathF.PI / 4;
				DrawQuad(wx + MathF.Cos(angle) * distance, wy + MathF.Sin(angle) * distance, offX, offY, w, h, u0, v0, u1, v1, rotDeg, texId, ocolor);
			}
		}

		DrawQuad(wx, wy, offX, offY, w, h, u0, v0, u1, v1, rotDeg, texId, fcolor);
	}

	static void DrawQuad(float wx, float wy, float offX, float offY, float w, float h, float u0, float v0, float u1, float v1, float rotDeg, uint texId, System.Numerics.Vector4 fcolor) {
		float screenX = wx + offX, screenY = -wy + offY;

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

public class SceneMeshMaterial
{
	public System.Numerics.Vector4 Color = new(1, 1, 1, 1);
	public bool Hidden;
	public ITexture? Texture;
	public System.Numerics.Vector2 TextureOffset;
	public System.Numerics.Vector2 TextureScale = new(1, 1);
	public bool Transparent;
}

public class SceneMeshRenderer : SceneComponent
{
	public SceneMeshRenderer(MeshRenderer renderer, MeshFilter? filter) {
		UnityMeshFilter = filter;
		UnityMeshRenderer = renderer;
	}

	public bool StaticBatched => UnityMeshRenderer.m_StaticBatchInfo?.subMeshCount > 0;
	public MeshFilter? UnityMeshFilter { get; }
	public MeshRenderer UnityMeshRenderer { get; }
}

public class SceneParticleSystem : SceneRenderer
{
	static readonly System.Numerics.Matrix4x4 UnityToWorld = System.Numerics.Matrix4x4.CreateScale(1, -1, 1);
	static int nextRandomSeed;

	ParticleCollection? particles;

	public SceneParticleSystem(ParticleSystemDefinition definition, ParticleSystemRenderer? renderer) {
		Definition = definition;
		UnityParticleSystemRenderer = renderer;
		if (renderer != null) {
			SortingOrder = renderer.m_SortingOrder;
			SortingLayerID = (int)renderer.m_SortingLayerID;
		}
	}

	public ParticleSystemDefinition Definition { get; }
	public ParticleSystemRenderer? UnityParticleSystemRenderer { get; }

	public override void Render(UnitySimScene scene) {
		if (particles != null && UnitySimScene.IsActiveInHierarchy(Object))
			particles.Render(UnityToWorld);
	}

	public void Simulate(float dt) {
		if (!UnitySimScene.IsActiveInHierarchy(Object)) {
			particles = null;
			return;
		}

		var transform = Transform.GetWorldMatrix();
		if (particles == null)
			particles = new ParticleCollection(Definition, transform, nextRandomSeed += 7919);
		else
			particles.SetControlPoint(0, transform);
		particles.Simulate(dt);
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

	public record struct ColorCurve(
		SceneObject Target,
		int ColorR, int ColorG, int ColorB, int ColorA
	);

	public record struct ActiveCurve(
		SceneObject Target,
		int IsActive
	);

	public record struct SpriteCurve(
		SceneSpriteRenderer Target,
		int Sprite
	);

	public DecodedClip Decoded;
	public List<TargetCurve> TransformTargets = [];
	public List<ColorCurve> ColorTargets = [];
	public List<ActiveCurve> ActiveTargets = [];
	public List<SpriteCurve> SpriteTargets = [];
	public float Duration;
	public bool Loop;
}

public class SceneAnimator : SceneComponent
{
	static readonly uint HashColorR = UnityCRC32.Hash("m_Color.r");
	static readonly uint HashColorG = UnityCRC32.Hash("m_Color.g");
	static readonly uint HashColorB = UnityCRC32.Hash("m_Color.b");
	static readonly uint HashColorA = UnityCRC32.Hash("m_Color.a");
	static readonly uint HashIsActive = UnityCRC32.Hash("m_IsActive");

	public Animator? UnityAnimator { get; set; }

	readonly List<RuntimeClip> runtimeClips = [];

	// State machine
	record struct AnimState(string Name, int ClipIndex, bool Loop, AnimTransition[] Transitions);
	record struct AnimTransition(int DestinationStateIndex, bool HasExitTime);

	readonly List<AnimState> states = [];
	readonly Dictionary<string, int> stateNameToIndex = [];
	int currentStateIndex = -1;
	int defaultStateIndex = -1;
	float time;
	public int ResolvedClipCount => runtimeClips.Count;
	public string? CurrentState => currentStateIndex >= 0 && currentStateIndex < states.Count ? states[currentStateIndex].Name : null;
	public float Time => time;

	public float GetStateLength(string stateName) {
		if (!stateNameToIndex.TryGetValue(stateName, out var index))
			return 0;
		int clip = states[index].ClipIndex;
		return clip >= 0 && clip < runtimeClips.Count ? runtimeClips[clip].Duration : 0;
	}

	public void ResetTime() { time = 0; }

	public void Play(string stateName) {
		if (stateNameToIndex.TryGetValue(stateName, out var idx)) {
			currentStateIndex = idx;
			time = 0;
		}
		else {
			Logs.Warn($"[SceneAnimator] State '{stateName}' not found on '{Object.Name}'. Available: {string.Join(", ", stateNameToIndex.Keys)}");
		}
	}

	public void Rebind() {
		time = 0;
		currentStateIndex = defaultStateIndex;
	}

	public void DumpClipInfo() {
		Logs.Info($"Object '{Object.Name}' clips={runtimeClips.Count}");
		foreach (var clip in runtimeClips) {
			Logs.Info($"  Clip: duration={clip.Duration:F3} loop={clip.Loop} start={clip.Decoded.StartTime:F3} stop={clip.Decoded.StopTime:F3}");
			Logs.Info($"  TransformTargets={clip.TransformTargets.Count} ColorTargets={clip.ColorTargets.Count} ActiveTargets={clip.ActiveTargets.Count}");

			for (int i = 0; i < clip.Decoded.Curves.Length; i++) {
				ref var curve = ref clip.Decoded.Curves[i];
				Logs.Info($"  Curve[{i}] typeID={curve.Binding.typeID} attr=0x{curve.Binding.attribute:X8} path=0x{curve.Binding.path:X8} keys={curve.Keys.Count}");
				if (curve.Keys.Count > 0 && curve.Keys.Count <= 10) {
					foreach (var key in curve.Keys)
						Logs.Info($"    t={key.time:F4} v={key.value:F4} inSlope={key.inSlope:F4} outSlope={key.outSlope:F4}");
				}
				else if (curve.Keys.Count > 10) {
					var first = curve.Keys[0];
					var last = curve.Keys[^1];
					Logs.Info($"    first : t={first.time:F4} v={first.value:F4}");
					Logs.Info($"    last  : t={last.time:F4} v={last.value:F4}");
				}
			}
		}
	}

	public override void Awake() {
		if (UnityAnimator == null) return;

		var controller = UnityAnimator.GetController();
		if (controller == null) return;

		var scene = (UnitySimScene)Object.Scene;
		List<AnimationClip> animClips = [];
		AnimatorController? ac = null;

		switch (controller) {
			case AnimatorController ac_:
				ac = ac_;
				foreach (var pptr in ac.m_AnimationClips)
					if (pptr.TryGet(out var clip)) animClips.Add(clip);
				break;
			case AnimatorOverrideController aoc:
				if (aoc.m_Controller.TryGet(out var baseCtrl) && baseCtrl is AnimatorController baseAc) {
					ac = baseAc;
					foreach (var pptr in baseAc.m_AnimationClips)
						if (pptr.TryGet(out var clip)) animClips.Add(clip);
				}
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
				Loop = decoded.LoopTime || clip.m_WrapMode == 2
			};

			var grouped = new Dictionary<uint, (int posX, int posY, int posZ, int rotX, int rotY, int rotZ, int rotW, int scX, int scY, int scZ)>();
			var colorGrouped = new Dictionary<uint, (int r, int g, int b, int a)>();
			var activeGrouped = new Dictionary<uint, int>();
			var spriteGrouped = new Dictionary<uint, int>();

			for (int i = 0; i < decoded.Curves.Length; i++) {
				ref var curve = ref decoded.Curves[i];
				if (curve.Keys.Count == 0) continue;

				uint pathHash = curve.Binding.path;
				uint attr = curve.Binding.attribute;

				if (curve.Binding.typeID == ClassIDType.Transform) {
					if (!grouped.TryGetValue(pathHash, out var g))
						g = (-1, -1, -1, -1, -1, -1, -1, -1, -1, -1);

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
				else if (curve.Binding.typeID == ClassIDType.SpriteRenderer && curve.Binding.isPPtrCurve == 1) {
					spriteGrouped[pathHash] = i;
				}
				else if (curve.Binding.typeID == ClassIDType.SpriteRenderer) {
					if (!colorGrouped.TryGetValue(pathHash, out var c))
						c = (-1, -1, -1, -1);

					if (attr == HashColorR) c.r = i;
					else if (attr == HashColorG) c.g = i;
					else if (attr == HashColorB) c.b = i;
					else if (attr == HashColorA) c.a = i;

					colorGrouped[pathHash] = c;
				}
				else if (curve.Binding.typeID == ClassIDType.GameObject) {
					if (attr == HashIsActive)
						activeGrouped[pathHash] = i;
				}
			}

			foreach (var (pathHash, g) in grouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				runtime.TransformTargets.Add(new(target, g.posX, g.posY, g.posZ,
					g.rotX, g.rotY, g.rotZ, g.rotW, g.scX, g.scY, g.scZ));
			}

			foreach (var (pathHash, c) in colorGrouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				runtime.ColorTargets.Add(new(target.Object, c.r, c.g, c.b, c.a));
			}

			foreach (var (pathHash, idx) in activeGrouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				runtime.ActiveTargets.Add(new(target.Object, idx));
			}

			foreach (var (pathHash, idx) in spriteGrouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				var spriteRenderer = target.Object.GetComponent<SceneSpriteRenderer>();
				if (spriteRenderer != null)
					runtime.SpriteTargets.Add(new(spriteRenderer, idx));
			}

			if (runtime.TransformTargets.Count > 0 || runtime.ColorTargets.Count > 0 || runtime.ActiveTargets.Count > 0 || runtime.SpriteTargets.Count > 0)
				runtimeClips.Add(runtime);
		}

		// Build state machine from the AnimatorController
		if (ac?.m_ControllerConstant != null) {
			var tosLookup = new Dictionary<uint, string>();
			foreach (var kv in ac.m_TOS) tosLookup[kv.Key] = kv.Value;

			foreach (var sm in ac.m_ControllerConstant.m_StateMachineArray) {
				for (int si = 0; si < sm.m_StateConstantArray.Length; si++) {
					var state = sm.m_StateConstantArray[si];
					if (!tosLookup.TryGetValue(state.m_NameID, out var stateName)) continue;

					int clipIndex = -1;
					if (state.m_BlendTreeConstantArray.Length > 0) {
						var bt = state.m_BlendTreeConstantArray[0];
						if (bt.m_NodeArray.Length > 0) {
							uint clipId = bt.m_NodeArray[0].m_ClipID;
							if (clipId < runtimeClips.Count)
								clipIndex = (int)clipId;
						}
					}

					// Build transitions for this state
					var transitions = new AnimTransition[state.m_TransitionConstantArray.Length];
					for (int ti = 0; ti < state.m_TransitionConstantArray.Length; ti++) {
						var tc = state.m_TransitionConstantArray[ti];
						transitions[ti] = new AnimTransition((int)tc.m_DestinationState, tc.m_HasExitTime);
					}

					int stateIdx = states.Count;
					states.Add(new AnimState(stateName, clipIndex, state.m_Loop, transitions));
					stateNameToIndex[stateName] = stateIdx;

					if (si == sm.m_DefaultState)
						defaultStateIndex = stateIdx;
				}
			}
		}

		currentStateIndex = defaultStateIndex;

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

		// Determine which clip to play based on current state
		int clipIndex;
		bool loop;
		if (currentStateIndex >= 0 && currentStateIndex < states.Count) {
			var state = states[currentStateIndex];
			clipIndex = state.ClipIndex;
			loop = state.Loop;

			// Check for exit-time transitions: if the clip has finished and there's a HasExitTime transition, take it
			if (clipIndex >= 0 && clipIndex < runtimeClips.Count) {
				var clip = runtimeClips[clipIndex];
				if (clip.Duration > 0 && time >= clip.Decoded.StopTime) {
					foreach (var transition in state.Transitions) {
						if (transition.HasExitTime && transition.DestinationStateIndex >= 0 && transition.DestinationStateIndex < states.Count) {
							currentStateIndex = transition.DestinationStateIndex;
							time = runtimeClips[states[currentStateIndex].ClipIndex].Decoded.StartTime;
							// Re-read from new state
							clipIndex = states[currentStateIndex].ClipIndex;
							loop = states[currentStateIndex].Loop;
							break;
						}
					}
				}
			}
		}
		else {
			// Legacy: no state machine, play all clips
			clipIndex = -1;
			loop = false;
		}

		for (int ci = 0; ci < runtimeClips.Count; ci++) {
			if (clipIndex >= 0 && ci != clipIndex) continue;

			var clip = runtimeClips[ci];
			float t;
			bool shouldLoop = clipIndex >= 0 ? loop : clip.Loop;
			if (shouldLoop && clip.Duration > 0) {
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

			foreach (var target in clip.ColorTargets) {
				var obj = target.Target;
				if (target.ColorR >= 0) obj.Color.X = DecodedClip.Eval(clip.Decoded.Curves[target.ColorR].Keys, t);
				if (target.ColorG >= 0) obj.Color.Y = DecodedClip.Eval(clip.Decoded.Curves[target.ColorG].Keys, t);
				if (target.ColorB >= 0) obj.Color.Z = DecodedClip.Eval(clip.Decoded.Curves[target.ColorB].Keys, t);
				if (target.ColorA >= 0) obj.Color.W = DecodedClip.Eval(clip.Decoded.Curves[target.ColorA].Keys, t);
			}

			foreach (var target in clip.ActiveTargets) {
				var obj = target.Target;
				if (target.IsActive >= 0) obj.Active = DecodedClip.Eval(clip.Decoded.Curves[target.IsActive].Keys, t) > 0.5f;
			}

			foreach (var target in clip.SpriteTargets) {
				int index = (int)DecodedClip.EvalStepped(clip.Decoded.Curves[target.Sprite].Keys, t);
				if (index >= 0 && index < clip.Decoded.PPtrMapping.Count && clip.Decoded.PPtrMapping[index].TryGet(out var obj) && obj is Sprite sprite)
					target.Target.SetSprite(sprite, Object.Scene);
			}
		}
	}
}
public class SceneObject
{
	public string Name = "";
	public bool Active = true;
	public SceneTransform Transform { get; } = new();
	public UnitySimScene Scene { get; internal set; } = null!;
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

	public static SceneObject Instantiate(SceneObject original) {
		return DeepClone(original, null, false, null, null);
	}

	public static SceneObject Instantiate(SceneObject original, SceneTransform? parent) {
		return DeepClone(original, parent, false, null, null);
	}

	public static SceneObject Instantiate(SceneObject original, SceneTransform? parent, bool instantiateInWorldSpace) {
		return DeepClone(original, parent, instantiateInWorldSpace, null, null);
	}

	public static SceneObject Instantiate(SceneObject original, Vector3 position, Quaternion rotation) {
		return DeepClone(original, null, false, position, rotation);
	}

	public static SceneObject Instantiate(SceneObject original, Vector3 position, Quaternion rotation, SceneTransform? parent) {
		return DeepClone(original, parent, false, position, rotation);
	}

	private static SceneObject DeepClone(SceneObject original, SceneTransform? parent, bool instantiateInWorldSpace, Vector3? position, Quaternion? rotation) {
		var clone = CloneHierarchy(original);

		if (parent != null) {
			if (instantiateInWorldSpace)
				clone.Transform.SetParent(parent);
			else
				clone.Transform.SetParent(parent);
		}

		if (position.HasValue) {
			clone.Transform.LocalX = position.Value.X;
			clone.Transform.LocalY = position.Value.Y;
			clone.Transform.LocalZ = position.Value.Z;
		}
		if (rotation.HasValue) {
			clone.Transform.LocalRotationX = rotation.Value.X;
			clone.Transform.LocalRotationY = rotation.Value.Y;
			clone.Transform.LocalRotationZ = rotation.Value.Z;
			clone.Transform.LocalRotationW = rotation.Value.W;
		}

		if (original.Scene != null)
			PropagateScene(clone, original.Scene);

		return clone;
	}

	private static SceneObject CloneHierarchy(SceneObject original) {
		var clone = new SceneObject {
			Name = original.Name,
			Active = original.Active,
			Color = original.Color,
		};

		var srcT = original.Transform;
		var dstT = clone.Transform;
		dstT.LocalX = srcT.LocalX;
		dstT.LocalY = srcT.LocalY;
		dstT.LocalZ = srcT.LocalZ;
		dstT.LocalScaleX = srcT.LocalScaleX;
		dstT.LocalScaleY = srcT.LocalScaleY;
		dstT.LocalScaleZ = srcT.LocalScaleZ;
		dstT.LocalRotationX = srcT.LocalRotationX;
		dstT.LocalRotationY = srcT.LocalRotationY;
		dstT.LocalRotationZ = srcT.LocalRotationZ;
		dstT.LocalRotationW = srcT.LocalRotationW;

		for (int i = 1; i < original.Components.Count; i++) {
			var comp = original.Components[i];
			switch (comp) {
				case SceneSpriteRenderer sr:
					clone.AddComponent(new SceneSpriteRenderer(sr.UnitySpriteRenderer));
					break;
				case SceneAnimator anim:
					clone.AddComponent(new SceneAnimator { UnityAnimator = anim.UnityAnimator });
					break;
				case SceneMeshRenderer mr:
					clone.AddComponent(new SceneMeshRenderer(mr.UnityMeshRenderer, mr.UnityMeshFilter));
					break;
				case SceneParticleSystem ps:
					clone.AddComponent(new SceneParticleSystem(ps.Definition, ps.UnityParticleSystemRenderer));
					break;
			}
		}

		foreach (var childTransform in srcT.Children) {
			var childClone = CloneHierarchy(childTransform.Object);
			childClone.Transform.SetParent(dstT);
		}

		return clone;
	}

	private static void PropagateScene(SceneObject obj, UnitySimScene scene) {
		obj.Scene = scene;
		scene.RegisterObject(obj);
		obj.Awake();
		foreach (var child in obj.Transform.Children)
			PropagateScene(child.Object, scene);
	}

	public System.Numerics.Vector4 GetColor() {
		var c = new System.Numerics.Vector4(1, 1, 1, 1);
		var p = this;
		while (p != null) { c *= p.Color; p = p.Transform.Parent?.Object; }
		return c;
	}

	public static void Destroy(SceneObject obj) {
		if (obj.Scene is UnitySimScene scene)
			scene.DestroyObject(obj);
	}
}

sealed class SceneMeshBatch
{
	public System.Numerics.Vector3 Center;
	public SceneMeshMaterial Material = null!;
	public Raylib_cs.Mesh Mesh;
}

sealed class SceneMeshBatchBuilder(SceneMeshMaterial material)
{
	readonly List<byte> colors = [];
	readonly List<ushort> indices = [];
	readonly List<float> positions = [];
	readonly List<float> uvs = [];
	System.Numerics.Vector3 total;

	public void Add(AssetStudio.Mesh mesh, int firstIndex, SubMesh subMesh, System.Numerics.Matrix4x4 matrix, List<SceneMeshBatch> batches) {
		Dictionary<uint, ushort> remap = [];
		bool flipV = material.Texture?.HasPublicFlags(PublicTextureFlags.RequiresFlippedV) ?? false;
		for (int i = 0; i + 2 < subMesh.indexCount; i += 3) {
			if (positions.Count / 3 + 3 > ushort.MaxValue) {
				Flush(batches);
				remap.Clear();
			}

			for (int corner = 0; corner < 3; corner++) {
				uint source = mesh.m_Indices[firstIndex + i + corner] + subMesh.baseVertex;
				if (!remap.TryGetValue(source, out var index)) {
					index = AddVertex(mesh, (int)source, matrix, flipV);
					remap[source] = index;
				}
				indices.Add(index);
			}
		}
	}

	public void Flush(List<SceneMeshBatch> batches) {
		int vertexCount = positions.Count / 3;
		if (vertexCount == 0 || indices.Count == 0) {
			Clear();
			return;
		}

		var mesh = new Raylib_cs.Mesh(vertexCount, indices.Count / 3);
		mesh.AllocVertices();
		mesh.AllocTexCoords();
		mesh.AllocColors();
		mesh.AllocIndices();
		System.Runtime.InteropServices.CollectionsMarshal.AsSpan(positions).CopyTo(mesh.VerticesAs<float>());
		System.Runtime.InteropServices.CollectionsMarshal.AsSpan(uvs).CopyTo(mesh.TexCoordsAs<float>());
		System.Runtime.InteropServices.CollectionsMarshal.AsSpan(colors).CopyTo(mesh.ColorsAs<byte>());
		System.Runtime.InteropServices.CollectionsMarshal.AsSpan(indices).CopyTo(mesh.IndicesAs<ushort>());
		Raylib.UploadMesh(ref mesh, false);

		batches.Add(new SceneMeshBatch { Center = total / vertexCount, Material = material, Mesh = mesh });
		Clear();
	}

	ushort AddVertex(AssetStudio.Mesh mesh, int source, System.Numerics.Matrix4x4 matrix, bool flipV) {
		int stride = mesh.m_Vertices.Length / mesh.m_VertexCount;
		var position = System.Numerics.Vector3.Transform(new(mesh.m_Vertices[source * stride], mesh.m_Vertices[source * stride + 1], mesh.m_Vertices[source * stride + 2]), matrix);
		position.Z = -position.Z;
		positions.Add(position.X);
		positions.Add(position.Y);
		positions.Add(position.Z);
		total += position;

		float u = 0, v = 0;
		if (mesh.m_UV0 != null && mesh.m_UV0.Length >= (source + 1) * 2) {
			u = mesh.m_UV0[source * 2] * material.TextureScale.X + material.TextureOffset.X;
			v = mesh.m_UV0[source * 2 + 1] * material.TextureScale.Y + material.TextureOffset.Y;
		}
		uvs.Add(u);
		uvs.Add(flipV ? v : 1 - v);

		var color = new System.Numerics.Vector4(1, 1, 1, 1);
		if (mesh.m_Colors != null && mesh.m_Colors.Length >= (source + 1) * 4)
			color = new(mesh.m_Colors[source * 4], mesh.m_Colors[source * 4 + 1], mesh.m_Colors[source * 4 + 2], 1);
		color *= material.Color;
		colors.Add((byte)(Math.Clamp(color.X, 0, 1) * 255));
		colors.Add((byte)(Math.Clamp(color.Y, 0, 1) * 255));
		colors.Add((byte)(Math.Clamp(color.Z, 0, 1) * 255));
		colors.Add((byte)(Math.Clamp(color.W, 0, 1) * 255));

		return (ushort)(positions.Count / 3 - 1);
	}

	void Clear() {
		colors.Clear();
		indices.Clear();
		positions.Clear();
		uvs.Clear();
		total = default;
	}
}

public abstract class UnitySimScene
{
	readonly List<SceneMeshBatch> meshBatches = [];
	readonly Dictionary<long, SceneMeshMaterial> meshMaterials = [];
	Raylib_cs.Material? meshDrawMaterial;
	Raylib_cs.Texture2D meshWhiteTexture;
	readonly Dictionary<long, int[]> subMeshStarts = [];

	protected readonly List<SceneObject> allObjects = [];
	protected readonly List<SceneRenderer> sortedRenderers = [];
	protected readonly List<SceneAnimator> animators = [];
	protected readonly List<SceneParticleSystem> particleSystems = [];
	protected SceneObject? root;
	protected readonly Dictionary<long, ITexture> textureCache = [];
	public bool RenderSpriteMeshes { get; protected set; }
	protected readonly Dictionary<long, SceneObject> pathIdToObject = [];


	protected void BuildMeshes(params SceneObject?[] roots) {
		Dictionary<SceneMeshMaterial, SceneMeshBatchBuilder> builders = [];
		foreach (var root in roots)
			if (root != null)
				AddMeshes(root, builders);
		foreach (var builder in builders.Values)
			builder.Flush(meshBatches);
	}

	protected virtual SceneMeshMaterial CreateMeshMaterial(AssetStudio.Material material) {
		var result = new SceneMeshMaterial();
		var properties = material.m_SavedProperties;
		foreach (var (key, color) in properties.m_Colors)
			if (key == "_Color")
				result.Color = new(color.R, color.G, color.B, color.A);
		foreach (var (key, value) in properties.m_Floats)
			if (key == "_Mode" && value >= 2)
				result.Transparent = true;
		foreach (var (key, environment) in properties.m_TexEnvs) {
			if (key != "_MainTex" || !environment.m_Texture.TryGet(out var texture) || texture is not AssetStudio.Texture2D texture2D)
				continue;

			result.Texture = LoadTexture(texture2D);
			result.Texture.SetWrap(TextureWrap.Repeat);
			result.TextureOffset = new(environment.m_Offset.X, environment.m_Offset.Y);
			result.TextureScale = new(environment.m_Scale.X, environment.m_Scale.Y);
		}
		return result;
	}

	protected unsafe void RenderMeshes(System.Numerics.Vector3 cameraPosition, System.Numerics.Quaternion cameraRotation, float fieldOfView, float aspect, float near, float far) {
		if (meshBatches.Count == 0)
			return;

		if (meshDrawMaterial == null) {
			var material = Raylib.LoadMaterialDefault();
			meshWhiteTexture = material.Maps[0].Texture;
			meshDrawMaterial = material;
		}
		var drawMaterial = meshDrawMaterial.Value;

		var forward = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, cameraRotation);
		var up = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, cameraRotation);
		var eye = cameraPosition with { Z = -cameraPosition.Z };
		var target = eye + forward with { Z = -forward.Z };
		up.Z = -up.Z;

		Rlgl.DrawRenderBatchActive();
		var modelview = Rlgl.GetMatrixModelview();
		Rlgl.MatrixMode(MatrixMode.Projection);
		Rlgl.PushMatrix();
		Rlgl.LoadIdentity();
		double top = near * Math.Tan(fieldOfView * 0.5 * Math.PI / 180);
		Rlgl.Frustum(-top * aspect, top * aspect, -top, top, near, far);
		Rlgl.MatrixMode(MatrixMode.ModelView);
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(Raymath.MatrixLookAt(eye, target, up)));
		Rlgl.EnableDepthTest();
		Rlgl.EnableDepthMask();
		Rlgl.DisableBackfaceCulling();

		foreach (var batch in meshBatches)
			if (!batch.Material.Transparent)
				DrawMeshBatch(batch, drawMaterial);

		Rlgl.DisableDepthMask();
		foreach (var batch in meshBatches.Where(batch => batch.Material.Transparent).OrderByDescending(batch => System.Numerics.Vector3.DistanceSquared(batch.Center, eye)))
			DrawMeshBatch(batch, drawMaterial);

		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableDepthMask();
		Rlgl.DisableDepthTest();
		Rlgl.MatrixMode(MatrixMode.Projection);
		Rlgl.PopMatrix();
		Rlgl.MatrixMode(MatrixMode.ModelView);
		Rlgl.SetMatrixModelView(modelview);
	}

	protected unsafe void UnloadMeshes() {
		foreach (var batch in meshBatches) {
			var mesh = batch.Mesh;
			Raylib.UnloadMesh(ref mesh);
		}
		meshBatches.Clear();

		if (meshDrawMaterial is Raylib_cs.Material material) {
			material.Maps[0].Texture = meshWhiteTexture;
			Raylib.UnloadMaterial(material);
			meshDrawMaterial = null;
		}
	}

	void AddMeshes(SceneObject obj, Dictionary<SceneMeshMaterial, SceneMeshBatchBuilder> builders) {
		if (!obj.Active)
			return;

		foreach (var renderer in obj.GetComponents<SceneMeshRenderer>())
			AddMesh(renderer, builders);
		foreach (var child in obj.Transform.Children)
			AddMeshes(child.Object, builders);
	}

	void AddMesh(SceneMeshRenderer renderer, Dictionary<SceneMeshMaterial, SceneMeshBatchBuilder> builders) {
		var unityRenderer = renderer.UnityMeshRenderer;
		if (!unityRenderer.m_Enabled || renderer.UnityMeshFilter == null || !renderer.UnityMeshFilter.m_Mesh.TryGet(out var mesh))
			return;

		if (!subMeshStarts.TryGetValue(mesh.m_PathID, out var starts)) {
			mesh.ProcessData();
			starts = new int[mesh.m_SubMeshes.Count];
			for (int i = 1; i < starts.Length; i++)
				starts[i] = starts[i - 1] + (int)mesh.m_SubMeshes[i - 1].indexCount;
			subMeshStarts[mesh.m_PathID] = starts;
		}
		if (mesh.m_VertexCount == 0)
			return;

		bool batched = renderer.StaticBatched;
		var matrix = batched ? System.Numerics.Matrix4x4.Identity : renderer.Transform.GetWorldMatrix();
		int firstSubMesh = batched ? unityRenderer.m_StaticBatchInfo.firstSubMesh : 0;
		for (int i = 0; i < unityRenderer.m_Materials.Count; i++) {
			int subMesh = firstSubMesh + i;
			if (subMesh >= mesh.m_SubMeshes.Count)
				break;
			if (!unityRenderer.m_Materials[i].TryGet(out var unityMaterial))
				continue;

			if (!meshMaterials.TryGetValue(unityMaterial.m_PathID, out var material))
				meshMaterials[unityMaterial.m_PathID] = material = CreateMeshMaterial(unityMaterial);
			if (material.Hidden)
				continue;

			if (!builders.TryGetValue(material, out var builder))
				builders[material] = builder = new(material);
			builder.Add(mesh, starts[subMesh], mesh.m_SubMeshes[subMesh], matrix, meshBatches);
		}
	}

	unsafe void DrawMeshBatch(SceneMeshBatch batch, Raylib_cs.Material drawMaterial) {
		var texture = batch.Material.Texture;
		drawMaterial.Maps[0].Texture = texture == null ? meshWhiteTexture : new Raylib_cs.Texture2D {
			Id = texture.GetTextureHandle(),
			Width = texture.GetWidth(),
			Height = texture.GetHeight(),
			Mipmaps = 1,
			Format = meshWhiteTexture.Format
		};
		drawMaterial.Maps[0].Color = new Nucleus.Common.Types.Color(255, 255, 255, 255);
		Raylib.DrawMesh(batch.Mesh, drawMaterial, System.Numerics.Matrix4x4.Identity);
	}

	protected void RunThinkFuncs(double dt) {
		foreach (var anim in animators) anim.Evaluate((float)dt);
		foreach (var system in particleSystems) system.Simulate((float)dt);
	}

	internal void RegisterObject(SceneObject obj) {
		allObjects.Add(obj);
		foreach (var anim in obj.GetComponents<SceneAnimator>())
			animators.Add(anim);
		foreach (var system in obj.GetComponents<SceneParticleSystem>())
			particleSystems.Add(system);
	}

	internal void DestroyObject(SceneObject obj) {
		// Recursively destroy children first
		for (int i = obj.Transform.Children.Count - 1; i >= 0; i--)
			DestroyObject(obj.Transform.Children[i].Object);

		allObjects.Remove(obj);
		foreach (var anim in obj.GetComponents<SceneAnimator>())
			animators.Remove(anim);
		foreach (var system in obj.GetComponents<SceneParticleSystem>())
			particleSystems.Remove(system);
		obj.Transform.SetParent(null);
		obj.Active = false;
	}

	protected virtual bool LoadMissingFile(string fileName) => false;

	protected internal ITexture LoadTexture(AssetStudio.Texture2D? texture2D) {
		if (textureCache.TryGetValue(texture2D!.m_PathID, out var tex)) return tex;
		textureCache[texture2D.m_PathID] = tex = UnityAssetUtils.ConvertTexture(EngineCore.Level, texture2D!);
		return tex;
	}


	protected SceneObject? ImportGameObject(GameObject? unityGO, SceneTransform? parent) {
		if (unityGO == null)
			return null;

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
				case MeshRenderer mr: obj.AddComponent(new SceneMeshRenderer(mr, unityGO.GetFirstComponent<MeshFilter>())); break;
				case ParticleSystem: particleSystems.Add(obj.AddComponent(new SceneParticleSystem(UnityParticleConverter.ConvertSystem(unityGO, LoadTexture, LoadMissingFile), unityGO.GetFirstComponent<ParticleSystemRenderer>()))); break;
			}
		}

		if (unityTransform != null)
			foreach (var childTransform in unityTransform.GetChildren()) {
				var childGO = childTransform.GetGameObject();
				if (childGO != null) ImportGameObject(childGO, obj.Transform);
			}
		return obj;
	}

	protected void BuildRenderOrder() {
		sortedRenderers.Clear();
		foreach (var obj in allObjects) {
			if (!IsActiveInHierarchy(obj)) continue;
			foreach (var renderer in obj.GetComponents<SceneRenderer>()) sortedRenderers.Add(renderer);
		}
		sortedRenderers.Sort(static (a, b) => {
			int cmp = a.SortingLayerID.CompareTo(b.SortingLayerID);
			if (cmp != 0) return cmp;
			cmp = a.SortingOrder.CompareTo(b.SortingOrder);
			if (cmp != 0) return cmp;
			a.Transform.GetWorldPosition(out _, out _, out float az);
			b.Transform.GetWorldPosition(out _, out _, out float bz);
			return bz.CompareTo(az);
		});
	}

	public static bool IsActiveInHierarchy(SceneObject obj) {
		var current = obj;
		while (current != null) {
			if (!current.Active) return false;
			current = current.Transform.Parent?.Object;
		}
		return true;
	}

}