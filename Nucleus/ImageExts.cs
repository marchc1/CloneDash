using Nucleus.Common.Images;
using Nucleus.Common.Types;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus;

public static class ImageExts
{
	extension(in Image image)
	{
		public unsafe Color GetPixelColor(Vector2F pos) {
			// sanity checking
			if (pos.X < 0) return Color.Blank;
			if (pos.Y < 0) return Color.Blank;
			if (pos.X >= image.Width) return Color.Blank;
			if (pos.Y >= image.Height) return Color.Blank;

			var size = Raylib.GetPixelDataSize(image.Width, image.Height, image.Format);
			var sizePerPixel = size / (image.Width * image.Height);
			void* src = (void*)nint.Add((nint)image.Data, sizePerPixel * (((int)pos.Y * image.Width) + (int)pos.X));
			return Raylib.GetPixelColor(src, image.Format);
		}

		public bool IsTransparent(Vector2F pos) => image.GetPixelColor(pos).A <= 0;
	}
}