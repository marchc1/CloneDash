using Nucleus.Common.Graphics;
using Raylib_cs;

namespace Nucleus.Common.Models;

public record struct AtlasNameIndex(string Name, int Index = -1);

public interface IModelAtlasRegion
{
	ITexture GetTexture();
	IModelAtlasPage GetPage();

	ReadOnlySpan<char> GetName();
	bool SetName(ReadOnlySpan<char> name);
	int GetIndex();
	bool SetIndex(int index);
	AtlasNameIndex GetNameIndex();
	bool SetNameIndex(AtlasNameIndex nameIndex);

	void GetBounds(out int x, out int y, out int w, out int h);
	void GetOffsets(out int x, out int y, out int w, out int h);
	float GetRotation();

	bool SetBounds(int x, int y, int w, int h);
	bool SetOffsets(int x, int y, int w, int h);
	bool SetRotation(float rot);
}

public interface IModelAtlasPage
{
	int GetRegionCount();
	IModelAtlasRegion? GetRegion(int index);
	IModelAtlasRegion? GetRegionByName(ReadOnlySpan<char> name, int index = -1);

	ITexture GetTexture();

	ReadOnlySpan<char> GetName();
	bool SetName(ReadOnlySpan<char> name);

	void GetSize(out int w, out int h);
	ImageFormat GetFormat();
	void GetFilter(out TextureFilter min, out TextureFilter max);
	bool SetFilter(TextureFilter min, TextureFilter max);

	TextureWrap GetWrap();
	bool SetWrap(TextureWrap wrapmode);

	bool GetPreMultipliedAlpha();
	bool SetPreMultipliedAlpha(bool pma);
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
	/// <summary> Set a page texture. This trusts the regions incoming for this page match up. </summary>
	bool SetPageTexture(IModelAtlasPage page, ITexture texture);
	/// <summary> Gets or creates a region on this page for editing directly. </summary>
	IModelAtlasRegion GetOrCreateRegion(IModelAtlasPage page, ReadOnlySpan<char> name, int index = -1);
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
	IModelAtlasRegion? GetRegion(ReadOnlySpan<char> name, int index = -1);
	IModelAtlasPage? GetPage(ReadOnlySpan<char> name);
}

/// <summary>
/// A runtime texture atlas.
/// </summary>
public interface IRuntimeTextureAtlas : ITextureAtlas
{

}

public delegate bool AtlasRegionCheckFn(ReadOnlySpan<char> text);

/// <summary>
/// An editor texture atlas.
/// </summary>
public interface IEditorTextureAtlas : ITextureAtlas
{
	/// <summary> Invalidate the texture atlas the next time an attempt to access it is made </summary>
	void Invalidate();
	void ClearTextures();
	bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<char> filepath);
	bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<byte> cpuImage);
	bool RemoveTexture(ReadOnlySpan<char> name);
	bool RemoveTexturesByName(AtlasRegionCheckFn fn);

	bool Serialize(StringWriter sw);
}