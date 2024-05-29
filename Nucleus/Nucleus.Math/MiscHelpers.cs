using System.Numerics;

namespace Nucleus
{
    public static partial class NMath
    {
        private static byte clampAndMakeByte(float value) => (byte)Math.Clamp(value, 0, 255);
        public static Matrix4x4 Invert(this Matrix4x4 mat) {
            Matrix4x4 ret;
            Matrix4x4.Invert(mat, out ret);
            return ret;
        }
        public static Matrix4x4 Transpose(this Matrix4x4 mat) => Matrix4x4.Transpose(mat);
    }
}
