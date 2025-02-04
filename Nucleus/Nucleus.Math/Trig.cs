namespace Nucleus
{
	public static partial class NMath
	{
		/// <summary>
		/// (degrees) / DEG2RAD = (radians)
		/// </summary>
		public const float DEG2RAD = 180f / 3.141592653589793f;
		public const float RAD2DEG = 3.141592653589793f / 180f;

		public static float ToRadians(float degrees) => degrees * RAD2DEG;
		public static float ToDegrees(float radians) => radians * DEG2RAD;
	}
}
