using Newtonsoft.Json;

namespace Nucleus.Models.Runtime;

public struct BoneApplied
{
	public float X, Y;
	public float Rotation;
	public float ScaleX, ScaleY;
	public float ShearX, ShearY;
	public bool Valid;
}

public interface IConstraintBone
{
	public IConstraintBone? ParentBone { get; }
	public float BoneLength { get; }
	public TransformMode BoneTransformMode { get; }
	public ref BoneApplied Applied { get; }
	public ref Transformation World { get; }
	public void UpdateWorldTransform(float x, float y, float rotation, float scaleX, float scaleY, float shearX, float shearY);
	public void UpdateWorldTransform();
}

public interface IPathSource
{
	public bool Closed { get; }
	public bool ConstantSpeed { get; }
	public float[] Lengths { get; }
	public int WorldVerticesLength { get; }
	public void ComputeWorldVertices(int start, int count, float[] world, int offset);
}

public interface IModelUpdatable
{
	public void Update();
}

public enum PositionMode
{
	Fixed,
	Percent
}
public enum SpacingMode
{
	Length,
	Fixed,
	Percent
}
public enum RotateMode
{
	Tangent,
	Chain,
	ChainScale
}

public abstract class ConstraintData : IModel4Nameable
{
	public string Name { get; set; } = "";
	/// <summary>
	/// Run order, shared across every constraint type
	/// </summary>
	public int Order { get; set; }
	/// <summary>
	/// Only active when the skin lists it
	/// </summary>
	public bool SkinRequired { get; set; }
	public List<BoneData> Bones { get; set; } = [];
}

public class IkConstraintData : ConstraintData
{
	public BoneData Target { get; set; } = null!;
	public int BendDirection { get; set; } = 1;
	public bool Compress { get; set; }
	public bool Stretch { get; set; }
	public bool Uniform { get; set; }
	public float Mix { get; set; } = 1;
	public float Softness { get; set; }
}

public class TransformConstraintData : ConstraintData
{
	public BoneData Target { get; set; } = null!;
	public bool Local { get; set; }
	public bool Relative { get; set; }
	public float OffsetRotation { get; set; }
	public float OffsetX { get; set; }
	public float OffsetY { get; set; }
	public float OffsetScaleX { get; set; }
	public float OffsetScaleY { get; set; }
	public float OffsetShearY { get; set; }
	public float RotateMix { get; set; } = 1;
	public float TranslateMix { get; set; } = 1;
	public float ScaleMix { get; set; } = 1;
	public float ShearMix { get; set; } = 1;
}

public class PathConstraintData : ConstraintData
{
	public SlotData Target { get; set; } = null!;
	public PositionMode PositionMode { get; set; }
	public SpacingMode SpacingMode { get; set; }
	public RotateMode RotateMode { get; set; }
	public float OffsetRotation { get; set; }
	public float Position { get; set; }
	public float Spacing { get; set; }
	public float RotateMix { get; set; } = 1;
	public float TranslateMix { get; set; } = 1;
}

public class EventData : IModel4Nameable
{
	public string Name { get; set; } = "";
	public int Int { get; set; }
	public float Float { get; set; }
	public string? String { get; set; }
	public string? AudioPath { get; set; }
	public float Volume { get; set; } = 1;
	public float Balance { get; set; }
}

public class ModelEvent
{
	public double Time;
	public EventData Data = null!;
	public int Int;
	public float Float;
	public string? String;
	public float Volume = 1;
	public float Balance;

	public override string ToString() => $"{Data?.Name} @ {Time}";
}

public class PathAttachment : VertexAttachment
{
	public bool Closed;
	public bool ConstantSpeed = true;
	public float[] Lengths = [];
	public Common.Types.Color Color = new(255, 128, 0, 255);
}

public class IkConstraintInstance : IModelUpdatable, IModel4Nameable
{
	public IkConstraintData Data = null!;
	public List<BoneInstance> Bones = [];
	public BoneInstance Target = null!;
	public float Mix;
	public float Softness;
	public int BendDirection;
	public bool Compress;
	public bool Stretch;
	public bool Active;

	public string Name => Data.Name;

	public void SetToSetupPose() {
		Mix = Data.Mix;
		Softness = Data.Softness;
		BendDirection = Data.BendDirection;
		Compress = Data.Compress;
		Stretch = Data.Stretch;
	}

	public void Update() {
		var world = Target.WorldTransform;
		switch (Bones.Count) {
			case 1:
				ConstraintMath.IkApply(Bones[0], world.X, world.Y, Compress, Stretch, Data.Uniform, Mix);
				break;
			case 2:
				ConstraintMath.IkApply(Bones[0], Bones[1], world.X, world.Y, BendDirection, Stretch, Softness, Mix);
				break;
		}
	}
}

public class TransformConstraintInstance : IModelUpdatable, IModel4Nameable
{
	public TransformConstraintData Data = null!;
	public List<BoneInstance> Bones = [];
	public BoneInstance Target = null!;
	public float RotateMix;
	public float TranslateMix;
	public float ScaleMix;
	public float ShearMix;
	public bool Active;

	public string Name => Data.Name;

	public void SetToSetupPose() {
		RotateMix = Data.RotateMix;
		TranslateMix = Data.TranslateMix;
		ScaleMix = Data.ScaleMix;
		ShearMix = Data.ShearMix;
	}

	public void Update() =>
		ConstraintMath.TransformApply(Target, Bones, Data, RotateMix, TranslateMix, ScaleMix, ShearMix);
}

public class PathConstraintInstance : IModelUpdatable, IModel4Nameable, IPathSource
{
	public PathConstraintData Data = null!;
	public List<BoneInstance> Bones = [];
	public SlotInstance Target = null!;
	public float Position;
	public float Spacing;
	public float RotateMix;
	public float TranslateMix;
	public bool Active;
	public readonly PathScratch Scratch = new();

	public string Name => Data.Name;

	PathAttachment? path;

	public void SetToSetupPose() {
		Position = Data.Position;
		Spacing = Data.Spacing;
		RotateMix = Data.RotateMix;
		TranslateMix = Data.TranslateMix;
	}

	public void Update() {
		path = Target.Attachment as PathAttachment;
		if (path == null) return;
		ConstraintMath.PathApply(this, Target.Bone, Bones, Data, Position, Spacing, RotateMix, TranslateMix, Scratch);
	}

	bool IPathSource.Closed => path!.Closed;
	bool IPathSource.ConstantSpeed => path!.ConstantSpeed;
	float[] IPathSource.Lengths => path!.Lengths;
	int IPathSource.WorldVerticesLength => path!.GetVertices().Length * 2;
	void IPathSource.ComputeWorldVertices(int start, int count, float[] world, int offset) =>
		path!.ComputeWorldFloats(Target, start, count, world, offset);
}

