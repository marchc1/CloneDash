using Raylib_cs;

namespace Nucleus
{
    public static partial class NMath
    {
        /// <summary>
        /// Multiplies <see cref="Color"/> <paramref name="c"/> by <paramref name="by"/> while keeping the alpha at 255
        /// </summary>
        /// <param name="c"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static Color Multiply(this Color c, float by) => new Color(clampAndMakeByte(c.R * by), clampAndMakeByte(c.G * by), clampAndMakeByte(c.B * by), (byte)255);


		public static int FloorToInt(float f) => Convert.ToInt32(Math.Floor(f));
		public static int FloorToInt(double f) => Convert.ToInt32(Math.Floor(f));
		public static int FloorToInt(decimal f) => Convert.ToInt32(Math.Floor(f));

		public static int CeilToInt(float f) => Convert.ToInt32(Math.Ceiling(f));
		public static int CeilToInt(double f) => Convert.ToInt32(Math.Ceiling(f));
		public static int CeilToInt(decimal f) => Convert.ToInt32(Math.Ceiling(f));

		public static int RoundToInt(float f) => Convert.ToInt32(Math.Round(f));
		public static int RoundToInt(double f) => Convert.ToInt32(Math.Round(f));
		public static int RoundToInt(decimal f) => Convert.ToInt32(Math.Round(f));
	}
}
