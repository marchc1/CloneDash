using Newtonsoft.Json;
using Nucleus.Types;
using System.Runtime.CompilerServices;

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

public static class KeyframeSettings
{
	/// <summary>
	/// Interpolation to use when none is passed, for stepped playback in the editor
	/// </summary>
	[ThreadStatic] public static KeyframeInterpolation? InterpolationOverride;
}

public struct Keyframe<T>
{
	[JsonConstructor]
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

	public readonly Keyframe<T> Copy(double scale = 1) {
		Keyframe<T> copy = new Keyframe<T>();

		copy.Id = Id;
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

	[JsonIgnore] public int Id;
	public double Time;
	public T? Value;
	public KeyframeHandle<T>? LeftHandle;
	public KeyframeHandle<T>? RightHandle;
	public KeyframeInterpolation Interpolation;
	public KeyframeEasing Easing;

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static T? LinearInterpolator(double time, in Keyframe<T> leftmostOfTime, in Keyframe<T> rightmostOfTime) {
		if (typeof(T) == typeof(float)) {
			ref readonly Keyframe<float> left = ref Unsafe.As<Keyframe<T>, Keyframe<float>>(ref Unsafe.AsRef(in leftmostOfTime));
			ref readonly Keyframe<float> right = ref Unsafe.As<Keyframe<T>, Keyframe<float>>(ref Unsafe.AsRef(in rightmostOfTime));
			return (T)(object)(float)NMath.Remap(time, left.Time, right.Time, left.Value, right.Value, true);
		}
		else
			return leftmostOfTime.Value;
	}

	/// <summary>
	/// Evaluates the bezier segment between two float keyframes at <paramref name="time"/>.
	/// Handles are absolute, a missing one is a third of the way to the other key.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static float EvaluateBezier(double time, in Keyframe<float> a, in Keyframe<float> b) {
		double t0 = a.Time, t1 = b.Time;
		double dt = t1 - t0;
		double v0 = a.Value, v1 = b.Value;
		if (dt <= 0) return b.Value;

		double h1t = a.RightHandle?.Time ?? t0 + dt / 3;
		double h1v = a.RightHandle?.Value ?? v0 + (v1 - v0) / 3;
		double h2t = b.LeftHandle?.Time ?? t1 - dt / 3;
		double h2v = b.LeftHandle?.Value ?? v1 - (v1 - v0) / 3;

		// Handle times inside the segment keep the curve from looping back in time
		h1t = Math.Clamp(h1t, t0, t1);
		h2t = Math.Clamp(h2t, t0, t1);

		double x = Math.Clamp(time, t0, t1);
		double lo = 0, hi = 1;
		double s = (x - t0) / dt;
		for (int i = 0; i < 40; i++) {
			double u = 1 - s;
			double bx = u * u * u * t0 + 3 * u * u * s * h1t + 3 * u * s * s * h2t + s * s * s * t1;
			if (Math.Abs(bx - x) < 1e-12) break;
			if (bx < x)
				lo = s;
			else
				hi = s;

			double dx = 3 * u * u * (h1t - t0) + 6 * u * s * (h2t - h1t) + 3 * s * s * (t1 - h2t);
			double next = dx > 1e-12 ? s - (bx - x) / dx : (lo + hi) / 2;
			s = next > lo && next < hi ? next : (lo + hi) / 2;
		}

		double w = 1 - s;
		return (float)(w * w * w * v0 + 3 * w * w * s * h1v + 3 * w * s * s * h2v + s * s * s * v1);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static T? BezierInterpolator(double time, in Keyframe<T> leftmostOfTime, in Keyframe<T> rightmostOfTime) {
		if (typeof(T) == typeof(float))
			return (T)(object)EvaluateBezier(time, in Unsafe.As<Keyframe<T>, Keyframe<float>>(ref Unsafe.AsRef(in leftmostOfTime)), in Unsafe.As<Keyframe<T>, Keyframe<float>>(ref Unsafe.AsRef(in rightmostOfTime)));
		else
			return leftmostOfTime.Value;
	}

	public static T? DetermineValue(double time, in Keyframe<T> leftmostOfTime, in Keyframe<T> rightmostOfTime, KeyframeInterpolation? interpolationOverride = null) {
		if (time < leftmostOfTime.Time)
			return leftmostOfTime.Value;

		if (time >= rightmostOfTime.Time)
			return rightmostOfTime.Value;

		var interpolation = interpolationOverride ?? KeyframeSettings.InterpolationOverride ?? leftmostOfTime.Interpolation;
		switch (interpolation) {
			case KeyframeInterpolation.Constant:
				return leftmostOfTime.Value;

			case KeyframeInterpolation.Linear:
				return LinearInterpolator(time, leftmostOfTime, rightmostOfTime);

			case KeyframeInterpolation.Bezier:
				return BezierInterpolator(time, leftmostOfTime, rightmostOfTime);

			default: return leftmostOfTime.Value;
		}
	}
}