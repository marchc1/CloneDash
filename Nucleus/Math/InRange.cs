namespace Nucleus
{
    public static partial class NMath
    {
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
    }
}
