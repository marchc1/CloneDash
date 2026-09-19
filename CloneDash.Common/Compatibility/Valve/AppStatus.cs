#if COMPILED_WINDOWS
#endif

namespace CloneDash.Common.Compatibility.Valve;

public enum AppStatus
{
	OK,
	SteamNotInstalled,
	AppNotInstalled,

	// Some custom ones that Steam app fetching won't catch, but other game loaders may catch
	OperatingSystemNotCompatible,
	CriticalPathNotFound
}
