namespace Raylib_cs;
// (todo: fix namespace!)
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
