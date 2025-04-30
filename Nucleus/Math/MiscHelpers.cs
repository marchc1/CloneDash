using Nucleus.Types;
using Raylib_cs;
using System.Numerics;

namespace Nucleus
{
    public static partial class NMath
    {
		public static int Modulo(int a, int b) => ((a % b) + b) % b;
		public static float Modulo(float a, float b) => ((a % b) + b) % b;
		public static double Modulo(double a, double b) => ((a % b) + b) % b;

		private static byte clampAndMakeByte(float value) => (byte)Math.Clamp(value, 0, 255);
        public static Matrix4x4 Invert(this Matrix4x4 mat) {
            Matrix4x4 ret;
            Matrix4x4.Invert(mat, out ret);
            return ret;
        }
        public static Matrix4x4 Transpose(this Matrix4x4 mat) => Matrix4x4.Transpose(mat);

		public static unsafe Color GetPixelColor(this Image image, Vector2F pos) {
			// sanity checking
			if (pos.X < 0) return Color.Blank;
			if (pos.Y < 0) return Color.Blank;
			if (pos.X >= image.Width) return Color.Blank;
			if (pos.Y >= image.Height) return Color.Blank;

			var size = Raylib.GetPixelDataSize(image.Width, image.Height, image.Format);
			var sizePerPixel = size / (image.Width * image.Height);
			void* src = (void*)nint.Add((nint)image.Data, sizePerPixel * (((int)pos.Y * image.Width) + (int)pos.X));
			return Raylib.GetPixelColor(src, image.Format);
		}

		public static bool IsTransparent(this Image image, Vector2F pos) => GetPixelColor(image, pos).A <= 0;
	}
}
