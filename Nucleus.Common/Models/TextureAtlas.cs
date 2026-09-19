using Nucleus.Common.Graphics;

namespace Nucleus.Common.Models;

public record struct AtlasNameIndex(string Name, int Index = -1);

public delegate bool AtlasRegionCheckFn(ReadOnlySpan<char> text);

public interface IModelAtlasPage
{
	void GetFilter(out TextureFilter min, out TextureFilter max);

	ImageFormat GetFormat();

	ReadOnlySpan<char> GetName();

	bool GetPreMultipliedAlpha();

	IModelAtlasRegion? GetRegion(int index);

	IModelAtlasRegion? GetRegionByName(ReadOnlySpan<char> name, int index = -1);

	int GetRegionCount();

	void GetSize(out int w, out int h);

	ITexture GetTexture();

	TextureWrap GetWrap();

	bool SetFilter(TextureFilter min, TextureFilter max);

	bool SetName(ReadOnlySpan<char> name);

	bool SetPreMultipliedAlpha(bool pma);

	bool SetWrap(TextureWrap wrapmode);
}

public interface IModelAtlasRegion
{
	void GetBounds(out int x, out int y, out int w, out int h);

	int GetIndex();

	ReadOnlySpan<char> GetName();

	AtlasNameIndex GetNameIndex();

	void GetOffsets(out int x, out int y, out int w, out int h);

	IModelAtlasPage GetPage();

	float GetRotation();

	ITexture GetTexture();

	bool SetBounds(int x, int y, int w, int h);

	bool SetIndex(int index);

	bool SetName(ReadOnlySpan<char> name);

	bool SetNameIndex(AtlasNameIndex nameIndex);

	bool SetOffsets(int x, int y, int w, int h);

	bool SetRotation(float rot);
}

/// <summary>
/// A runtime texture atlas.
/// </summary>
public interface IRuntimeTextureAtlas : ITextureAtlas
{
}

/// <summary>
/// A texture atlas with its basic capabilities.
/// </summary>
public interface ITextureAtlas : IDisposable
{
	/// <summary>
	/// Gets an interface for editing texture atlases. May or may not be available, depending on the implementation.
	/// The implementation also may return itself as the implementor of ITextureAtlasEdit.
	/// </summary>
	ITextureAtlasEdit? Edit();

	IModelAtlasPage? GetPage(ReadOnlySpan<char> name);

	IModelAtlasRegion? GetRegion(ReadOnlySpan<char> name, int index = -1);
}

public interface ITextureAtlasDeserializeResources
{
	Stream? Load(ReadOnlySpan<char> pageName, out ReadOnlySpan<char> ext);
}

/// <summary>
/// Interface for editing texture atlases. May or may not be available, depending on the implementation.
/// </summary>
public interface ITextureAtlasEdit
{
	/// <summary> Clear and deserialize a texture atlas from a string reader. </summary>
	bool Deserialize(StringReader sr, ITextureAtlasDeserializeResources resourceDeserializer);

	/// <summary> Get or create a page for editing directly. </summary>
	IModelAtlasPage GetOrCreatePage(ReadOnlySpan<char> name);

	/// <summary> Gets or creates a region on this page for editing directly. </summary>
	IModelAtlasRegion GetOrCreateRegion(IModelAtlasPage page, ReadOnlySpan<char> name, int index = -1);

	/// <summary> Set a page texture. This trusts the regions incoming for this page match up. </summary>
	bool SetPageTexture(IModelAtlasPage page, ITexture texture);
}

/// <summary>
/// An editor texture atlas.
/// </summary>
public interface IEditorTextureAtlas : ITextureAtlas
{
	bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<char> filepath);

	bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<byte> cpuImage);

	void ClearTextures();

	/// <summary> Invalidate the texture atlas the next time an attempt to access it is made </summary>
	void Invalidate();

	bool RemoveTexture(ReadOnlySpan<char> name);

	bool RemoveTexturesByName(AtlasRegionCheckFn fn);

	bool Serialize(StringWriter sw);
}