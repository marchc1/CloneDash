using System.Numerics;

namespace Nucleus.Util;

public static unsafe partial class Util
{
	public static uint RoundUpToPowerOf2(this uint x) {
		return BitOperations.RoundUpToPowerOf2(x);
	}
}