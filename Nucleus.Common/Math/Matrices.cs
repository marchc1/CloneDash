using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nucleus;

// These are coping mechanism extensions to help port C code over from Raymath.
public static partial class NMath
{
	extension(ref Vector3 v)
	{
		public ref float x { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref v.X; }
		public ref float y { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref v.Y; }
		public ref float z { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref v.Z; }
	}

	extension(ref Quaternion q)
	{
		public ref float x { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref q.X; }
		public ref float y { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref q.Y; }
		public ref float z { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref q.Z; }
		public ref float w { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref q.W; }
	}

	extension(ref Matrix4x4 m)
	{
		public ref float M0 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M11; }
		public ref float M4 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M12; }
		public ref float M8 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M13; }
		public ref float M12 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M14; }

		public ref float M1 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M21; }
		public ref float M5 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M22; }
		public ref float M9 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M23; }
		public ref float M13 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M24; }

		public ref float M2 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M31; }
		public ref float M6 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M32; }
		public ref float M10 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M33; }
		public ref float M14 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M34; }

		public ref float M3 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M41; }
		public ref float M7 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M42; }
		public ref float M11 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M43; }
		public ref float M15 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M44; }



		public ref float m0 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M11; }
		public ref float m4 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M12; }
		public ref float m8 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M13; }
		public ref float m12 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M14; }

		public ref float m1 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M21; }
		public ref float m5 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M22; }
		public ref float m9 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M23; }
		public ref float m13 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M24; }

		public ref float m2 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M31; }
		public ref float m6 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M32; }
		public ref float m10 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M33; }
		public ref float m14 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M34; }

		public ref float m3 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M41; }
		public ref float m7 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M42; }
		public ref float m11 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M43; }
		public ref float m15 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref m.M44; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static float asinf(float x) => MathF.Asin(x);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static float atan2f(float x, float y) => MathF.Atan2(x, y);

}
public static partial class NMath
{

	public static Matrix4x4 MatrixTranslate(float x, float y, float z) => new(
		1.0f, 0.0f, 0.0f, x,
		0.0f, 1.0f, 0.0f, y,
		0.0f, 0.0f, 1.0f, z,
		0.0f, 0.0f, 0.0f, 1.0f
	);

	public static Vector3 QuaternionToEuler(Quaternion q) {
		Vector3 result = default;

		// Roll (x-axis rotation)
		float x0 = 2.0f * (q.w * q.x + q.y * q.z);
		float x1 = 1.0f - 2.0f * (q.x * q.x + q.y * q.y);
		result.x = atan2f(x0, x1);

		// Pitch (y-axis rotation)
		float y0 = 2.0f * (q.w * q.y - q.z * q.x);
		y0 = y0 > 1.0f ? 1.0f : y0;
		y0 = y0 < -1.0f ? -1.0f : y0;
		result.y = asinf(y0);

		// Yaw (z-axis rotation)
		float z0 = 2.0f * (q.w * q.z + q.x * q.y);
		float z1 = 1.0f - 2.0f * (q.y * q.y + q.z * q.z);
		result.z = atan2f(z0, z1);

		return result;
	}

	public static Matrix4x4 QuaternionToMatrix(Quaternion q) {
		Matrix4x4 result = new(1.0f, 0.0f, 0.0f, 0.0f,
					  0.0f, 1.0f, 0.0f, 0.0f,
					  0.0f, 0.0f, 1.0f, 0.0f,
					  0.0f, 0.0f, 0.0f, 1.0f);

		float a2 = q.x * q.x;
		float b2 = q.y * q.y;
		float c2 = q.z * q.z;
		float ac = q.x * q.z;
		float ab = q.x * q.y;
		float bc = q.y * q.z;
		float ad = q.w * q.x;
		float bd = q.w * q.y;
		float cd = q.w * q.z;

		result.m0 = 1 - 2 * (b2 + c2);
		result.m1 = 2 * (ab + cd);
		result.m2 = 2 * (ac - bd);

		result.m4 = 2 * (ab - cd);
		result.m5 = 1 - 2 * (a2 + c2);
		result.m6 = 2 * (bc + ad);

		result.m8 = 2 * (ac + bd);
		result.m9 = 2 * (bc - ad);
		result.m10 = 1 - 2 * (a2 + b2);

		return result;
	}
	public static Matrix4x4 MatrixScale(float x, float y, float z) => new(
		x, 0.0f, 0.0f, 0.0f,
		0.0f, y, 0.0f, 0.0f,
		0.0f, 0.0f, z, 0.0f,
		0.0f, 0.0f, 0.0f, 1.0f
	);
}