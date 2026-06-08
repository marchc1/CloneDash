using CloneDash.Common.Compatibility.Valve;
using CloneDash.Compatibility.Valve;

namespace CloneDash.Compatibility.MuseDash
{
	public static partial class MuseDash1Compatibility
	{
		private static MD1CompatLayerInitResult INIT_LINUX() {
			if (!OperatingSystem.IsLinux())
				return MD1CompatLayerInitResult.OperatingSystemNotCompatible;

			// Figure out from Steam where Muse Dash is installed, if it is installed, otherwise break out
			if (SteamGames.WhereIsSteamInstalled() == null) return MD1CompatLayerInitResult.SteamNotInstalled;
			var musedash_installdir = SteamGames.WhereIsGameInstalled(MUSEDASH_APPID);
			if (musedash_installdir == null) return MD1CompatLayerInitResult.MuseDashNotInstalled;

			WhereIsMuseDashInstalled = musedash_installdir;
			WhereIsMuseDashDataFolder = Path.Combine(musedash_installdir, "MuseDash_Data");

			// If installed, load noteinfo.json for BMS references
			// The bundle is named globalconfigs_assets_notedatamananger

			string platform = "StandaloneWindows64"; // Not StandaloneLinux64, Muse Dash doesn't build targetting linux...
			StandalonePlatform = platform;

			string musedash_streamingassets = Path.Combine(musedash_installdir, "MuseDash_Data", "StreamingAssets", "aa", platform); // TODO: support multiple platforms
			if (!Directory.Exists(musedash_streamingassets))
				return MD1CompatLayerInitResult.StreamingAssetsNotFound;

			BuildTarget = musedash_streamingassets;
			StreamingFiles = Directory.GetFiles(musedash_streamingassets);
			// The note data file would be loaded here from the assetbundle, then the notedata extracted

			return MD1CompatLayerInitResult.OK;
		}
	}
}
