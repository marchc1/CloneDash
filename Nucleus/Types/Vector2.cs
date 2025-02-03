using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nucleus.Types
{
    /// <summary>
    /// Two-dimesional floating point vector.
    /// </summary>
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    public struct Vector2F
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    {
        public float x;
        public float y;

        public static readonly Vector2F Zero = new(0, 0);
        public static readonly Vector2F One = new(1, 1);

        public Vector2F(float X, float Y) { this.x = X; this.y = Y; }
        public Vector2F(float Both) { this.x = Both; this.y = Both; }
        public static Vector2F FromXY(Vector3 xyz) => new(xyz.X, xyz.Y);

        // Aliases to X, Y, W, H to the internal value

        /// <summary>
        /// Convenience property, internally equivalant to <see cref="x"/>
        /// </summary>
        [JsonIgnore] public float X { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalant to <see cref="y"/>
        /// </summary>
        [JsonIgnore] public float Y { get { return y; } set { y = value; } }

        /// <summary>
        /// Convenience property, internally equivalent to <see cref="x"/>
        /// </summary>
        [JsonIgnore] public float w { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalent to <see cref="y"/>
        /// </summary>
        [JsonIgnore] public float h { get { return y; } set { y = value; } }

        /// <summary>
        /// Convenience property, internally equivalent to <see cref="x"/>
        /// </summary>
        [JsonIgnore] public float W { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalent to <see cref="y"/>
        /// </summary>
        [JsonIgnore] public float H { get { return y; } set { y = value; } }

        public static Vector2F operator +(Vector2F from, float by) => new Vector2F(from.X + by, from.Y + by);
        public static Vector2F operator -(Vector2F from, float by) => new Vector2F(from.X - by, from.Y - by);
        public static Vector2F operator *(Vector2F from, float by) => new Vector2F(from.X * by, from.Y * by);
        public static Vector2F operator /(Vector2F from, float by) => new Vector2F((float)((double)from.X / (double)by), (float)((double)from.Y / (double)by));

        public static Vector2F operator +(Vector2F from, Vector2F by) => new Vector2F(from.X + by.X, from.Y + by.Y);
        public static Vector2F operator -(Vector2F from, Vector2F by) => new Vector2F(from.X - by.X, from.Y - by.Y);
        public static Vector2F operator *(Vector2F from, Vector2F by) => new Vector2F(from.X * by.X, from.Y * by.Y);
        public static Vector2F operator /(Vector2F from, Vector2F by) => new Vector2F((float)((double)from.X / (double)by.X), (float)((double)from.Y / (double)by.Y));

        public static bool operator <(Vector2F a, Vector2F b) => a.X < b.X || a.Y < b.Y;
        public static bool operator >(Vector2F a, Vector2F b) => a.X > b.X || a.Y > b.Y;
        public static bool operator <=(Vector2F a, Vector2F b) => a.X <= b.X || a.Y <= b.Y;
        public static bool operator >=(Vector2F a, Vector2F b) => a.X >= b.X || a.Y >= b.Y;

        public static Vector2F operator -(Vector2F on) => new Vector2F(-on.X, -on.Y);

        private static bool CompareVector2F(Vector2F a, Vector2F b) {
            if (a.X == b.X && a.Y == b.Y)
                return true;
            return false;
        }

        public static bool operator ==(Vector2F a, Vector2F b) => CompareVector2F(a, b);
        public static bool operator !=(Vector2F a, Vector2F b) => !CompareVector2F(a, b);

        public override string ToString() {
            return $"vec2({x}, {y})";
        }

        public Vector2F Round(int digits = 0) {
            return new((float)Math.Round(X, digits), (float)Math.Round(Y, digits));
        }
        public bool IsZero() => X == 0 && Y == 0;

        public Vector2F DownscaleRatio() {
            if (X == Y) return new(1, 1);
            else if (X > Y) return new(Y / X, 1);
            else return new(1, X / Y);
        }
        public Vector2F UpscaleRatio() {
            if (X == Y) return new(1, 1);
            else if (X > Y) return new(X / Y, 1);
            else return new(1, Y / X);
        }

        public float Distance(Vector2F other) => MathF.Sqrt(MathF.Pow(other.X - X, 2) + MathF.Pow(other.Y - Y, 2));
        public bool InRadiusOfCircle(Vector2F focus, float radius) {
            var dist = this.Distance(focus);
            return dist < radius;
        }

        [JsonIgnore]
        public Vector2F XX => new(X, X);
        [JsonIgnore]
        public Vector2F YX => new(Y, X);
        [JsonIgnore]
        public Vector2F YY => new(Y, Y);

		public float Length => MathF.Sqrt(x * x + y * y);

		public Vector2F Abs() => new(MathF.Abs(X), MathF.Abs(Y));

		/// <summary>
		/// Performs linear interpolation where ratio (0 -> 1) is translated to a -> b
		/// </summary>
		/// <param name="ratio"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2F Lerp(float ratio, Vector2F a, Vector2F b) {
			return new(
				NMath.Lerp(ratio, a.X, b.X),
				NMath.Lerp(ratio, a.Y, b.Y)
				);
		}

		public static Vector2F Lerp(Vector2F ratio, Vector2F a, Vector2F b) {
			return new(
				NMath.Lerp(ratio.X, a.X, b.X),
				NMath.Lerp(ratio.Y, a.Y, b.Y)
				);
		}

		public static Vector2F Remap(float ratio, Vector2F iMi, Vector2F iMa, Vector2F oMi, Vector2F oMa) {
			return new(
				(float)NMath.Remap(ratio, iMi.X, iMa.X, oMi.X, oMa.X),
				(float)NMath.Remap(ratio, iMi.Y, iMa.Y, oMi.Y, oMa.Y)
				);
		}
		public static Vector2F Remap(Vector2F ratio, Vector2F iMi, Vector2F iMa, Vector2F oMi, Vector2F oMa) {
			return new(
				(float)NMath.Remap(ratio.X, iMi.X, iMa.X, oMi.X, oMa.X),
				(float)NMath.Remap(ratio.Y, iMi.Y, iMa.Y, oMi.Y, oMa.Y)
				);
		}

		/// <summary>
		/// Return a normalized <see cref="Vector2F"/> with a <see cref="Length"/> of 1.
		/// </summary>
		/// <returns></returns>
		public Vector2F Normalize() {
			return new(X / Length, Y / Length);
		}

		/// <summary>
		/// Rotates a Vector2F around a center by a rotation specified in degrees.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="degrees">Rotation in degrees</param>
		/// <returns></returns>
		public Vector2F RotateAroundPoint(Vector2F center, float degrees) {
			float radians = degrees / (180f / MathF.PI);
			float s = MathF.Sin(radians);
			float c = MathF.Cos(radians);

			Vector2F p = new(this.X, this.Y);

			// translate point back to origin:
			p.x -= center.X;
			p.y -= center.Y;

			// rotate point
			float xnew = p.x * c - p.y * s;
			float ynew = p.x * s + p.y * c;

			// translate point back:
			p.x = xnew + center.Y;
			p.y = ynew + center.X;

			return p;
		}
		/// <summary>
		/// Get the rotation of this point in degrees. Zero means straight Y up, no X.
		/// </summary>
		/// <param name="center"></param>
		/// <returns></returns>
		public float GetRotationFromCenter(Vector2F center) {
			var normalized = (this - center).Normalize();

			return 360 - (((MathF.Atan2(normalized.X, normalized.Y) * NMath.DEG2RAD) + 180) % 360);
		}

		public bool InTriangle(Triangle2D triangle) => triangle.IsPointInTriangle(this);

		public bool InRing(Vector2F focus, float outerRing, float innerRing) {
			// in the outer ring radius but not in the inner ring radius
			return InRadiusOfCircle(focus, outerRing) && !InRadiusOfCircle(focus, innerRing);
		}

		public Vector2F FitInto(RectangleF cropRect) {
            Vector2F newR = this;

            newR.X = Math.Clamp(newR.X, cropRect.X, cropRect.X + cropRect.W);
            newR.Y = Math.Clamp(newR.Y, cropRect.Y, cropRect.Y + cropRect.H);

            return newR;
        }
    }
    public static class VectorConverters
    {
        public static Vector2F ToNucleus(this Vector2 vector) => new Vector2F(vector.X, vector.Y);
        public static Vector2 ToNumerics(this Vector2F vector) => new Vector2(vector.X, vector.Y);
    }
}
