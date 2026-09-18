using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Files;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.ManagedMemory
{
	internal class Texture : ITexture
	{
		private readonly TextureManager parent;
		private Texture2D underlying;
		private readonly bool selfDisposing;
		private Image? underlyingImage;
		private readonly bool shouldSelfDisposeImage;

		internal Texture(ITextureManager parent, Texture2D underlying, bool selfDisposing = true, Image? underlyingImage = null, bool shouldSelfDisposeImage = true) {
			this.parent = (TextureManager)parent;
			this.underlying = underlying;
			this.selfDisposing = selfDisposing;
			this.underlyingImage = underlyingImage;
			this.shouldSelfDisposeImage = shouldSelfDisposeImage;
		}

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

		public Image GetCPUImage() => UnderlyingImage ?? default;


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
				if (UnderlyingImage.HasValue && shouldSelfDisposeImage) Image.UnloadImage(UnderlyingImage.Value); // todo: something in modeleditor causes this to access violation
				underlyingImage = null;
				Raylib.UnloadTexture(Underlying);
				parent?.EnsureTextureRemoved(this);
			});
			regenerator?.Dispose();
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
}