using Nucleus.Rendering;
using Raylib_cs;

namespace Nucleus.Types;

public class ComplexRenderTexture : IDisposable
{
	public int Width { get; set; }
	public int Height { get; set; }

	public uint TextureID { get; private set; }
	public uint FramebufferMSAA { get; private set; }
	public uint RenderbufferColor { get; private set; }
	public uint RenderbufferDepth { get; private set; }
	public uint Framebuffer { get; private set; }
	public uint Renderbuffer { get; private set; }
	public unsafe ComplexRenderTexture(int width, int height, int MSAA = 4) {
		Width = width;
		Height = height;

		uint textureId, fboMsaaId, rboColorId, rboDepthId, fboId, rboId;
		OpenGL.GenTextures(1, &textureId);
		OpenGL.BindTexture(OpenGL.TEXTURE_2D, textureId);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_MAG_FILTER, OpenGL.LINEAR);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_MIN_FILTER, OpenGL.LINEAR);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_WRAP_S, OpenGL.CLAMP_TO_EDGE);
		OpenGL.TexParameteri(OpenGL.TEXTURE_2D, OpenGL.TEXTURE_WRAP_T, OpenGL.CLAMP_TO_EDGE);
		OpenGL.TexImage2D(OpenGL.TEXTURE_2D, 0, OpenGL.RGBA8, width, height, 0, OpenGL.RGBA, OpenGL.UNSIGNED_BYTE, 0);
		OpenGL.BindTexture(OpenGL.TEXTURE_2D, 0);
		TextureID = textureId;

		// create a MSAA framebuffer object
		// NOTE: All attachment images must have the same # of samples.
		// Ohterwise, the framebuffer status will not be completed.
		OpenGL.GenFramebuffers(1, &fboMsaaId);
		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, fboMsaaId);
		FramebufferMSAA = fboMsaaId;

		// create a MSAA renderbuffer object to store color info
		OpenGL.GenRenderbuffers(1, &rboColorId);
		OpenGL.BindRenderbuffer(rboColorId);
		OpenGL.RenderbufferStorageMultisample(OpenGL.RENDERBUFFER, MSAA, OpenGL.RGB8, width, height);
		OpenGL.BindRenderbuffer(0);
		RenderbufferColor = rboColorId;

		// create a MSAA renderbuffer object to store depth info
		// NOTE: A depth renderable image should be attached the FBO for depth test.
		// If we don't attach a depth renderable image to the FBO, then
		// the rendering output will be corrupted because of missing depth test.
		// If you also need stencil test for your rendering, then you must
		// attach additional image to the stencil attachement point, too.
		OpenGL.GenRenderbuffers(1, &rboDepthId);
		OpenGL.BindRenderbuffer(rboDepthId);
		OpenGL.RenderbufferStorageMultisample(OpenGL.RENDERBUFFER, MSAA, OpenGL.DEPTH24_STENCIL8, width, height);
		OpenGL.BindRenderbuffer(0);
		RenderbufferDepth = rboDepthId;

		// attach msaa RBOs to FBO attachment points
		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.COLOR_ATTACHMENT0, OpenGL.RENDERBUFFER, rboColorId);
		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.DEPTH_STENCIL_ATTACHMENT, OpenGL.RENDERBUFFER, rboDepthId);

		// create a normal (no MSAA) FBO to hold a render-to-texture
		OpenGL.GenFramebuffers(1, &fboId);
		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, fboId);
		Framebuffer = fboId;

		OpenGL.GenRenderbuffers(1, &rboId);
		OpenGL.BindRenderbuffer(rboId);
		OpenGL.RenderbufferStorage(OpenGL.RENDERBUFFER, OpenGL.DEPTH24_STENCIL8, width, height);
		OpenGL.BindRenderbuffer(0);
		Renderbuffer = rboId;

		// attach a texture to FBO color attachement point
		OpenGL.FramebufferTexture2D(OpenGL.FRAMEBUFFER, OpenGL.COLOR_ATTACHMENT0, OpenGL.TEXTURE_2D, textureId, 0);

		// attach a rbo to FBO depth attachement point
		OpenGL.FramebufferRenderbuffer(OpenGL.FRAMEBUFFER, OpenGL.DEPTH_STENCIL_ATTACHMENT, OpenGL.RENDERBUFFER, rboId);

		//@@ disable color buffer if you don't attach any color buffer image,
		//@@ for example, rendering the depth buffer only to a texture.
		//@@ Otherwise, glCheckFramebufferStatus will not be complete.
		//glDrawBuffer(GL_NONE);
		//glReadBuffer(GL_NONE);

		// check FBO status
		if (OpenGL.CheckFramebufferStatus(OpenGL.FRAMEBUFFER) != OpenGL.FRAMEBUFFER_COMPLETE)
			throw new Exception("FBO bad.");

		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, 0);
	}

	public void BeginDrawing() {
		// Reset current matrix (projection)
		//GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, FramebufferMSAA);
		//GL.glViewport(0, 0, Width, Height);
		EngineCore.Window.BeginTextureMode(new() {
			Id = FramebufferMSAA,
			Texture = new() {
				Width = Width,
				Height = Height
			}
		});
	}
	public void EndDrawing() {
		OpenGL.BindFramebuffer(OpenGL.READ_FRAMEBUFFER, FramebufferMSAA);
		OpenGL.BindFramebuffer(OpenGL.DRAW_FRAMEBUFFER, Framebuffer);
		OpenGL.BlitFramebuffer(0, 0, Width, Height,  // src rect
			0, 0, Width, Height,  // dst rect
			OpenGL.COLOR_BUFFER_BIT, // buffer mask
			OpenGL.LINEAR); // scale filter

		OpenGL.BindFramebuffer(OpenGL.FRAMEBUFFER, 0);
		OpenGL.Viewport(0, 0, (int)EngineCore.Window.Size.W, (int)EngineCore.Window.Size.H);
		EngineCore.Window.EndTextureMode();
	}
	bool Disposed = false;
	public void Dispose() {
		if (Disposed)
			return;
		Disposed = true;
		MainThread.RunASAP(() => {
			if (TextureID != 0)
				OpenGL.DeleteTexture(TextureID);
			if (FramebufferMSAA != 0)
				OpenGL.DeleteFramebuffer(FramebufferMSAA);
			if (RenderbufferColor != 0)
				OpenGL.DeleteRenderbuffer(RenderbufferColor);
			if (RenderbufferDepth != 0)
				OpenGL.DeleteRenderbuffer(RenderbufferDepth);
			if (Framebuffer != 0)
				OpenGL.DeleteFramebuffer(Framebuffer);
			if (Renderbuffer != 0)
				OpenGL.DeleteRenderbuffer(Renderbuffer);


			Logs.Debug($"ComplexRenderTexture: Unloading... ");
			Logs.Debug($"    TexID:{TextureID}, FrbMSAAID: {FramebufferMSAA}, RbColID: {RenderbufferColor}, RbDepID: {RenderbufferDepth}, FrbID: {Framebuffer}, RbID: {Renderbuffer}");
		});
	}

	public void Draw(Rectangle source, Vector2F position, Color tint) {
		Rectangle dest = new(position.X, position.Y, MathF.Abs(source.Width), MathF.Abs(source.Height));
		Vector2F origin = new(0, 0);

		Raylib.DrawTexturePro(Texture, source, dest, origin.ToNumerics(), 0, tint);
	}

	public Texture2D Texture => new() {
		Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
		Width = Width,
		Height = Height,
		Id = TextureID
	};

	~ComplexRenderTexture() {
		Dispose();
	}

	public static unsafe ComplexRenderTexture CreateComplexRenderTexture(int width, int height, PixelFormat pixelFormat = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8) {
		return new ComplexRenderTexture(width, height);
	}
}
