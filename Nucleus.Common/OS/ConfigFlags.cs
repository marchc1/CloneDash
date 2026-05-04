namespace Nucleus.Common.OS;

/// <summary>
/// System config flags<br/>
/// NOTE: Every bit registers one state (use it with bit masks)<br/>
/// By default all flags are set to 0
/// </summary>
[Flags]
public enum ConfigFlags : uint
{
	/// <summary>
	/// Set to try enabling V-Sync on GPU
	/// </summary>
	VSyncHint = 0x00000040,

	/// <summary>
	/// Set to run program in fullscreen
	/// </summary>
	FullScreenMode = 0x00000002,

	/// <summary>
	/// Set to allow resizable window
	/// </summary>
	WindowResizable = 0x00000004,

	/// <summary>
	/// Set to disable window decoration (frame and buttons)
	/// </summary>
	WindowUndecorated = 0x00000008,

	/// <summary>
	/// Set to hide window
	/// </summary>
	WindowHidden = 0x00000080,

	/// <summary>
	/// Set to minimize window (iconify)
	/// </summary>
	WindowMinimized = 0x00000200,

	/// <summary>
	/// Set to maximize window (expanded to monitor)
	/// </summary>
	WindowMaximized = 0x00000400,

	/// <summary>
	/// Set to window non focused
	/// </summary>
	WindowUnfocused = 0x00000800,

	/// <summary>
	/// Set to window always on top
	/// </summary>
	WindowTopMost = 0x00001000,

	/// <summary>
	/// Set to allow windows running while minimized
	/// </summary>
	WindowAlwaysRun = 0x00000100,

	/// <summary>
	/// Set to allow transparent framebuffer
	/// </summary>
	WindowTransparent = 0x00000010,

	/// <summary>
	/// Set to support HighDPI
	/// </summary>
	WindowHighDPI = 0x00002000,

	/// <summary>
	/// Set to support mouse passthrough, only supported when FLAG_WINDOW_UNDECORATED
	/// </summary>
	WindowMousePassthrough = 0x00004000,

	/// <summary>
	/// Set to run program in borderless windowed mode
	/// </summary>
	WindowBorderlessWindowedMode = 0x00008000,

	/// <summary>
	/// Set to try enabling MSAA 4X
	/// </summary>
	WindowMSAA4XHint = 0x00000020,

	/// <summary>
	/// Set to try enabling interlaced video format (for V3D)
	/// </summary>
	WindowInterlacedHint = 0x00010000,
}