public class PathScratch
{
	public float[] Spaces = [];
	public float[] Positions = [];
	public float[] World = [];
	public float[] Curves = [];
	public float[] Lengths = [];
	public readonly float[] Segments = new float[10];

	public static float[] Fit(ref float[] array, int size) {
		if (array.Length < size)
			array = new float[size];
		return array;
	}
}

public static class ConstraintMath
{
	public const float PI = MathF.PI;
	public const float PI2 = MathF.PI * 2;
	public const float RAD_DEG = 180f / MathF.PI;
	public const float DEG_RAD = MathF.PI / 180f;
	private const float EPSILON = 0.00001f;

	private static float WrapPI(float radians) {
		if (radians > PI)
			radians -= PI2;
		else if (radians < -PI)
			radians += PI2;
		return radians;
	}

	private static float Wrap180(float degrees) {
		if (degrees > 180)
			degrees -= 360;
		else if (degrees < -180)
			degrees += 360;
		return degrees;
	}

	public static void UpdateAppliedTransform(IConstraintBone bone) {
		ref var applied = ref bone.Applied;
		applied.Valid = true;
		var world = bone.World;
		float a = world.A, b = world.B, c = world.C, d = world.D;
		var parent = bone.ParentBone;
		if (parent == null) {
			applied.X = world.X;
			applied.Y = world.Y;
			applied.Rotation = MathF.Atan2(c, a) * RAD_DEG;
			applied.ScaleX = MathF.Sqrt(a * a + c * c);
			applied.ScaleY = MathF.Sqrt(b * b + d * d);
			applied.ShearX = 0;
			applied.ShearY = MathF.Atan2(a * b + c * d, a * d - b * c) * RAD_DEG;
			return;
		}

		var parentWorld = parent.World;
		float pa = parentWorld.A, pb = parentWorld.B, pc = parentWorld.C, pd = parentWorld.D;
		float pid = 1 / (pa * pd - pb * pc);
		float dx = world.X - parentWorld.X, dy = world.Y - parentWorld.Y;
		applied.X = dx * pd * pid - dy * pb * pid;
		applied.Y = dy * pa * pid - dx * pc * pid;

		float ia = pid * pd, id = pid * pa, ib = pid * pb, ic = pid * pc;
		float ra = ia * a - ib * c, rb = ia * b - ib * d;
		float rc = id * c - ic * a, rd = id * d - ic * b;
		applied.ShearX = 0;
		applied.ScaleX = MathF.Sqrt(ra * ra + rc * rc);
		if (applied.ScaleX > 0.0001f) {
			float det = ra * rd - rb * rc;
			applied.ScaleY = det / applied.ScaleX;
			applied.ShearY = MathF.Atan2(ra * rb + rc * rd, det) * RAD_DEG;
			applied.Rotation = MathF.Atan2(rc, ra) * RAD_DEG;
		}
		else {
			applied.ScaleX = 0;
			applied.ScaleY = MathF.Sqrt(rb * rb + rd * rd);
			applied.ShearY = 0;
			applied.Rotation = 90 - MathF.Atan2(rd, rb) * RAD_DEG;
		}
	}

	private static void Validate(IConstraintBone bone) {
		if (!bone.Applied.Valid)
			UpdateAppliedTransform(bone);
	}

	public static void IkApply(IConstraintBone bone, float targetX, float targetY, bool compress, bool stretch, bool uniform, float alpha) {
		Validate(bone);
		ref var applied = ref bone.Applied;
		var parent = bone.ParentBone;
		if (parent == null) return;
		var parentWorld = parent.World;
		float pa = parentWorld.A, pb = parentWorld.B, pc = parentWorld.C, pd = parentWorld.D;
		float rotationIK = -applied.ShearX - applied.Rotation;
		float tx, ty;
		var world = bone.World;

		switch (bone.BoneTransformMode) {
			case TransformMode.OnlyTranslation:
				tx = targetX - world.X;
				ty = targetY - world.Y;
				break;
			case TransformMode.NoRotationOrReflection: {
					float s = MathF.Abs(pa * pd - pb * pc) / (pa * pa + pc * pc);
					float sa = pa, sc = pc;
					pb = -sc * s;
					pd = sa * s;
					rotationIK += MathF.Atan2(sc, sa) * RAD_DEG;
					goto default;
				}
			default: {
					float x = targetX - parentWorld.X, y = targetY - parentWorld.Y;
					float d = pa * pd - pb * pc;
					tx = (x * pd - y * pb) / d - applied.X;
					ty = (y * pa - x * pc) / d - applied.Y;
					break;
				}
		}

		rotationIK += MathF.Atan2(ty, tx) * RAD_DEG;
		if (applied.ScaleX < 0)
			rotationIK += 180;
		rotationIK = Wrap180(rotationIK);

		float sx = applied.ScaleX, sy = applied.ScaleY;
		if (compress || stretch) {
			switch (bone.BoneTransformMode) {
				case TransformMode.NoScale:
				case TransformMode.NoScaleOrReflection:
					tx = targetX - world.X;
					ty = targetY - world.Y;
					break;
			}
			float b = bone.BoneLength * sx;
			float dd = MathF.Sqrt(tx * tx + ty * ty);
			if (((compress && dd < b) || (stretch && dd > b)) && b > 0.0001f) {
				float s = (dd / b - 1) * alpha + 1;
				sx *= s;
				if (uniform)
					sy *= s;
			}
		}
		bone.UpdateWorldTransform(applied.X, applied.Y, applied.Rotation + rotationIK * alpha, sx, sy, applied.ShearX, applied.ShearY);
	}

