using System;
using System.IO;
using Nucleus.Common.Images;

namespace AssetStudio
{
    public static class Texture2DExtensions
    {
        public static unsafe Image ConvertToImage(this Texture2D m_Texture2D, bool flip)
        {
            var converter = new Texture2DConverter(m_Texture2D);
            var buff = BigArrayPool<byte>.Shared.Rent(converter.OutputDataSize);
            var spanBuff = buff.AsSpan(0, converter.OutputDataSize);
            try
            {
                if (!converter.DecodeTexture2D(buff))
                    return default;

                var size = converter.UsesSwitchSwizzle ? converter.GetUncroppedSize() : new System.Drawing.Size(m_Texture2D.m_Width, m_Texture2D.m_Height);
                var image = Image.GenImageColor(size.Width, size.Height, default);
                var pixels = new Span<byte>(image.Data, size.Width * size.Height * 4);
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = spanBuff[i + 2];
                    pixels[i + 1] = spanBuff[i + 1];
                    pixels[i + 2] = spanBuff[i];
                    pixels[i + 3] = spanBuff[i + 3];
                }

                if (converter.UsesSwitchSwizzle)
                {
                    Image.ImageCrop(ref image, 0, 0, m_Texture2D.m_Width, m_Texture2D.m_Height);
                }

                if (flip)
                {
                    Image.ImageFlipVertical(ref image);
                }
                return image;
            }
            finally
            {
                BigArrayPool<byte>.Shared.Return(buff, clearArray: true);
            }
        }

        public static MemoryStream ConvertToStream(this Texture2D m_Texture2D, ImageFormat imageFormat, bool flip)
        {
            var image = ConvertToImage(m_Texture2D, flip);
            if (image.IsValid())
            {
                try
                {
                    return image.ConvertToStream(imageFormat);
                }
                finally
                {
                    Image.UnloadImage(ref image);
                }
            }
            return null;
        }
    }
}
