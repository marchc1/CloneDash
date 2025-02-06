using Raylib_cs;
using System.Numerics;

namespace Nucleus
{
    public static class ColorExtensions
    {
        public static Vector3 ToHSV(this Color color) {
            return Raylib.ColorToHSV(color);
        }
        public static int ToInt(this Color color) {
            return Raylib.ColorToInt(color);
        }
        public static Color ToRGB(this Vector3 value, float alpha = 1) {
            var c = Raylib.ColorFromHSV(value.X, value.Y, value.Z);
            c.A = (byte)Math.Clamp(alpha * 255, 0, 255);
            return c;
        }
        public static Color Adjust(this Color color, double hue, double saturation, double value) => Adjust(color, (float)hue, (float)saturation, (float)value);
        public static Color Adjust(this Color color, float hue, float saturation, float value) {
            var hsv = color.ToHSV();
            hsv.X += hue;
            hsv.Y *= 1 + saturation;
            hsv.Z *= 1 + value;

			hsv.Y = Math.Clamp(hsv.Y, 0, 1);
			hsv.Z = Math.Clamp(hsv.Z, 0, 1);

            return hsv.ToRGB((float)color.A / 255f);
        }

        public static Vector3 SetHSV(this Vector3 hsv, float? hue = null, float? saturation = null, float? value = null) {
            hsv.X = hue ?? hsv.X;
            hsv.Y = saturation ?? hsv.Y;
            hsv.Z = value ?? hsv.Z;
            return hsv;
        }

        public static Color FromHSV(float H, float S, float V) {
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

        public static Color FromHexRGB(string hex, int alpha = 255) {
            if (hex.Length != 6 && (hex.Length == 7 && hex[0] != '#'))
                throw new Exception("Bad hex argument (expected six-character string OR seven-character with # at the start");

            if (hex[0] == '#')
                hex = hex.Substring(1);

            string rS = hex.Substring(0, 2);
            string gS = hex.Substring(2, 2);
            string bS = hex.Substring(4, 2);

            return new Color(
                int.Parse(rS, System.Globalization.NumberStyles.HexNumber),
                int.Parse(gS, System.Globalization.NumberStyles.HexNumber),
                int.Parse(bS, System.Globalization.NumberStyles.HexNumber),
                alpha
            );
        }

		public static bool TryParseHexToColor(this string hex, out Color col) {
			col = default;

			if (hex.Length < 6)
				return false;

			if (hex[0] == '#')
				hex = hex.Substring(1);

			string rS = hex.Substring(0, 2);
			string gS = hex.Substring(2, 2);
			string bS = hex.Substring(4, 2);
			string aS = "FF";
			if (hex.Length == 8)
				aS = hex.Substring(6, 2);

			col = new Color(
				int.Parse(rS, System.Globalization.NumberStyles.HexNumber),
				int.Parse(gS, System.Globalization.NumberStyles.HexNumber),
				int.Parse(bS, System.Globalization.NumberStyles.HexNumber),
				int.Parse(aS, System.Globalization.NumberStyles.HexNumber)
			);
			return true;
		}
		public static string ToHex(this Color color, bool includeAlpha) {
			string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
			if (includeAlpha)
				hex += $"{color.A:X2}";

			return hex;
		}
    }
}