	public static void IkApply(IConstraintBone parent, IConstraintBone child, float targetX, float targetY, int bendDir, bool stretch, float softness, float alpha) {
		if (alpha == 0) {
			child.UpdateWorldTransform();
			return;
		}

		Validate(parent);
		Validate(child);

		var parentApplied = parent.Applied;
		var childApplied = child.Applied;
		var grandparent = parent.ParentBone;
		if (grandparent == null) return;

		float px = parentApplied.X, py = parentApplied.Y;
		float psx = parentApplied.ScaleX;
		float sx = psx;
		float psy = parentApplied.ScaleY;
		float csx = childApplied.ScaleX;
		int os1, os2;
		int s2;
		if (psx < 0) {
			psx = -psx;
			os1 = 180;
			s2 = -1;
		}
		else {
			os1 = 0;
			s2 = 1;
		}
		if (psy < 0) {
			psy = -psy;
			s2 = -s2;
		}
		if (csx < 0) {
			csx = -csx;
			os2 = 180;
		}
		else
			os2 = 0;

		var parentWorld = parent.World;
		float cx = childApplied.X, cy;
		float cwx, cwy;
		float a = parentWorld.A, b = parentWorld.B, c = parentWorld.C, d = parentWorld.D;
		bool uniform = MathF.Abs(psx - psy) <= 0.0001f;
		if (!uniform) {
			cy = 0;
			cwx = a * cx + parentWorld.X;
			cwy = c * cx + parentWorld.Y;
		}
		else {
			cy = childApplied.Y;
			cwx = a * cx + b * cy + parentWorld.X;
			cwy = c * cx + d * cy + parentWorld.Y;
		}

		var grandparentWorld = grandparent.World;
		a = grandparentWorld.A;
		b = grandparentWorld.B;
		c = grandparentWorld.C;
		d = grandparentWorld.D;
		float id = 1 / (a * d - b * c);
		float x = cwx - grandparentWorld.X, y = cwy - grandparentWorld.Y;
		float dx = (x * d - y * b) * id - px, dy = (y * a - x * c) * id - py;
		float l1 = MathF.Sqrt(dx * dx + dy * dy), l2 = child.BoneLength * csx;
		float a1, a2;
		if (l1 < 0.0001f) {
			IkApply(parent, targetX, targetY, false, stretch, false, alpha);
			child.UpdateWorldTransform(cx, cy, 0, childApplied.ScaleX, childApplied.ScaleY, childApplied.ShearX, childApplied.ShearY);
			return;
		}

		x = targetX - grandparentWorld.X;
		y = targetY - grandparentWorld.Y;
		float tx = (x * d - y * b) * id - px, ty = (y * a - x * c) * id - py;
		float dd = tx * tx + ty * ty;
		if (softness != 0) {
			softness *= psx * (csx + 1) / 2;
			float td = MathF.Sqrt(dd);
			float sd = td - l1 - l2 * psx + softness;
			if (sd > 0) {
				float p = MathF.Min(1, sd / (softness * 2)) - 1;
				p = (sd - softness * (1 - p * p)) / td;
				tx -= p * tx;
				ty -= p * ty;
				dd = tx * tx + ty * ty;
			}
		}

		if (uniform) {
			l2 *= psx;
			float cos = (dd - l1 * l1 - l2 * l2) / (2 * l1 * l2);
			if (cos < -1)
				cos = -1;
			else if (cos > 1) {
				cos = 1;
				if (stretch)
					sx *= (MathF.Sqrt(dd) / (l1 + l2) - 1) * alpha + 1;
			}
			a2 = MathF.Acos(cos) * bendDir;
			a = l1 + l2 * cos;
			b = l2 * MathF.Sin(a2);
			a1 = MathF.Atan2(ty * a - tx * b, tx * a + ty * b);
		}
		else {
			a = psx * l2;
			b = psy * l2;
			float aa = a * a, bb = b * b;
			float ta = MathF.Atan2(ty, tx);
			c = bb * l1 * l1 + aa * dd - aa * bb;
			float c1 = -2 * bb * l1, c2 = bb - aa;
			d = c1 * c1 - 4 * c2 * c;
			bool solved = false;
			a1 = a2 = 0;
			if (d >= 0) {
				float q = MathF.Sqrt(d);
				if (c1 < 0)
					q = -q;
				q = -(c1 + q) / 2;
				float r0 = q / c2, r1 = c / q;
				float r = MathF.Abs(r0) < MathF.Abs(r1) ? r0 : r1;
				if (r * r <= dd) {
					y = MathF.Sqrt(dd - r * r) * bendDir;
					a1 = ta - MathF.Atan2(y, r);
					a2 = MathF.Atan2(y / psy, (r - l1) / psx);
					solved = true;
				}
			}
			if (!solved) {
				float minAngle = PI;
				float minX = l1 - a, minY = 0;
				float minDist = minX * minX;
				float maxAngle = 0;
				float maxX = l1 + a, maxY = 0;
				float maxDist = maxX * maxX;
				c = -a * l1 / (aa - bb);
				if (c >= -1 && c <= 1) {
					c = MathF.Acos(c);
					x = a * MathF.Cos(c) + l1;
					y = b * MathF.Sin(c);
					d = x * x + y * y;
					if (d < minDist) {
						minAngle = c;
						minDist = d;
						minX = x;
						minY = y;
					}
					if (d > maxDist) {
						maxAngle = c;
						maxDist = d;
						maxX = x;
						maxY = y;
					}
				}
				if (dd <= (minDist + maxDist) / 2) {
					a1 = ta - MathF.Atan2(minY * bendDir, minX);
					a2 = minAngle * bendDir;
				}
				else {
					a1 = ta - MathF.Atan2(maxY * bendDir, maxX);
					a2 = maxAngle * bendDir;
				}
			}
		}

		float os = MathF.Atan2(cy, cx) * s2;
		float rotation = parentApplied.Rotation;
		a1 = Wrap180((a1 - os) * RAD_DEG + os1 - rotation);
		parent.UpdateWorldTransform(px, py, rotation + a1 * alpha, sx, parentApplied.ScaleY, 0, 0);
		rotation = childApplied.Rotation;
		a2 = Wrap180(((a2 + os) * RAD_DEG - childApplied.ShearX) * s2 + os2 - rotation);
		child.UpdateWorldTransform(cx, cy, rotation + a2 * alpha, childApplied.ScaleX, childApplied.ScaleY, childApplied.ShearX, childApplied.ShearY);
	}

	private static void RotateWorld(IConstraintBone bone, float radians) {
		ref var world = ref bone.World;
		var sinCos = MathF.SinCos(radians);
		float sin = sinCos.Sin;
		float cos = sinCos.Cos;
		float a = world.A, b = world.B, c = world.C, d = world.D;
		world = world.WithMatrix(cos * a - sin * c, cos * b - sin * d, sin * a + cos * c, sin * b + cos * d, world.X, world.Y);
	}

	private static float WrapDegrees(float degrees) => degrees - (16384 - (int)(16384.499999999996 - degrees / 360)) * 360;

	public static void TransformApply<TBone>(IConstraintBone target, List<TBone> bones, TransformConstraintData data,
		float rotateMix, float translateMix, float scaleMix, float shearMix) where TBone : IConstraintBone {
		if (data.Local) {
			if (data.Relative)
				RelativeLocal(target, bones, data, rotateMix, translateMix, scaleMix, shearMix);
			else
				AbsoluteLocal(target, bones, data, rotateMix, translateMix, scaleMix, shearMix);
		}
		else {
			if (data.Relative)
				RelativeWorld(target, bones, data, rotateMix, translateMix, scaleMix, shearMix);
			else
				AbsoluteWorld(target, bones, data, rotateMix, translateMix, scaleMix, shearMix);
		}
	}

