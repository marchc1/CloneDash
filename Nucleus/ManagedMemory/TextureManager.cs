using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;
using Nucleus.Util;
using Raylib_cs;
using System.Runtime.CompilerServices;

namespace Nucleus.ManagedMemory;

public class TextureManager : ITextureManager
{
	private WeakCollection<ITexture> Textures = [];

	private bool disposedValue;

	internal void EnsureTextureAdded(ITexture tex) {
		Textures.Add(tex);
	}

	internal ITexture CreateTexture(Texture2D underlying, bool selfDisposing = true, Image? cpuImage = null, bool shouldSelfDisposeImage = true) {
		Texture tex = new(this, underlying, selfDisposing, cpuImage, shouldSelfDisposeImage);
		EnsureTextureAdded(tex);
		return tex;
	}

	public ITexture CreateTexture(ReadOnlySpan<byte> encoded, bool retainCPUImage = false) {
		Image image = Image.LoadImageFromMemory(encoded);
		return CreateTextureFromImage(image, retainCPUImage);
	}

	public ITexture CreateTexture(Stream encoded, bool retainCPUImage = false) {
		using var img = new Raylib.ImageRef(encoded);
		return CreateTextureFromImage(img, retainCPUImage);
	}

	private ITexture CreateTextureFromImage(Image image, bool retainCPUImage) {
		Texture2D underlying = Raylib.LoadTextureFromImage(image);
		ITexture tex = CreateTexture(underlying, true, retainCPUImage ? image : null, retainCPUImage);
		if (!retainCPUImage) Image.UnloadImage(image);
		tex.SetFilter(TextureFilter.Bilinear);
		return tex;
	}

	public ITexture CreateTexture(Image image, bool retainCPUImage = false) {
		Texture2D underlying = Raylib.LoadTextureFromImage(image);
		ITexture tex = CreateTexture(underlying, true, retainCPUImage ? image : null, retainCPUImage);
		tex.SetFilter(TextureFilter.Bilinear);
		return tex;
	}

	public ITexture CreateTexture(int width, int height, ImageFormat format) {
		Image image = Image.GenImageColor(width, height, Color.Blank);
		if (image.Format != format) Image.Reformat(ref image, format);
		Texture2D underlying = Raylib.LoadTextureFromImage(image);
		return CreateTexture(underlying, true, image, true);
	}

	public ITexture CreateProcedural(int width, int height, ImageFormat format, ITextureRegenerator generator) {
		Image image = Image.GenImageColor(width, height, Color.Blank);
		if (image.Format != format) Image.Reformat(ref image, format);
		Texture2D underlying = Raylib.LoadTextureFromImage(image);
		Texture tex = new(this, underlying, true, image, true);
		tex.SetTextureRegenerator(generator);
		tex.AddPublicFlags(PublicTextureFlags.Procedural);
		EnsureTextureAdded(tex);
		tex.Download();
		return tex;
	}

	public bool IsValid() => !disposedValue;

	protected virtual void Dispose(bool disposing) {
		if (!disposedValue) {
			lock (Textures) {
				foreach (ITexture t in Textures) {
					t.Dispose();
				}
				disposedValue = true;
			}
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~TextureManager() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private Dictionary<UtlSymId_t, Texture> LoadedTexturesFromFile = [];
	private Dictionary<Texture, UtlSymId_t> LoadedFilesFromTexture = [];

	public ITexture LoadTextureFromFile(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) {
		Span<char> finalPath = stackalloc char[IManagedMemoryUnit.MergePathSize(pathID, path)];
		IManagedMemoryUnit.MergePath(pathID, path, finalPath);
		var managedPath = new UtlSymbol(finalPath);
		if (LoadedTexturesFromFile.TryGetValue(managedPath, out Texture? texFromFile)) return texFromFile;

		Image image = Image.LoadImage(path, pathID);
		Texture2D tex2D = Raylib.LoadTextureFromImage(image);
		Image.UnloadImage(ref image);

		Texture tex = new(this, tex2D, true);
		EnsureTextureAdded(tex);
		tex.GenerateMipmaps();
		tex.SetFilter(TextureFilter.Bilinear);
		LoadedTexturesFromFile.Add(managedPath, tex);
		LoadedFilesFromTexture.Add(tex, managedPath);

		return tex;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ITexture LoadTextureFromFile(ReadOnlySpan<char> filepath, bool localToImages = true) =>
		localToImages ? LoadTextureFromFile("images", filepath)
		: LoadTextureFromFileDisk(filepath);

	private ITexture LoadTextureFromFileDisk(ReadOnlySpan<char> filepath) {
		UtlSymbol filepathSymbol = new UtlSymbol(filepath);
		if (LoadedTexturesFromFile.TryGetValue(filepathSymbol, out Texture? texFromFile)) return texFromFile;

		Texture tex = new(this, Raylib.LoadTexture(new string(filepath) /* << TODO */), true);
		EnsureTextureAdded(tex);
		tex.SetFilter(TextureFilter.Bilinear);

		LoadedTexturesFromFile.Add(filepathSymbol, tex);
		LoadedFilesFromTexture.Add(tex, filepathSymbol);

		return tex;
	}

	internal void EnsureTextureRemoved(ITexture itex) {
		if (itex is Texture tex && LoadedFilesFromTexture.TryGetValue(tex, out var filepath)) {
			LoadedTexturesFromFile.Remove(filepath);
			LoadedFilesFromTexture.Remove(tex);
		}
		Textures.Remove(itex);
	}

	public IRenderTexture CreateRenderTexture(in RenderTextureDesc desc) {
		RenderTexture rt = new(this, desc);
		EnsureTextureAdded(rt);
		return rt;
	}

	public IRenderTexture CreateRenderTexture(int width, int height, int samples = 1, bool depth = true, ImageFormat format = ImageFormat.R8G8B8A8)
		=> CreateRenderTexture(new RenderTextureDesc(width, height, samples, depth, format));

	public ITexture GetErrorTexture() {
		throw new NotImplementedException();
	}

	public WeakCollection<ITexture> GetTextureCollectionForIteration() => Textures;

	public int GetTextureCount() {
		int c = 0;
		for (int i = 0; i < Textures.Count; i++) {
			if (Textures[i] != null)
				c++;
		}
		return c;
	}

	public TextureEnumerator GetTextures() {
		return new(this);
	}

	public ulong GetTotalBits(MemoryRealm realm) {
		ulong ret = 0;

		foreach (var tex in Textures)
			if (tex != null)
				ret += tex.GetUsedBits(realm);

		return ret;
	}
}