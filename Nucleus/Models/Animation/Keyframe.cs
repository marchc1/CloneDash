using Nucleus.Types;

namespace Nucleus.Models
{
	public struct Keyframe<T>
	{
		public double Time;
		public T Value;
		public KeyframeHandle<T>? LeftHandle;
		public KeyframeHandle<T>? RightHandle;
		public KeyframeInterpolation Interpolation;
		public KeyframeEasing Easing;

		private static T LinearInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (leftmostOfTime) {
				case Keyframe<float> kfL: return rightmostOfTime is Keyframe<float> kfR ? (T)(object)(float)NMath.Remap(time, kfL.Time, kfR.Time, kfL.Value, kfR.Value) : throw new Exception();
				default: return leftmostOfTime.Value;
			}
		}
		private static T BezierInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (leftmostOfTime) {
				case Keyframe<float> kfL:
					Keyframe<float> kfR = (Keyframe<float>)(object)rightmostOfTime;

					NMath.CubicBezier(
						kfL.Time,
						kfL.Value,

						kfL.Time + (kfL.RightHandle?.Time ?? 0),
						kfL.Value + (kfL.RightHandle?.Time ?? 0),

						kfR.Time + (kfR.LeftHandle?.Time ?? 0),
						kfR.Value + (kfR.RightHandle?.Time ?? 0),

						kfR.Time,
						kfR.Value,

						NMath.Remap(time, leftmostOfTime.Time, rightmostOfTime.Time, 0, 1),

						out var rx,
						out var ry
						);

					return (T)(object)(float)ry;

				default: return leftmostOfTime.Value;
			}
		}
		private static T SinusoidalInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T QuadraticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T CubicInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T QuarticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T QuinticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T ExponentialInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T CircularInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T BackInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T BounceInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}
		private static T ElasticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
			switch (default(T)) {
				default: return leftmostOfTime.Value;
			}
		}

		public static T DetermineValue(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime, KeyframeInterpolation? interpolationOverride = null) {
			var interpolation = interpolationOverride ?? leftmostOfTime.Interpolation;
			switch (interpolation) {
				case KeyframeInterpolation.Constant: return leftmostOfTime.Value;
				case KeyframeInterpolation.Linear: return LinearInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Bezier: return BezierInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Sinusoidal: return SinusoidalInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Quadratic: return QuadraticInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Cubic: return CubicInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Quartic: return QuarticInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Quintic: return QuinticInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Exponential: return ExponentialInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Circular: return CircularInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Back: return BackInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Bounce: return BounceInterpolator(time, leftmostOfTime, rightmostOfTime);
				case KeyframeInterpolation.Elastic: return ElasticInterpolator(time, leftmostOfTime, rightmostOfTime);
				default: return leftmostOfTime.Value;
			}
		}
	}
}
