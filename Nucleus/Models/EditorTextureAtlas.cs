using Nucleus.Common.Graphics;
using Nucleus.Common.Models;
using Nucleus.Common.Types;
using Nucleus.ManagedMemory;
using Nucleus.Util;
using Raylib_cs;
using RectpackSharp;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models;

public class EditorAtlasPage : IModelAtlasPage
{
	string Name;
	internal ITexture? Texture;
	internal readonly List<EditorAtlasRegion> Regions = [];

	internal void AddRegion(EditorAtlasRegion region) {
		region.Page = this;
		Regions.Add(region);
	}

	internal void ClearRegions() => Regions.Clear();

	public EditorAtlasPage(string name) {
		Name = name;
	}

	public int GetRegionCount() => Regions.Count;
	public IModelAtlasRegion? GetRegion(int index) {
		if (index < 0 || index >= Regions.Count)
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
	public ITexture GetTexture() => Texture!;
	public ReadOnlySpan<char> GetName() => Name;
	public bool SetName(ReadOnlySpan<char> name) {
		Name = new(name.SliceNullTerminatedString());
		return true;
	}
	public void GetSize(out int w, out int h) {
		w = Texture?.Width ?? 0;
		h = Texture?.Height ?? 0;
	}
	public ImageFormat GetFormat() => Texture?.Format ?? ImageFormat.None;
	public void GetFilter(out TextureFilter min, out TextureFilter max) {
		min = TextureFilter.Bilinear;
		max = TextureFilter.Bilinear;
	}
	public bool SetFilter(TextureFilter min, TextureFilter max) => false;
	public TextureWrap GetWrap() => default;
	public bool SetWrap(TextureWrap wrapmode) => false;
	public bool GetPreMultipliedAlpha() => false;
	public bool SetPreMultipliedAlpha(bool pma) => false;
}

public class EditorAtlasRegion : IModelAtlasRegion
{
	internal EditorAtlasPage Page = null!;
	AtlasNameIndex NameIndex;
	float Rotate;
	int X, Y, W, H;
	int OX, OY, OW, OH;

	public EditorAtlasRegion(string name, int index = -1) {
		NameIndex = new(name, index);
	}

	public void GetBounds(out int x, out int y, out int w, out int h) {
		x = X; y = Y; w = W; h = H;
	}
	public int GetIndex() => NameIndex.Index;
	public ReadOnlySpan<char> GetName() => NameIndex.Name;
	public AtlasNameIndex GetNameIndex() => NameIndex;
	public void GetOffsets(out int x, out int y, out int w, out int h) {
		x = OX; y = OY; w = OW; h = OH;
	}
	public IModelAtlasPage GetPage() => Page;
	public float GetRotation() => Rotate;
	public ITexture GetTexture() => Page.GetTexture();
	public bool SetBounds(int x, int y, int w, int h) {
		X = x; Y = y; W = w; H = h;
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
		OX = x; OY = y; OW = w; OH = h;
		return true;
	}
	public bool SetRotation(float rot) {
		Rotate = rot;
		return true;
	}
}

public class EditorTextureAtlas : IEditorTextureAtlas, IRuntimeTextureAtlas
{
	public bool Debugging { get; set; }

	private Texture? packedTex;
	private Image? packedImg;
	public Dictionary<string, Image> UnpackedImages = [];

	private EditorAtlasPage page;

	private bool valid = false;
	private bool locked = false;
	private bool disposedValue;

	public float AcceptableDensity => .5f;
	public uint StepSize => 1;

	public EditorTextureAtlas() {
		page = new EditorAtlasPage("default");
	}

	public void Invalidate() => valid = false;

	[MemberNotNull(nameof(packedTex))]
	public void Validate() {
		if (valid && packedTex != null) return;

		page.ClearRegions();
		if (packedTex != null)
			packedTex = null;
		if (packedImg != null) {
			Raylib.UnloadImage(packedImg.Value);
			packedImg = null;
		}

		Span<PackingRectangle> rects = stackalloc PackingRectangle[UnpackedImages.Count];
		int i = 0;
		string[] keys = new string[UnpackedImages.Count];
		int additionalPadding = 4;

		foreach (var strTexPair in UnpackedImages) {
			rects[i] = new PackingRectangle(
				0,
				0,
				(uint)(strTexPair.Value.Width + (additionalPadding * 2)),
				(uint)(strTexPair.Value.Height + (additionalPadding * 2)),
				i
			);
			keys[i] = strTexPair.Key;
			i++;
		}

		if (UnpackedImages.Count == 0) {
			Image workingImage = Raylib.GenImageColor(1, 1, Color.Blank);
			packedImg = workingImage;
			var tex = Raylib.LoadTextureFromImage(workingImage);
			Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
			packedTex = new Texture(EngineCore.Level.Textures, tex, true, workingImage, false);
			page.Texture = packedTex;
			valid = true;
			return;
		}

		RectanglePacker.Pack(
			rectangles: rects,
			bounds: out PackingRectangle bounds,
			packingHint: PackingHints.FindBest,
			acceptableDensity: AcceptableDensity,
			stepSize: StepSize,
			maxBoundsWidth: null,
			maxBoundsHeight: null
		);

		int rw = (int)bounds.Width.RoundUpToPowerOf2();
		int rh = (int)bounds.Height.RoundUpToPowerOf2();
		bool testing = Debugging;

		Image workingImg = Raylib.GenImageColor(rw, rh, testing ? Color.LightGray : Color.Blank);

		for (int j = 0; j < rects.Length; j++) {
			var rect = rects[j];
			var key = keys[rect.Id];
			var src = UnpackedImages[key];

			int dx = (int)rect.X + additionalPadding;
			int dy = (int)rect.Y + additionalPadding;

			Raylib.ImageDraw(ref workingImg, src,
				new Rectangle(0, 0, src.Width, src.Height),
				new Rectangle(dx, dy, src.Width, src.Height),
				Color.White);

			var region = new EditorAtlasRegion(key);
			region.SetBounds(dx, dy, src.Width, src.Height);
			page.AddRegion(region);

			if (testing) {
				renderTestData(rect, key, src, ref workingImg);
			}
		}

		packedImg = workingImg;
		var gpuTex = Raylib.LoadTextureFromImage(workingImg);
		Raylib.SetTextureFilter(gpuTex, TextureFilter.Bilinear);
		packedTex = new Texture(EngineCore.Level.Textures, gpuTex, true, workingImg, false);
		page.Texture = packedTex;

		valid = true;
	}

