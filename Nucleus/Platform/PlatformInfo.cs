namespace Nucleus;

file static class PlatformInfo
{
	public static Platform.DisplayServerType DisplayServer;

	static PlatformInfo() {
#if COMPILED_WINDOWS
		DisplayServer = Platform.DisplayServerType.WDDM;
#elif COMPILED_OSX
		// This is an assumption...
		DisplayServer = Platform.DisplayServerType.Quartz;
#elif COMPILED_LINUX
		// This is an assumption...
		DisplayServer = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null ? Platform.DisplayServerType.X11 : Platform.DisplayServerType.Wayland;
#endif
	}
}

public static partial class Platform
{
	public enum DisplayServerType
	{
		// Windows Display Driver Model
		WDDM,

		// Mac OS
		Quartz,

		// Linux display servers
		X11,
		Wayland
	}
}


public static partial class Platform
{
	public static Platform.DisplayServerType DisplayServer => PlatformInfo.DisplayServer;
}
