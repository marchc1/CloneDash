using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucleus.Types;

/// <summary>
/// We got ourselves in a sticky situation with backwards compatibility. We switched things in the Vector2F struct to have X, Y be the serialized fields,
/// while x, y used to be the serialized fields with the rest being properties. To handle any possible situation where bad data coming in could cause a default
/// struct, this JSON converter will read all of the permutations of X/Y for deserialization, while only writing X/Y keys during serialization.
/// </summary>
public sealed class Vector2FJsonConverter : JsonConverter<Vector2F>
{
	public override Vector2F ReadJson(JsonReader reader, Type objectType, Vector2F existingValue, bool hasExistingValue, JsonSerializer serializer) {
		if (reader.TokenType == JsonToken.Null)
			throw new JsonSerializationException("Vector2F cannot be null");

		var obj = JObject.Load(reader);

		float? x = null;
		float? y = null;

		foreach (var prop in obj.Properties()) {
			switch (prop.Name) {
				// X aliases
				case "X":
				case "x":
				case "W":
				case "w":
					x ??= prop.Value.Value<float>();
					break;

				// Y aliases
				case "Y":
				case "y":
				case "H":
				case "h":
					y ??= prop.Value.Value<float>();
					break;
			}
		}

		if (x is null || y is null)
			throw new JsonSerializationException("Missing vector component");

		return new Vector2F {
			X = x.Value,
			Y = y.Value
		};
	}

	public override void WriteJson(JsonWriter writer, Vector2F value, JsonSerializer serializer) {
		writer.WriteStartObject();
		writer.WritePropertyName("X");
		writer.WriteValue(value.X);
		writer.WritePropertyName("Y");
		writer.WriteValue(value.Y);
		writer.WriteEndObject();
	}
}



/// <summary>
/// Two-dimesional floating point vector.
/// </summary>
/// 
[StructLayout(LayoutKind.Explicit)]
public struct Vector2F : ISpanFormattable
{
	const int BYTE_OFFSET_X = 0;
	const int BYTE_OFFSET_Y = sizeof(float);
	[FieldOffset(BYTE_OFFSET_X)] public float X;
	[FieldOffset(BYTE_OFFSET_Y)] public float Y;
	[FieldOffset(BYTE_OFFSET_X)] public float x;
	[FieldOffset(BYTE_OFFSET_X)] public float w;
	[FieldOffset(BYTE_OFFSET_X)] public float W;
	[FieldOffset(BYTE_OFFSET_Y)] public float y;
	[FieldOffset(BYTE_OFFSET_Y)] public float h;
	[FieldOffset(BYTE_OFFSET_Y)] public float H;

	public static readonly Vector2F Zero = new(0, 0);
	public static readonly Vector2F One = new(1, 1);
	public static readonly Vector2F Right = new(1, 0);
	public static readonly Vector2F Up = new(0, 1);

