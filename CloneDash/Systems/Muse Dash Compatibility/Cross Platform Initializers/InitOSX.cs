using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        private static MDCompatLayerInitResult INIT_OSX() {
            if (!OperatingSystem.IsMacOS())
                return MDCompatLayerInitResult.OperatingSystemNotCompatible;

            // Where is Steam installed?
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string steamPath = Path.Combine(homeDirectory, "Library", "Application Support", "Steam", "steamapps", "libraryfolders.vdf");
            // Figure out from Steam where Muse Dash is installed, if it is installed, otherwise break out
            ValveDataFile games = ValveDataFile.FromFile(steamPath);
            string musedash_appid = "" + MUSEDASH_APPID;
            string musedash_installdir = "";
            bool musedash_installed = false;

            foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
                var apps = vdfItemPair.Value["apps"] as ValveDataFile.VDFDict;
                if (apps.Contains(musedash_appid)) {
                    ValveDataFile appManifest = ValveDataFile.FromFile(Path.Combine(vdfItemPair.Value.GetString("path"), "steamapps", $"appmanifest_{musedash_appid}.acf"));
                    musedash_installed = true;
                    musedash_installdir = Path.Combine(vdfItemPair.Value.GetString("path") ,"steamapps", "common", appManifest["AppState"].GetString("installdir"));
                }
            }

            if (!musedash_installed)
                return MDCompatLayerInitResult.MuseDashNotInstalled;
            musedash_installdir = Path.Combine(musedash_installdir, "MuseDash_Mac_Steam.app", "Contents", "Resources");
            WhereIsMuseDashInstalled = musedash_installdir;

            // If installed, load noteinfo.json for BMS references
            // The bundle is named globalconfigs_assets_notedatamananger

            string platform = "StandaloneOSX";
            string musedash_streamingassets = Path.Combine(musedash_installdir, "Data", "StreamingAssets", "aa", platform); // TODO: support multiple platforms
            if (!Directory.Exists(musedash_streamingassets))
                return MDCompatLayerInitResult.StreamingAssetsNotFound;

			BuildTarget = musedash_streamingassets;
			StreamingFiles = Directory.GetFiles(musedash_streamingassets);

            // The note data file would be loaded here from the assetbundle, then the notedata extracted

            return MDCompatLayerInitResult.OK;
        }
    }
}
