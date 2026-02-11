namespace Nucleus
{
	public static partial class NMath
	{
		/// <summary>
		/// (degrees) / DEG2RAD = (radians)
		/// </summary>
		public const float DEG2RAD = MathF.PI / 180f;
		public const float RAD2DEG = 180f / MathF.PI;

		public static float ToRadians(this float degrees) => degrees * DEG2RAD;
		public static float ToDegrees(this float radians) => radians * RAD2DEG;
	}
}
