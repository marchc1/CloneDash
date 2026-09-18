using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using System.Runtime.InteropServices;

namespace Nucleus.Common.Images;

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
