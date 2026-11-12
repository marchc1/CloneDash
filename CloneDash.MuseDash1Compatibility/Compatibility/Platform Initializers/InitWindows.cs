using CloneDash.Common.Compatibility.Valve;

namespace CloneDash.Compatibility.MuseDash;

public static partial class MuseDash1Compatibility
{
	private static AppStatus INIT_WINDOWS() {
		if (!OperatingSystem.IsWindows())
			return AppStatus.OperatingSystemNotCompatible;

		if (!SteamApps.WhereIsAppInstalled(MUSEDASH_APPID).IsOK(out string? installdir, out AppStatus err))
			return err;

		WhereIsMuseDashInstalled = installdir;
		WhereIsMuseDashDataFolder = Path.Combine(installdir, "MuseDash_Data");

		// If installed, load noteinfo.json for BMS references
		// The bundle is named globalconfigs_assets_notedatamananger

		string platform = "StandaloneWindows64";
		StandalonePlatform = platform;

		string streamingassets = installdir + $"\\MuseDash_Data\\StreamingAssets\\aa\\{platform}\\"; // TODO: support multiple platforms
		if (!Directory.Exists(streamingassets))
			return AppStatus.CriticalPathNotFound;

		BuildTarget = streamingassets;
		StreamingFiles = Directory.GetFiles(streamingassets);

		// The note data file would be loaded here from the assetbundle, then the notedata extracted

		return AppStatus.OK;
	}
}
