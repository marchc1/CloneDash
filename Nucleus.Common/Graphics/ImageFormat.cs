namespace Nucleus.Common.Graphics;

public static class ImageFormatExts
{
	extension(ImageFormat format)
	{
		public bool IsCompressed() => format >= ImageFormat.DXT1_RGB;
		public int GetBitsPerPixel() {
			switch (format) {
				case ImageFormat.Grayscale:
					return 8;

				case ImageFormat.GrayAlpha:
					return 16;

				case ImageFormat.R5G6B5:
					return 16;

				case ImageFormat.R8G8B8:
					return 24;

				case ImageFormat.R5G5B5A1:
					return 16;

				case ImageFormat.R4G4B4A4:
					return 16;

				case ImageFormat.R8G8B8A8:
					return 32;

				case ImageFormat.R32:
					return 32;

				case ImageFormat.R32G32B32:
					return 96;

				case ImageFormat.R32G32B32A32:
					return 128;

				case ImageFormat.DXT1_RGB:
					return 4;

				case ImageFormat.DXT1_RGBA:
					return 4;

				case ImageFormat.DXT3_RGBA:
					return 8;

				case ImageFormat.DXT5_RGBA:
					return 8;

				case ImageFormat.ETC1_RGB:
					return 4;

				case ImageFormat.ETC2_RGB:
					return 4;

				case ImageFormat.ETC2_EAC_RGBA:
					return 8;

				case ImageFormat.PVRT_RGB:
					return 4;

				case ImageFormat.PVRT_RGBA:
					return 4;

				case ImageFormat.ASTC_4x4_RGBA:
					return 8;

				case ImageFormat.ASTC_8x8_RGBA:
					return 2;
				default:
					throw new ArgumentOutOfRangeException(nameof(format), format, null);
			}
		}
		public int GetBytesPerPixel() => GetBitsPerPixel(format) >> 3;
	}
}

public enum ImageFormat
{
	None,
	// Uncompressed Raylib formats
	Grayscale = 1,
	GrayAlpha,

	R5G6B5,
	R8G8B8,
	R5G5B5A1,
	R4G4B4A4,
	R8G8B8A8,
	R32,
	R32G32B32,
	R32G32B32A32,
	R16,
	R16G16B16,
	R16G16B16A16,

	// Compressed Raylib formats
	DXT1_RGB,
	DXT1_RGBA,
	DXT3_RGBA,
	DXT5_RGBA,
	ETC1_RGB,
	ETC2_RGB,
	ETC2_EAC_RGBA,
	PVRT_RGB,
	PVRT_RGBA,
	ASTC_4x4_RGBA,
	ASTC_8x8_RGBA,

	// User formats
	// Trying not to break Raylib compatibility here for now.

	BPTC_UNORM_RGBA
}
