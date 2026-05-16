using Nucleus.ManagedMemory;

namespace Nucleus.Common.Graphics;

/// <summary>
/// Some flags that can be set on textures by any producer of the texture
/// </summary>
public enum PublicTextureFlags
{
	/// <summary>
	/// Signals that the texture requires flipping the V texture coordinate. This is required for some textures from DirectX textures.
	/// Some methods may not respect this method, its new, so fix if anything isnt respecting this
	/// </summary>
	RequiresFlippedV = 0x01
}

public interface ITexture : IManagedMemoryUnit
{
	uint GetTextureHandle();
	int Width { get; }
	int Height { get; }
	ImageFormat Format { get; }
	ulong UsedBits_CPU { get; }

	PublicTextureFlags GetPublicFlags();
	void AddPublicFlags(PublicTextureFlags flags);
	bool HasPublicFlags(PublicTextureFlags flags);
	void RemovePublicFlags(PublicTextureFlags flags);
	int GetMipmapCount();
}
