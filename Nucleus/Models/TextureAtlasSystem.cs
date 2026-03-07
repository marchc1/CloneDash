using Nucleus.Common.Graphics;
using Nucleus.Common.Models;
using Raylib_cs;
using System;

namespace Nucleus.Models;


public class RuntimeModelAtlasRegion : IModelAtlasRegion
{
	internal RuntimeModelAtlasPage Page = null!;
	AtlasNameIndex NameIndex = new("");
	float Rotate;
	int X, Y, W, H;
	int OX, OY, OW, OH;

	public RuntimeModelAtlasRegion(ReadOnlySpan<char> name, int index = -1){
		SetNameIndex(new(new(name.SliceNullTerminatedString()), index));
	}

	public void GetBounds(out int x, out int y, out int w, out int h) {
		x = X;
		y = Y;
		w = W;
		h = H;
	}
	public int GetIndex() => NameIndex.Index;
	public ReadOnlySpan<char> GetName() => NameIndex.Name;
	public AtlasNameIndex GetNameIndex() => NameIndex;
	public void GetOffsets(out int x, out int y, out int w, out int h) {
		x = OX;
		y = OY;
		w = OW;
		h = OH;
	}
	public IModelAtlasPage GetPage() => Page;
	public float GetRotation() => Rotate;
	public ITexture GetTexture() => Page.GetTexture();
	public bool SetBounds(int x, int y, int w, int h) {
		X = x;
		Y = y;
		W = w;
		H = h;
		return true;
	}
	public bool SetIndex(int index) {
		NameIndex.Index = index;
		return true;
	}
	public bool SetName(ReadOnlySpan<char> name) {
		NameIndex.Name = new(name.SliceNullTerminatedString());
		return true;
	}
	public bool SetNameIndex(AtlasNameIndex nameIndex) {
		NameIndex = nameIndex;
		return true;
	}
	public bool SetOffsets(int x, int y, int w, int h) {
		OX = x;
		OY = y;
		OW = w;
		OH = h;
		return true;
	}
	public bool SetRotation(float rot) {
		Rotate = rot;
		return true;
	}
}

public class RuntimeModelAtlasPage : IModelAtlasPage
{
	string Name = "";
	bool PreMultipliedAlpha;
	internal ITexture? Texture;
	internal readonly List<RuntimeModelAtlasRegion> Regions = [];

	internal void AddRegion(RuntimeModelAtlasRegion region) => Regions.Add(region);

	public RuntimeModelAtlasPage(ReadOnlySpan<char> name) {
		SetName(name.SliceNullTerminatedString());
	}

	public int GetRegionCount() {
		return Regions.Count;
	}
	public IModelAtlasRegion? GetRegion(int index) {
		if (index < 0)
			return null;
		if (index >= Regions.Count)
			return null;
		return Regions[index];
	}
	public IModelAtlasRegion? GetRegionByName(ReadOnlySpan<char> name, int index = -1) {
		for (int i = 0, c = Regions.Count; i < c; i++) {
			var region = Regions[i];
			if (region.GetName().Equals(name, StringComparison.InvariantCulture))
				return region;
		}
		return null;
	}
	public ITexture GetTexture() {
		return Texture!;
	}
	public ReadOnlySpan<char> GetName() {
		return Name;
	}
	public bool SetName(ReadOnlySpan<char> name) {
		Name = new(name.SliceNullTerminatedString());
		return true;
	}
	public void GetSize(out int w, out int h) {
		w = Texture?.Width ?? 0;
		h = Texture?.Height ?? 0;
	}
	public ImageFormat GetFormat() {
		return Texture?.Format ?? ImageFormat.None;
	}
	public void GetFilter(out TextureFilter min, out TextureFilter max) {
		min = TextureFilter.Bilinear;
		max = TextureFilter.Bilinear;
		// todo. Need ITexture exposing this
	}
	public bool SetFilter(TextureFilter min, TextureFilter max) {
		// todo. Need ITexture exposing this
		return false;
	}
	public TextureWrap GetWrap() {
		// todo. Need ITexture exposing this
		return default;
	}
	public bool SetWrap(TextureWrap wrapmode) {
		// todo. Need ITexture exposing this
		return false;
	}
	public bool GetPreMultipliedAlpha() {
		return PreMultipliedAlpha;
	}
	public bool SetPreMultipliedAlpha(bool pma) {
		PreMultipliedAlpha = pma;
		return true;
	}
}

public class TextureAtlas : ITextureAtlasEdit, IRuntimeTextureAtlas
{
	readonly List<RuntimeModelAtlasPage> Pages = [];

	public ITextureAtlasEdit? Edit() => this;

	public void Dispose() {
		// todo
	}

	public IModelAtlasPage? GetPage(ReadOnlySpan<char> name) {
		for (int i = 0, c = Pages.Count; i < c; i++) {
			var page = Pages[i];
			if (page.GetName().Equals(name, StringComparison.InvariantCulture))
				return page;
		}
		return null;
	}

	public IModelAtlasRegion? GetRegion(ReadOnlySpan<char> name, int index = -1) {
		for (int i = 0, c = Pages.Count; i < c; i++) {
			var page = Pages[i];
			var region = page.GetRegionByName(name, index);
			if (region != null)
				return region;
		}
		return null;
	}

	public bool Deserialize(StringReader sr, ITextureAtlasDeserializeResources resourceDeserializer) {
		throw new NotImplementedException();
	}

	public IModelAtlasPage GetOrCreatePage(ReadOnlySpan<char> name) {
		RuntimeModelAtlasPage? page = (RuntimeModelAtlasPage?)GetPage(name);
		if(page == null){
			page = new(name);
			Pages.Add(page);
		}
		return page;
	}

	public bool SetPageTexture(IModelAtlasPage page, ITexture texture) {
		throw new NotImplementedException();
	}

	public IModelAtlasRegion GetOrCreateRegion(IModelAtlasPage ipage, ReadOnlySpan<char> name, int index = -1) {
		if (ipage is not RuntimeModelAtlasPage page)
			throw new InvalidCastException("Got an unexpected implementation of IModelAtlasPage here!");

		RuntimeModelAtlasRegion? region = (RuntimeModelAtlasRegion?)page.GetRegionByName(name, index);
		if(region == null){
			region = new(name, index);
			page.AddRegion(region);
		}

		return region;
	}
}