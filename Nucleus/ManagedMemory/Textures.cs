using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;
using Nucleus.Files;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;
using System.Runtime.CompilerServices;

namespace Nucleus.ManagedMemory
{
	public class Texture : ITexture
	{
		private readonly TextureManagement? parent;
		private Texture2D underlying;
		private readonly bool selfDisposing;
		private Image? underlyingImage;
		private readonly bool shouldSelfDisposeImage;

		internal Texture(TextureManagement? parent, Texture2D underlying, bool selfDisposing = true, Image? underlyingImage = null, bool shouldSelfDisposeImage = true) {
			this.parent = parent;
			this.underlying = underlying;
			this.selfDisposing = selfDisposing;
			this.underlyingImage = underlyingImage;
			this.shouldSelfDisposeImage = shouldSelfDisposeImage;
		}

		// Unmanaged missing texture; should not be freed...
		public static readonly Texture MISSING = new Texture(null, Filesystem.ReadTexture("images", "missing_texture.png"), false);

		public RectangleF GetBounds() {
			return RectangleF.XYWH(0, 0, GetWidth(), GetHeight());
		}

		public uint GetTextureHandle() => underlying.Id;

		public string? DebugName { get; set; }
		public int GetWidth() {
			return underlying.Width;
		}

		public int GetHeight() {
			return underlying.Height;
		}

		public uint UWidth => (uint)underlying.Width;
		public uint UHeight => (uint)underlying.Height;
		public ImageFormat GetFormat() {
			return underlying.Format;
		}

		public int GetMipmapCount() => underlying.Mipmaps;

		private PublicTextureFlags publicFlags;

		public void AddPublicFlags(PublicTextureFlags flags) => publicFlags |= flags;

		public void RemovePublicFlags(PublicTextureFlags flags) => publicFlags &= ~flags;

		public bool HasPublicFlags(PublicTextureFlags flags) => (publicFlags & flags) != 0;

		public PublicTextureFlags GetPublicFlags() => publicFlags;

		private Image? UnderlyingImage => underlyingImage;
		internal Texture2D Underlying => underlying;

		private bool disposed;

		public ulong GetUsedBits(MemoryRealm realm) {
			switch (realm) {
				case MemoryRealm.CPU: return underlyingImage == null ? 0 : (ulong)(underlyingImage.Value.Width * underlyingImage.Value.Height * Underlying.Format.GetBitsPerPixel());
				case MemoryRealm.GPU: return (ulong)(underlying.Width * underlying.Height * Underlying.Format.GetBitsPerPixel());
				default: return 0;
			}
		}

		public bool IsValid() => !disposed;

		public void GenerateMipmaps() {
			if (underlying.Mipmaps <= 1) {
				Raylib.GenTextureMipmaps(ref underlying);
			}
		}

		private TextureFilter filter;
		private TextureWrap wrap;

		public TextureFilter GetFilter() => filter;

		public TextureWrap GetWrap() => wrap;

		public void SetFilter(TextureFilter filter) {
			this.filter = filter;
			Raylib.SetTextureFilter(underlying, filter);
		}

		public void SetWrap(TextureWrap wrap) {
			this.wrap = wrap;
			Raylib.SetTextureWrap(underlying, wrap);
		}

		public bool HasCPUImage() {
			return UnderlyingImage.HasValue;
		}

		public Image GetCPUImage() => UnderlyingImage ?? throw new Exception("No CPU image available. The texture creation call must store the image.");

		private ITextureRegenerator? regenerator;

		public ITextureRegenerator? GetTextureRegenerator() {
			return regenerator;
		}

		public void SetTextureRegenerator(ITextureRegenerator? regenerator) {
			this.regenerator = regenerator;
		}

		internal void SetRegenerator(ITextureRegenerator? value) {
			regenerator = value;
		}

		public unsafe Span<byte> GetPixels() {
			var img = UnderlyingImage ?? throw new Exception("No CPU image available; cannot access Pixels.");
			int size = Raylib.GetPixelDataSize(img.Width, img.Height, img.Format);
			return new Span<byte>(img.Data, size);
		}

		public void Download() {
			if (!HasCPUImage()) throw new Exception("Cannot Download() a texture with no CPU image.");
			GetTextureRegenerator()?.Regenerate(new TextureCanvas(UnderlyingImage!.Value));
			Raylib.UpdateTexture(underlying, (ReadOnlySpan<byte>)GetPixels());
		}

