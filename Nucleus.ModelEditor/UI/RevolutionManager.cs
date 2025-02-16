using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// Manages the revolution around a single point. Allows the rotation/shear gizmos to go past 360 degrees.
	/// </summary>
	public class RevolutionManager {
		private float __initialAngle;
		private float __lastAngle;
		private Vector2F __targetPos;

		private float calculateAngle(Vector2F targetGridPos, Vector2F mouseGridPos) {
			var delta = mouseGridPos - targetGridPos;
			var rotation = MathF.Atan2(delta.Y, delta.X).ToDegrees();

			return rotation;
		}

		/// <summary>
		/// Initializes the revolution manager with the current angle from gridPos -> targetPos.
		/// </summary>
		/// <param name="targetPos"></param>
		/// <param name="gridPos"></param>
		public RevolutionManager(Vector2F targetPos, Vector2F gridPos) {
			__targetPos = targetPos;
			__initialAngle = calculateAngle(targetPos, gridPos);
			__lastAngle = __initialAngle;
		}

		public float CalculateDelta(Vector2F gridPos) {
			var ang = calculateAngle(__targetPos, gridPos);
			var ret = __lastAngle - ang;

			__lastAngle = ang;
			ret += (MathF.Abs(ret) > 330 ? (ret > 0 ? -1 : 1) : 0) * 360;
			return ret;
		}
	}
}
