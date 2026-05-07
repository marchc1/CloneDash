using CloneDash.Compatibility.Valve;

#if COMPILED_WINDOWS
using Microsoft.Win32;
#endif

namespace CloneDash.Common.Compatibility.Valve;

public static class SteamGames
{
	public static string? WhereIsSteamInstalled() {
#if COMPILED_WINDOWS
		// Where is Steam installed?
#pragma warning disable CA1416 // Validate platform compatibility
		string? steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
		if (steamInstallPath == null) { // Sometimes the install path will be here instead
			steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432NODE\\Valve\\Steam", "InstallPath", null) as string;
			if (steamInstallPath == null)
				return null;
		}
#pragma warning restore CA1416 // Validate platform compatibility
		return steamInstallPath;

#elif COMPILED_OSX
		// Where is Steam installed?
		string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string steamPath = Path.Combine(homeDirectory, "Library", "Application Support", "Steam", "steamapps", "libraryfolders.vdf");
		return Directory.Exists(steamPath) ? steamPath : null;

#elif COMPILED_LINUX
		// Where is Steam installed?
		string home = Environment.GetEnvironmentVariable("HOME")!;
		string steamClassicInstallPath = Path.Combine(home, ".local", "share", "Steam");
		string steamFlatpakInstallPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");

		string? steamInstallPath = Directory.Exists(steamClassicInstallPath) ? steamClassicInstallPath : Directory.Exists(steamFlatpakInstallPath) ? steamFlatpakInstallPath : null;
		if (steamInstallPath == null)
			return null;
		return steamInstallPath;
#else
#error Please implement WhereIsSteamInstalled on this platform
return null;
#endif
	}
	public static string? WhereIsGameInstalled(ulong steamAppID) {
		string? steamInstallPath = WhereIsSteamInstalled();
		if (steamInstallPath == null)
			return null;

		string game_appid = "" + steamAppID;
		string game_installdir = "";
		bool game_installed = false;

#if COMPILED_WINDOWS
		ValveDataFile games = ValveDataFile.FromFile(steamInstallPath + "\\steamapps\\libraryfolders.vdf");
		
		foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
			var apps = (vdfItemPair.Value["apps"] as ValveDataFile.VDFDict)!;
			if (apps.Contains(game_appid)) {
				ValveDataFile appManifest = ValveDataFile.FromFile(vdfItemPair.Value.GetString("path") + $"\\steamapps\\appmanifest_{game_appid}.acf");
				game_installed = true;
				game_installdir = vdfItemPair.Value.GetString("path") + "\\steamapps\\common\\" + appManifest["AppState"].GetString("installdir");
			}
		}
#elif COMPILED_OSX
		ValveDataFile games = ValveDataFile.FromFile(steamInstallPath);

		foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
			var apps = (vdfItemPair.Value["apps"] as ValveDataFile.VDFDict)!;
			if (apps.Contains(game_appid)) {
				ValveDataFile appManifest = ValveDataFile.FromFile(Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", $"appmanifest_{game_appid}.acf"));
				game_installed = true;
				game_installdir = Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", "common", appManifest["AppState"].GetString("installdir"));
			}
		}
#elif COMPILED_LINUX
		ValveDataFile games = ValveDataFile.FromFile(Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf"));

		foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
			var apps = (vdfItemPair.Value["apps"] as ValveDataFile.VDFDict)!;
			if (apps.Contains(game_appid)) {
				ValveDataFile appManifest = ValveDataFile.FromFile(Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", $"appmanifest_{game_appid}.acf"));
				game_installed = true;
				game_installdir = Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", "common", appManifest["AppState"].GetString("installdir"));
			}
		}
#else
#error Please implement WhereIsGameInstalled on this platform
return null;
#endif

		if (!game_installed)
			return null;
		return game_installdir;
	}
}