		protected virtual void Dispose(bool usercall) {
			if (disposed) return;
			if (!selfDisposing) {
				Logs.Info("Non-self-disposing texture found; ignoring disposal.");
				disposed = true;
				return;
			}

			MainThread.RunASAP(() => {
				if (UnderlyingImage.HasValue && shouldSelfDisposeImage) Raylib.UnloadImage(UnderlyingImage.Value); // todo: something in modeleditor causes this to access violation
				underlyingImage = null;
				Raylib.UnloadTexture(Underlying);
				parent?.EnsureTextureRemoved(this);
			});

			disposed = true;
		}

		// .NET runtime disposes the resource
		~Texture() { if (selfDisposing) Dispose(usercall: false); }

		// User disposes the resource
		public void Dispose() {
			Dispose(usercall: true);
			GC.SuppressFinalize(this);
		}

	}

	internal readonly struct TextureCanvas : ITextureCanvas
	{
		private readonly Image image;
		public TextureCanvas(Image image) => this.image = image;

		public int Width => image.Width;
		public int Height => image.Height;
		public ImageFormat Format => image.Format;

		public unsafe Span<byte> Pixels =>
			new(image.Data, Raylib.GetPixelDataSize(image.Width, image.Height, image.Format));
	}

	public class TextureManagement
	{
		private WeakCollection<ITexture> Textures = [];
		public int Count => Textures.ReferencedCount;

		private bool disposedValue;

		public ulong GetUsedBits(MemoryRealm realm) {
			ulong ret = 0;

			foreach (var tex in Textures)
				ret += tex!.GetUsedBits(realm);

			return ret;
		}

		internal void EnsureTextureAdded(ITexture tex) {
			Textures.Add(tex);
		}

		internal ITexture CreateTexture(Texture2D underlying, bool selfDisposing = true, Image? cpuImage = null, bool shouldSelfDisposeImage = true) {
			Texture tex = new(this, underlying, selfDisposing, cpuImage, shouldSelfDisposeImage);
			EnsureTextureAdded(tex);
			return tex;
		}

		public ITexture CreateTexture(ReadOnlySpan<byte> encoded, string fileType = ".png", bool retainCPUImage = false) {
			Image image = Raylib.LoadImageFromMemory(fileType, encoded.ToArray());
			return CreateTextureFromImage(image, retainCPUImage);
		}

		public ITexture CreateTexture(Stream encoded, string fileType = ".png", bool retainCPUImage = false) {
			using var img = new Raylib.ImageRef(fileType, encoded);
			return CreateTextureFromImage(img, retainCPUImage);
		}

		private ITexture CreateTextureFromImage(Image image, bool retainCPUImage) {
			Texture2D underlying = Raylib.LoadTextureFromImage(image);
			ITexture tex = CreateTexture(underlying, true, retainCPUImage ? image : null, retainCPUImage);
			if (!retainCPUImage) Raylib.UnloadImage(image);
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
			Image image = Raylib.GenImageColor(width, height, Color.Blank);
			if (image.Format != format) Raylib.ImageFormat(ref image, format);
			Texture2D underlying = Raylib.LoadTextureFromImage(image);
			return CreateTexture(underlying, true, image, true);
		}

		public ITexture CreateProcedural(int width, int height, ImageFormat format, ITextureRegenerator generator) {
			Image image = Raylib.GenImageColor(width, height, Color.Blank);
			if (image.Format != format) Raylib.ImageFormat(ref image, format);
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
		~TextureManagement() {
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

		public Texture LoadTextureFromFile(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) {
			Span<char> finalPath = stackalloc char[IManagedMemoryUnit.MergePathSize(pathID, path)];
			IManagedMemoryUnit.MergePath(pathID, path, finalPath);
			var managedPath = new UtlSymbol(finalPath);
			if (LoadedTexturesFromFile.TryGetValue(managedPath, out Texture? texFromFile)) return texFromFile;

			Texture tex = new(this, Filesystem.ReadTexture(new(pathID), new(path) /* << TODO */), true);
			EnsureTextureAdded(tex);
			tex.GenerateMipmaps();
			tex.SetFilter(TextureFilter.Bilinear);
			LoadedTexturesFromFile.Add(managedPath, tex);
			LoadedFilesFromTexture.Add(tex, managedPath);

			return tex;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Texture LoadTextureFromFile(ReadOnlySpan<char> filepath, bool localToImages = true) =>
			localToImages ? LoadTextureFromFile("images", filepath)
			: LoadTextureFromFileDisk(filepath);

		private Texture LoadTextureFromFileDisk(ReadOnlySpan<char> filepath) {
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
	}
}