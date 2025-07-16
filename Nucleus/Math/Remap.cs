using Nucleus.Types;

using Raylib_cs;

using System.Numerics;

namespace Nucleus
{
	public static partial class NMath
	{
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
				ret = Math.Clamp(ret, outStart > outEnd ? outEnd : outStart, outStart > outEnd ? outStart : outEnd);

			return ret;
		}
		public static float Remap(float input, float inStart, float inEnd, float outStart, float outEnd, bool clampInput = false, bool clampOutput = false) {
			if (clampInput)
				input = Math.Clamp(input, inStart, inEnd);

			var ret = outStart + (input - inStart) * (outEnd - outStart) / (inEnd - inStart);

			if (clampOutput)
				ret = Math.Clamp(ret, outStart, outEnd);

			return ret;
		}

		public static Vector2F VectorRemap(Vector2F input, Vector2F inStart, Vector2F inEnd, Vector2F outStart, Vector2F outEnd) {
			return new(
				Remap(input.X, inStart.X, inEnd.X, outStart.X, outEnd.X),
				Remap(input.Y, inStart.Y, inEnd.Y, outStart.Y, outEnd.Y)
				);
		}
		public static Vector3 VectorRemap(float input, float inStart, float inEnd, Vector3 outStart, Vector3 outEnd) {
			return new(
				Remap(input, inStart, inEnd, outStart.X, outEnd.X),
				Remap(input, inStart, inEnd, outStart.Y, outEnd.Y),
				Remap(input, inStart, inEnd, outStart.Z, outEnd.Z)
				);
		}
	}
}
