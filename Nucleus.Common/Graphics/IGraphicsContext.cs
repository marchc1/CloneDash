namespace Nucleus.Common.Graphics;

/// <summary>
/// A graphics context. 
/// </summary>
public interface IGraphicsContext{
	nint GetOSHandle();
	void MakeCurrent();
	void SwapBuffers();
}
