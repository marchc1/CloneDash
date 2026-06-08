using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using System.Runtime.InteropServices;

namespace Raylib_cs;

/// <summary>
/// Image, pixel data stored in CPU memory (RAM)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct Image
{
	/// <summary>
	/// Image raw data
	/// </summary>
	public void* Data;

	public nint GetDataSemiSafe() => (nint)Data;

	/// <summary>
	/// Image base width
	/// </summary>
	public int Width;

	/// <summary>
	/// Image base height
	/// </summary>
	public int Height;

	/// <summary>
	/// Mipmap levels, 1 by default
	/// </summary>
	public int Mipmaps;

	/// <summary>
	/// Data format (PixelFormat type)
	/// </summary>
	public ImageFormat Format;

	public unsafe Color GetPixelColor(Vector2F pos) {
		// sanity checking
		if (pos.X < 0) return Color.Blank;
		if (pos.Y < 0) return Color.Blank;
		if (pos.X >= Width) return Color.Blank;
		if (pos.Y >= Height) return Color.Blank;

		var size = Raylib.GetPixelDataSize(Width, Height, Format);
		var sizePerPixel = size / (Width * Height);
		void* src = (void*)nint.Add((nint)Data, sizePerPixel * (((int)pos.Y * Width) + (int)pos.X));
		return Raylib.GetPixelColor(src, Format);
	}
	public bool IsTransparent(Vector2F pos) => GetPixelColor(pos).A <= 0;

}
