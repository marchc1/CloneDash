using System;
using System.Runtime.CompilerServices;

namespace AssetStudio
{
    public static partial class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float minValue, float maxValue)
        {
#if NETFRAMEWORK
            return Math.Max(minValue, Math.Min(value, maxValue));
#else
            return Math.Clamp(value, minValue, maxValue);
#endif
        }
    }
}