	private static void AbsoluteWorld<TBone>(IConstraintBone target, List<TBone> bones, TransformConstraintData data,
		float rotateMix, float translateMix, float scaleMix, float shearMix) where TBone : IConstraintBone {
		var targetWorld = target.World;
		float ta = targetWorld.A, tb = targetWorld.B, tc = targetWorld.C, td = targetWorld.D;
		float degRadReflect = ta * td - tb * tc > 0 ? DEG_RAD : -DEG_RAD;
		float offsetRotation = data.OffsetRotation * degRadReflect;
		float offsetShearY = data.OffsetShearY * degRadReflect;

		foreach (var bone in bones) {
			bool modified = false;
			if (rotateMix != 0) {
				var world = bone.World;
				float r = WrapPI(MathF.Atan2(tc, ta) - MathF.Atan2(world.C, world.A) + offsetRotation);
				RotateWorld(bone, r * rotateMix);
				modified = true;
			}
			if (translateMix != 0) {
				var targetPoint = targetWorld.LocalToWorld(data.OffsetX, data.OffsetY);
				ref var world = ref bone.World;
				world = world.WithMatrix(world.A, world.B, world.C, world.D, world.X + (targetPoint.X - world.X) * translateMix, world.Y + (targetPoint.Y - world.Y) * translateMix);
				modified = true;
			}
			if (scaleMix > 0) {
				ref var world = ref bone.World;
				float a = world.A, b = world.B, c = world.C, d = world.D;
				float s = MathF.Sqrt(a * a + c * c), ts = MathF.Sqrt(ta * ta + tc * tc);
				if (s > EPSILON) {
					s = (s + (ts - s + data.OffsetScaleX) * scaleMix) / s;
					a *= s;
					c *= s;
				}
				s = MathF.Sqrt(b * b + d * d);
				ts = MathF.Sqrt(tb * tb + td * td);
				if (s > EPSILON) {
					s = (s + (ts - s + data.OffsetScaleY) * scaleMix) / s;
					b *= s;
					d *= s;
				}
				world = world.WithMatrix(a, b, c, d, world.X, world.Y);
				modified = true;
			}
			if (shearMix > 0) {
				ref var world = ref bone.World;
				float b = world.B, d = world.D;
				float by = MathF.Atan2(d, b);
				float r = WrapPI(MathF.Atan2(td, tb) - MathF.Atan2(tc, ta) - (by - MathF.Atan2(world.C, world.A)));
				r = by + (r + offsetShearY) * shearMix;
				float s = MathF.Sqrt(b * b + d * d);
				world = world.WithMatrix(world.A, MathF.Cos(r) * s, world.C, MathF.Sin(r) * s, world.X, world.Y);
				modified = true;
			}
			if (modified)
				bone.Applied.Valid = false;
		}
	}

	private static void RelativeWorld<TBone>(IConstraintBone target, List<TBone> bones, TransformConstraintData data,
		float rotateMix, float translateMix, float scaleMix, float shearMix) where TBone : IConstraintBone {
		var targetWorld = target.World;
		float ta = targetWorld.A, tb = targetWorld.B, tc = targetWorld.C, td = targetWorld.D;
		float degRadReflect = ta * td - tb * tc > 0 ? DEG_RAD : -DEG_RAD;
		float offsetRotation = data.OffsetRotation * degRadReflect;
		float offsetShearY = data.OffsetShearY * degRadReflect;

		foreach (var bone in bones) {
			bool modified = false;
			if (rotateMix != 0) {
				float r = WrapPI(MathF.Atan2(tc, ta) + offsetRotation);
				RotateWorld(bone, r * rotateMix);
				modified = true;
			}
			if (translateMix != 0) {
				var targetPoint = targetWorld.LocalToWorld(data.OffsetX, data.OffsetY);
				ref var world = ref bone.World;
				world = world.WithMatrix(world.A, world.B, world.C, world.D, world.X + targetPoint.X * translateMix, world.Y + targetPoint.Y * translateMix);
				modified = true;
			}
			if (scaleMix > 0) {
				ref var world = ref bone.World;
				float s = (MathF.Sqrt(ta * ta + tc * tc) - 1 + data.OffsetScaleX) * scaleMix + 1;
				float s2 = (MathF.Sqrt(tb * tb + td * td) - 1 + data.OffsetScaleY) * scaleMix + 1;
				world = world.WithMatrix(world.A * s, world.B * s2, world.C * s, world.D * s2, world.X, world.Y);
				modified = true;
			}
			if (shearMix > 0) {
				float r = WrapPI(MathF.Atan2(td, tb) - MathF.Atan2(tc, ta));
				ref var world = ref bone.World;
				float b = world.B, d = world.D;
				r = MathF.Atan2(d, b) + (r - PI / 2 + offsetShearY) * shearMix;
				float s = MathF.Sqrt(b * b + d * d);
				world = world.WithMatrix(world.A, MathF.Cos(r) * s, world.C, MathF.Sin(r) * s, world.X, world.Y);
				modified = true;
			}
			if (modified)
				bone.Applied.Valid = false;
		}
	}

	private static void AbsoluteLocal<TBone>(IConstraintBone target, List<TBone> bones, TransformConstraintData data,
		float rotateMix, float translateMix, float scaleMix, float shearMix) where TBone : IConstraintBone {
		Validate(target);
		var targetApplied = target.Applied;
		foreach (var bone in bones) {
			Validate(bone);
			var applied = bone.Applied;

			float rotation = applied.Rotation;
			if (rotateMix != 0)
				rotation += WrapDegrees(targetApplied.Rotation - rotation + data.OffsetRotation) * rotateMix;

			float x = applied.X, y = applied.Y;
			if (translateMix != 0) {
				x += (targetApplied.X - x + data.OffsetX) * translateMix;
				y += (targetApplied.Y - y + data.OffsetY) * translateMix;
			}

			float scaleX = applied.ScaleX, scaleY = applied.ScaleY;
			if (scaleMix != 0) {
				if (scaleX > EPSILON)
					scaleX = (scaleX + (targetApplied.ScaleX - scaleX + data.OffsetScaleX) * scaleMix) / scaleX;
				if (scaleY > EPSILON)
					scaleY = (scaleY + (targetApplied.ScaleY - scaleY + data.OffsetScaleY) * scaleMix) / scaleY;
			}

			float shearY = applied.ShearY;
			if (shearMix != 0)
				shearY += WrapDegrees(targetApplied.ShearY - shearY + data.OffsetShearY) * shearMix;

			bone.UpdateWorldTransform(x, y, rotation, scaleX, scaleY, applied.ShearX, shearY);
		}
	}

