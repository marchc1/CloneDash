using Nucleus.Types;
using System.Numerics;

namespace Nucleus
{
    public static partial class NMath
    {
        public static class Random
        {
            private static System.Random Instance = new System.Random();
            public static float Single(float minValue, float maxValue) => NMath.Lerp(Instance.NextSingle(), minValue, maxValue);
            public static float Float(float minValue, float maxValue) => Single(minValue, maxValue);
            public static double Double(double minValue, double maxValue) => NMath.Lerp(Instance.NextDouble(), minValue, maxValue);
            public static Vector2F Vec2(Vector2F minValue, Vector2F maxValue) => new(Single(minValue.X, maxValue.X), Single(minValue.Y, maxValue.Y));
            public static Vector3 Vec3(Vector3 minValue, Vector3 maxValue) => new(Single(minValue.X, maxValue.X), Single(minValue.Y, maxValue.Y), Single(minValue.Z, maxValue.Z));
        }
    }
}
