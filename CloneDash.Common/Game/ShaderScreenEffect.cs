using Nucleus;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;

using Color = Nucleus.Common.Types.Color;

namespace CloneDash.Common.Game;

public abstract class ShaderScreenEffect(string shaderName) : ScreenEffect
{
	IShader? Shader;

	public override void Initialize() {
		Shader = Level.Shaders.LoadFragmentShaderFromFile("shaders", $"{shaderName}.fs");
	}

	public override void Render(FrameState frameState, ref RenderTexture read, ref RenderTexture write) {
		var shader = Shader;
		if (shader == null) return;

		if (SetUniforms(shader) == false)
			return;

		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableFramebuffer(write.Framebuffer);
		Rlgl.ClearScreenBuffers();

		shader.Activate();
		OnShaderActivated(shader);
		Raylib.DrawTextureRec(
			read.Texture,
			new Rectangle(0, 0, read.GetWidth(), -read.GetHeight()),
			System.Numerics.Vector2.Zero,
			Color.White
		);
		shader.Deactivate();

		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableFramebuffer(0);
		Rlgl.Viewport(0, 0, (int)EngineCore.Window.Size.W, (int)EngineCore.Window.Size.H);

		(read, write) = (write, read);
	}

	protected virtual void OnShaderActivated(IShader shader) { }

	protected abstract bool SetUniforms(IShader shader);
}
