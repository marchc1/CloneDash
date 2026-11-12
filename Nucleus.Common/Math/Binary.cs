using System.Numerics;

namespace Nucleus;

public static partial class NMath
{
	public static int CeilPow2(int value) {
		if (value <= 1) return 1;
		// Decrement value to handle cases where 'value' is already a power of two
		int leadingZeros = BitOperations.LeadingZeroCount((uint)(value - 1));
		return 1 << (32 - leadingZeros);
	}
}