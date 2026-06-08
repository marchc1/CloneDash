using Nucleus.Types;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Nucleus.Models
{
	public readonly struct Transformation
	{
		// matrix components
		public readonly float A;
		public readonly float B;
		public readonly float C;
		public readonly float D;
		public readonly float X;
		public readonly float Y;

		public void Decompose(out float a, out float b, out float c, out float d, out float x, out float y) {
			a = A; b = B;
			c = C; d = D;
			x = X; y = Y;
		}

		// cached values from CalculateWorldTransformation
		// needed for World/Local rotation operations
		private readonly float Rotation;
		private readonly float ShearX;
		private readonly float WTL_Inverse;

		public readonly Vector2F Translation => new(X, Y);

		public Transformation(float a, float b, float c, float d, float x, float y, float rot, float shearX) {
			A = a;
			B = b;
			C = c;
			D = d;
			X = x;
			Y = y;
			Rotation = rot;
			ShearX = shearX;
			WTL_Inverse = 1f / (A * D - B * C);
		}

		public static Transformation CalculateWorldTransformation(in Vector2F pos, float rot, in Vector2F scale, in Vector2F shear, TransformMode transformType = TransformMode.Normal, in Transformation? parent = null, bool triggerDebugger = false) {
			if (triggerDebugger) Debug.Assert(false, "Debugger triggered!");

			float posX = pos.X, posY = pos.Y;
			float scaleX = scale.X, scaleY = scale.Y;
			float shearX = shear.X, shearY = shear.Y;

			float a = 0, b = 0, c = 0, d = 0, x = 0, y = 0;

			float r_p_90_p_sy_RADS = NMath.ToRadians(rot + 90 + shearY);
			float r_p_sx_RADS = NMath.ToRadians(rot + shearX);

			if (parent == null) {
				(d, b) = MathF.SinCos(r_p_90_p_sy_RADS);
				(c, a) = MathF.SinCos(r_p_sx_RADS);
				a *= scaleX;
				b *= scaleY;
				c *= scaleX;
				d *= scaleY;

				x = posX;
				y = posY;

				return new(a, b, c, d, x, y, rot, shearX);
			}
			else {
				Transformation parentMatrix = parent.Value;
				float pA = parentMatrix.A, pB = parentMatrix.B,
					  pC = parentMatrix.C, pD = parentMatrix.D,
					  pX = parentMatrix.X, pY = parentMatrix.Y;

				x = pA * posX + pB * posY + pX;
				y = pC * posX + pD * posY + pY;

				float lA, lB, lC, lD;

				switch (transformType) {
					case TransformMode.Normal:
						(lD, lB) = MathF.SinCos(r_p_90_p_sy_RADS);
						(lC, lA) = MathF.SinCos(r_p_sx_RADS);

						lA *= scaleX;
						lB *= scaleY;
						lC *= scaleX;
						lD *= scaleY;

						a = pA * lA + pB * lC;
						b = pA * lB + pB * lD;
						c = pC * lA + pD * lC;
						d = pC * lB + pD * lD;

						break;
					case TransformMode.OnlyTranslation:
						(c, a) = MathF.SinCos(r_p_sx_RADS);
						(d, b) = MathF.SinCos(r_p_90_p_sy_RADS);
						a *= scaleX;
						b *= scaleY;
						c *= scaleX;
						d *= scaleY;

						break;
					case TransformMode.NoRotationOrReflection:
						float sc = pA * pA + pC * pC;
						float prX;

						if (sc > 0.0001f) {
							sc = MathF.Abs(pA * pD - pB * pC) / sc;
							pB = pC * sc;
							pD = pA * sc;
							prX = MathF.Atan2(pC, pA).ToDegrees();
						}
						else {
							pA = pC = 0;
							prX = 90 - MathF.Atan2(pD, pB).ToDegrees();
						}

						float rX = (rot + shear.X - prX).ToRadians();
						float rY = (rot + shear.Y - prX + 90).ToRadians();

						(lC, lA) = MathF.SinCos(rX);
						(lD, lB) = MathF.SinCos(rY);
						lA *= scaleX;
						lB *= scaleY;
						lC *= scaleX;
						lD *= scaleY;

						a = pA * lA - pB * lC;
						b = pA * lB - pB * lD;
						c = pC * lA + pD * lC;
						d = pC * lB + pD * lD;

						break;
					case TransformMode.NoScale:
					case TransformMode.NoScaleOrReflection:
						var rr = rot.ToRadians();
						(float cs, float ss) = MathF.SinCos(rr);

						float zA = pA * cs + pB * ss, zC = pC * cs + pD * ss, acsqr = MathF.Sqrt((zA * zA) + (zC * zC)), iacsqr = 1f / acsqr;
						zA *= acsqr > 0.00001 ? iacsqr : acsqr; zC *= acsqr > 0.00001 ? iacsqr : acsqr;
						float ca2 = MathF.PI / 2f + MathF.Atan2(zC, zA);
						(float zD, float zB) = MathF.SinCos(ca2);
						zB *= acsqr;
						zD *= acsqr;

						(float ns_lC, float ns_lA) = MathF.SinCos(shearX.ToRadians());
						(float ns_lD, float ns_lB) = MathF.SinCos((90 + shearY).ToRadians());

						a = zA * ns_lA + zB * ns_lC;
						b = zA * ns_lB + zB * ns_lD;
						c = zC * ns_lA + zD * ns_lC;
						d = zC * ns_lB + zD * ns_lD;

						break;
				}
			}

			return new(a, b, c, d, x, y, rot, shearX);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2F WorldToLocal(float worldX, float worldY) {
			float invDet = WTL_Inverse;
			float x = worldX - X, y = worldY - Y;

			return new(
				x * D * invDet - y * B * invDet,
				y * A * invDet - x * C * invDet
			);
		}

		public Vector2F WorldToLocal(in Vector2F worldPos) => WorldToLocal(worldPos.X, worldPos.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2F LocalToWorld(float localX, float localY) => new(
			localX * A + localY * B + X,
			localX * C + localY * D + Y
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2F LocalToWorld(in Vector2F localPos) => LocalToWorld(localPos.X, localPos.Y);

		public float WorldToLocalRotation(float worldRotation) {
			(float sin, float cos) = MathF.SinCos(worldRotation.ToRadians());
			return MathF.Atan2(A * sin - C * cos, D * cos - B * sin).ToDegrees() + Rotation - ShearX;
		}

		public float LocalToWorldRotation(float localRotation) {
			localRotation -= Rotation - ShearX;
			(float sin, float cos) = MathF.SinCos(localRotation.ToRadians());
			return MathF.Atan2(cos * C + sin * D, cos * A + sin * B).ToDegrees();
		}
	}
}
