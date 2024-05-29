using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        private static MDCompatLayerInitResult INIT_WINDOWS() {
            if (!OperatingSystem.IsWindows())
                return MDCompatLayerInitResult.OperatingSystemNotCompatible;

            // Where is Steam installed?
            string steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string;
            if (steamInstallPath == null) { // Sometimes the install path will be here instead
                steamInstallPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432NODE\\Valve\\Steam", "InstallPath", null) as string;
                if (steamInstallPath == null)
                    return MDCompatLayerInitResult.SteamNotInstalled;
            }

            // Figure out from Steam where Muse Dash is installed, if it is installed, otherwise break out
            ValveDataFile games = ValveDataFile.FromFile(steamInstallPath + "\\steamapps\\libraryfolders.vdf");
            string musedash_appid = "" + MUSEDASH_APPID;
            string musedash_installdir = "";
            bool musedash_installed = false;

            foreach (KeyValuePair<string, ValveDataFile.VDFItem> vdfItemPair in games["libraryfolders"]) {
                var apps = vdfItemPair.Value["apps"] as ValveDataFile.VDFDict;
                if (apps.Contains(musedash_appid)) {
                    ValveDataFile appManifest = ValveDataFile.FromFile(vdfItemPair.Value.GetString("path") + $"\\steamapps\\appmanifest_{musedash_appid}.acf");
                    musedash_installed = true;
                    musedash_installdir = vdfItemPair.Value.GetString("path") + "\\steamapps\\common\\" + appManifest["AppState"].GetString("installdir");
                }
            }

            if (!musedash_installed)
                return MDCompatLayerInitResult.MuseDashNotInstalled;
            WhereIsMuseDashInstalled = musedash_installdir;

            // If installed, load noteinfo.json for BMS references
            // The bundle is named globalconfigs_assets_notedatamananger

            string platform = "StandaloneWindows64";
            string musedash_streamingassets = musedash_installdir + $"\\MuseDash_Data\\StreamingAssets\\aa\\{platform}\\"; // TODO: support multiple platforms
            if (!Directory.Exists(musedash_streamingassets))
                return MDCompatLayerInitResult.StreamingAssetsNotFound;

            StreamingFiles = Directory.GetFiles(musedash_streamingassets);
            string? musedash_notedatamanager = StreamingFiles.Where(x => Path.GetFileName(x).Contains("globalconfigs_assets_notedatamananger")).FirstOrDefault();
            if (musedash_notedatamanager == default)
                return MDCompatLayerInitResult.NoteDataManagerNotFound;

            NoteManagerAssetBundle = musedash_notedatamanager;
            // The note data file would be loaded here from the assetbundle, then the notedata extracted

            return MDCompatLayerInitResult.OK;
        }
    }
}
