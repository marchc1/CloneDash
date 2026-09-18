using Nucleus.ManagedMemory;
using Nucleus.Types;

namespace Nucleus.Common.Graphics;

/// <summary>
/// Some flags that can be set on textures by any producer of the texture
/// </summary>
[Flags]
public enum PublicTextureFlags
{
	/// <summary>
	/// Signals that the texture requires flipping the V texture coordinate. This is required for some textures from DirectX textures.
	/// Some methods may not respect this method, its new, so fix if anything isnt respecting this
	/// </summary>
	RequiresFlippedV = 0x01,

	/// <summary>
	/// Marker only: signals that this texture is procedurally generated (it carries an <see cref="ITextureRegenerator"/>).
	/// Nothing acts on this automatically. It exists for informational/debug purposes
	/// </summary>
	Procedural = 0x02,
}

/// <summary>
/// Fills or patches the CPU-side pixels of a procedural texture, invoked by <see cref="ITexture.Download"/>.
/// </summary>
public interface ITextureRegenerator
{
	void Regenerate(ITextureCanvas canvas);
}

/// <summary>
/// A write surface over a texture's CPU-side image, handed to <see cref="ITextureRegenerator.Regenerate"/>.
/// </summary>
public interface ITextureCanvas
{
	int Width { get; }
	int Height { get; }
	ImageFormat Format { get; }

	/// <summary>
	/// The raw CPU-side pixel bytes, laid out per <see cref="Format"/>
	/// </summary>
	Span<byte> Pixels { get; }
}

/// <summary>
/// Describes a render texture to be created.
/// </summary>
public readonly struct RenderTextureDesc
{
	public readonly int Width;
	public readonly int Height;
	public readonly int Samples;
	public readonly bool Depth;
	public readonly ImageFormat ColorFormat;

	public RenderTextureDesc(int width, int height, int samples = 1, bool depth = true, ImageFormat colorFormat = ImageFormat.R8G8B8A8) {
		Width = width;
		Height = height;
		Samples = samples;
		Depth = depth;
		ColorFormat = colorFormat;
	}
}

/// <summary>
/// A render target that is itself an <see cref="ITexture"/>
public interface IRenderTexture : ITexture
{
	/// <summary>Binds the render target so subsequent drawing goes into it.</summary>
	void Begin();

	/// <summary>Unbinds the render target.</summary>
	void End();
}

public interface ITexture : IManagedMemoryUnit
{
	ImageFormat GetFormat();

	int GetWidth();

	int GetHeight();

	RectangleF GetBounds();

	void AddPublicFlags(PublicTextureFlags flags);

	int GetMipmapCount();

	PublicTextureFlags GetPublicFlags();

	uint GetTextureHandle();

	bool HasPublicFlags(PublicTextureFlags flags);

	void RemovePublicFlags(PublicTextureFlags flags);

	TextureFilter GetFilter();

	TextureWrap GetWrap();

	void SetFilter(TextureFilter filter);

	void SetWrap(TextureWrap wrap);

	void GenerateMipmaps();
	
	bool HasCPUImage();

	ITextureRegenerator? GetTextureRegenerator();

	Span<byte> GetPixels();

	void Download();
}
