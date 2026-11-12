namespace Nucleus.Models;

/// <summary>
/// A keyframe inside an <see cref="FCurve{T}"/>, found by its id so it survives sorting. Each curve hands out one per key.
/// <br/><b>NOTE:</b> Once the key is removed this keeps the last values it saw.
/// </summary>
public sealed class KeyframeRef<T> : IKeyframe
{
	public readonly FCurve<T> Curve;
	public readonly int Id;
	internal Keyframe<T> Detached;
	internal int IndexHint;

	internal KeyframeRef(FCurve<T> curve, in Keyframe<T> keyframe, int index) {
		Curve = curve;
		Id = keyframe.Id;
		Detached = keyframe;
		IndexHint = index;
	}

	public ref Keyframe<T> Data => ref Curve.FindRef(this);

	public double Time {
		get => Data.Time;
		set => Data.Time = value;
	}

	public T? Value {
		get => Data.Value;
		set => Data.Value = value;
	}

	public KeyframeHandle<T>? LeftHandle {
		get => Data.LeftHandle;
		set => Data.LeftHandle = value;
	}

	public KeyframeHandle<T>? RightHandle {
		get => Data.RightHandle;
		set => Data.RightHandle = value;
	}

	public KeyframeInterpolation Interpolation {
		get => Data.Interpolation;
		set => Data.Interpolation = value;
	}

	public KeyframeEasing Easing {
		get => Data.Easing;
		set => Data.Easing = value;
	}

	public Keyframe<T> Copy(double scale = 1) => Data.Copy(scale);

	public double GetTime() => Time;

	public object? GetValue() => Value;

	public T2? GetValue<T2>() => Value is T2 tV ? tV : default;

	public Type GetValueType() => typeof(T);

	public void SetTime(double time) => Time = time;

	public void SetValue(object? value) => Value = value is T tV ? tV : (T?)value;

	public void SetValue<T2>(T2? value) => SetValue((object?)value);
}