	private static void RelativeLocal<TBone>(IConstraintBone target, List<TBone> bones, TransformConstraintData data,
		float rotateMix, float translateMix, float scaleMix, float shearMix) where TBone : IConstraintBone {
		Validate(target);
		var targetApplied = target.Applied;
		foreach (var bone in bones) {
			Validate(bone);
			var applied = bone.Applied;

			float rotation = applied.Rotation;
			if (rotateMix != 0)
				rotation += (targetApplied.Rotation + data.OffsetRotation) * rotateMix;

			float x = applied.X, y = applied.Y;
			if (translateMix != 0) {
				x += (targetApplied.X + data.OffsetX) * translateMix;
				y += (targetApplied.Y + data.OffsetY) * translateMix;
			}

			float scaleX = applied.ScaleX, scaleY = applied.ScaleY;
			if (scaleMix != 0) {
				if (scaleX > EPSILON)
					scaleX *= ((targetApplied.ScaleX - 1 + data.OffsetScaleX) * scaleMix) + 1;
				if (scaleY > EPSILON)
					scaleY *= ((targetApplied.ScaleY - 1 + data.OffsetScaleY) * scaleMix) + 1;
			}

			float shearY = applied.ShearY;
			if (shearMix != 0)
				shearY += (targetApplied.ShearY + data.OffsetShearY) * shearMix;

			bone.UpdateWorldTransform(x, y, rotation, scaleX, scaleY, applied.ShearX, shearY);
		}
	}

	public static void PathApply<TBone>(IPathSource path, IConstraintBone slotBone, List<TBone> bones,
		PathConstraintData data, float position, float spacing, float rotateMix, float translateMix, PathScratch scratch)
		where TBone : IConstraintBone {
		bool translate = translateMix > 0;
		bool rotate = rotateMix > 0;
		if (!translate && !rotate) return;

		bool percentSpacing = data.SpacingMode == SpacingMode.Percent;
		var rotateMode = data.RotateMode;
		bool tangents = rotateMode == RotateMode.Tangent;
		bool scale = rotateMode == RotateMode.ChainScale;
		int boneCount = bones.Count;
		int spacesCount = tangents ? boneCount : boneCount + 1;
		float[] spaces = PathScratch.Fit(ref scratch.Spaces, spacesCount);
		float[] lengths = scale ? PathScratch.Fit(ref scratch.Lengths, boneCount) : scratch.Lengths;
		spaces[0] = 0;

		if (scale || !percentSpacing) {
			bool lengthSpacing = data.SpacingMode == SpacingMode.Length;
			for (int i = 0, n = spacesCount - 1; i < n;) {
				var bone = bones[i];
				float setupLength = bone.BoneLength;
				if (setupLength < EPSILON) {
					if (scale)
						lengths[i] = 0;
					spaces[++i] = 0;
				}
				else if (percentSpacing) {
					if (scale) {
						var world = bone.World;
						float x = setupLength * world.A, y = setupLength * world.C;
						lengths[i] = MathF.Sqrt(x * x + y * y);
					}
					spaces[++i] = spacing;
				}
				else {
					var world = bone.World;
					float x = setupLength * world.A, y = setupLength * world.C;
					float length = MathF.Sqrt(x * x + y * y);
					if (scale)
						lengths[i] = length;
					spaces[++i] = (lengthSpacing ? setupLength + spacing : spacing) * length / setupLength;
				}
			}
		}
		else {
			for (int i = 1; i < spacesCount; i++)
				spaces[i] = spacing;
		}

		float[] positions = ComputePathPositions(path, spacesCount, tangents, data.PositionMode == PositionMode.Percent, percentSpacing, position, scratch);
		float boneX = positions[0], boneY = positions[1];
		float offsetRotation = data.OffsetRotation;
		bool tip;
		if (offsetRotation == 0)
			tip = rotateMode == RotateMode.Chain;
		else {
			tip = false;
			var slotWorld = slotBone.World;
			offsetRotation *= slotWorld.A * slotWorld.D - slotWorld.B * slotWorld.C > 0 ? DEG_RAD : -DEG_RAD;
		}

		for (int i = 0, p = 3; i < boneCount; i++, p += 3) {
			var bone = bones[i];
			ref var world = ref bone.World;
			float wa = world.A, wb = world.B, wc = world.C, wd = world.D;
			float wx = world.X + (boneX - world.X) * translateMix, wy = world.Y + (boneY - world.Y) * translateMix;
			float x = positions[p], y = positions[p + 1];
			float dx = x - boneX, dy = y - boneY;
			if (scale) {
				float length = lengths[i];
				if (length != 0) {
					float s = (MathF.Sqrt(dx * dx + dy * dy) / length - 1) * rotateMix + 1;
					wa *= s;
					wc *= s;
				}
			}
			boneX = x;
			boneY = y;
			if (rotate) {
				float r;
				if (tangents)
					r = positions[p - 1];
				else if (spaces[i + 1] == 0)
					r = positions[p + 2];
				else
					r = MathF.Atan2(dy, dx);
				r -= MathF.Atan2(wc, wa);
				if (tip) {
					var sinCos1 = MathF.SinCos(r);
					float sin1 = sinCos1.Sin;
					float cos1 = sinCos1.Cos;
					float length = bone.BoneLength;
					boneX += (length * (cos1 * wa - sin1 * wc) - dx) * rotateMix;
					boneY += (length * (sin1 * wa + cos1 * wc) - dy) * rotateMix;
				}
				else
					r += offsetRotation;
				r = WrapPI(r) * rotateMix;
				var sinCos = MathF.SinCos(r);
				float sin = sinCos.Sin;
				float cos = sinCos.Cos;
				float na = cos * wa - sin * wc, nb = cos * wb - sin * wd;
				float nc = sin * wa + cos * wc, nd = sin * wb + cos * wd;
				wa = na;
				wb = nb;
				wc = nc;
				wd = nd;
			}
			world = world.WithMatrix(wa, wb, wc, wd, wx, wy);
			bone.Applied.Valid = false;
		}
	}

