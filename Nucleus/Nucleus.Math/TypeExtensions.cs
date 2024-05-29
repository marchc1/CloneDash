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

    }
}
