using Raylib_cs;

namespace Nucleus
{
    public static partial class NMath
    {
        /// <summary>
        /// Linear interpolation function, converts <paramref name="t"/> (a zero - one value) into a <paramref name="a"/> - <paramref name="b"/> value.<br></br>
        /// ex. <paramref name="t"/> = 0.5, <paramref name="a"/> = 5, <paramref name="b"/> = 10 <br></br>
        /// </summary>
        /// <param name="t">Input, should be within a zero - one range. Be careful with clamping as a value over 1 can cause the output to be greater than <paramref name="b"/>.</param>
        /// <param name="a">The start of the new range</param>
        /// <param name="b">The end of the new range</param>
        /// <returns><paramref name="t"/> converted from 0 -> 1 range into <paramref name="a"/> -> <paramref name="b"/> range</returns>
        public static float Lerp(float t, float a, float b) => a + t * (b - a);
        /// <summary>
        /// Linear interpolation function, converts <paramref name="t"/> (a zero - one value) into a <paramref name="a"/> - <paramref name="b"/> value.<br></br>
        /// ex. <paramref name="t"/> = 0.5, <paramref name="a"/> = 5, <paramref name="b"/> = 10 <br></br>
        /// </summary>
        /// <param name="t">Input, should be within a zero - one range. Be careful with clamping as a value over 1 can cause the output to be greater than <paramref name="b"/> and vice versa with a lower-than-zero input and <paramref name="a"/></param>
        /// <param name="a">The start of the new range</param>
        /// <param name="b">The end of the new range</param>
        /// <returns><paramref name="t"/> converted from 0 -> 1 range into <paramref name="a"/> -> <paramref name="b"/> range</returns>
        public static double Lerp(double t, double a, double b) => a + t * (b - a);

        /// <summary>
        /// Performs linear interpolation on <paramref name="min"/> and <paramref name="max"/> using <paramref name="input"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="alpha">Optional parameter, but if not set to -1, will override the alphas specified by the min/max colors</param>
        /// <returns></returns>
        public static Color LerpColor(float input, Color min, Color max, float alpha = -1f) {
            float r = Lerp(input, min.R, max.R);
            float g = Lerp(input, min.G, max.G);
            float b = Lerp(input, min.B, max.B);
            float a = alpha == -1f ? Lerp(input, min.A, max.A) : alpha;

            return new Color(clampAndMakeByte(r), clampAndMakeByte(g), clampAndMakeByte(b), clampAndMakeByte(a));
        }
    }
}
