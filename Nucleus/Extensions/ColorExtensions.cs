using Nucleus.Common.Types;
using Raylib_cs;
using System.Numerics;

namespace Nucleus.Extensions;

public static class ColorExtensions
{
	public static Vector3 RGBubToHSVf(this Color color) {
		return Raylib.ColorToHSV(color);
	}
	public static int RGBubToInt(this Color color) {
		return Raylib.ColorToInt(color);
	}
	public static Color HSVfToRGBub(this Vector3 value, float alpha = 1) {
		var c = Raylib.ColorFromHSV(value.X, value.Y, value.Z);
		c.A = (byte)Math.Clamp(alpha * 255, 0, 255);
		return c;
	}
	public static Color Adjust(this Color color, double hue, double saturation, double value, bool bleed = true) => color.Adjust((float)hue, (float)saturation, (float)value, bleed);
	public static Color Adjust(this Color color, float hue, float saturation, float value, bool bleed = true) {
		var hsv = color.RGBubToHSVf();
		hsv.X += hue;
		hsv.Y *= 1 + saturation;
		hsv.Z *= 1 + value;

		// bleed out saturation with value overlap
		if (bleed)
			hsv.Y -= Math.Max(hsv.Z - 1, 0);

		hsv.Y = Math.Clamp(hsv.Y, 0, 1);
		hsv.Z = Math.Clamp(hsv.Z, 0, 1);

		return hsv.HSVfToRGBub(color.A / 255f);
	}

	public static Vector3 SetHSVf(this Vector3 hsv, float? hue = null, float? saturation = null, float? value = null) {
		hsv.X = hue ?? hsv.X;
		hsv.Y = saturation ?? hsv.Y;
		hsv.Z = value ?? hsv.Z;
		return hsv;
	}

	public static Color FromHSVf(float H, float S, float V) {
		H = H % 360;

		if (V <= 0)
			return new Color(0, 0, 0, 255);

		if (S <= 0) {
			var nv = (int)Math.Clamp(V * 255, 0, 255);
			return new Color(nv, nv, nv, 255);
		}

		float R = 0, G = 0, B = 0;

		float hF = H / 60f;
		int i = (int)Math.Floor(hF);
		float f = hF - i;
		float pv = V * (1 - S);
		float qv = V * (1 - S * f);
		float tv = V * (1 - S * (1 - f));

		switch (i) {
			case 0:
			case 6:
				R = V;
				G = tv;
				B = pv;
				break;
			case 1:
				R = qv;
				G = V;
				B = pv;
				break;
			case 2:
				R = pv;
				G = V;
				B = tv;
				break;
			case 3:
				R = pv;
				G = qv;
				B = V;
				break;
			case 4:
				R = tv;
				G = pv;
				B = V;
				break;
			case 5:
			case -1:
				R = V;
				G = pv;
				B = qv;
				break;

			default:
				throw new Exception($"Wtf? HSV conversion failure? I = {i}, HSV = {H} {S} {V}");
		}

		return new Color((int)Math.Clamp(R * 255, 0, 255), (int)Math.Clamp(G * 255, 0, 255), (int)Math.Clamp(B * 255, 0, 255), 255);
	}

	public static bool TryParseHexToColor(this ReadOnlySpan<char> hex, out Color color, out ReadOnlySpan<char> error) {
		color = default;
		error = default;

		if (hex.Length == 0) {
			error = "Expected string with length greater than or equal to 6 characters";
			return false;
		}

		if (hex[0] == '#') hex = hex[1..];

		if (hex.Length < 6){
			error = "Expected string with length greater than or equal to 6 characters";
			return false;
		}

		ReadOnlySpan<char> rS = hex.Slice(0, 2);
		ReadOnlySpan<char> gS = hex.Slice(2, 2);
		ReadOnlySpan<char> bS = hex.Slice(4, 2);
		ReadOnlySpan<char> aS = "";
		if (hex.Length >= 8)
			aS = hex.Slice(6, 2);

		if (!int.TryParse(rS, System.Globalization.NumberStyles.HexNumber, null, out int r)) { error = "Hexadecimal number for red channel was not in the expected format (hexadecimal two-letter)"; return false; }
		if (!int.TryParse(gS, System.Globalization.NumberStyles.HexNumber, null, out int g)) { error = "Hexadecimal number for green channel was not in the expected format (hexadecimal two-letter)"; return false; }
		if (!int.TryParse(bS, System.Globalization.NumberStyles.HexNumber, null, out int b)) { error = "Hexadecimal number for blue channel was not in the expected format (hexadecimal two-letter)"; return false; }

		int a;
		if (aS.Length == 0 || !int.TryParse(aS, System.Globalization.NumberStyles.HexNumber, null, out a))
			a = 255;

		color = new Color(r, g, b, a);
		return true;
	}

	public static ReadOnlySpan<char> ToHex(this Color color, bool includeAlpha) {
		string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
		if (includeAlpha)
			hex += $"{color.A:X2}";

		return hex;
	}

	public static bool ToHex(this Color color, bool includeAlpha, Span<char> writeBuffer) {
		int targetLen = includeAlpha ? 9 : 7;
		if (writeBuffer.Length < targetLen) return false;

		writeBuffer[0] = '#';
		if (!color.R.TryFormat(writeBuffer[1..], out _, "X2")) return false;
		if (!color.G.TryFormat(writeBuffer[3..], out _, "X2")) return false;
		if (!color.B.TryFormat(writeBuffer[5..], out _, "X2")) return false;
		if (includeAlpha)
			if (!color.A.TryFormat(writeBuffer[7..], out _, "X2")) return false;

		if (writeBuffer.Length > targetLen)
			writeBuffer[targetLen] = '\0';

		return true;
	}
}
