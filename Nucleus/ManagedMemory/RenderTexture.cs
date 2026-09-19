using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;
using Nucleus.Rendering;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.ManagedMemory;

public sealed class RenderTexture : IRenderTexture
{
	private readonly TextureManager? parent;
	private readonly int width;

	public int GetWidth() {
		return width;
	}

	private readonly int height;

	public int GetHeight() {
		return height;
	}

	public int Samples { get; }
	public bool HasDepth { get; }

	private readonly ImageFormat format;

	public ImageFormat GetFormat() {
		return format;
	}

	public Image GetCPUImage() => default;

	private uint colorTextureId;
	private uint framebuffer;
	private uint depthRenderbuffer;
	private uint framebufferMSAA;
	private uint renderbufferColorMSAA;
	private uint renderbufferDepthMSAA;

	private PublicTextureFlags publicFlags = PublicTextureFlags.RequiresFlippedV;
	private TextureFilter filter = TextureFilter.Bilinear;
	private TextureWrap wrap = TextureWrap.Clamp;
	private bool disposed;

	internal unsafe RenderTexture(TextureManager? parent, in RenderTextureDesc desc) {
		this.parent = parent;
		width = desc.Width;
		height = desc.Height;
		Samples = Math.Max(1, desc.Samples);
		HasDepth = desc.Depth;
		format = desc.ColorFormat == 0 ? ImageFormat.R8G8B8A8 : desc.ColorFormat;

		if (Samples > 1) CreateMSAA();
		else CreateSimple();
	}

	private unsafe void CreateSimple() {
		framebuffer = Rlgl.LoadFramebuffer(GetWidth(), GetHeight());
		if (framebuffer == 0) {
			Logs.Warn("RenderTexture: simple framebuffer failed to initialize");
			return;
		}

		Rlgl.EnableFramebuffer(framebuffer);

		colorTextureId = Rlgl.LoadTexture(null, GetWidth(), GetHeight(), GetFormat(), 1);
		Rlgl.FramebufferAttach(framebuffer, colorTextureId, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL0, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);

		if (HasDepth) {
			depthRenderbuffer = Rlgl.LoadTextureDepth(GetWidth(), GetHeight(), true);
			Rlgl.FramebufferAttach(framebuffer, depthRenderbuffer, FramebufferAttachType.RL_ATTACHMENT_DEPTH, FramebufferAttachTextureType.RL_ATTACHMENT_RENDERBUFFER, 0);
		}

		if (!Rlgl.FramebufferComplete(framebuffer))
			Logs.Warn($"RenderTexture: simple FBO [ID {framebuffer}] incomplete");

		Rlgl.DisableFramebuffer();

		SetFilter(filter);
		SetWrap(wrap);
	}

