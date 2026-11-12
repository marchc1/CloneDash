using CloneDash.Common.Compatibility.Valve;

#if COMPILED_WINDOWS
using Microsoft.Win32;
#endif

namespace CloneDash.Common.Compatibility.Valve;

public static class SteamApps
{
#if COMPILED_WINDOWS
	private const string SteamRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";
	private const string SteamRegistryPathAlt = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432NODE\Valve\Steam";
	private const string SteamRegistryInstallPathKey = "InstallPath";
#endif

	public static string? WhereIsSteamInstalled() {
#if COMPILED_WINDOWS
#pragma warning disable CA1416 // Validate platform compatibility
		string? steamInstallPath =
			Registry.GetValue(SteamRegistryPath, SteamRegistryInstallPathKey, null) as string
			?? Registry.GetValue(SteamRegistryPathAlt, SteamRegistryInstallPathKey, null) as string; //Sometimes the installation path will be here instead
#pragma warning restore CA1416 // Validate platform compatibility

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

	/// <summary>
	/// Finds all library folders on the system.
	/// </summary>
	public static IEnumerable<(string Path, ValveDataFile.VDFDict? Apps)> GetLibraryFolders(string? steamInstallPath = null) {
		steamInstallPath ??= WhereIsSteamInstalled();
		if (steamInstallPath == null)
			yield break;

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

	/// <summary>
	/// Finds the fully qualified path of the game in a single library folder.
	/// </summary>
	public static string? FindAppInLibrary(string libraryPath, ValveDataFile.VDFDict? apps, string appId) {
		string manifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{appId}.acf");

		bool foundViaCache = apps != null && apps.Contains(appId);
		bool foundViaManifestFile = File.Exists(manifestPath); //Fallback. `apps` is just a cache and sometimes doesn't include recently installed or updated games

		if (!foundViaCache && !foundViaManifestFile)
			return null;

		ValveDataFile appManifest = ValveDataFile.FromFile(manifestPath);
		string installDir = appManifest["AppState"].GetString("installdir");

		return Path.Combine(libraryPath, "steamapps", "common", installDir);
	}

	/// <summary>
	/// Finds where a Steam app is installed as an absolute filesystem path, given an app ID.
	/// </summary>
	/// <returns>If Steam is not installed, or the app is not found in the app table; this will be a null string.
	/// <br/>Otherwise, the fully qualified path pointing to where the game is installed.</returns>
	public static AppInstall WhereIsAppInstalled(ulong steamAppId) {
		string? steamInstallPath = WhereIsSteamInstalled();
		if (steamInstallPath == null)
			return new AppInstall(AppStatus.SteamNotInstalled);

		string appId = steamAppId.ToString();

		foreach ((string libraryPath, ValveDataFile.VDFDict? apps) in GetLibraryFolders(steamInstallPath)) {
			string? installDir = FindAppInLibrary(libraryPath, apps, appId);
			if (installDir != null)
				return new AppInstall(installDir);
		}

		return new AppInstall(AppStatus.AppNotInstalled);
	}
}