using Nucleus.Types;

using Raylib_cs;

namespace Nucleus.Rendering;

public enum StencilFunction
{
	Never = OpenGL.NEVER,
	Less = OpenGL.LESS,
	LessEqual = OpenGL.LEQUAL,
	Greater = OpenGL.GREATER,
	GreaterEqual = OpenGL.GEQUAL,
	Equal = OpenGL.EQUAL,
	NotEqual = OpenGL.NOTEQUAL,
	Always = OpenGL.ALWAYS
}

public enum StencilOperation
{
	Keep = OpenGL.KEEP,
	Zero = OpenGL.KEEP,
	Replace = OpenGL.REPLACE,
	Increment = OpenGL.INCR,
	IncrementWrapped = OpenGL.INCR_WRAP,
	Decrement = OpenGL.DECR,
	DecrementWrapped = OpenGL.DECR_WRAP,
	Invert = OpenGL.INVERT
}

/// <summary>
/// A stencil library designed for OpenGL 3.3. If Raylib is using a different context, this likely will cause issues...
/// but Raylib doesn't want to add stencil functionality it seems.
/// </summary>
public static class Stencils
{
	public static void Begin() {
		Rlgl.DrawRenderBatchActive();
		Reset();
		OpenGL.Enable(OpenGL.STENCIL_TEST);
	}

	static Stencils() {
		Reset();
	}

	public static void Reset() {
		Function = StencilFunction.Always;
		Reference = 1;
		Mask = 0xff;
		OnFail = StencilOperation.Keep;
		OnDepthFail = StencilOperation.Keep;
		OnDepthPass = StencilOperation.Replace;
	}

	public static StencilFunction Function;
	public static int Reference;
	public static uint Mask;
	public static StencilOperation OnFail;
	public static StencilOperation OnDepthFail;
	public static StencilOperation OnDepthPass;

	/// <summary>
	/// Make sure to set the stencil parameters before calling this method. Clears the stencil buffer.
	/// </summary>
	public static void BeginMask() {
		OpenGL.Clear(OpenGL.STENCIL_BUFFER_BIT);
		OpenGL.ColorMask(false, false, false, false);
		OpenGL.Enable(GLEnum.ALPHA_TEST);
		OpenGL.AlphaFunc(OpenGL.GREATER, 0.5f);
		Update();
	}

	public static void Update() {
		OpenGL.StencilFunc((int)Function, Reference, Mask);
		OpenGL.StencilOp((int)OnFail, (int)OnDepthFail, (int)OnDepthPass);
	}

	public static void EndMask() {
		Rlgl.DrawRenderBatchActive();
		OpenGL.Disable(GLEnum.ALPHA_TEST);
		OpenGL.StencilFunc(OpenGL.EQUAL, 1, 0xFF);
		OpenGL.StencilOp(OpenGL.KEEP, OpenGL.KEEP, OpenGL.KEEP);
		OpenGL.ColorMask(true, true, true, true);
	}

	public static void End() {
		Rlgl.DrawRenderBatchActive();
		OpenGL.Disable(OpenGL.STENCIL_TEST);
	}
}