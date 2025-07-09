using CloneDash.Compatibility.Valve;

namespace CloneDash.Compatibility.MuseDash
{
	public static partial class MuseDashCompatibility
	{
		private static MDCompatLayerInitResult INIT_LINUX() {
			if (!OperatingSystem.IsLinux())
				return MDCompatLayerInitResult.OperatingSystemNotCompatible;

			// Where is Steam installed?
			string home = Environment.GetEnvironmentVariable("HOME")!;
			string steamClassicInstallPath = Path.Combine(home, ".local", "share", "Steam");
			string steamFlatpakInstallPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
			
			string? steamInstallPath = Directory.Exists(steamClassicInstallPath) ? steamClassicInstallPath : Directory.Exists(steamFlatpakInstallPath) ? steamFlatpakInstallPath : null;
			if(steamInstallPath == null)
				return MDCompatLayerInitResult.SteamNotInstalled;

			// Figure out from Steam where Muse Dash is installed, if it is installed, otherwise break out
			ValveDataFile games = ValveDataFile.FromFile(Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf"));
			string musedash_appid = "" + MUSEDASH_APPID;
			string musedash_installdir = "";
			bool musedash_installed = false;

			foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
				var apps = (vdfItemPair.Value["apps"] as ValveDataFile.VDFDict)!;
				if (apps.Contains(musedash_appid)) {
					ValveDataFile appManifest = ValveDataFile.FromFile(Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", $"appmanifest_{musedash_appid}.acf"));
					musedash_installed = true;
					musedash_installdir = Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", "common", appManifest["AppState"].GetString("installdir"));
				}
			}

			if (!musedash_installed)
				return MDCompatLayerInitResult.MuseDashNotInstalled;
			WhereIsMuseDashInstalled = musedash_installdir;
			WhereIsMuseDashDataFolder = Path.Combine(musedash_installdir, "MuseDash_Data");

			// If installed, load noteinfo.json for BMS references
			// The bundle is named globalconfigs_assets_notedatamananger

			string platform = "StandaloneWindows64"; // Not StandaloneLinux64, Muse Dash doesn't build targetting linux...
			StandalonePlatform = platform;

			string musedash_streamingassets = Path.Combine(musedash_installdir, "MuseDash_Data", "StreamingAssets", "aa", platform); // TODO: support multiple platforms
			if (!Directory.Exists(musedash_streamingassets))
				return MDCompatLayerInitResult.StreamingAssetsNotFound;

			BuildTarget = musedash_streamingassets;
			StreamingFiles = Directory.GetFiles(musedash_streamingassets);
			// The note data file would be loaded here from the assetbundle, then the notedata extracted

			return MDCompatLayerInitResult.OK;
		}
	}
}
