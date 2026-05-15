using Nucleus.Types;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Nucleus.Models;

/// <summary>
/// If you don't know the underlying value type, you can use this interface to have some non-generic
/// reference to a keyframe.
/// </summary>
public interface IKeyframe
{
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
	public T? Value;
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

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static T? LinearInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		if (typeof(T) == typeof(float))
			return (T)(object)(float)NMath.Remap(time, ((Keyframe<float>)(object)leftmostOfTime).Time, ((Keyframe<float>)(object)rightmostOfTime).Time, ((Keyframe<float>)(object)leftmostOfTime).Value, ((Keyframe<float>)(object)rightmostOfTime).Value, true);
		else
			return leftmostOfTime.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	static float CubicBezierYForX(in Vector2F ip1, in Vector2F ic2, in Vector2F ic3, in Vector2F ip4, float targetX, float epsilon = 1e-5f) {
		float tLow = 0f;
		float tHigh = 1f;

		for (int i = 0; i < 17; i++) {
			float tMid = (tLow + tHigh) * 0.5f;
			float u = 1 - tMid;
			float x = u * u * u * 0f + 3f * u * u * tMid * ic2.X + 3f * u * tMid * tMid * ic3.X + tMid * tMid * tMid * 1f;

			if (x < targetX)
				tLow = tMid;
			else
				tHigh = tMid;
		}

		float t = (tLow + tHigh) * 0.5f;
		float u2 = 1 - t;
		return u2 * u2 * u2 * 0f + 3f * u2 * u2 * t * ic2.Y + 3f * u2 * t * t * ic3.Y + t * t * t * 1f;
	}
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] private static Vector2F KeyframeToVector2F(Keyframe<float> kf) => new((float)kf.Time, kf.Value);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] private static Vector2F KeyframeToVector2F(in KeyframeHandle<float>? kf) => kf.HasValue ? new((float)kf.Value.Time, kf.Value.Value) : Vector2F.Zero;
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static T? BezierInterpolator(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		if (typeof(T) == typeof(float)) {
			Keyframe<float> kfL = (Keyframe<float>)(object)leftmostOfTime;
			Keyframe<float> kfR = (Keyframe<float>)(object)rightmostOfTime;

			var factor = CubicBezierYForX(
					KeyframeToVector2F(kfL),
					KeyframeToVector2F(in kfL.RightHandle),
					KeyframeToVector2F(in kfR.LeftHandle),
					KeyframeToVector2F(kfR),
					(float)NMath.Remap(time, kfL.Time, kfR.Time, 0, 1, clampOutput: true)
				);

			return (T)(object)factor;
		}
		else
			return leftmostOfTime.Value;
	}
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static T? BezierInterpolatorLerped(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		if (typeof(T) == typeof(float)) {
			Keyframe<float> kfL = (Keyframe<float>)(object)leftmostOfTime;
			Keyframe<float> kfR = (Keyframe<float>)(object)rightmostOfTime;

			var factor = CubicBezierYForX(
					KeyframeToVector2F(kfL),
					KeyframeToVector2F(in kfL.RightHandle),
					KeyframeToVector2F(in kfR.LeftHandle),
					KeyframeToVector2F(kfR),
					(float)NMath.Remap(time, kfL.Time, kfR.Time, 0, 1, clampOutput: true)
				);

			factor = NMath.Lerp(factor, kfL.Value, kfR.Value);

			return (T)(object)factor;
		}
		else
			return leftmostOfTime.Value;
	}

	static readonly Keyframe<float> DUMMY_LEFT = new();
	static readonly Keyframe<float> DUMMY_RIGHT = new();
	public static float GetPercentage(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime) {
		if (time < leftmostOfTime.Time)
			return 0;

		if (time > rightmostOfTime.Time)
			return 1;

		var interpolation = leftmostOfTime.Interpolation;
		DUMMY_LEFT.SetTime(leftmostOfTime.Time); DUMMY_LEFT.SetValue(0f);
		DUMMY_RIGHT.SetTime(rightmostOfTime.Time); DUMMY_RIGHT.SetValue(1f);
		if (typeof(T) == typeof(float)) {
			DUMMY_LEFT.RightHandle = ((Keyframe<float>)(object)leftmostOfTime).RightHandle;
			DUMMY_RIGHT.LeftHandle = ((Keyframe<float>)(object)rightmostOfTime).RightHandle;
		}
		switch (interpolation) {
			case KeyframeInterpolation.Constant: return 0;
			case KeyframeInterpolation.Linear: return Keyframe<float>.LinearInterpolator(time, DUMMY_LEFT, DUMMY_RIGHT);
			case KeyframeInterpolation.Bezier: return Keyframe<float>.BezierInterpolator(time, DUMMY_LEFT, DUMMY_RIGHT);
			default: return 0;
		}
	}
	public static T? DetermineValue(double time, Keyframe<T> leftmostOfTime, Keyframe<T> rightmostOfTime, KeyframeInterpolation? interpolationOverride = null) {
		if (time < leftmostOfTime.Time)
			return leftmostOfTime.Value;

		if (time > rightmostOfTime.Time)
			return rightmostOfTime.Value;

		var interpolation = interpolationOverride ?? leftmostOfTime.Interpolation;
		switch (interpolation) {
			case KeyframeInterpolation.Constant:
				return leftmostOfTime.Value;
			case KeyframeInterpolation.Linear:
				return LinearInterpolator(time, leftmostOfTime, rightmostOfTime);
			case KeyframeInterpolation.Bezier:
				return BezierInterpolatorLerped(time, leftmostOfTime, rightmostOfTime);
			default: return leftmostOfTime.Value;
		}
	}
}