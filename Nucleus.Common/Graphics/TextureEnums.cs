namespace Nucleus.Common.Graphics;

/// <summary>
/// Texture parameters: filter mode<br/>
/// NOTE 1: Filtering considers mipmaps if available in the texture<br/>
/// NOTE 2: Filter is accordingly set for minification and magnification
/// </summary>
public enum TextureFilter
{
	/// <summary>
	/// No filter, just pixel aproximation
	/// </summary>
	Point = 0,

	/// <summary>
	/// Linear filtering
	/// </summary>
	Bilinear,

	/// <summary>
	/// Trilinear filtering (linear with mipmaps)
	/// </summary>
	Trilinear,

	/// <summary>
	/// Anisotropic filtering 4x
	/// </summary>
	Anisotropic4x,

	/// <summary>
	/// Anisotropic filtering 8x
	/// </summary>
	Anisotropic8x,

	/// <summary>
	/// Anisotropic filtering 16x
	/// </summary>
	Anisotropic16x,
}

public enum TextureFilterFunc
{
	/// <summary>
	///  Returns the value of the texture element that is nearest (in Manhattan distance) to the specified texture coordinates. 
	/// </summary>
	Nearest,
	/// <summary>
	///  Returns the weighted average of the four texture elements that are closest to the specified texture coordinates. 
	///  These can include items wrapped or repeated from other parts of a texture, depending on the values of <see cref="TextureWrap"/> S
	///  and T, and on the exact mapping. 
	/// </summary>
	Linear,
	/// <summary>
	///  Chooses the mipmap that most closely matches the size of the pixel being textured and uses the <see cref="Nearest"/> criterion 
	///  (the texture element closest to the specified texture coordinates) to produce a texture value. 
	/// </summary>
	NearestMipmapNearest,
	/// <summary>
	///  Chooses the mipmap that most closely matches the size of the pixel being textured and uses the <see cref="Linear"/> criterion 
	///  (a weighted average of the four texture elements that are closest to the specified texture coordinates) to produce a texture value. 
	/// </summary>
	LinearMipmapNearest,
	/// <summary>
	///  Chooses the two mipmaps that most closely match the size of the pixel being textured and uses the <see cref="Nearest"/> criterion 
	///  (the texture element closest to the specified texture coordinates ) to produce a texture value from each mipmap. 
	///  The final texture value is a weighted average of those two values. 
	/// </summary>
	NearestMipmapLinear,
	/// <summary>
	///  Chooses the two mipmaps that most closely match the size of the pixel being textured and uses the <see cref="Linear"/> criterion 
	///  (a weighted average of the texture elements that are closest to the specified texture coordinates) to produce a texture 
	///  value from each mipmap. The final texture value is a weighted average of those two values. 
	/// </summary>
	LinearMipmapLinear,
}

/// <summary>
/// Texture parameters: wrap mode
/// </summary>
public enum TextureWrap
{
	/// <summary>
	/// Repeats texture in tiled mode
	/// </summary>
	Repeat = 0,

	/// <summary>
	/// Clamps texture to edge pixel in tiled mode
	/// </summary>
	Clamp,

	/// <summary>
	/// Mirrors and repeats the texture in tiled mode
	/// </summary>
	MirrorRepeat,

	/// <summary>
	/// Mirrors and clamps to border the texture in tiled mode
	/// </summary>
	MirrorClamp
}