	public void renderTestData(PackingRectangle rect, string key, Image src, ref Image workingImage) {
		Raylib.ImageDrawRectangleLines(ref workingImage,
			new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height),
			1, Color.Red);
	}

	public void Lock() => locked = true;

	public void ClearTextures() {
		if (locked) return;
		foreach (var img in UnpackedImages.Values) {
			Raylib.UnloadImage(img);
		}
		UnpackedImages.Clear();
		page.ClearRegions();
		Invalidate();
	}

	public bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<char> filepath) {
		if (locked) return false;
		string nameStr = new(name);
		string filepathStr = new(filepath);

		if (UnpackedImages.ContainsKey(nameStr)) {
			Raylib.UnloadImage(UnpackedImages[nameStr]);
			UnpackedImages.Remove(nameStr);
		}

		Image cpuImage = Raylib.LoadImage(filepathStr);
		UnpackedImages[nameStr] = cpuImage;
		Invalidate();
		return true;
	}

	public bool AddTexture(ReadOnlySpan<char> name, ReadOnlySpan<byte> cpuImage) {
		return false;
	}

	public bool RemoveTexture(ReadOnlySpan<char> name) {
		if (locked) return false;
		string nameStr = new(name);
		if (UnpackedImages.TryGetValue(nameStr, out var img)) {
			Raylib.UnloadImage(img);
			UnpackedImages.Remove(nameStr);
			Invalidate();
			return true;
		}
		return false;
	}

	public bool RemoveTexturesByName(AtlasRegionCheckFn fn) {
		if (locked) return false;
		List<string> toRemove = [];
		foreach (var key in UnpackedImages.Keys) {
			if (fn(key))
				toRemove.Add(key);
		}
		foreach (var key in toRemove) {
			if (UnpackedImages.TryGetValue(key, out var img))
				Raylib.UnloadImage(img);
			UnpackedImages.Remove(key);
		}
		if (toRemove.Count > 0)
			Invalidate();
		return toRemove.Count > 0;
	}

	public bool Serialize(StringWriter sw) {
		Validate();
		sw.WriteLine("TextureAtlasVersion1");
		for (int i = 0; i < page.Regions.Count; i++) {
			var region = page.Regions[i];
			region.GetBounds(out int x, out int y, out int w, out int h);
			sw.WriteLine(region.GetName().ToString());
			sw.WriteLine($"\tX: {x}");
			sw.WriteLine($"\tY: {y}");
			sw.WriteLine($"\tW: {w}");
			sw.WriteLine($"\tH: {h}");
		}
		return true;
	}

	public ITextureAtlasEdit? Edit() => null; 

	public IModelAtlasRegion? GetRegion(ReadOnlySpan<char> name, int index = -1) {
		Validate();
		return page.GetRegionByName(name, index);
	}

	public IModelAtlasPage? GetPage(ReadOnlySpan<char> name) {
		if (page.GetName().Equals(name, StringComparison.InvariantCulture))
			return page;
		return null;
	}

	public Texture PackedTexture {
		get {
			Validate();
			return packedTex;
		}
	}
	public Image? PackedImage => packedImg;
	protected virtual void Dispose(bool usercall) {
		if (disposedValue) return;

		if (usercall) {
			if (packedImg.HasValue)
				Raylib.UnloadImage(packedImg.Value);
			ClearTextures();
			packedImg = null;
		}
		else MainThread.RunASAP(() => {
			if (packedImg.HasValue)
				Raylib.UnloadImage(packedImg.Value);
			ClearTextures();
			packedImg = null;
		});

		if (IValidatable.IsValid(packedTex))
			packedTex.Dispose();

		disposedValue = true;
	}

	~EditorTextureAtlas() {
		Dispose(usercall: false);
	}

	public void Dispose() {
		Dispose(usercall: true);
		GC.SuppressFinalize(this);
	}
}
