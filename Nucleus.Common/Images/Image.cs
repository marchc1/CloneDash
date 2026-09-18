using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using StbImageResizeSharp;
using StbImageSharp;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Nucleus.Common.Images;

/// <summary>
/// Image, pixel data stored in CPU memory (RAM)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct Image : IValidatable
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

	public readonly bool IsValid() => Data != null && Width != 0 && Height != 0;

	// These are ways to load Image's from various sources.
	// These are separated from Raylib now so they can live in common (but are based on Raylib)

	public static Image LoadImage(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID = default) {
		Stream? stream;
		if (pathID.IsEmpty && Path.IsPathFullyQualified(fileName)) {
			string fileNameStr = new(fileName);
			stream = File.Exists(fileNameStr) ? File.OpenRead(fileNameStr) : null;
		}
		else {
			stream = filesystem.Open(pathID.IsEmpty ? "images" : pathID, fileName, FileAccess.Read, FileMode.Open);
		}

		Image image = default;

		if (stream != null) {
			image = LoadImageFromStream(stream);
			stream.Dispose();
		}

		return image;
	}

	public static unsafe Image LoadImageFromMemory(ReadOnlySpan<byte> buffer) {
		fixed (byte* ptr = buffer) {
			using UnmanagedMemoryStream stream = new UnmanagedMemoryStream(ptr, 0, buffer.Length, FileAccess.Read);
			return LoadImageFromStream(stream);
		}
	}

	public static Image LoadImageFromStream(Stream stream) {
		void* ptr = null;
		int width = 0;
		int height = 0;
		int comp = 0;
		int requiredComponents = 0;
		try {
			ptr = StbImage.stbi__load_and_postprocess_8bit(new StbImage.stbi__context(stream), &width, &height, &comp, (int)requiredComponents);
		}
		catch { }

		if (ptr == null)
			return default;

		return new() {
			Data = ptr,
			Width = width,
			Height = height,
			Mipmaps = 1,
			Format = (ColorComponents)comp switch {
				ColorComponents.RedGreenBlue => ImageFormat.R8G8B8,
				ColorComponents.RedGreenBlueAlpha => ImageFormat.R8G8B8A8,
				ColorComponents.Grey => ImageFormat.Grayscale,
				ColorComponents.GreyAlpha => ImageFormat.GrayAlpha,
				_ => throw new NotSupportedException()
			}
		};
	}

	/// <summary>
	/// Does not null the pointer; pass by ref if you can just to be safe...
	/// </summary>
	public static void UnloadImage(Image image) => UnloadImage(ref image);

	public static void UnloadImage(ref Image image) {
		if (image.Data == null) return;

		// I checked just to be sure. This is the function that StbSharp uses
		Marshal.FreeHGlobal((IntPtr)image.Data);

		image = default;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct UNI
	{
		[FieldOffset(0)] public float fm;
		[FieldOffset(0)] public uint ui;
	}

	static float HalfToFloat(ushort x) {
		float result = 0.0f;

		UNI uni = default;

		uint e = (uint)(x & 0x7c00) >> 10;
		uint m = (uint)(x & 0x03ff) << 13;
		uni.fm = (float)m;
		uint v = uni.ui >> 23;
		uni.ui = (uint)(x & 0x8000) << 16
			| (e != 0 ? 1u : 0u) * ((e + 112) << 23 | m)
			| ((e == 0 && m != 0) ? 1u : 0u) * ((v - 37) << 23 | ((m << (int)(150 - v)) & 0x007fe000));

		result = uni.fm;

		return result;
	}

	static ushort FloatToHalf(float x) {
		ushort result = 0;

		UNI uni = default;
		uni.fm = x;

		uint b = uni.ui + 0x00001000;
		uint e = (b & 0x7f800000) >> 23;
		uint m = b & 0x007fffff;

		result = (ushort)(
			  (b & 0x80000000) >> 16
			| (e > 112 ? 1u : 0u) * ((((e - 112) << 10) & 0x7c00) | m >> 13)
			| ((e < 113 && e > 101) ? 1u : 0u) * ((((0x007ff000 + m) >> (int)(125 - e)) + 1) >> 1)
			| (e > 143 ? 1u : 0u) * 0x7fff);

		return result;
	}

	// These are ports of Raylib image generation/manipulation functions.

	public static unsafe Image Copy(Image image) {
		Image newimage = default;

		int width = image.Width;
		int height = image.Height;
		int size = 0;

		for (int i = 0; i < image.Mipmaps; i++) {
			size += image.Format.GetBytesPerPixel() * width * height;

			width /= 2;
			height /= 2;

			if (width < 1) width = 1;
			if (height < 1) height = 1;
		}

		newimage.Data = (void*)Marshal.AllocHGlobal(size);

		if (newimage.Data != null) {
			Buffer.MemoryCopy(image.Data, newimage.Data, size, size);

			newimage.Width = image.Width;
			newimage.Height = image.Height;
			newimage.Mipmaps = image.Mipmaps;
			newimage.Format = image.Format;
		}

		return newimage;
	}

	public static unsafe Vector4* LoadImageDataNormalized(Image image) {
		Vector4* pixels = (Vector4*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(Vector4));

		if (image.Format >= ImageFormat.DXT1_RGB)
			Logs.Warn("LoadImageDataNormalized: Pixel data retrieval not supported for compressed image formats.");
		else {
			for (int i = 0, k = 0; i < image.Width * image.Height; i++) {
				switch (image.Format) {
					case ImageFormat.Grayscale: {
							pixels[i].X = (float)((byte*)image.Data)[i] / 255.0f;
							pixels[i].Y = (float)((byte*)image.Data)[i] / 255.0f;
							pixels[i].Z = (float)((byte*)image.Data)[i] / 255.0f;
							pixels[i].W = 1.0f;

						}
						break;

					case ImageFormat.GrayAlpha: {
							pixels[i].X = (float)((byte*)image.Data)[k] / 255.0f;
							pixels[i].Y = (float)((byte*)image.Data)[k] / 255.0f;
							pixels[i].Z = (float)((byte*)image.Data)[k] / 255.0f;
							pixels[i].W = (float)((byte*)image.Data)[k + 1] / 255.0f;

							k += 2;
						}
						break;
					case ImageFormat.R5G5B5A1: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].X = (float)((pixel & 0b1111100000000000) >> 11) * (1.0f / 31);
							pixels[i].Y = (float)((pixel & 0b0000011111000000) >> 6) * (1.0f / 31);
							pixels[i].Z = (float)((pixel & 0b0000000000111110) >> 1) * (1.0f / 31);
							pixels[i].W = ((pixel & 0b0000000000000001) == 0) ? 0.0f : 1.0f;

						}
						break;
					case ImageFormat.R5G6B5: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].X = (float)((pixel & 0b1111100000000000) >> 11) * (1.0f / 31);
							pixels[i].Y = (float)((pixel & 0b0000011111100000) >> 5) * (1.0f / 63);
							pixels[i].Z = (float)(pixel & 0b0000000000011111) * (1.0f / 31);
							pixels[i].W = 1.0f;

						}
						break;
					case ImageFormat.R4G4B4A4: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].X = (float)((pixel & 0b1111000000000000) >> 12) * (1.0f / 15);
							pixels[i].Y = (float)((pixel & 0b0000111100000000) >> 8) * (1.0f / 15);
							pixels[i].Z = (float)((pixel & 0b0000000011110000) >> 4) * (1.0f / 15);
							pixels[i].W = (float)(pixel & 0b0000000000001111) * (1.0f / 15);

						}
						break;
					case ImageFormat.R8G8B8A8: {
							pixels[i].X = (float)((byte*)image.Data)[k] / 255.0f;
							pixels[i].Y = (float)((byte*)image.Data)[k + 1] / 255.0f;
							pixels[i].Z = (float)((byte*)image.Data)[k + 2] / 255.0f;
							pixels[i].W = (float)((byte*)image.Data)[k + 3] / 255.0f;

							k += 4;
						}
						break;
					case ImageFormat.R8G8B8: {
							pixels[i].X = (float)((byte*)image.Data)[k] / 255.0f;
							pixels[i].Y = (float)((byte*)image.Data)[k + 1] / 255.0f;
							pixels[i].Z = (float)((byte*)image.Data)[k + 2] / 255.0f;
							pixels[i].W = 1.0f;

							k += 3;
						}
						break;
					case ImageFormat.R32: {
							pixels[i].X = ((float*)image.Data)[k];
							pixels[i].Y = 0.0f;
							pixels[i].Z = 0.0f;
							pixels[i].W = 1.0f;

							k += 1;
						}
						break;
					case ImageFormat.R32G32B32: {
							pixels[i].X = ((float*)image.Data)[k];
							pixels[i].Y = ((float*)image.Data)[k + 1];
							pixels[i].Z = ((float*)image.Data)[k + 2];
							pixels[i].W = 1.0f;

							k += 3;
						}
						break;
					case ImageFormat.R32G32B32A32: {
							pixels[i].X = ((float*)image.Data)[k];
							pixels[i].Y = ((float*)image.Data)[k + 1];
							pixels[i].Z = ((float*)image.Data)[k + 2];
							pixels[i].W = ((float*)image.Data)[k + 3];

							k += 4;
						}
						break;
					case ImageFormat.R16: {
							pixels[i].X = HalfToFloat(((ushort*)image.Data)[k]);
							pixels[i].Y = 0.0f;
							pixels[i].Z = 0.0f;
							pixels[i].W = 1.0f;

							k += 1;
						}
						break;
					case ImageFormat.R16G16B16: {
							pixels[i].X = HalfToFloat(((ushort*)image.Data)[k]);
							pixels[i].Y = HalfToFloat(((ushort*)image.Data)[k + 1]);
							pixels[i].Z = HalfToFloat(((ushort*)image.Data)[k + 2]);
							pixels[i].W = 1.0f;

							k += 3;
						}
						break;
					case ImageFormat.R16G16B16A16: {
							pixels[i].X = HalfToFloat(((ushort*)image.Data)[k]);
							pixels[i].Y = HalfToFloat(((ushort*)image.Data)[k + 1]);
							pixels[i].Z = HalfToFloat(((ushort*)image.Data)[k + 2]);
							pixels[i].W = HalfToFloat(((ushort*)image.Data)[k + 3]);

							k += 4;
						}
						break;
					default: break;
				}
			}
		}

		return pixels;
	}


	public static unsafe void Reformat(ref Image image, ImageFormat newFormat) {
		if ((image.Data == null) || (image.Width == 0) || (image.Height == 0)) return;

		if ((newFormat != 0) && (image.Format != newFormat)) {
			if ((image.Format < ImageFormat.DXT1_RGB) && (newFormat < ImageFormat.DXT1_RGB)) {
				Vector4* pixels = LoadImageDataNormalized(image);     // Supports 8 to 32 bit per channel

				Marshal.FreeHGlobal((IntPtr)image.Data);
				image.Data = null;
				image.Format = newFormat;

				switch (image.Format) {
					case ImageFormat.Grayscale: {
							image.Data = (byte*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(byte));

							for (int i = 0; i < image.Width * image.Height; i++) {
								((byte*)image.Data)[i] = (byte)((pixels[i].X * 0.299f + pixels[i].Y * 0.587f + pixels[i].Z * 0.114f) * 255.0f);
							}

						}
						break;

					case ImageFormat.GrayAlpha: {
							image.Data = (byte*)Marshal.AllocHGlobal(image.Width * image.Height * 2 * sizeof(byte));

							for (int i = 0, k = 0; i < image.Width * image.Height * 2; i += 2, k++) {
								((byte*)image.Data)[i] = (byte)((pixels[k].X * 0.299f + (float)pixels[k].Y * 0.587f + (float)pixels[k].Z * 0.114f) * 255.0f);
								((byte*)image.Data)[i + 1] = (byte)(pixels[k].W * 255.0f);
							}

						}
						break;

					case ImageFormat.R5G6B5: {
							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(ushort));

							byte r = 0;
							byte g = 0;
							byte b = 0;

							for (int i = 0; i < image.Width * image.Height; i++) {
								r = (byte)(Math.Round(pixels[i].X * 31.0f));
								g = (byte)(Math.Round(pixels[i].Y * 63.0f));
								b = (byte)(Math.Round(pixels[i].Z * 31.0f));

								((ushort*)image.Data)[i] = (ushort)((ushort)r << 11 | (ushort)g << 5 | (ushort)b);
							}

						}
						break;

					case ImageFormat.R8G8B8: {
							image.Data = (byte*)Marshal.AllocHGlobal(image.Width * image.Height * 3 * sizeof(byte));

							for (int i = 0, k = 0; i < image.Width * image.Height * 3; i += 3, k++) {
								((byte*)image.Data)[i] = (byte)(pixels[k].X * 255.0f);
								((byte*)image.Data)[i + 1] = (byte)(pixels[k].Y * 255.0f);
								((byte*)image.Data)[i + 2] = (byte)(pixels[k].Z * 255.0f);
							}
						}
						break;

					case ImageFormat.R5G5B5A1: {
							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(ushort));

							byte r = 0;
							byte g = 0;
							byte b = 0;
							byte a = 0;

							for (int i = 0; i < image.Width * image.Height; i++) {
								r = (byte)(Math.Round(pixels[i].X * 31.0f));
								g = (byte)(Math.Round(pixels[i].Y * 31.0f));
								b = (byte)(Math.Round(pixels[i].Z * 31.0f));
								const int R5G5B5A1_ALPHA_THRESHOLD = 50;
								a = (byte)((pixels[i].W > ((float)R5G5B5A1_ALPHA_THRESHOLD / 255.0f)) ? 1 : 0);

								((ushort*)image.Data)[i] = (ushort)((ushort)r << 11 | (ushort)g << 6 | (ushort)b << 1 | (ushort)a);
							}

						}
						break;

					case ImageFormat.R4G4B4A4: {
							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(ushort));

							byte r = 0;
							byte g = 0;
							byte b = 0;
							byte a = 0;

							for (int i = 0; i < image.Width * image.Height; i++) {
								r = (byte)(Math.Round(pixels[i].X * 15.0f));
								g = (byte)(Math.Round(pixels[i].Y * 15.0f));
								b = (byte)(Math.Round(pixels[i].Z * 15.0f));
								a = (byte)(Math.Round(pixels[i].W * 15.0f));

								((ushort*)image.Data)[i] = (ushort)((ushort)r << 12 | (ushort)g << 8 | (ushort)b << 4 | (ushort)a);
							}

						}
						break;

					case ImageFormat.R8G8B8A8: {
							image.Data = (byte*)Marshal.AllocHGlobal(image.Width * image.Height * 4 * sizeof(byte));

							for (int i = 0, k = 0; i < image.Width * image.Height * 4; i += 4, k++) {
								((byte*)image.Data)[i] = (byte)(pixels[k].X * 255.0f);
								((byte*)image.Data)[i + 1] = (byte)(pixels[k].Y * 255.0f);
								((byte*)image.Data)[i + 2] = (byte)(pixels[k].Z * 255.0f);
								((byte*)image.Data)[i + 3] = (byte)(pixels[k].W * 255.0f);
							}
						}
						break;

					case ImageFormat.R32: {
							// WARNING: Image is converted to GRAYSCALE equivalent 32bit

							image.Data = (float*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(float));

							for (int i = 0; i < image.Width * image.Height; i++) {
								((float*)image.Data)[i] = (float)(pixels[i].X * 0.299f + pixels[i].Y * 0.587f + pixels[i].Z * 0.114f);
							}
						}
						break;
					case ImageFormat.R32G32B32: {
							image.Data = (float*)Marshal.AllocHGlobal(image.Width * image.Height * 3 * sizeof(float));

							for (int i = 0, k = 0; i < image.Width * image.Height * 3; i += 3, k++) {
								((float*)image.Data)[i] = pixels[k].X;
								((float*)image.Data)[i + 1] = pixels[k].Y;
								((float*)image.Data)[i + 2] = pixels[k].Z;
							}
						}
						break;
					case ImageFormat.R32G32B32A32: {
							image.Data = (float*)Marshal.AllocHGlobal(image.Width * image.Height * 4 * sizeof(float));

							for (int i = 0, k = 0; i < image.Width * image.Height * 4; i += 4, k++) {
								((float*)image.Data)[i] = pixels[k].X;
								((float*)image.Data)[i + 1] = pixels[k].Y;
								((float*)image.Data)[i + 2] = pixels[k].Z;
								((float*)image.Data)[i + 3] = pixels[k].W;
							}
						}
						break;
					case ImageFormat.R16: {
							// WARNING: Image is converted to GRAYSCALE equivalent 16bit

							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(ushort));

							for (int i = 0; i < image.Width * image.Height; i++) {
								((ushort*)image.Data)[i] = FloatToHalf((float)(pixels[i].X * 0.299f + pixels[i].Y * 0.587f + pixels[i].Z * 0.114f));
							}
						}
						break;

					case ImageFormat.R16G16B16: {
							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * 3 * sizeof(ushort));

							for (int i = 0, k = 0; i < image.Width * image.Height * 3; i += 3, k++) {
								((ushort*)image.Data)[i] = FloatToHalf(pixels[k].X);
								((ushort*)image.Data)[i + 1] = FloatToHalf(pixels[k].Y);
								((ushort*)image.Data)[i + 2] = FloatToHalf(pixels[k].Z);
							}
						}
						break;

					case ImageFormat.R16G16B16A16: {
							image.Data = (ushort*)Marshal.AllocHGlobal(image.Width * image.Height * 4 * sizeof(ushort));

							for (int i = 0, k = 0; i < image.Width * image.Height * 4; i += 4, k++) {
								((ushort*)image.Data)[i] = FloatToHalf(pixels[k].X);
								((ushort*)image.Data)[i + 1] = FloatToHalf(pixels[k].Y);
								((ushort*)image.Data)[i + 2] = FloatToHalf(pixels[k].Z);
								((ushort*)image.Data)[i + 3] = FloatToHalf(pixels[k].W);
							}
						}
						break;
					default: break;
				}

				Marshal.FreeHGlobal((IntPtr)pixels);
				pixels = null;

				// In case original image had mipmaps, generate mipmaps for formatted image
				// NOTE: Original mipmaps are replaced by new ones, if custom mipmaps were used, they are lost
				if (image.Mipmaps > 1) {
					image.Mipmaps = 1;
					if (image.Data != null) ImageMipmaps(ref image);
				}
			}
			else Logs.Warn("Image.Reformat: Data format is compressed, can not be converted");
		}
	}

	public static unsafe void ImageMipmaps(ref Image image) {
		if ((image.Data == null) || (image.Width == 0) || (image.Height == 0)) return;

		int mipCount = 1;
		int mipWidth = image.Width;
		int mipHeight = image.Height;
		int mipSize = image.Format.GetBytesPerPixel() * mipWidth * mipHeight;

		// Count mipmap levels required
		while ((mipWidth != 1) || (mipHeight != 1)) {
			if (mipWidth != 1) mipWidth /= 2;
			if (mipHeight != 1) mipHeight /= 2;

			// Security check for NPOT textures
			if (mipWidth < 1) mipWidth = 1;
			if (mipHeight < 1) mipHeight = 1;

			mipCount++;
			mipSize += image.Format.GetBytesPerPixel() * mipWidth * mipHeight;       // Add mipmap size (in bytes)
		}

		if (image.Mipmaps < mipCount) {
			// Create second buffer and copy data manually to it
			void* temp = (void*)Marshal.AllocHGlobal(mipSize);
			int bytesTemp = image.Format.GetBytesPerPixel() * image.Width * image.Height;
			Buffer.MemoryCopy(image.Data, temp, bytesTemp, bytesTemp);
			Marshal.FreeHGlobal((nint)image.Data);
			image.Data = temp;

			// Pointer to allocated memory point where store next mipmap level data
			byte* nextmip = (byte*)image.Data;

			mipWidth = image.Width;
			mipHeight = image.Height;
			mipSize = image.Format.GetBytesPerPixel() * mipWidth * mipHeight;
			Image imCopy = Copy(image);

			for (int i = 1; i < mipCount; i++) {
				nextmip += mipSize;

				mipWidth /= 2;
				mipHeight /= 2;

				// Security check for NPOT textures
				if (mipWidth < 1) mipWidth = 1;
				if (mipHeight < 1) mipHeight = 1;

				mipSize = image.Format.GetBytesPerPixel() * mipWidth * mipHeight;

				if (i < image.Mipmaps) continue;

				ImageResize(ref imCopy, mipWidth, mipHeight); // Uses internally Mitchell cubic downscale filter
				Buffer.MemoryCopy(imCopy.Data, nextmip, mipSize, mipSize);
			}

			UnloadImage(imCopy);

			image.Mipmaps = mipCount;
		}
		else
			Logs.Warn("Mipmaps already available");
	}

	public static unsafe void ImageResize(ref Image image, int newWidth, int newHeight) {
		// Security check to avoid program crash
		if ((image.Data == null) || (image.Width == 0) || (image.Height == 0)) return;

		// Check if a fast path can be used on image scaling
		// It can be for 8 bit per channel images with 1 to 4 channels per pixel
		if ((image.Format == ImageFormat.Grayscale) ||
			(image.Format == ImageFormat.GrayAlpha) ||
			(image.Format == ImageFormat.R8G8B8) ||
			(image.Format == ImageFormat.R8G8B8A8)) {
			int bytesPerPixel = image.Format.GetBytesPerPixel();
			byte* output = (byte*)Marshal.AllocHGlobal(newWidth * newHeight * bytesPerPixel);

			switch (image.Format) {
				case ImageFormat.Grayscale: StbImageResize.stbir_resize_uint8((byte*)image.Data, image.Width, image.Height, 0, output, newWidth, newHeight, 0, 1); break;
				case ImageFormat.GrayAlpha: StbImageResize.stbir_resize_uint8((byte*)image.Data, image.Width, image.Height, 0, output, newWidth, newHeight, 0, 2); break;
				case ImageFormat.R8G8B8: StbImageResize.stbir_resize_uint8((byte*)image.Data, image.Width, image.Height, 0, output, newWidth, newHeight, 0, 3); break;
				case ImageFormat.R8G8B8A8: StbImageResize.stbir_resize_uint8((byte*)image.Data, image.Width, image.Height, 0, output, newWidth, newHeight, 0, 4); break;
				default: break;
			}

			Marshal.FreeHGlobal((nint)image.Data);
			image.Data = output;
			image.Width = newWidth;
			image.Height = newHeight;
		}
		else {
			// Get data as Color pixels array to work with it
			Color* pixels = LoadImageColors(image);
			Color* output = (Color*)Marshal.AllocHGlobal(newWidth * newHeight * sizeof(Color));

			// NOTE: Color data is cast to (byte*), there shouldn't been any problem...
			StbImageResize.stbir_resize_uint8((byte*)pixels, image.Width, image.Height, 0, (byte*)output, newWidth, newHeight, 0, 4);

			ImageFormat format = image.Format;

			UnloadImageColors(pixels);
			Marshal.FreeHGlobal((nint)image.Data);

			image.Data = output;
			image.Width = newWidth;
			image.Height = newHeight;
			image.Format = ImageFormat.R8G8B8A8;

			Image.Reformat(ref image, format);  // Reformat 32bit RGBA image to original format
		}
	}

	public static unsafe Image GenImageColor(int width, int height, Color color) {
		Color* pixels = (Color*)Marshal.AllocHGlobal(width * height * sizeof(Color));

		for (int i = 0; i < width * height; i++) pixels[i] = color;

		Image image = new() {
			Data = pixels,
			Width = width,
			Height = height,
			Mipmaps = 1,
			Format = ImageFormat.R8G8B8A8
		};

		return image;
	}

	static Color* LoadImageColors(Image image) {
		if ((image.Width == 0) || (image.Height == 0)) return null; // Security check

		Color* pixels = (Color*)Marshal.AllocHGlobal(image.Width * image.Height * sizeof(Color));

		if (image.Format >= ImageFormat.DXT1_RGB) Logs.Warn("IMAGE: Pixel data retrieval not supported for compressed image formats");
		else {
			if ((image.Format == ImageFormat.R32) ||
				(image.Format == ImageFormat.R32G32B32) ||
				(image.Format == ImageFormat.R32G32B32A32)) Logs.Warn("IMAGE: Pixel format converted from 32bit to 8bit per channel");

			if ((image.Format == ImageFormat.R16) ||
				(image.Format == ImageFormat.R16G16B16) ||
				(image.Format == ImageFormat.R16G16B16A16)) Logs.Warn("IMAGE: Pixel format converted from 16bit to 8bit per channel");

			for (int i = 0, k = 0; i < image.Width * image.Height; i++) {
				switch (image.Format) {
					case ImageFormat.Grayscale: {
							pixels[i].R = ((byte*)image.Data)[i];
							pixels[i].G = ((byte*)image.Data)[i];
							pixels[i].B = ((byte*)image.Data)[i];
							pixels[i].A = 255;

						}
						break;

					case ImageFormat.GrayAlpha: {
							pixels[i].R = ((byte*)image.Data)[k];
							pixels[i].G = ((byte*)image.Data)[k];
							pixels[i].B = ((byte*)image.Data)[k];
							pixels[i].A = ((byte*)image.Data)[k + 1];

							k += 2;
						}
						break;
					case ImageFormat.R5G5B5A1: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].R = (byte)((float)((pixel & 0b1111100000000000) >> 11) * (255 / 31));
							pixels[i].G = (byte)((float)((pixel & 0b0000011111000000) >> 6) * (255 / 31));
							pixels[i].B = (byte)((float)((pixel & 0b0000000000111110) >> 1) * (255 / 31));
							pixels[i].A = (byte)((pixel & 0b0000000000000001) * 255);

						}
						break;
					case ImageFormat.R5G6B5: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].R = (byte)((float)((pixel & 0b1111100000000000) >> 11) * (255 / 31));
							pixels[i].G = (byte)((float)((pixel & 0b0000011111100000) >> 5) * (255 / 63));
							pixels[i].B = (byte)((float)(pixel & 0b0000000000011111) * (255 / 31));
							pixels[i].A = 255;

						}
						break;
					case ImageFormat.R4G4B4A4: {
							ushort pixel = ((ushort*)image.Data)[i];

							pixels[i].R = (byte)((float)((pixel & 0b1111000000000000) >> 12) * (255 / 15));
							pixels[i].G = (byte)((float)((pixel & 0b0000111100000000) >> 8) * (255 / 15));
							pixels[i].B = (byte)((float)((pixel & 0b0000000011110000) >> 4) * (255 / 15));
							pixels[i].A = (byte)((float)(pixel & 0b0000000000001111) * (255 / 15));

						}
						break;
					case ImageFormat.R8G8B8A8: {
							pixels[i].R = ((byte*)image.Data)[k];
							pixels[i].G = ((byte*)image.Data)[k + 1];
							pixels[i].B = ((byte*)image.Data)[k + 2];
							pixels[i].A = ((byte*)image.Data)[k + 3];

							k += 4;
						}
						break;
					case ImageFormat.R8G8B8: {
							pixels[i].R = (byte)((byte*)image.Data)[k];
							pixels[i].G = (byte)((byte*)image.Data)[k + 1];
							pixels[i].B = (byte)((byte*)image.Data)[k + 2];
							pixels[i].A = 255;

							k += 3;
						}
						break;
					case ImageFormat.R32: {
							pixels[i].R = (byte)(((float*)image.Data)[k] * 255.0f);
							pixels[i].G = 0;
							pixels[i].B = 0;
							pixels[i].A = 255;

							k += 1;
						}
						break;
					case ImageFormat.R32G32B32: {
							pixels[i].R = (byte)(((float*)image.Data)[k] * 255.0f);
							pixels[i].G = (byte)(((float*)image.Data)[k + 1] * 255.0f);
							pixels[i].B = (byte)(((float*)image.Data)[k + 2] * 255.0f);
							pixels[i].A = 255;

							k += 3;
						}
						break;
					case ImageFormat.R32G32B32A32: {
							pixels[i].R = (byte)(((float*)image.Data)[k] * 255.0f);
							pixels[i].G = (byte)(((float*)image.Data)[k + 1] * 255.0f);
							pixels[i].B = (byte)(((float*)image.Data)[k + 2] * 255.0f);
							pixels[i].A = (byte)(((float*)image.Data)[k + 3] * 255.0f);

							k += 4;
						}
						break;
					case ImageFormat.R16: {
							pixels[i].R = (byte)(HalfToFloat(((ushort*)image.Data)[k]) * 255.0f);
							pixels[i].G = 0;
							pixels[i].B = 0;
							pixels[i].A = 255;

							k += 1;
						}
						break;
					case ImageFormat.R16G16B16: {
							pixels[i].R = (byte)(HalfToFloat(((ushort*)image.Data)[k]) * 255.0f);
							pixels[i].G = (byte)(HalfToFloat(((ushort*)image.Data)[k + 1]) * 255.0f);
							pixels[i].B = (byte)(HalfToFloat(((ushort*)image.Data)[k + 2]) * 255.0f);
							pixels[i].A = 255;

							k += 3;
						}
						break;
					case ImageFormat.R16G16B16A16: {
							pixels[i].R = (byte)(HalfToFloat(((ushort*)image.Data)[k]) * 255.0f);
							pixels[i].G = (byte)(HalfToFloat(((ushort*)image.Data)[k + 1]) * 255.0f);
							pixels[i].B = (byte)(HalfToFloat(((ushort*)image.Data)[k + 2]) * 255.0f);
							pixels[i].A = (byte)(HalfToFloat(((ushort*)image.Data)[k + 3]) * 255.0f);

							k += 4;
						}
						break;
					default: break;
				}
			}
		}

		return pixels;
	}

	static void UnloadImageColors(Color* colors) {
		Marshal.FreeHGlobal((nint)colors);
	}
}
