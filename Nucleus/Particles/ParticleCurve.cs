using System.Numerics;

namespace Nucleus.Particles;

public readonly record struct ParticleKeyframe(float Time, float Value, float InSlope, float OutSlope);

public sealed class ParticleCurve(ParticleKeyframe[] keys)
{
	public ParticleKeyframe[] Keys { get; } = keys;

	public float Evaluate(float time) {
		var keys = Keys;
		if (keys.Length == 0)
			return 0;
		if (keys.Length == 1 || time <= keys[0].Time)
			return keys[0].Value;
		if (time >= keys[^1].Time)
			return keys[^1].Value;

		int next = 1;
		while (keys[next].Time < time)
			next++;

		var a = keys[next - 1];
		var b = keys[next];
		float dt = b.Time - a.Time;
		if (dt <= 0)
			return b.Value;
		if (!float.IsFinite(a.OutSlope) || !float.IsFinite(b.InSlope))
			return a.Value;

		float s = (time - a.Time) / dt;
		float s2 = s * s, s3 = s2 * s;
		return (2 * s3 - 3 * s2 + 1) * a.Value
			+ (s3 - 2 * s2 + s) * a.OutSlope * dt
			+ (-2 * s3 + 3 * s2) * b.Value
			+ (s3 - s2) * b.InSlope * dt;
	}
}

public enum ParticleRangeMode
{
	Constant,
	Curve,
	RandomBetweenCurves,
	RandomBetweenConstants
}

public sealed class ParticleRange
{
	public ParticleRangeMode Mode;
	public float Max;
	public float Min;
	public ParticleCurve? MaxCurve;
	public ParticleCurve? MinCurve;

	public static ParticleRange Constant(float value) => new() { Mode = ParticleRangeMode.Constant, Max = value, Min = value };

	public static ParticleRange Between(float min, float max) => new() { Mode = ParticleRangeMode.RandomBetweenConstants, Max = max, Min = min };

	public bool IsZero => Mode switch {
		ParticleRangeMode.Constant => Max == 0,
		ParticleRangeMode.RandomBetweenConstants => Max == 0 && Min == 0,
		_ => Max == 0
	};

	public float Evaluate(float time, float random) => Mode switch {
		ParticleRangeMode.Curve => Max * (MaxCurve?.Evaluate(time) ?? 1),
		ParticleRangeMode.RandomBetweenCurves => Max * float.Lerp(MinCurve?.Evaluate(time) ?? 1, MaxCurve?.Evaluate(time) ?? 1, random),
		ParticleRangeMode.RandomBetweenConstants => float.Lerp(Min, Max, random),
		_ => Max
	};

	public ParticleRange Scaled(float scale) => new() {
		Mode = Mode,
		Max = Max * scale,
		Min = Mode == ParticleRangeMode.RandomBetweenConstants ? Min * scale : Min,
		MaxCurve = MaxCurve,
		MinCurve = MinCurve
	};
}

public readonly record struct ParticleColorKey(float Time, Vector3 Color);

public readonly record struct ParticleAlphaKey(float Time, float Alpha);

public sealed class ParticleGradient(ParticleColorKey[] colorKeys, ParticleAlphaKey[] alphaKeys, bool stepped = false)
{
	public ParticleColorKey[] ColorKeys { get; } = colorKeys;
	public ParticleAlphaKey[] AlphaKeys { get; } = alphaKeys;
	public bool Stepped { get; } = stepped;

	public Vector4 Evaluate(float time) => new(EvaluateColor(time), EvaluateAlpha(time));

	Vector3 EvaluateColor(float time) {
		var keys = ColorKeys;
		if (keys.Length == 0)
			return Vector3.One;
		if (time <= keys[0].Time)
			return keys[0].Color;
		for (int i = 1; i < keys.Length; i++) {
			if (time > keys[i].Time)
				continue;
			if (Stepped)
				return keys[i].Color;
			float span = keys[i].Time - keys[i - 1].Time;
			return span <= 0 ? keys[i].Color : Vector3.Lerp(keys[i - 1].Color, keys[i].Color, (time - keys[i - 1].Time) / span);
		}
		return keys[^1].Color;
	}

	float EvaluateAlpha(float time) {
		var keys = AlphaKeys;
		if (keys.Length == 0)
			return 1;
		if (time <= keys[0].Time)
			return keys[0].Alpha;
		for (int i = 1; i < keys.Length; i++) {
			if (time > keys[i].Time)
				continue;
			if (Stepped)
				return keys[i].Alpha;
			float span = keys[i].Time - keys[i - 1].Time;
			return span <= 0 ? keys[i].Alpha : float.Lerp(keys[i - 1].Alpha, keys[i].Alpha, (time - keys[i - 1].Time) / span);
		}
		return keys[^1].Alpha;
	}
}

public enum ParticleColorRangeMode
{
	Color,
	Gradient,
	RandomBetweenColors,
	RandomBetweenGradients,
	RandomColor
}

public sealed class ParticleColorRange
{
	public ParticleColorRangeMode Mode;
	public Vector4 MaxColor = Vector4.One;
	public Vector4 MinColor = Vector4.One;
	public ParticleGradient? MaxGradient;
	public ParticleGradient? MinGradient;

	public static ParticleColorRange Constant(Vector4 color) => new() { Mode = ParticleColorRangeMode.Color, MaxColor = color, MinColor = color };

	public Vector4 Evaluate(float time, float random) => Mode switch {
		ParticleColorRangeMode.Gradient => MaxGradient?.Evaluate(time) ?? Vector4.One,
		ParticleColorRangeMode.RandomBetweenColors => Vector4.Lerp(MinColor, MaxColor, random),
		ParticleColorRangeMode.RandomBetweenGradients => Vector4.Lerp(MinGradient?.Evaluate(time) ?? Vector4.One, MaxGradient?.Evaluate(time) ?? Vector4.One, random),
		ParticleColorRangeMode.RandomColor => MaxGradient?.Evaluate(random) ?? Vector4.One,
		_ => MaxColor
	};
}
