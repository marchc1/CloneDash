using Nucleus.Common.Graphics;
using Raylib_cs;

namespace Nucleus.ManagedMemory;

/// <summary>
/// Temporary interop for game consumers that still issue raw Raylib draw calls... todo: get rid of that!
/// </summary>
public static class TextureInterop
{
	public static Texture2D ToRaylibTexture(this ITexture tex) => new() {
		Id = tex.GetTextureHandle(),
		Width = tex.GetWidth(),
		Height = tex.GetHeight(),
		Format = tex.GetFormat(),
		Mipmaps = tex.GetMipmapCount()
	};
}