	public static float[] ComputePathPositions(IPathSource path, int spacesCount, bool tangents, bool percentPosition, bool percentSpacing, float position, PathScratch scratch) {
		const int NONE = -1;
		const int BEFORE = -2;
		const int AFTER = -3;
		float[] spaces = scratch.Spaces;
		float[] output = PathScratch.Fit(ref scratch.Positions, spacesCount * 3 + 2);
		float[] world;
		bool closed = path.Closed;
		int verticesLength = path.WorldVerticesLength;
		int curveCount = verticesLength / 6;
		int prevCurve = NONE;
		float pathLength;

		if (!path.ConstantSpeed) {
			float[] lengths = path.Lengths;
			curveCount -= closed ? 1 : 2;
			pathLength = lengths[curveCount];
			if (percentPosition)
				position *= pathLength;
			if (percentSpacing) {
				for (int i = 1; i < spacesCount; i++)
					spaces[i] *= pathLength;
			}
			world = PathScratch.Fit(ref scratch.World, 8);
			for (int i = 0, o = 0, curve = 0; i < spacesCount; i++, o += 3) {
				float space = spaces[i];
				position += space;
				float p = position;

				if (closed) {
					p %= pathLength;
					if (p < 0)
						p += pathLength;
					curve = 0;
				}
				else if (p < 0) {
					if (prevCurve != BEFORE) {
						prevCurve = BEFORE;
						path.ComputeWorldVertices(2, 4, world, 0);
					}
					AddBeforePosition(p, world, 0, output, o);
					continue;
				}
				else if (p > pathLength) {
					if (prevCurve != AFTER) {
						prevCurve = AFTER;
						path.ComputeWorldVertices(verticesLength - 6, 4, world, 0);
					}
					AddAfterPosition(p - pathLength, world, 0, output, o);
					continue;
				}

				for (; ; curve++) {
					float length = lengths[curve];
					if (p > length) continue;
					if (curve == 0)
						p /= length;
					else {
						float prev = lengths[curve - 1];
						p = (p - prev) / (length - prev);
					}
					break;
				}
				if (curve != prevCurve) {
					prevCurve = curve;
					if (closed && curve == curveCount) {
						path.ComputeWorldVertices(verticesLength - 4, 4, world, 0);
						path.ComputeWorldVertices(0, 4, world, 4);
					}
					else
						path.ComputeWorldVertices(curve * 6 + 2, 8, world, 0);
				}
				AddCurvePosition(p, world[0], world[1], world[2], world[3], world[4], world[5], world[6], world[7], output, o, tangents || (i > 0 && space < EPSILON));
			}
			return output;
		}

		if (closed) {
			verticesLength += 2;
			world = PathScratch.Fit(ref scratch.World, verticesLength);
			path.ComputeWorldVertices(2, verticesLength - 4, world, 0);
			path.ComputeWorldVertices(0, 2, world, verticesLength - 4);
			world[verticesLength - 2] = world[0];
			world[verticesLength - 1] = world[1];
		}
		else {
			curveCount--;
			verticesLength -= 4;
			world = PathScratch.Fit(ref scratch.World, verticesLength);
			path.ComputeWorldVertices(2, verticesLength, world, 0);
		}

		float[] curves = PathScratch.Fit(ref scratch.Curves, curveCount);
		pathLength = 0;
		float x1 = world[0], y1 = world[1];
		float cx1 = 0, cy1 = 0;
		float cx2 = 0, cy2 = 0;
		float x2 = 0, y2 = 0;
		float tmpx, tmpy;
		float dddfx, dddfy;
		float ddfx, ddfy;
		float dfx, dfy;
		for (int i = 0, w = 2; i < curveCount; i++, w += 6) {
			cx1 = world[w];
			cy1 = world[w + 1];
			cx2 = world[w + 2];
			cy2 = world[w + 3];
			x2 = world[w + 4];
			y2 = world[w + 5];
			tmpx = (x1 - cx1 * 2 + cx2) * 0.1875f;
			tmpy = (y1 - cy1 * 2 + cy2) * 0.1875f;
			dddfx = ((cx1 - cx2) * 3 - x1 + x2) * 0.09375f;
			dddfy = ((cy1 - cy2) * 3 - y1 + y2) * 0.09375f;
			ddfx = tmpx * 2 + dddfx;
			ddfy = tmpy * 2 + dddfy;
			dfx = (cx1 - x1) * 0.75f + tmpx + dddfx * 0.16666667f;
			dfy = (cy1 - y1) * 0.75f + tmpy + dddfy * 0.16666667f;
			pathLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
			dfx += ddfx;
			dfy += ddfy;
			ddfx += dddfx;
			ddfy += dddfy;
			pathLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
			dfx += ddfx;
			dfy += ddfy;
			pathLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
			dfx += ddfx + dddfx;
			dfy += ddfy + dddfy;
			pathLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
			curves[i] = pathLength;
			x1 = x2;
			y1 = y2;
		}

		if (percentPosition)
			position *= pathLength;
		else
			position *= pathLength / path.Lengths[curveCount - 1];
		if (percentSpacing) {
			for (int i = 1; i < spacesCount; i++)
				spaces[i] *= pathLength;
		}

		float[] segments = scratch.Segments;
		float curveLength = 0;
		for (int i = 0, o = 0, curve = 0, segment = 0; i < spacesCount; i++, o += 3) {
			float space = spaces[i];
			position += space;
			float p = position;

			if (closed) {
				p %= pathLength;
				if (p < 0)
					p += pathLength;
				curve = 0;
			}
			else if (p < 0) {
				AddBeforePosition(p, world, 0, output, o);
				continue;
			}
			else if (p > pathLength) {
				AddAfterPosition(p - pathLength, world, verticesLength - 4, output, o);
				continue;
			}

			for (; ; curve++) {
				float length = curves[curve];
				if (p > length) continue;
				if (curve == 0)
					p /= length;
				else {
					float prev = curves[curve - 1];
					p = (p - prev) / (length - prev);
				}
				break;
			}

			if (curve != prevCurve) {
				prevCurve = curve;
				int ii = curve * 6;
				x1 = world[ii];
				y1 = world[ii + 1];
				cx1 = world[ii + 2];
				cy1 = world[ii + 3];
				cx2 = world[ii + 4];
				cy2 = world[ii + 5];
				x2 = world[ii + 6];
				y2 = world[ii + 7];
				tmpx = (x1 - cx1 * 2 + cx2) * 0.03f;
				tmpy = (y1 - cy1 * 2 + cy2) * 0.03f;
				dddfx = ((cx1 - cx2) * 3 - x1 + x2) * 0.006f;
				dddfy = ((cy1 - cy2) * 3 - y1 + y2) * 0.006f;
				ddfx = tmpx * 2 + dddfx;
				ddfy = tmpy * 2 + dddfy;
				dfx = (cx1 - x1) * 0.3f + tmpx + dddfx * 0.16666667f;
				dfy = (cy1 - y1) * 0.3f + tmpy + dddfy * 0.16666667f;
				curveLength = MathF.Sqrt(dfx * dfx + dfy * dfy);
				segments[0] = curveLength;
				for (ii = 1; ii < 8; ii++) {
					dfx += ddfx;
					dfy += ddfy;
					ddfx += dddfx;
					ddfy += dddfy;
					curveLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
					segments[ii] = curveLength;
				}
				dfx += ddfx;
				dfy += ddfy;
				curveLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
				segments[8] = curveLength;
				dfx += ddfx + dddfx;
				dfy += ddfy + dddfy;
				curveLength += MathF.Sqrt(dfx * dfx + dfy * dfy);
				segments[9] = curveLength;
				segment = 0;
			}

			p *= curveLength;
			for (; ; segment++) {
				float length = segments[segment];
				if (p > length) continue;
				if (segment == 0)
					p /= length;
				else {
					float prev = segments[segment - 1];
					p = segment + (p - prev) / (length - prev);
				}
				break;
			}
			AddCurvePosition(p * 0.1f, x1, y1, cx1, cy1, cx2, cy2, x2, y2, output, o, tangents || (i > 0 && space < EPSILON));
		}
		return output;
	}

