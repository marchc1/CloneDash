using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Models;
using Nucleus.Common.Types;
using Nucleus.Models.Runtime;

namespace Nucleus.Models;

public class RuntimeModelAtlasRegion : IModelAtlasRegion
{
	internal RuntimeModelAtlasPage Page = null!;
	private AtlasNameIndex NameIndex = new("");
	private float Rotate;
	private int X, Y, W, H;
	private int OX, OY, OW, OH;

	public RuntimeModelAtlasRegion(ReadOnlySpan<char> name, int index = -1) {
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
	private string Name = "";
	private bool PreMultipliedAlpha;
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
		w = Texture?.GetWidth() ?? 0;
		h = Texture?.GetHeight() ?? 0;
	}

	public ImageFormat GetFormat() {
		return Texture?.GetFormat() ?? ImageFormat.None;
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
	private readonly List<RuntimeModelAtlasPage> Pages = [];

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

	/// <summary>
	/// Reads the format <see cref="ModelAtlasFiles.Write(PackedAtlas, TextWriter)"/> writes
	/// </summary>
	public bool Deserialize(StringReader sr, ITextureAtlasDeserializeResources resourceDeserializer) {
		if (sr.ReadLine()?.Trim() != ModelAtlasFiles.FORMAT_HEADER)
			return false;

		RuntimeModelAtlasPage? page = null;
		RuntimeModelAtlasRegion? region = null;
		TextureFilterFunc min = TextureFilterFunc.Linear, mag = TextureFilterFunc.Linear;
		TextureWrap wrap = TextureWrap.Clamp;

		string? line;
		while ((line = sr.ReadLine()) != null) {
			if (line.Length == 0) continue;
			if (line[0] != '	') {
				if (line.StartsWith("page ")) {
					if (!FinishPage(resourceDeserializer, page, min, mag, wrap)) return false;
					page = (RuntimeModelAtlasPage)GetOrCreatePage(line.Substring(5));
					region = null;
					min = mag = TextureFilterFunc.Linear;
					wrap = TextureWrap.Clamp;
				}
				else if (line.StartsWith("region ") && page != null)
					region = (RuntimeModelAtlasRegion)GetOrCreateRegion(page, line.Substring(7));
				continue;
			}

			var parts = line.Trim().Split(':', 2);
			if (parts.Length != 2) continue;
			var values = parts[1].Split(',', StringSplitOptions.TrimEntries);
			if (region != null) {
				switch (parts[0]) {
					case "Bounds":
						region.SetBounds(Value(0, values), Value(1, values), Value(2, values), Value(3, values));
						break;
					case "Offsets":
						region.SetOffsets(Value(0, values), Value(1, values), Value(2, values), Value(3, values));
						break;
					case "Rotate":
						region.SetRotation(Value(0, values));
						break;
				}
			}
			else if (page != null) {
				switch (parts[0]) {
					case "Filter":
						Enum.TryParse(values[0], out min);
						if (values.Length > 1)
							Enum.TryParse(values[1], out mag);
						break;
					case "Wrap":
						Enum.TryParse(values[0], out wrap);
						break;
					case "PMA":
						page.SetPreMultipliedAlpha(values[0] == "true");
						break;
				}
			}
		}
		return FinishPage(resourceDeserializer, page, min, mag, wrap);
	}

	private static int Value(int i, string[] values) => i < values.Length && int.TryParse(values[i], out int parsed) ? parsed : 0;

	private bool FinishPage(ITextureAtlasDeserializeResources resourceDeserializer, RuntimeModelAtlasPage? page, TextureFilterFunc min, TextureFilterFunc mag, TextureWrap wrap) {
		if (page == null) return true;
		using var stream = resourceDeserializer.Load(page.GetName(), out _);
		if (stream == null) return false;
		var texture = textures.CreateTexture(stream, true);
		texture.SetFilter(min, mag);
		texture.SetWrap(wrap);
		return SetPageTexture(page, texture);
	}

	public IModelAtlasPage GetOrCreatePage(ReadOnlySpan<char> name) {
		RuntimeModelAtlasPage? page = (RuntimeModelAtlasPage?)GetPage(name);
		if (page == null) {
			page = new(name);
			Pages.Add(page);
		}
		return page;
	}

	public bool SetPageTexture(IModelAtlasPage page, ITexture texture) {
		if (page is not RuntimeModelAtlasPage runtimePage) return false;
		runtimePage.Texture = texture;
		return true;
	}

	public IModelAtlasRegion GetOrCreateRegion(IModelAtlasPage ipage, ReadOnlySpan<char> name, int index = -1) {
		if (ipage is not RuntimeModelAtlasPage page)
			throw new InvalidCastException("Got an unexpected implementation of IModelAtlasPage here!");

		RuntimeModelAtlasRegion? region = (RuntimeModelAtlasRegion?)page.GetRegionByName(name, index);
		if (region == null) {
			region = new(name, index);
			page.AddRegion(region);
		}

		return region;
	}
}

public class AtlasPageResources(Func<string, byte[]?> read) : ITextureAtlasDeserializeResources
{
	public Stream? Load(ReadOnlySpan<char> pageName, out ReadOnlySpan<char> ext) {
		var name = pageName.ToString();
		ext = Path.GetExtension(name);
		var bytes = read(name);
		return bytes == null ? null : new MemoryStream(bytes, false);
	}
}

public static class ModelAtlasFiles
{
	public const string FORMAT_HEADER = "TextureAtlasVersion2";

