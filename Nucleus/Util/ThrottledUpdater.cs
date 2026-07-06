using System.Runtime.InteropServices;

namespace Nucleus.Util
{
	[StructLayout(LayoutKind.Auto)]
	public struct ThrottledUpdater(double interval)
	{
		private double accumulator;
		public readonly double Interval = interval;

		/// <summary>
		/// Checks if enough time has passed to trigger an update.
		/// </summary>
		/// <param name="delta">The time elapsed since the last frame.</param>
		/// <returns>True if the interval has been reached.</returns>
		public bool TryUpdate(double delta) {
			accumulator += delta;

			if (accumulator < Interval)
				return false;

			accumulator %= Interval;
			return true;
		}
	}
}