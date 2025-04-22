using Nucleus.Types;
using System.Security.Cryptography.X509Certificates;

namespace Nucleus.Models;
 
/// <summary>
/// If you don't know the underlying value type, you can use this interface to have some non-generic
/// reference to a keyframe.
/// </summary>
public interface IKeyframe {
	public double GetTime();
	public object? GetValue();
	public T? GetValue<T>();
	public Type GetValueType();

	/// <summary>
	/// DO NOT USE THIS: USE IFCURVE METHODS
	/// KEYFRAMES WONT BE RECOMPUTED IF YOU USE THIS WITHOUT A RECOMPUTE CALL!!!
	/// </summary>
	/// <param name="time"></param>
	public void SetTime(double time);
	public void SetValue(object? value);
	public void SetValue<T>(T? value);
}

public class Keyframe<T> : IKeyframe
{
	public Keyframe() {
		Time = 0;
		Value = default;
		LeftHandle = new() { HandleType = KeyframeHandleType.AutoClamped };
		RightHandle = new() { HandleType = KeyframeHandleType.AutoClamped };
		Interpolation = KeyframeInterpolation.Bezier;
		Easing = KeyframeEasing.Automatic;
	}

	public Keyframe(double time, T value) {
		Time = time;
		Value = value;
		LeftHandle = new() { HandleType = KeyframeHandleType.AutoClamped };
		RightHandle = new() { HandleType = KeyframeHandleType.AutoClamped };
		Interpolation = KeyframeInterpolation.Bezier;
		Easing = KeyframeEasing.Automatic;
	}

	public Keyframe<T> Copy(double scale = 1) {
		Keyframe<T> copy = new Keyframe<T>();

		copy.Time = Time * scale;
		copy.Value = Value;
		copy.Interpolation = Interpolation;
		copy.Easing = Easing;
		copy.LeftHandle = LeftHandle == null ? null : new() {
			HandleType = LeftHandle.Value.HandleType,
			Time = LeftHandle.Value.Time * scale,
			Value = LeftHandle.Value.Value,
		}; 
		copy.RightHandle = RightHandle == null ? null : new() {
			HandleType = RightHandle.Value.HandleType,
			Time = RightHandle.Value.Time * scale,
			Value = RightHandle.Value.Value,
		};

		return copy;
	}

	public double Time;
	public T Value;
	public KeyframeHandle<T>? LeftHandle;
	public KeyframeHandle<T>? RightHandle;
	public KeyframeInterpolation Interpolation;
	public KeyframeEasing Easing;


	public double GetTime() => Time;
	public object? GetValue() => Value;
	public T2? GetValue<T2>() => Value is T2 tV ? tV : default;
	public Type GetValueType() => typeof(T);

	public void SetTime(double time) => Time = time;
	public void SetValue(object? value) => Value = value is T tV ? tV : (T?)value;
	public void SetValue<T2>(T2? value) => SetValue((object?)value);

	private static T LinearInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		switch (leftmostOfTime) {
			case Keyframe<float> kfL: return rightmostOfTime is Keyframe<float> kfR ? (T)(object)(float)NMath.Remap(time, kfL.Time, kfR.Time, kfL.Value, kfR.Value, true) : throw new Exception();
			default: return leftmostOfTime.Value;
		}
	}

	private static Vector2F CubicBezier(Vector2F p1, Vector2F c2, Vector2F c3, Vector2F p4, float t) {
		// Clamp t between 0 and 1
		t = (float)NMath.Remap(t, p1.X, p4.X, 0, 1);

		// Convert c2 and c3 from relative to absolute positions
		Vector2F p2 = p1 + new Vector2F(c2.X, c2.Y);
		Vector2F p3 = p4 + new Vector2F(c3.X, c3.Y);

		float u = 1 - t;
		float tt = t * t;
		float uu = u * u;
		float uuu = uu * u;
		float ttt = tt * t;

		return uuu * p1
			 + 3 * uu * t * p2
			 + 3 * u * tt * p3
			 + ttt * p4;
	}
	private static Vector2F KeyframeToVector2F(Keyframe<float> kf) => new((float)kf.Time, kf.Value);
	private static Vector2F KeyframeToVector2F(KeyframeHandle<float>? kf) => kf.HasValue ? new((float)kf.Value.Time, kf.Value.Value) : Vector2F.Zero;
	private static T BezierInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		switch (leftmostOfTime) {
			case Keyframe<float> kfL:
				Keyframe<float> kfR = (Keyframe<float>)(object)rightmostOfTime;

				var factor = CubicBezier(
					KeyframeToVector2F(kfL),
					KeyframeToVector2F(kfL.RightHandle),
					KeyframeToVector2F(kfR.LeftHandle),
					KeyframeToVector2F(kfR),
					(float)time
				);

				return (T)(object)factor.Y;
			default: return leftmostOfTime.Value;
		}
	}
	//private static T SinusoidalInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T QuadraticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T CubicInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T QuarticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T QuinticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T ExponentialInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T CircularInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T BackInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T BounceInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}
	//private static T ElasticInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
	//	switch (default(T)) {
	//		default: return leftmostOfTime.Value;
	//	}
	//}

	public static T DetermineValue(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime, KeyframeInterpolation? interpolationOverride = null) {
		var interpolation = interpolationOverride ?? leftmostOfTime.Interpolation;
		switch (interpolation) {
			case KeyframeInterpolation.Constant: return rightmostOfTime.Value;
			case KeyframeInterpolation.Linear: return LinearInterpolator(time, leftmostOfTime, rightmostOfTime);
			case KeyframeInterpolation.Bezier: return BezierInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Sinusoidal: return SinusoidalInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Quadratic: return QuadraticInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Cubic: return CubicInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Quartic: return QuarticInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Quintic: return QuinticInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Exponential: return ExponentialInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Circular: return CircularInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Back: return BackInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Bounce: return BounceInterpolator(time, leftmostOfTime, rightmostOfTime);
			//case KeyframeInterpolation.Elastic: return ElasticInterpolator(time, leftmostOfTime, rightmostOfTime);
			default: return leftmostOfTime.Value;
		}
	}
}
