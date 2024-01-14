using Raylib_cs;

namespace CloneDash
{
    public static class DashMath
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
        /// Remapping function. Given an <paramref name="input"/>, converts that input from the input range <paramref name="inStart"/> -> <paramref name="inEnd"/> into a range from <paramref name="outStart"/> -> <paramref name="outEnd"/>.
        /// </summary>
        /// <param name="input">The input value</param>
        /// <param name="inStart">The start of the input range</param>
        /// <param name="inEnd">The end of the input range</param>
        /// <param name="outStart">The start of the output range</param>
        /// <param name="outEnd">The end of the output range</param>
        /// <param name="clampInput">Should the input be clamped to fit within <paramref name="inStart"/> -> <paramref name="inEnd"/></param>
        /// <param name="clampOutput">Should the input be clamped to fit within <paramref name="outStart"/> -> <paramref name="outEnd"/></param>
        /// <returns><paramref name="input"/> remapped to be between <paramref name="outStart"/> -> <paramref name="outEnd"/></returns>
        public static double Remap(double input, double inStart, double inEnd, double outStart, double outEnd, bool clampInput = false, bool clampOutput = false) {
            if (clampInput)
                input = Math.Clamp(input, inStart, inEnd);

            var ret = outStart + (input - inStart) * (outEnd - outStart) / (inEnd - inStart);

            if (clampOutput)
                ret = Math.Clamp(ret, outStart, outEnd);

            return ret;
        }
        /// <summary>
        /// Returns if <paramref name="this"/> is within the range of <paramref name="minValue"/> and <paramref name="maxValue"/>
        /// </summary>
        /// <param name="this"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool InRange(float @this, float minValue, float maxValue) {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }
        /// <summary>
        /// Returns if <paramref name="this"/> is within the range of <paramref name="minValue"/> and <paramref name="maxValue"/>
        /// </summary>
        /// <param name="this"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool InRange(double @this, double minValue, double maxValue) {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }

        private static byte clampAndMakeByte(float value) => (byte)Math.Clamp(value, 0, 255);
        
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

        /// <summary>
        /// Multiplies <see cref="Color"/> <paramref name="c"/> by <paramref name="by"/> while keeping the alpha at 255
        /// </summary>
        /// <param name="c"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static Color Multiply(this Color c, float by) => new Color(clampAndMakeByte(c.R * by), clampAndMakeByte(c.G * by), clampAndMakeByte(c.B * by), (byte)255);
    }
}
