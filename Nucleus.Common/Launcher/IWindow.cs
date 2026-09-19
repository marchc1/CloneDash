namespace Nucleus.Common.Launcher;

/// <summary>
/// A single operating system window.
/// </summary>
public interface IWindow
{
	nint GetOSHandle();
}