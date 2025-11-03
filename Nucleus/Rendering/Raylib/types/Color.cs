using System;
using System.Runtime.InteropServices;

namespace Raylib_cs;

/// <summary>
/// Color type, RGBA (32bit)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct Color
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    // Example - Color.RED instead of RED
    // Custom raylib color palette for amazing visuals
    public static readonly Color LightGray = new Color(200, 200, 200, 255);
    public static readonly Color Gray = new Color(130, 130, 130, 255);
    public static readonly Color DarkGray = new Color(80, 80, 80, 255);
    public static readonly Color Yellow = new Color(253, 249, 0, 255);
    public static readonly Color Gold = new Color(255, 203, 0, 255);
    public static readonly Color Orange = new Color(255, 161, 0, 255);
    public static readonly Color Pink = new Color(255, 109, 194, 255);
    public static readonly Color Red = new Color(230, 41, 55, 255);
    public static readonly Color Maroon = new Color(190, 33, 55, 255);
    public static readonly Color Green = new Color(0, 228, 48, 255);
    public static readonly Color Lime = new Color(0, 158, 47, 255);
    public static readonly Color DarkGreen = new Color(0, 117, 44, 255);
    public static readonly Color SkyBlue = new Color(102, 191, 255, 255);
    public static readonly Color Blue = new Color(0, 121, 241, 255);
    public static readonly Color DarkBlue = new Color(0, 82, 172, 255);
    public static readonly Color Purple = new Color(200, 122, 255, 255);
    public static readonly Color Violent = new Color(135, 60, 190, 255);
    public static readonly Color DarkPurple = new Color(112, 31, 126, 255);
    public static readonly Color Beige = new Color(211, 176, 131, 255);
    public static readonly Color Brown = new Color(127, 106, 79, 255);
    public static readonly Color DarkBrown = new Color(76, 63, 47, 255);
    public static readonly Color White = new Color(255, 255, 255, 255);
    public static readonly Color Black = new Color(0, 0, 0, 255);
    public static readonly Color Blank = new Color(0, 0, 0, 0);
    public static readonly Color Magenta = new Color(255, 0, 255, 255);

    public Color(byte r, byte g, byte b, byte a)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    public Color(int r, int g, int b, int a)
    {
        this.R = Convert.ToByte(Math.Clamp(r, 0, 255));
        this.G = Convert.ToByte(Math.Clamp(g, 0, 255));
        this.B = Convert.ToByte(Math.Clamp(b, 0, 255));
        this.A = Convert.ToByte(Math.Clamp(a, 0, 255));
    }

	// Helper initializers so you dont have to specify everything
	public Color(byte rgb) {
		this.R = this.G = this.B = rgb;
		this.A = 255;
	}
	public Color(int rgb) {
		this.R = this.G = this.B = Convert.ToByte(rgb);
		this.A = 255;
	}
	public Color(byte rgb, byte a) {
		this.R = this.G = this.B = rgb;
		this.A = a;
	}
	public Color(int rgb, int a) {
		this.R = this.G = this.B = Convert.ToByte(rgb);
		this.A = Convert.ToByte(a);
	}
	public Color(byte r, byte g, byte b) {
		this.R = r;
		this.G = g;
		this.B = b;
		this.A = 255;
	}
	public Color(int r, int g, int b) {
		this.R = Convert.ToByte(r);
		this.G = Convert.ToByte(g);
		this.B = Convert.ToByte(b);
		this.A = 255;
	}

	public override string ToString()
    {
        return $"{{R:{R} G:{G} B:{B} A:{A}}}";
    }
}
