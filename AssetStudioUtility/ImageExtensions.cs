using Nucleus.Common.Images;
using System.IO;

namespace AssetStudio
{
    public static class ImageExtensions
    {
        public static void WriteToStream(this Image image, Stream stream, ImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case ImageFormat.Jpeg:
                    Image.ExportImageToStream(image, stream, ".jpg");
                    break;
                case ImageFormat.Png:
                    Image.ExportImageToStream(image, stream, ".png");
                    break;
                case ImageFormat.Bmp:
                    Image.ExportImageToStream(image, stream, ".bmp");
                    break;
                case ImageFormat.Tga:
                    Image.ExportImageToStream(image, stream, ".tga");
                    break;
            }
        }

        public static MemoryStream ConvertToStream(this Image image, ImageFormat imageFormat)
        {
            var stream = new MemoryStream();
            image.WriteToStream(stream, imageFormat);
            return stream;
        }
    }
}
