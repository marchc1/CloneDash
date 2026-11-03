using Nucleus.Common.Images;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace AssetStudio
{
    public enum SpriteMaskMode
    {
        Off,
        On,
        MaskOnly,
        Export
    }

    public static class SpriteHelper
    {
        public static Image GetImage(this Sprite m_Sprite, SpriteMaskMode spriteMaskMode = SpriteMaskMode.On)
        {
            if (m_Sprite.m_SpriteAtlas != null && m_Sprite.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
            {
                if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(m_Sprite.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var m_Texture2D))
                {
                    return CutImage(m_Sprite, m_Texture2D, spriteAtlasData.textureRect, spriteAtlasData.textureRectOffset, spriteAtlasData.downscaleMultiplier, spriteAtlasData.settingsRaw);
                }
            }
            else
            {
                if (m_Sprite.m_RD.texture.TryGet(out var m_Texture2D) && m_Sprite.m_RD.alphaTexture.TryGet(out var m_AlphaTexture2D) && spriteMaskMode != SpriteMaskMode.Off)
                {
                    Image tex = default;
                    if (spriteMaskMode != SpriteMaskMode.MaskOnly)
                    {
                        tex = CutImage(m_Sprite, m_Texture2D, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw);
                    }
                    var alphaTex = CutImage(m_Sprite, m_AlphaTexture2D, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw);

                    switch (spriteMaskMode)
                    {
                        case SpriteMaskMode.On:
                            tex.ApplyRGBMask(alphaTex, isPreview: true);
                            return tex;
                        case SpriteMaskMode.Export:
                            tex.ApplyRGBMask(alphaTex);
                            return tex;
                        case SpriteMaskMode.MaskOnly:
                            return alphaTex;
                    }
                }
                else if (m_Sprite.m_RD.texture.TryGet(out m_Texture2D))
                {
                    return CutImage(m_Sprite, m_Texture2D, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw);
                }
            }
            return default;
        }

        private static unsafe void ApplyRGBMask(this Image tex, Image texMask, bool isPreview = false)
        {
            try
            {
                if (!tex.IsValid() || !texMask.IsValid())
                    return;

                if (tex.Width != texMask.Width || tex.Height != texMask.Height)
                {
                    if (isPreview)
                        Image.ImageResizeNN(ref texMask, tex.Width, tex.Height);
                    else
                        Image.ImageResize(ref texMask, tex.Width, tex.Height);
                }

                var texPixels = new Span<byte>(tex.Data, tex.Width * tex.Height * 4);
                var maskPixels = new ReadOnlySpan<byte>(texMask.Data, texMask.Width * texMask.Height * 4);
                for (int i = 0; i < texPixels.Length; i += 4)
                {
                    var grayscale = (byte)((maskPixels[i] + maskPixels[i + 1] + maskPixels[i + 2]) / 3);
                    texPixels[i + 3] = grayscale;
                }
            }
            finally
            {
                Image.UnloadImage(ref texMask);
            }
        }

        private static Image CutImage(Sprite m_Sprite, Texture2D m_Texture2D, Rectf textureRect, Vector2 textureRectOffset, float downscaleMultiplier, SpriteSettings settingsRaw)
        {
            var spriteImage = m_Texture2D.ConvertToImage(false);
            if (spriteImage.IsValid())
            {
                if (downscaleMultiplier > 0f && downscaleMultiplier != 1f)
                {
                    var width = (int)(m_Texture2D.m_Width / downscaleMultiplier);
                    var height = (int)(m_Texture2D.m_Height / downscaleMultiplier);
                    Image.ImageResize(ref spriteImage, width, height);
                }
                var rectX = (int)MathF.Floor(textureRect.x);
                var rectY = (int)MathF.Floor(textureRect.y);
                var rectRight = (int)MathF.Ceiling(textureRect.x + textureRect.width);
                var rectBottom = (int)MathF.Ceiling(textureRect.y + textureRect.height);
                rectRight = Math.Min(rectRight, spriteImage.Width);
                rectBottom = Math.Min(rectBottom, spriteImage.Height);
                Image.ImageCrop(ref spriteImage, rectX, rectY, rectRight - rectX, rectBottom - rectY);
                if (settingsRaw.packed == 1)
                {
                    //RotateAndFlip
                    switch (settingsRaw.packingRotation)
                    {
                        case SpritePackingRotation.FlipHorizontal:
                            Image.ImageFlipHorizontal(ref spriteImage);
                            break;
                        case SpritePackingRotation.FlipVertical:
                            Image.ImageFlipVertical(ref spriteImage);
                            break;
                        case SpritePackingRotation.Rotate180:
                            Image.ImageFlipHorizontal(ref spriteImage);
                            Image.ImageFlipVertical(ref spriteImage);
                            break;
                        case SpritePackingRotation.Rotate90:
                            Image.ImageRotateCCW(ref spriteImage);
                            break;
                    }
                }

                //Tight
                if (settingsRaw.packingMode == SpritePackingMode.Tight)
                {
                    try
                    {
                        var matrix = Matrix3x2.CreateScale(m_Sprite.m_PixelsToUnits);
                        matrix *= Matrix3x2.CreateTranslation(m_Sprite.m_Rect.width * m_Sprite.m_Pivot.X - textureRectOffset.X, m_Sprite.m_Rect.height * m_Sprite.m_Pivot.Y - textureRectOffset.Y);
                        var triangles = GetTriangles(m_Sprite.m_RD);
                        ClearOutsideTriangles(spriteImage, triangles, matrix);
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"{m_Sprite.m_Name} Unable to render the packed sprite correctly.\n{e}");
                    }
                }

                //Rectangle
                Image.ImageFlipVertical(ref spriteImage);
                return spriteImage;
            }

            return default;
        }

        private static unsafe void ClearOutsideTriangles(Image image, Vector2[][] triangles, Matrix3x2 matrix)
        {
            var inside = new bool[image.Width * image.Height];
            foreach (var triangle in triangles)
            {
                var a = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(triangle[0].X, triangle[0].Y), matrix);
                var b = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(triangle[1].X, triangle[1].Y), matrix);
                var c = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(triangle[2].X, triangle[2].Y), matrix);
                FillTriangle(inside, image.Width, image.Height, a, b, c);
            }

            var pixels = new Span<uint>(image.Data, image.Width * image.Height);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (!inside[i])
                    pixels[i] = 0;
            }
        }

        private static void FillTriangle(bool[] inside, int width, int height, System.Numerics.Vector2 a, System.Numerics.Vector2 b, System.Numerics.Vector2 c)
        {
            var minX = Math.Max((int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))), 0);
            var minY = Math.Max((int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))), 0);
            var maxX = Math.Min((int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))), width - 1);
            var maxY = Math.Min((int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))), height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new System.Numerics.Vector2(x + 0.5f, y + 0.5f);
                    var w0 = Cross(b - a, p - a);
                    var w1 = Cross(c - b, p - b);
                    var w2 = Cross(a - c, p - c);
                    if ((w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0))
                        inside[y * width + x] = true;
                }
            }
        }

        private static float Cross(System.Numerics.Vector2 u, System.Numerics.Vector2 v) => u.X * v.Y - u.Y * v.X;

        private static Vector2[][] GetTriangles(SpriteRenderData m_RD)
        {
            if (m_RD.vertices != null) //5.6 down
            {
                var vertices = m_RD.vertices.Select(x => (Vector2)x.pos).ToArray();
                var triangleCount = m_RD.indices.Length / 3;
                var triangles = new Vector2[triangleCount][];
                for (int i = 0; i < triangleCount; i++)
                {
                    var first = m_RD.indices[i * 3];
                    var second = m_RD.indices[i * 3 + 1];
                    var third = m_RD.indices[i * 3 + 2];
                    var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                    triangles[i] = triangle;
                }
                return triangles;
            }
            else //5.6 and up
            {
                var triangles = new List<Vector2[]>();
                var m_VertexData = m_RD.m_VertexData;
                var m_Channel = m_VertexData.m_Channels[0]; //kShaderChannelVertex
                var m_Stream = m_VertexData.m_Streams[m_Channel.stream];
                using (var vertexReader = new BinaryReader(new MemoryStream(m_VertexData.m_DataSize)))
                {
                    using (var indexReader = new BinaryReader(new MemoryStream(m_RD.m_IndexBuffer)))
                    {
                        foreach (var subMesh in m_RD.m_SubMeshes)
                        {
                            vertexReader.BaseStream.Position = m_Stream.offset + subMesh.firstVertex * m_Stream.stride + m_Channel.offset;

                            var vertices = new Vector2[subMesh.vertexCount];
                            for (int v = 0; v < subMesh.vertexCount; v++)
                            {
                                vertices[v] = vertexReader.ReadVector3();
                                vertexReader.BaseStream.Position += m_Stream.stride - 12;
                            }

                            indexReader.BaseStream.Position = subMesh.firstByte;

                            var triangleCount = subMesh.indexCount / 3u;
                            for (int i = 0; i < triangleCount; i++)
                            {
                                var first = indexReader.ReadUInt16() - subMesh.firstVertex;
                                var second = indexReader.ReadUInt16() - subMesh.firstVertex;
                                var third = indexReader.ReadUInt16() - subMesh.firstVertex;
                                var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                                triangles.Add(triangle);
                            }
                        }
                    }
                }
                return triangles.ToArray();
            }
        }
    }
}