	public Vector2F(float X, float Y) { this.x = X; this.y = Y; }
	public Vector2F(float Both) { this.x = Both; this.y = Both; }
	public static Vector2F FromXY(Vector3 xyz) => new(xyz.X, xyz.Y);

	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator +(Vector2F from, float by) => new Vector2F(from.X + by, from.Y + by);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator -(Vector2F from, float by) => new Vector2F(from.X - by, from.Y - by);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator *(Vector2F from, float by) => new Vector2F(from.X * by, from.Y * by);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator /(Vector2F from, float by) => new Vector2F(from.X / by, from.Y / by);

	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator +(float from, Vector2F by) => new Vector2F(from + by.X, from + by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator -(float from, Vector2F by) => new Vector2F(from - by.X, from - by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator *(float from, Vector2F by) => new Vector2F(from * by.X, from * by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator /(float from, Vector2F by) => new Vector2F(from / by.X, from / by.Y);

	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator +(Vector2F from, Vector2F by) => new Vector2F(from.X + by.X, from.Y + by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator -(Vector2F from, Vector2F by) => new Vector2F(from.X - by.X, from.Y - by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator *(Vector2F from, Vector2F by) => new Vector2F(from.X * by.X, from.Y * by.Y);
	[MethodImpl(MethodImplOptions.AggressiveOptimization)] public static Vector2F operator /(Vector2F from, Vector2F by) => new Vector2F(from.X / by.X, from.Y / by.Y);

	public static bool operator <(Vector2F a, Vector2F b) => a.X < b.X || a.Y < b.Y;
	public static bool operator >(Vector2F a, Vector2F b) => a.X > b.X || a.Y > b.Y;
	public static bool operator <=(Vector2F a, Vector2F b) => a.X <= b.X || a.Y <= b.Y;
	public static bool operator >=(Vector2F a, Vector2F b) => a.X >= b.X || a.Y >= b.Y;

	public static Vector2F operator -(Vector2F on) => new Vector2F(-on.X, -on.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool CompareVector2F(Vector2F a, Vector2F b) {
		if (a.X == b.X && a.Y == b.Y)
			return true;
		return false;
	}

	public static bool operator ==(Vector2F a, Vector2F b) => CompareVector2F(a, b);
	public static bool operator !=(Vector2F a, Vector2F b) => !CompareVector2F(a, b);

	public override readonly string ToString() => $"vec2({x}, {y})";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F Round(int digits = 0) => new((float)Math.Round(X, digits), (float)Math.Round(Y, digits));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsZero() => X == 0 && Y == 0;

	public readonly Vector2F DownscaleRatio() {
		if (X == Y) return new(1, 1);
		else if (X > Y) return new(Y / X, 1);
		else return new(1, X / Y);
	}
	public readonly Vector2F UpscaleRatio() {
		if (X == Y) return new(1, 1);
		else if (X > Y) return new(X / Y, 1);
		else return new(1, Y / X);
	}

	public readonly float Distance(Vector2F other) {
		float dx = other.x - x;
		float dy = other.y - y;
		return MathF.Sqrt(dx * dx + dy * dy);
	}

	public readonly bool InRadiusOfCircle(Vector2F focus, float radius) {
		float dx = x - focus.x;
		float dy = y - focus.y;
		return (dx * dx + dy * dy) < (radius * radius);
	}

	[JsonIgnore] public readonly Vector2F XX { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new(X, X); }
	[JsonIgnore] public readonly Vector2F YX { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new(Y, X); }
	[JsonIgnore] public readonly Vector2F YY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new(Y, Y); }


	public readonly float Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => MathF.Sqrt(x * x + y * y); }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F Abs() => new(MathF.Abs(X), MathF.Abs(Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Abs(in Vector2F self) => new(MathF.Abs(self.X), MathF.Abs(self.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Sign(in Vector2F self) => new(MathF.Sign(self.X), MathF.Sign(self.Y));

	/// <summary>
	/// Performs linear interpolation where ratio (0 -> 1) is translated to a -> b
	/// </summary>
	/// <param name="ratio"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Lerp(float ratio, Vector2F a, Vector2F b) {
		return new(
			NMath.Lerp(ratio, a.X, b.X),
			NMath.Lerp(ratio, a.Y, b.Y)
			);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Lerp(Vector2F ratio, Vector2F a, Vector2F b) {
		return new(
			NMath.Lerp(ratio.X, a.X, b.X),
			NMath.Lerp(ratio.Y, a.Y, b.Y)
			);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Remap(float ratio, Vector2F iMi, Vector2F iMa, Vector2F oMi, Vector2F oMa) {
		return new(
			(float)NMath.Remap(ratio, iMi.X, iMa.X, oMi.X, oMa.X),
			(float)NMath.Remap(ratio, iMi.Y, iMa.Y, oMi.Y, oMa.Y)
			);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2F Remap(Vector2F ratio, Vector2F iMi, Vector2F iMa, Vector2F oMi, Vector2F oMa) {
		return new(
			(float)NMath.Remap(ratio.X, iMi.X, iMa.X, oMi.X, oMa.X),
			(float)NMath.Remap(ratio.Y, iMi.Y, iMa.Y, oMi.Y, oMa.Y)
			);
	}

	/// <summary>
	/// Return a normalized <see cref="Vector2F"/> with a <see cref="Length"/> of 1.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F Normalize() {
		float invLen = 1.0f / MathF.Sqrt(x * x + y * y);
		return new(X * invLen, Y * invLen);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Dot(Vector2F a, Vector2F b) => a.X * b.X + a.Y * b.Y;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float Dot(Vector2F other) => Dot(this, other);

	/// <summary>
	/// Rotates a Vector2F around a center by a rotation specified in degrees.
	/// </summary>
	/// <param name="center"></param>
	/// <param name="degrees">Rotation in degrees</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F RotateAroundPoint(Vector2F center, float degrees) {
		float radians = degrees / (180f / MathF.PI);
		float s = MathF.Sin(radians);
		float c = MathF.Cos(radians);

		float px = x - center.x;
		float py = y - center.y;

		float xnew = px * c - py * s;
		float ynew = px * s + py * c;

		return new(xnew + center.x, ynew + center.y);
	}
	/// <summary>
	/// Get the rotation of this point in degrees. Zero means straight Y up, no X.
	/// </summary>
	/// <param name="center"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float GetRotationFromCenter(Vector2F center) {
		var normalized = (this - center).Normalize();

		return 360 - (((MathF.Atan2(normalized.X, normalized.Y).ToDegrees()) + 180) % 360);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool InTriangle(Triangle2D triangle) => triangle.IsPointInTriangle(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool InRing(Vector2F focus, float outerRing, float innerRing) {
		// in the outer ring radius but not in the inner ring radius
		return InRadiusOfCircle(focus, outerRing) && !InRadiusOfCircle(focus, innerRing);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F FitInto(RectangleF cropRect) {
		Vector2F newR = this;

		newR.X = Math.Clamp(newR.X, cropRect.X, cropRect.X + cropRect.W);
		newR.Y = Math.Clamp(newR.Y, cropRect.Y, cropRect.Y + cropRect.H);

		return newR;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TestPointInQuad(Vector2F q1, Vector2F q2, Vector2F q3, Vector2F q4) {
		return InTriangle(new Triangle2D(q1, q2, q3)) || InTriangle(new Triangle2D(q2, q3, q4));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2F Mutate(bool zeroX = false, bool zeroY = false, bool negateX = false, bool negateY = false, bool flip = false) {
		Vector2F ret = new(
			zeroX ? 0 : negateX ? -X : X,
			zeroY ? 0 : negateY ? -Y : Y
			);

		if (flip)
			return new(ret.Y, ret.X);

		return ret;
	}

	public readonly override bool Equals(object? obj) {
		switch (obj) {
			case Vector2 v2: return v2.X == X && v2.Y == Y;
			case Vector2F v2: return v2.X == X && v2.Y == Y;
			default: return false;
		}
	}

	public readonly override int GetHashCode() => HashCode.Combine(X, Y);

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		int charsWrittenThisPass = 0;
		charsWritten = 0;

		if (!X.TryFormat(destination, out charsWrittenThisPass, format, provider)) return false;
		charsWritten += charsWrittenThisPass;
		destination = destination[charsWrittenThisPass..];

		if (!" x ".TryCopyTo(destination)) return false;
		charsWritten += 3;
		destination = destination[3..];

		if (!Y.TryFormat(destination, out charsWrittenThisPass, format, provider)) return false;
		charsWritten += charsWrittenThisPass;
		destination = destination[charsWrittenThisPass..];

		return true;
	}

	public string ToString(string? format, IFormatProvider? formatProvider) {
		throw new NotImplementedException();
	}

	public static Vector2F Floor(Vector2F vec) {
		vec.X = float.Floor(vec.X);
		vec.Y = float.Floor(vec.Y);
		return vec;
	}

	public static Vector2F Ceil(Vector2F vec) {
		vec.X = float.Ceiling(vec.X);
		vec.Y = float.Ceiling(vec.Y);
		return vec;
	}
}
public static class VectorConverters
{
	public static Vector2F ToNucleus(this Vector2 vector) => new(vector.X, vector.Y);
	public static Vector2 ToNumerics(this Vector2F vector) => new(vector.X, vector.Y);
}
