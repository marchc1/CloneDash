using Nucleus.Common.Graphics;
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
}
