using CloneDash.Compatibility.Valve;

#if COMPILED_WINDOWS
using Microsoft.Win32;
#endif

namespace CloneDash.Common.Compatibility.Valve
{
	public static class SteamGames
	{
		private const string SteamRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";
		private const string SteamRegistryPathAlt = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432NODE\Valve\Steam";
		private const string SteamRegistryInstallPathKey = "InstallPath";

		public static string? WhereIsSteamInstalled() {
#if COMPILED_WINDOWS
			string? steamInstallPath =
				Registry.GetValue(SteamRegistryPath, SteamRegistryInstallPathKey, null) as string
				?? Registry.GetValue(SteamRegistryPathAlt, SteamRegistryInstallPathKey, null) as string; //Sometimes the installation path will be here instead

			return steamInstallPath;

#elif COMPILED_OSX
		string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string steamPath = Path.Combine(homeDirectory, "Library", "Application Support", "Steam");
		return Directory.Exists(steamPath) ? steamPath : null;

#elif COMPILED_LINUX
		string home = Environment.GetEnvironmentVariable("HOME")!;
		string steamClassicInstallPath = Path.Combine(home, ".local", "share", "Steam");
		string steamFlatpakInstallPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");

		if (Directory.Exists(steamClassicInstallPath))
			return steamClassicInstallPath;
		if (Directory.Exists(steamFlatpakInstallPath))
			return steamFlatpakInstallPath;
		return null;
#else
#error Please implement WhereIsSteamInstalled on this platform
#endif
		}

		private static IEnumerable<(string Path, ValveDataFile.VDFDict? Apps)> GetLibraryFolders(string steamInstallPath) {
			string libraryFoldersPath = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
			if (!File.Exists(libraryFoldersPath))
				yield break;

			ValveDataFile libraryFolders = ValveDataFile.FromFile(libraryFoldersPath);
			foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in libraryFolders["libraryfolders"]) {
				string path = vdfItemPair.Value.GetString("path");

				ValveDataFile.VDFDict? apps = vdfItemPair.Value["apps"] as ValveDataFile.VDFDict;
				yield return (path, apps);
			}
		}


		private static string? FindGameInLibrary(string libraryPath, ValveDataFile.VDFDict? apps, string appId) {
			string manifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{appId}.acf");

			bool foundViaCache = apps != null && apps.Contains(appId);
			bool foundViaManifestFile = File.Exists(manifestPath); //Fallback. `apps` is just a cache and sometimes doesn't include recently installed or updated games

			if (!foundViaCache && !foundViaManifestFile)
				return null;

			ValveDataFile appManifest = ValveDataFile.FromFile(manifestPath);
			string installDir = appManifest["AppState"].GetString("installdir");

			return Path.Combine(libraryPath, "steamapps", "common", installDir);
		}


		public static string? WhereIsGameInstalled(ulong steamAppId) {
			string? steamInstallPath = WhereIsSteamInstalled();
			if (steamInstallPath == null)
				return null;

			string appId = steamAppId.ToString();

			foreach ((string libraryPath, ValveDataFile.VDFDict? apps) in GetLibraryFolders(steamInstallPath)) {
				string? installDir = FindGameInLibrary(libraryPath, apps, appId);
				if (installDir != null)
					return installDir;
			}

			return null;
		}
	}
}