	public static void Write(PackedAtlas atlas, TextWriter writer) {
		writer.WriteLine(FORMAT_HEADER);
		var settings = atlas.Settings;
		for (int pageIndex = 0; pageIndex < atlas.Pages.Count; pageIndex++) {
			var page = atlas.Pages[pageIndex];
			writer.WriteLine($"page {page.Name}");
			writer.WriteLine($"	Size: {page.Width}, {page.Height}");
			writer.WriteLine($"	Filter: {settings.FilterMin}, {settings.FilterMag}");
			writer.WriteLine($"	Wrap: {settings.Wrap}");
			writer.WriteLine($"	PMA: {(settings.PremultiplyAlpha ? "true" : "false")}");
			foreach (var region in atlas.Regions) {
				if (region.Page != pageIndex) continue;
				writer.WriteLine($"region {region.Name}");
				writer.WriteLine($"	Bounds: {region.X}, {region.Y}, {region.Width}, {region.Height}");
				writer.WriteLine($"	Offsets: {region.OffsetX}, {region.OffsetY}, {region.OriginalWidth}, {region.OriginalHeight}");
				if (region.Rotation != 0)
					writer.WriteLine($"	Rotate: {region.Rotation}");
			}
		}
	}

	/// <summary>
	/// Writes the .texatlas and page images beside <paramref name="modelPath"/>.
	/// </summary>
	public static unsafe void Save(string modelPath, PackedAtlas atlas) {
		using (var writer = new StreamWriter(Path.ChangeExtension(modelPath, ".texatlas")))
			Write(atlas, writer);

		var dir = Path.GetDirectoryName(modelPath) ?? "";
		foreach (var page in atlas.Pages) {
			var image = Image.GenImageColor(page.Width, page.Height, Color.Blank);
			page.Pixels.CopyTo(new Span<byte>(image.Data, page.Pixels.Length));
			bool ok = Raylib_cs.Raylib.ExportImage(image, Path.Combine(dir, page.Name));
			Image.UnloadImage(image);
			if (!ok)
				throw new IOException($"Couldn't write the atlas page '{page.Name}'.");
		}
	}

	/// <summary>
	/// Image names used by the model's region and mesh attachments
	/// </summary>
	public static HashSet<string> UsedImages(ModelData data) {
		var used = new HashSet<string>();
		foreach (var skin in data.Skins) {
			foreach (var attachment in skin.Attachments.Values) {
				switch (attachment) {
					case RegionAttachment region when region.Path != null: used.Add(region.Path); break;
					case MeshAttachment mesh when mesh.Path != null: used.Add(mesh.Path); break;
				}
			}
		}
		return used;
	}

	/// <summary>
	/// Packs the images the model uses and writes them beside <paramref name="modelPath"/>. Only editor atlases can be packed.
	/// </summary>
	/// <returns>The packed atlas, or null without one</returns>
	public static PackedAtlas? SaveNextTo(string modelPath, ModelData data, AtlasPackSettings? settings = null) {
		var atlas = data.TextureAtlas;
		if (atlas == null) return null;
		if (atlas is not EditorTextureAtlas editorAtlas)
			throw new NotSupportedException($"Can't save a {atlas.GetType().Name}, only editor atlases can be packed.");

		var used = UsedImages(data);
		List<(string, Image)> images = [];
		foreach (var pair in editorAtlas.UnpackedImages) {
			if (used.Contains(pair.Key))
				images.Add((pair.Key, pair.Value));
		}
		var packed = AtlasPacker.Pack(images, settings ?? new(), Path.GetFileNameWithoutExtension(modelPath) + ".png");
		Save(modelPath, packed);
		return packed;
	}

	/// <summary>
	/// Builds a runtime atlas from .texatlas text, null without one.
	/// </summary>
	/// <param name="readPage">Reads a page image by file name</param>
	public static TextureAtlas? Load(string? texatlas, Func<string, byte[]?> readPage) {
		if (texatlas == null)
			return null;

		var atlas = new TextureAtlas();
		if (!atlas.Deserialize(new StringReader(texatlas), new AtlasPageResources(readPage)))
			throw new FormatException("Couldn't read the texture atlas.");
		return atlas;
	}
}