	private unsafe void CreateMSAA() {
		uint textureId, fboMsaaId, rboColorId, rboDepthId, fboId, rboId;

		// Resolve target color texture
		OpenGL.GenTextures(1, &textureId);
		OpenGL.BindTexture(OpenGL.TEXTURE_2D, textureId);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_MAG_FILTER, OpenGL.LINEAR);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_MIN_FILTER, OpenGL.LINEAR);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_WRAP_S, OpenGL.CLAMP_TO_EDGE);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_WRAP_T, OpenGL.CLAMP_TO_EDGE);
		OpenGL.TexImage2D(OpenGL.TEXTURE_2D, 0, OpenGL.RGBA8, GetWidth(), GetHeight(), 0, OpenGL.RGBA, OpenGL.UNSIGNED_BYTE, 0);
		OpenGL.BindTexture(OpenGL.TEXTURE_2D, 0);
		colorTextureId = textureId;

		// MSAA framebuffer + color/depth renderbuffers
		OpenGL.GenFramebuffers(1, &fboMsaaId);
		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, fboMsaaId);
		framebufferMSAA = fboMsaaId;

		OpenGL.GenRenderbuffers(1, &rboColorId);
		OpenGL.BindRenderbuffer(rboColorId);
		OpenGL.RenderbufferStorageMultisample(OpenGL.RENDERBUFFER, Samples, OpenGL.RGB8, GetWidth(), GetHeight());
		OpenGL.BindRenderbuffer(0);
		renderbufferColorMSAA = rboColorId;

		OpenGL.GenRenderbuffers(1, &rboDepthId);
		OpenGL.BindRenderbuffer(rboDepthId);
		OpenGL.RenderbufferStorageMultisample(OpenGL.RENDERBUFFER, Samples, OpenGL.DEPTH24_STENCIL8, GetWidth(), GetHeight());
		OpenGL.BindRenderbuffer(0);
		renderbufferDepthMSAA = rboDepthId;

		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.COLOR_ATTACHMENT0, OpenGL.RENDERBUFFER, rboColorId);
		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.DEPTH_STENCIL_ATTACHMENT, OpenGL.RENDERBUFFER, rboDepthId);

		// Non-MSAA resolve framebuffer holding the render-to-texture
		OpenGL.GenFramebuffers(1, &fboId);
		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, fboId);
		framebuffer = fboId;

		OpenGL.GenRenderbuffers(1, &rboId);
		OpenGL.BindRenderbuffer(rboId);
		OpenGL.RenderbufferStorage(OpenGL.RENDERBUFFER, OpenGL.DEPTH24_STENCIL8, GetWidth(), GetHeight());
		OpenGL.BindRenderbuffer(0);
		depthRenderbuffer = rboId;

		OpenGL.FramebufferTexture2D(OpenGL.FRAMEBUFFER, OpenGL.COLOR_ATTACHMENT0, OpenGL.TEXTURE_2D, textureId, 0);
		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.DEPTH_STENCIL_ATTACHMENT, OpenGL.RENDERBUFFER, rboId);

		if (OpenGL.CheckFramebufferStatus(OpenGL.FRAMEBUFFER) != OpenGL.FRAMEBUFFER_COMPLETE)
			throw new Exception("RenderTexture: MSAA FBO incomplete.");

		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, 0);
	}

	public void Begin() {
		uint fbo = Samples > 1 ? framebufferMSAA : framebuffer;
		EngineCore.Window.BeginTextureMode(new RenderTexture2D {
			Id = fbo,
			Texture = new Texture2D { Width = GetWidth(), Height = GetHeight() }
		});
	}

	public void End() {
		if (Samples > 1) {
			Rlgl.DrawRenderBatchActive();
			OpenGL.BindFramebuffer(OpenGL.READ_FRAMEBUFFER, framebufferMSAA);
			OpenGL.BindFramebuffer(OpenGL.DRAW_FRAMEBUFFER, framebuffer);
			OpenGL.BlitFramebuffer(0, 0, GetWidth(), GetHeight(), 0, 0, GetWidth(), GetHeight(), OpenGL.COLOR_BUFFER_BIT, OpenGL.LINEAR);
			OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, 0);
			OpenGL.Viewport(0, 0, (int)EngineCore.Window.Size.W, (int)EngineCore.Window.Size.H);
		}
		EngineCore.Window.EndTextureMode();
	}

	// --- Advanced "complex" surface

	public uint Framebuffer => framebuffer;

	public Texture2D Texture => AsTexture2D();

	public void Draw(Rectangle source, Vector2F position, Color tint) {
		Rectangle dest = new(position.X, position.Y, MathF.Abs(source.Width), MathF.Abs(source.Height));
		Raylib.DrawTexturePro(AsTexture2D(), source, dest, new Vector2F(0, 0).ToNumerics(), 0, tint);
	}

	public RectangleF GetBounds() {
		return RectangleF.XYWH(0, 0, GetWidth(), GetHeight());
	}

	public uint GetTextureHandle() => colorTextureId;
	public int GetMipmapCount() => 1;

	public void AddPublicFlags(PublicTextureFlags flags) => publicFlags |= flags;
	public void RemovePublicFlags(PublicTextureFlags flags) => publicFlags &= ~flags;
	public bool HasPublicFlags(PublicTextureFlags flags) => (publicFlags & flags) != 0;
	public PublicTextureFlags GetPublicFlags() => publicFlags;

	public TextureFilter GetFilter() => filter;
	public TextureWrap GetWrap() => wrap;

	public void SetFilter(TextureFilter filter) {
		this.filter = filter;
		Raylib.SetTextureFilter(AsTexture2D(), filter);
	}

	public void SetWrap(TextureWrap wrap) {
		this.wrap = wrap;
		Raylib.SetTextureWrap(AsTexture2D(), wrap);
	}

	public void GenerateMipmaps() { /* render textures are single-mip */ }

	private Texture2D AsTexture2D() => new() { Id = colorTextureId, Width = GetWidth(), Height = GetHeight(), Format = GetFormat(), Mipmaps = 1 };

	// Render textures are GPU-only; no CPU-side image or regeneration.
	public bool HasCPUImage() {
		return false;
	}

	public ITextureRegenerator? GetTextureRegenerator() {
		return null;
	}

	public Span<byte> GetPixels() {
		return [];
	}

	public void Download() { /* Do nothing, since there's no CPU data to download from. */ }

	public ulong GetUsedBits(MemoryRealm realm) {
		if (realm != MemoryRealm.GPU) return 0;
		ulong bits = (ulong)(GetFormat().GetBitsPerPixel() * GetWidth() * GetHeight());
		if (HasDepth) bits += (ulong)(32 * GetWidth() * GetHeight()); // DEPTH24_STENCIL8
		if (Samples > 1) bits *= (ulong)Samples; // multisample color + depth renderbuffers
		return bits;
	}

	public bool IsValid() => !disposed;

	private void Dispose(bool usercall) {
		if (disposed) return;
		disposed = true;

		uint colorTex = colorTextureId, fbo = framebuffer, depthRbo = depthRenderbuffer;
		uint fboMsaa = framebufferMSAA, colorRbo = renderbufferColorMSAA, depthRboMsaa = renderbufferDepthMSAA;
		int samples = Samples;

		MainThread.RunASAP(() => {
			if (samples > 1) {
				if (colorTex != 0) OpenGL.DeleteTexture(colorTex);
				if (fboMsaa != 0) OpenGL.DeleteFramebuffer(fboMsaa);
				if (colorRbo != 0) OpenGL.DeleteRenderbuffer(colorRbo);
				if (depthRboMsaa != 0) OpenGL.DeleteRenderbuffer(depthRboMsaa);
				if (fbo != 0) OpenGL.DeleteFramebuffer(fbo);
				if (depthRbo != 0) OpenGL.DeleteRenderbuffer(depthRbo);
			}
			else {
				// Simple path was created through rlgl; unload the same way.
				Rlgl.UnloadFramebuffer(fbo);
			}
			parent?.EnsureTextureRemoved(this);
		});
	}

	~RenderTexture() => Dispose(usercall: false);

	public void Dispose() {
		Dispose(usercall: true);
		GC.SuppressFinalize(this);
	}
}