	private static void AddBeforePosition(float distance, float[] temp, int i, float[] output, int offset) {
		float x1 = temp[i], y1 = temp[i + 1];
		float dx = temp[i + 2] - x1, dy = temp[i + 3] - y1;
		float r = MathF.Atan2(dy, dx);
		output[offset] = x1 + distance * MathF.Cos(r);
		output[offset + 1] = y1 + distance * MathF.Sin(r);
		output[offset + 2] = r;
	}

	private static void AddAfterPosition(float distance, float[] temp, int i, float[] output, int offset) {
		float x1 = temp[i + 2], y1 = temp[i + 3];
		float dx = x1 - temp[i], dy = y1 - temp[i + 1];
		float r = MathF.Atan2(dy, dx);
		output[offset] = x1 + distance * MathF.Cos(r);
		output[offset + 1] = y1 + distance * MathF.Sin(r);
		output[offset + 2] = r;
	}

	private static void AddCurvePosition(float t, float x1, float y1, float cx1, float cy1, float cx2, float cy2, float x2, float y2, float[] output, int offset, bool tangents) {
		if (t < EPSILON || float.IsNaN(t)) {
			output[offset] = x1;
			output[offset + 1] = y1;
			output[offset + 2] = MathF.Atan2(cy1 - y1, cx1 - x1);
			return;
		}
		float tt = t * t, ttt = tt * t;
		float u = 1 - t, uu = u * u, uuu = uu * u;
		float ut = u * t, ut3 = ut * 3, uut3 = u * ut3, utt3 = ut3 * t;
		float x = x1 * uuu + cx1 * uut3 + cx2 * utt3 + x2 * ttt;
		float y = y1 * uuu + cy1 * uut3 + cy2 * utt3 + y2 * ttt;
		output[offset] = x;
		output[offset + 1] = y;
		if (tangents) {
			if (t < 0.001f)
				output[offset + 2] = MathF.Atan2(cy1 - y1, cx1 - x1);
			else
				output[offset + 2] = MathF.Atan2(y - (y1 * uu + cy1 * ut * 2 + cy2 * tt), x - (x1 * uu + cx1 * ut * 2 + cx2 * tt));
		}
	}
}

public class DeformTimeline : Timeline, ISlotTimeline
{
	public int SlotIndex { get; set; }
	public string? Skin;
	public string Attachment = "";
	public FCurve<float> Curve = new();
	public List<float[]> Offsets = [];

	[JsonIgnore] Attachment? resolved;
	[JsonIgnore] ModelData? resolvedFor;

	public Attachment? Resolve(ModelData data) {
		if (resolvedFor != data) {
			resolvedFor = data;
			var skin = Skin == null ? data.DefaultSkin : data.FindSkin(Skin);
			resolved = skin?.GetAttachment(SlotIndex, Attachment);
		}
		return resolved;
	}

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var slot = model.Slots[SlotIndex];
		if (!slot.Bone.Active) return;
		if (slot.Attachment is not VertexAttachment attachment || attachment.DeformAttachment != Resolve(model.Data)) return;
		if (Offsets.Count == 0 || Curve.Count == 0) return;

		float alpha = (float)mix;
		if (slot.DeformCount == 0)
			blend = MixBlendMode.Setup;
		int count = Offsets[0].Length;

		if (time < Curve.Keyframes[0].Time) {
			switch (blend) {
				case MixBlendMode.Setup:
					slot.DeformCount = 0;
					return;
				case MixBlendMode.First:
					if (alpha == 1) {
						slot.DeformCount = 0;
						return;
					}
					var current = slot.EnsureDeform(count);
					for (int i = 0; i < count; i++)
						current[i] *= 1 - alpha;
					return;
			}
			return;
		}

		float[] deform = slot.EnsureDeform(count);
		float[] keyed;
		float[]? next = null;
		float percent = 0;
		if (time >= Curve.Keyframes[Curve.Keyframes.Count - 1].Time)
			keyed = Offsets[Offsets.Count - 1];
		else {
			int index = Curve.DetermineIndexAtTime(time);
			percent = Curve.DetermineValueAtTime(time, index) - index;
			keyed = Offsets[index];
			next = Offsets[Math.Min(index + 1, Offsets.Count - 1)];
		}

		for (int i = 0; i < count; i++) {
			float value = next == null ? keyed[i] : keyed[i] + (next[i] - keyed[i]) * percent;
			if (alpha == 1) {
				if (blend == MixBlendMode.Add)
					deform[i] += value;
				else
					deform[i] = value;
				continue;
			}
			switch (blend) {
				case MixBlendMode.Setup:
					deform[i] = value * alpha;
					break;
				case MixBlendMode.First:
				case MixBlendMode.Replace:
					deform[i] += (value - deform[i]) * alpha;
					break;
				case MixBlendMode.Add:
					deform[i] += value * alpha;
					break;
			}
		}
	}
}

public class IkConstraintTimeline() : CurveTimeline<float>(5)
{
	public int ConstraintIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var constraint = model.IkConstraints[ConstraintIndex];
		if (!constraint.Active) return;
		var data = constraint.Data;
		float alpha = (float)mix;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup:
					constraint.Mix = data.Mix;
					constraint.Softness = data.Softness;
					constraint.BendDirection = data.BendDirection;
					constraint.Compress = data.Compress;
					constraint.Stretch = data.Stretch;
					return;
				case MixBlendMode.First:
					constraint.Mix += (data.Mix - constraint.Mix) * alpha;
					constraint.Softness += (data.Softness - constraint.Softness) * alpha;
					constraint.BendDirection = data.BendDirection;
					constraint.Compress = data.Compress;
					constraint.Stretch = data.Stretch;
					return;
			}
			return;
		}

		float keyMix;
		float keySoftness;
		float keyBend;
		float keyCompress;
		float keyStretch;
		if (AfterLastFrame(time)) {
			keyMix = Curve(0).Last!.Value;
			keySoftness = Curve(1).Last!.Value;
			keyBend = Curve(2).Last!.Value;
			keyCompress = Curve(3).Last!.Value;
			keyStretch = Curve(4).Last!.Value;
		}
		else {
			int index = Curve(0).DetermineIndexAtTime(time);
			keyMix = Curve(0).DetermineValueAtTime(time, index);
			keySoftness = Curve(1).DetermineValueAtTime(time, index);
			keyBend = Curve(2).DetermineValueAtTime(time, index);
			keyCompress = Curve(3).DetermineValueAtTime(time, index);
			keyStretch = Curve(4).DetermineValueAtTime(time, index);
		}

		if (blend == MixBlendMode.Setup) {
			constraint.Mix = data.Mix + (keyMix - data.Mix) * alpha;
			constraint.Softness = data.Softness + (keySoftness - data.Softness) * alpha;
			if (dir == MixDirection.Out) {
				constraint.BendDirection = data.BendDirection;
				constraint.Compress = data.Compress;
				constraint.Stretch = data.Stretch;
			}
			else {
				constraint.BendDirection = (int)keyBend;
				constraint.Compress = keyCompress != 0;
				constraint.Stretch = keyStretch != 0;
			}
		}
		else {
			constraint.Mix += (keyMix - constraint.Mix) * alpha;
			constraint.Softness += (keySoftness - constraint.Softness) * alpha;
			if (dir == MixDirection.In) {
				constraint.BendDirection = (int)keyBend;
				constraint.Compress = keyCompress != 0;
				constraint.Stretch = keyStretch != 0;
			}
		}
	}
}

