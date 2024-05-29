using System.Numerics;

namespace Nucleus.Types
{
    /// <summary>
    /// Two-dimesional floating point vector.
    /// </summary>
    public struct Vector2F
    {
        public float x;
        public float y;

        public static readonly Vector2F Zero = new(0, 0);
        public static readonly Vector2F One = new(1, 1);

        public Vector2F(float X, float Y) { this.x = X; this.y = Y; }
        public Vector2F(float Both) { this.x = Both; this.y = Both; }

        // Aliases to X, Y, W, H to the internal value

        /// <summary>
        /// Convenience property, internally equivalant to <see cref="x"/>
        /// </summary>
        public float X { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalant to <see cref="y"/>
        /// </summary>
        public float Y { get { return y; } set { y = value; } }

        /// <summary>
        /// Convenience property, internally equivalent to <see cref="x"/>
        /// </summary>
        public float w { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalent to <see cref="y"/>
        /// </summary>
        public float h { get { return y; } set { y = value; } }

        /// <summary>
        /// Convenience property, internally equivalent to <see cref="x"/>
        /// </summary>
        public float W { get { return x; } set { x = value; } }
        /// <summary>
        /// Convenience property, internally equivalent to <see cref="y"/>
        /// </summary>
        public float H { get { return y; } set { y = value; } }

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
    }
    public static class VectorConverters {
        public static Vector2F ToNucleus(this Vector2 vector) => new Vector2F(vector.X, vector.Y);
        public static Vector2 ToNumerics(this Vector2F vector) => new Vector2(vector.X, vector.Y);
    }
}
