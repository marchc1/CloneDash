using CloneDash.Common.Compatibility.Valve;
using CloneDash.Compatibility.Valve;

namespace CloneDash.Compatibility.MuseDash
{
	public static partial class MuseDash1Compatibility
	{
		private static MD1CompatLayerInitResult INIT_OSX() {
			if (!OperatingSystem.IsMacOS())
				return MD1CompatLayerInitResult.OperatingSystemNotCompatible;

			if (SteamGames.WhereIsSteamInstalled() == null) return MD1CompatLayerInitResult.SteamNotInstalled;
			var musedash_installdir = SteamGames.WhereIsGameInstalled(MUSEDASH_APPID);
			if (musedash_installdir == null) return MD1CompatLayerInitResult.MuseDashNotInstalled;

			WhereIsMuseDashInstalled = Path.Combine(musedash_installdir, "MuseDash_Mac_Steam.app", "Contents", "Resources");
			WhereIsMuseDashDataFolder = Path.Combine(musedash_installdir, "Data");

			// If installed, load noteinfo.json for BMS references
			// The bundle is named globalconfigs_assets_notedatamananger

			string platform = "StandaloneOSX";
			StandalonePlatform = platform;

			string musedash_streamingassets = Path.Combine(musedash_installdir, "Data", "StreamingAssets", "aa", platform); // TODO: support multiple platforms
			if (!Directory.Exists(musedash_streamingassets))
				return MD1CompatLayerInitResult.StreamingAssetsNotFound;

			BuildTarget = musedash_streamingassets;
			StreamingFiles = Directory.GetFiles(musedash_streamingassets);

			// The note data file would be loaded here from the assetbundle, then the notedata extracted

			return MD1CompatLayerInitResult.OK;
		}
	}
}