public class TransformConstraintTimeline() : CurveTimeline<float>(4)
{
	public int ConstraintIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var constraint = model.TransformConstraints[ConstraintIndex];
		if (!constraint.Active) return;
		var data = constraint.Data;
		float alpha = (float)mix;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup:
					constraint.RotateMix = data.RotateMix;
					constraint.TranslateMix = data.TranslateMix;
					constraint.ScaleMix = data.ScaleMix;
					constraint.ShearMix = data.ShearMix;
					return;
				case MixBlendMode.First:
					constraint.RotateMix += (data.RotateMix - constraint.RotateMix) * alpha;
					constraint.TranslateMix += (data.TranslateMix - constraint.TranslateMix) * alpha;
					constraint.ScaleMix += (data.ScaleMix - constraint.ScaleMix) * alpha;
					constraint.ShearMix += (data.ShearMix - constraint.ShearMix) * alpha;
					return;
			}
			return;
		}

		var values = AfterLastFrame(time) ? [Curve(0).Last!.Value, Curve(1).Last!.Value, Curve(2).Last!.Value, Curve(3).Last!.Value] : DetermineValue(time);
		float rotate = values[0];
		float translate = values[1];
		float scale = values[2];
		float shear = values[3];

		if (blend == MixBlendMode.Setup) {
			constraint.RotateMix = data.RotateMix + (rotate - data.RotateMix) * alpha;
			constraint.TranslateMix = data.TranslateMix + (translate - data.TranslateMix) * alpha;
			constraint.ScaleMix = data.ScaleMix + (scale - data.ScaleMix) * alpha;
			constraint.ShearMix = data.ShearMix + (shear - data.ShearMix) * alpha;
		}
		else {
			constraint.RotateMix += (rotate - constraint.RotateMix) * alpha;
			constraint.TranslateMix += (translate - constraint.TranslateMix) * alpha;
			constraint.ScaleMix += (scale - constraint.ScaleMix) * alpha;
			constraint.ShearMix += (shear - constraint.ShearMix) * alpha;
		}
	}
}

public class PathConstraintPositionTimeline() : CurveTimeline<float>(1)
{
	public int ConstraintIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var constraint = model.PathConstraints[ConstraintIndex];
		if (!constraint.Active) return;
		float alpha = (float)mix;
		float setup = constraint.Data.Position;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup: constraint.Position = setup; return;
				case MixBlendMode.First: constraint.Position += (setup - constraint.Position) * alpha; return;
			}
			return;
		}

		float value = AfterLastFrame(time) ? Curve(0).Last!.Value : Curve(0).DetermineValueAtTime(time);
		if (blend == MixBlendMode.Setup)
			constraint.Position = NMath.Lerp(alpha, setup, value);
		else
			constraint.Position += (value - constraint.Position) * alpha;
	}
}

public class PathConstraintSpacingTimeline() : CurveTimeline<float>(1)
{
	public int ConstraintIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var constraint = model.PathConstraints[ConstraintIndex];
		if (!constraint.Active) return;
		float alpha = (float)mix;
		float setup = constraint.Data.Spacing;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup: constraint.Spacing = setup; return;
				case MixBlendMode.First: constraint.Spacing += (setup - constraint.Spacing) * alpha; return;
			}
			return;
		}

		float value = AfterLastFrame(time) ? Curve(0).Last!.Value : Curve(0).DetermineValueAtTime(time);
		if (blend == MixBlendMode.Setup)
			constraint.Spacing = NMath.Lerp(alpha, setup, value);
		else
			constraint.Spacing += (value - constraint.Spacing) * alpha;
	}
}

public class PathConstraintMixTimeline() : CurveTimeline<float>(2)
{
	public int ConstraintIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out) {
		var constraint = model.PathConstraints[ConstraintIndex];
		if (!constraint.Active) return;
		var data = constraint.Data;
		float alpha = (float)mix;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup:
					constraint.RotateMix = data.RotateMix;
					constraint.TranslateMix = data.TranslateMix;
					return;
				case MixBlendMode.First:
					constraint.RotateMix += (data.RotateMix - constraint.RotateMix) * alpha;
					constraint.TranslateMix += (data.TranslateMix - constraint.TranslateMix) * alpha;
					return;
			}
			return;
		}

		float rotate;
		float translate;
		if (AfterLastFrame(time)) {
			rotate = Curve(0).Last!.Value;
			translate = Curve(1).Last!.Value;
		}
		else {
			int index = Curve(0).DetermineIndexAtTime(time);
			rotate = Curve(0).DetermineValueAtTime(time, index);
			translate = Curve(1).DetermineValueAtTime(time, index);
		}

		if (blend == MixBlendMode.Setup) {
			constraint.RotateMix = data.RotateMix + (rotate - data.RotateMix) * alpha;
			constraint.TranslateMix = data.TranslateMix + (translate - data.TranslateMix) * alpha;
		}
		else {
			constraint.RotateMix += (rotate - constraint.RotateMix) * alpha;
			constraint.TranslateMix += (translate - constraint.TranslateMix) * alpha;
		}
	}
}

public class EventTimeline : Timeline
{
	public List<ModelEvent> Events = [];

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend,
		MixDirection dir = MixDirection.Out) { }

	public override void CollectEvents(double lastTime, double time, List<ModelEvent> into) {
		int count = Events.Count;
		if (count == 0) return;

		if (lastTime > time) {
			CollectEvents(lastTime, double.MaxValue, into);
			lastTime = -1;
		}
		else if (lastTime >= Events[count - 1].Time)
			return;
		if (time < Events[0].Time) return;

		int frame;
		if (lastTime < Events[0].Time)
			frame = 0;
		else {
			frame = 0;
			while (frame < count && Events[frame].Time <= lastTime)
				frame++;
		}
		for (; frame < count && time >= Events[frame].Time; frame++)
			into.Add(Events[frame]);
	}
}
