using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Files;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;


namespace Nucleus.ManagedMemory
{
	public interface ITexture : IManagedMemory {
        public int Width { get; }
        public int Height { get; }
        public PixelFormat Format { get; }
    }

    public class Texture(TextureManagement? parent, Texture2D underlying, bool selfDisposing = true, Image? underlyingImage = null, bool shouldSelfDisposeImage = true) : ITexture
    {
		// Unmanaged missing texture; should not be freed...
		public static readonly Texture MISSING = new Texture(null, Filesystem.ReadTexture("images", "missing_texture.png"), false);

		public RectangleF Bounds => RectangleF.XYWH(0, 0, Width, Height);

		public uint HardwareID => underlying.Id;
		public string? DebugName { get; set; }
        public int Width => underlying.Width;
        public int Height => underlying.Height;
        public uint UWidth => (uint)underlying.Width;
        public uint UHeight => (uint)underlying.Height;
        public PixelFormat Format => underlying.Format;

		private Image? UnderlyingImage => underlyingImage;
        private Texture2D Underlying => underlying;

        private bool disposed;
        public ulong UsedBits => (ulong)(underlying.Width * underlying.Height * TextureManagement.GetBitsPerPixel(Underlying.Format));

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

		public bool HasCPUImage => UnderlyingImage.HasValue;
		public Image GetCPUImage() => UnderlyingImage ?? throw new Exception("No CPU image available. The texture creation call must store the image.");

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

        public static implicit operator Texture2D(Texture self) => self.Underlying;
    }
    public class TextureManagement : IManagedMemory
    {
        private List<ITexture> Textures = [];
        private List<RenderTexture2D> RenderTextures = [];
        private bool disposedValue;
        public ulong UsedBits {
            get {
                ulong ret = 0;

				foreach (var tex in Textures) {
					ret += tex.UsedBits;
				}

				return ret;
            }
        }

        public static int GetBitsPerPixel(PixelFormat format) {
            switch (format) {
                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_GRAYSCALE:
                    return 8;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_GRAY_ALPHA:
                    return 16;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R5G6B5:
                    return 16;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8:
                    return 24;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R5G5B5A1:
                    return 16;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R4G4B4A4:
                    return 16;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8:
                    return 32;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32:
                    return 32;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32:
                    return 96;

                case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32A32:
                    return 128;

                case PixelFormat.PIXELFORMAT_COMPRESSED_DXT1_RGB:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_DXT1_RGBA:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_DXT3_RGBA:
                    return 8;

                case PixelFormat.PIXELFORMAT_COMPRESSED_DXT5_RGBA:
                    return 8;

                case PixelFormat.PIXELFORMAT_COMPRESSED_ETC1_RGB:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_ETC2_RGB:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_ETC2_EAC_RGBA:
                    return 8;

                case PixelFormat.PIXELFORMAT_COMPRESSED_PVRT_RGB:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_PVRT_RGBA:
                    return 4;

                case PixelFormat.PIXELFORMAT_COMPRESSED_ASTC_4x4_RGBA:
                    return 8;

                case PixelFormat.PIXELFORMAT_COMPRESSED_ASTC_8x8_RGBA:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
        public static int GetBytesPerPixel(PixelFormat format) => (int)Math.Ceiling(GetBitsPerPixel(format) / 8d);
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
			Span<char> finalPath = stackalloc char[IManagedMemory.MergePathSize(pathID, path)];
			IManagedMemory.MergePath(pathID, path, finalPath);
			var managedPath = new UtlSymbol(finalPath);
			if (LoadedTexturesFromFile.TryGetValue(managedPath, out Texture? texFromFile)) return texFromFile;

			Texture tex = new(this, Filesystem.ReadTexture(new(pathID), new(path) /* << TODO */), true);
			Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);

			LoadedTexturesFromFile.Add(managedPath, tex);
			LoadedFilesFromTexture.Add(tex, managedPath);
			Textures.Add(tex);

			return tex;
		}

		public Texture LoadTextureFromFile(ReadOnlySpan<char> filepath, bool localToImages = true) =>
			localToImages ? LoadTextureFromFile("images", filepath)
			: LoadTextureFromFileDisk(filepath);

		private Texture LoadTextureFromFileDisk(ReadOnlySpan<char> filepath) {
			UtlSymbol filepathSymbol = new UtlSymbol(filepath);
			if (LoadedTexturesFromFile.TryGetValue(filepathSymbol, out Texture? texFromFile)) return texFromFile;

			Texture tex = new(this, Raylib.LoadTexture(new string(filepath) /* << TODO */), true);
			Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);

			LoadedTexturesFromFile.Add(filepathSymbol, tex);
			LoadedFilesFromTexture.Add(tex, filepathSymbol);
			Textures.Add(tex);

			return tex;
		}

		public void EnsureTextureRemoved(ITexture itex) {
            if(itex is Texture tex) {
                if(LoadedFilesFromTexture.TryGetValue(tex, out var filepath)) {
                    LoadedTexturesFromFile.Remove(filepath);
                    LoadedFilesFromTexture.Remove(tex);
                    Textures.Remove(tex);

                    tex.Dispose();
                }
            }
        }
		public unsafe RenderTexture2D LoadRenderTexture(int width, int height, PixelFormat format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8) {
			RenderTexture2D target = new();

			target.Id = Rlgl.LoadFramebuffer(width, height);   // Load an empty framebuffer

			if (target.Id > 0) {
				Rlgl.EnableFramebuffer(target.Id);

				// Create color texture (default to RGBA)
				target.Texture.Id = Rlgl.LoadTexture(null, width, height, format, 1);
				target.Texture.Width = width;
				target.Texture.Height = height;
				target.Texture.Format = format;
				target.Texture.Mipmaps = 1;

				// Create depth renderbuffer/texture
				target.Depth.Id = Rlgl.LoadTextureDepth(width, height, true);
				target.Depth.Width = width;
				target.Depth.Height = height;
				target.Depth.Format = (PixelFormat)19;       //DEPTH_COMPONENT_24BIT?
				target.Depth.Mipmaps = 1;

				// Attach color texture and depth renderbuffer/texture to FBO
				Rlgl.FramebufferAttach(target.Id, target.Texture.Id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL0, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
				Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.RL_ATTACHMENT_DEPTH, FramebufferAttachTextureType.RL_ATTACHMENT_RENDERBUFFER, 0);

				// Check if fbo is complete with attachments (valid)
				if (Rlgl.FramebufferComplete(target.Id)) Raylib.TraceLog(TraceLogLevel.LOG_INFO, $"FBO: [ID {target.Id}] Framebuffer object created successfully");

				Rlgl.DisableFramebuffer();
			}
			else Raylib.TraceLog(TraceLogLevel.LOG_WARNING, "FBO: Framebuffer object can not be created");
			RenderTextures.Add(target);
			return target;
		}
		public void UnloadRenderTexture(RenderTexture2D? tex) {
			if (!tex.HasValue)
				return;

			RenderTextures.Remove(tex.Value);
			if (Thread.CurrentThread != MainThread.Thread) {
				MainThread.RunASAP(() => Raylib.UnloadRenderTexture(tex.Value));
			}
			else {
				Raylib.UnloadRenderTexture(tex.Value);
			}
		}
		public unsafe ComplexRenderTexture CreateComplexRenderTexture(int width, int height, PixelFormat pixelFormat = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8) {
			return new ComplexRenderTexture(width, height);
		}
	}
}
