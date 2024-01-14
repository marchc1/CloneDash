using AssetStudio;
using CloneDash.Game.Entities;
using CloneDash.Game.Events;
using CloneDash.Game.Sheets;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Microsoft.Win32;
using Newtonsoft.Json;
using OdinSerializer;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;

namespace CloneDash
{
    /// <summary>
    /// Muse Dash style level converter
    /// </summary>
    public static class MuseDashCompatibility
    {
        private const uint MUSEDASH_APPID = 774171;
        public static bool Initialized { get; private set; } = false;
        public static string? WhereIsMuseDashInstalled;
        public static string? NoteManagerAssetBundle;

        private static Dictionary<string, List<string>> IBMSToDesc = new();

        public static bool IsMuseDashInstalled => WhereIsMuseDashInstalled != null;

        private static Dictionary<string, NoteConfigData> IDToNote { get; set; } = new();
        private static Dictionary<string, NoteConfigData> IBMSToNote { get; set; } = new();
        private static Dictionary<string, NoteConfigData> UIDToNote { get; set; } = new();

        private static List<NoteConfigData> NoteDataManager { get; set; }

        public static string[] StreamingFiles { get; private set; }

        private class MusicConfigData
        {
            public int id;
            public decimal time;
            public string note_uid;
            public decimal length;
            public bool blood;
            public int pathway;
        }
        private class NoteConfigData
        {
            public string id;
            public string ibms_id;
            public string uid;
            public string mirror_uid;
            public string scene;
            public string des;
            public string prefab_name;
            public uint type;
            public string effect;
            public string key_audio;
            public string boss_action;
            public List<string> sceneChangeNames;
            public decimal left_perfect_range;
            public decimal left_great_range;
            public decimal right_perfect_range;
            public decimal right_great_range;
            public int damage;
            public int pathway;
            public int speed;
            public int score;
            public int fever;
            public bool missCombo;
            public bool addCombo;
            public bool jumpNote;
            public bool isShowPlayEffect;
        }
        private class MusicData
        {
            public short objId;
            public decimal tick;
            public MusicConfigData configData;
            public NoteConfigData? noteData;
            public bool isLongPressing;
            public int doubleIdx;
            public bool isDouble;
            public bool isLongPressEnd;
            public decimal longPressPTick;
            public int endIndex;
            public decimal dt;
            public int longPressNum;
            public decimal showTick;
        }
        private class SceneEvent
        {
            public string uid;
            public decimal time;
        }
        private class GameDialogArgs
        {
            public int index;
            public decimal time;
            public int dialogType;
            public int dialogIndex;
            public string text;
            public float speed;
            public int fontSize;
            public int dialogState;
            public int alignment;
        }
        private class SerializationData
        {
            public int SerializedFormat = 0;
            public byte[] SerializedBytes;
        }
        private class StageInfo
        {
            public List<MusicData> musicDatas = new();
            public decimal delay;
            public string mapName;
            public string music;
            public string scene;
            public int difficulty;
            public string md5;
            public float bpm;
            public List<SceneEvent> sceneEvents = new();
            public Dictionary<string, List<GameDialogArgs>> dialogEvents = new();

            public SerializationData serializationData = new();
            public byte[] MusicStream;
        }

        /// <summary>
        /// Expects the actual map file (a noteasset_assets_(mapname)_map(difficulty)_hash.bundle file)
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        /// <exception cref="FileLoadException"></exception>
        private static StageInfo ParseStageFromAssetBundle(string filepath, bool localPath = true) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();

            var rr = InitializeCompatibilityLayer();
            if (rr != MDCompatLayerInitResult.OK)
                throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

            AssetsManager manager = new();
            if (localPath) {
                string? find = StreamingFiles.Where(x => x.Contains(filepath)).FirstOrDefault();
                if (find == null)
                    throw new FileNotFoundException($"Can't find the mapdata for {filepath}");

                filepath = find;
            }

            manager.LoadFiles(filepath);
            var map = manager.assetsFileList[0].Objects.FirstOrDefault(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;

            var obj = map.ToType();
            var str = JsonConvert.SerializeObject(obj, Formatting.Indented);

            ConsoleSystem.Print($"STOPWATCH: ParseStageFromAssetBundle: {measureFunctionTime.Elapsed.TotalSeconds} seconds");
            return ParseStageFromRawData(str);
        }

        private static StageInfo ParseStageFromRawData(string rawData) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();

            var rr = InitializeCompatibilityLayer();
            if (rr != MDCompatLayerInitResult.OK)
                throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

            StageInfo stage = JsonConvert.DeserializeObject<StageInfo>(rawData);
            stage.musicDatas = OdinSerializer.SerializationUtility.DeserializeValue<List<MusicData>>(stage.serializationData.SerializedBytes, DataFormat.Binary);
            
            FillInTheBlankAudio(stage);
            FillInTheBlankNotes(stage);

            ConsoleSystem.Print($"STOPWATCH: ParseStageFromRawData: {measureFunctionTime.Elapsed.TotalSeconds} seconds");
            return stage;
        }
        public enum MDCompatLayerInitResult
        {
            OK,
            SteamNotInstalled,
            MuseDashNotInstalled,
            StreamingAssetsNotFound,
            NoteDataManagerNotFound,
            OperatingSystemNotCompatible
        }

        private static void FillInTheBlankAudio(StageInfo stage) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();
            //music should be music_assets_{stage.music}
            string? music_asset_file = StreamingFiles.Where(x => x.Contains($"music_assets_{stage.music}")).FirstOrDefault();
            if (music_asset_file == default)
                return;

            AssetsManager manager = new AssetsManager();
            manager.LoadFiles(music_asset_file);

            var audioclip = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.AudioClip) as AudioClip;
            var audiodata = audioclip.m_AudioData.GetData();

            if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
                FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                bank.Samples[0].RebuildAsStandardFileFormat(out stage.MusicStream, out var fileExtension);
            }

            ConsoleSystem.Print($"STOPWATCH: FillInTheBlankAudio: {measureFunctionTime.Elapsed.TotalSeconds} seconds");
        }
        private static void FillInTheBlankNotes(StageInfo stage) {
            foreach (var md in stage.musicDatas) {
                if (md.noteData == null && md.configData.note_uid != null) {
                    md.noteData = UIDToNote[md.configData.note_uid];
                }
            }
        }

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

        public static MDCompatLayerInitResult InitializeCompatibilityLayer() {
            if (Initialized)
                return MDCompatLayerInitResult.OK;

            MDCompatLayerInitResult result = MDCompatLayerInitResult.OperatingSystemNotCompatible;

            if (OperatingSystem.IsWindows())
                result = INIT_WINDOWS();

            if (result != MDCompatLayerInitResult.OK)
                return result;

            AssetsManager manager = new AssetsManager();
            manager.LoadFiles(NoteManagerAssetBundle);

            var monobehavior = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;
            var monobehavior_obj = (monobehavior.ToType()["serializationData"] as OrderedDictionary);
            var serializedList = monobehavior_obj["SerializedBytes"] as List<object>;
            byte[] serializedBytes = new byte[serializedList.Count];
            for (int i = 0; i < serializedList.Count; i++) serializedBytes[i] = (byte)serializedList[i];

            NoteDataManager = OdinSerializer.SerializationUtility.DeserializeValue<List<NoteConfigData>>(serializedBytes, DataFormat.Binary);

            foreach (var notedata in NoteDataManager) {
                IDToNote[notedata.id] = notedata;
                IBMSToNote[notedata.ibms_id] = notedata;
                UIDToNote[notedata.uid] = notedata;
                if (!IBMSToDesc.ContainsKey(notedata.ibms_id))
                    IBMSToDesc[notedata.ibms_id] = [];

                IBMSToDesc[notedata.ibms_id].Add(notedata.des);
            }

            Initialized = true;
            return result;
        }

        /// <summary>
        /// Converts a Muse Dash IBMS ID (base-36) into the <see cref="IBMSCode"/> it represents
        /// </summary>
        /// <param name="ibms_id"></param>
        /// <returns></returns>
        public static (IBMSCode code, string name) ConvertIBMSCode(string ibms_id) {
            int decValue = Base36StringToNumber(ibms_id);
            IBMSCode code = (IBMSCode)decValue;
            string name = Enum.GetName(typeof(IBMSCode), decValue);

            return (code, name == null ? "Unknown (" + ibms_id + ")" : name);
        }
        /// <summary>
        /// Converts a base-36 character into an integer
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int Base36CharToNumber(char c) {
            if (char.IsDigit(c))
                return (int)c - '0';

            if (char.IsLetter(c))
                return (int)char.ToUpper(c) - 'A' + 10;

            throw new Exception("Hex36 only supports [0-9][A-Z] as an input to c");
        }
        /// <summary>
        /// Converts a base-36 string into an integer
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int Base36StringToNumber(string s) {
            int mul = 1;
            int ret = 0;
            for (int i = s.Length - 1; i >= 0; i--) {
                ret += (mul * Base36CharToNumber(s[i]));
                mul *= 36;
            }
            return ret;
        }
        /// <summary>
        /// Converts an integer into a base-36 string
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string NumberToBase36String(int num) {
            StringBuilder sb = new StringBuilder();

            while (num > 0) {
                int remainder = num % 36;
                char c = (remainder < 10) ? (char)(remainder + '0') : (char)(remainder - 10 + 'A');
                sb.Insert(0, c);
                num /= 36;
            }

            return sb.ToString();
        }
        /// <summary>
        /// Muse Dash's IBMS codes, which defines behavior of certain entities
        /// </summary>
        public enum IBMSCode
        {
            None,
            SmallNormal,
            SmallUp,
            SmallDown,
            Medium1Normal,
            Medium1Up,
            Medium1Down,
            Medium2Normal,
            Medium2Up,
            Medium2Down,
            Large1,
            Large2,
            Raider,
            Hammer,
            Gemini,
            LongPress,
            Mul,
            Block,
            RaiderFlip,
            HammerFlip,
            DoubleSpeed1 = 24,
            DoubleSpeed2,
            DoubleSpeed3,
            RoadSpeed1,
            RoadSpeed2,
            RoadSpeed3,
            AirSpeed1,
            AirSpeed2,
            AirSpeed3,
            BossNear1 = 37,
            BossNear2,
            BossAttack1,
            BossAttack2_1,
            BossAttack2_2,
            BossMul1,
            BossMul2,
            BossBlock,
            BossIn = 46,
            BossOut,
            BossFar1Start,
            BossFar1End,
            BossFar2Start,
            BossFar2End,
            BossFar1To2,
            BossFar2To1,
            NoteHide = 55,
            NoteShow,
            BossHide,
            BossShow,
            ToggleScene1 = 60,
            ToggleScene2,
            ToggleScene3,
            ToggleScene4,
            ToggleScene5,
            ToggleScene6,
            ToggleScene7,
            ToggleScene8,
            ToggleScene9,
            ToggleScene10,
            TouhouRedPoint = 72,
            Ghost,
            Hp,
            Music,
            SceneHide = 77,
            SceneShow,
            CanvasUpScroll,
            CanvasDownScroll,
            CanvasScrollOver,
            RandomWave,
            RandomWaveOver,
            RgbSplit,
            RgbSplitOver,
            ShadowEdgeIn,
            ShadowEdgeOut,
            OldTv,
            OldTvOver,
            FlashStart,
            FlashHigh,
            FlashEnd,
            NoteFreeze,
            NoteUnfreeze,
            BgFreeze,
            BgUnfreeze,
            PixelStart,
            PixelEnd,
            GrayScaleStart,
            GrayScaleEnd,
            OpenAuto = 106,
            CloseAuto,
            TouhouLightNormal,
            TouhouLightUp,
            TouhouLightDown,
            TouhouLightCross,
            TouhouStarNormal,
            TouhouStarUp,
            TouhouStarDown,
            TouhouStarCross,
            TouhouBigNormal,
            TouhouBigUp,
            TouhouBigDown,
            TouhouBigCross,
            TouhouScalyNormal,
            TouhouScalyCross,
            TouhouKnifeNormal,
            TouhouKnifeCross,
            TouhouDivideDanmaku1,
            TouhouDivideDanmaku2,
            TouhouDivideDanmaku3
        }

        /// <summary>
        /// Dumps the <see cref="IBMSCode"/> enumeration to console
        /// </summary>
        public static void DumpIBMSCodes() {
            foreach (var mbr in Enum.GetValuesAsUnderlyingType(typeof(IBMSCode))) {
                Console.WriteLine($"{Enum.GetName(typeof(IBMSCode), mbr)}: {mbr} ({NumberToBase36String((int)mbr)})");
            }
        }

        /// <summary>
        /// Converts a Muse Dash unity asset bundle into a <see cref="DashSheet"/> for use in a <see cref="DashGame"/>
        /// </summary>
        /// <param name="bundlename"></param>
        /// <returns></returns>
        public static DashSheet ConvertAssetBundleToDashSheet(string bundlename) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();

            DashSheet sheet = new();
            StageInfo MDinfo = ParseStageFromAssetBundle(bundlename);

            sheet.Header.TempoChanges.Add(new(0, MDinfo.bpm));
            sheet.Music = SheetMusic.FromMemory(MDinfo.MusicStream);
            var sheetHeader = sheet.Header;

            bool first = true;
            Dictionary<int, List<MusicData>> LongPresses = new();
            foreach (var s in MDinfo.musicDatas) {
                if (s.noteData == null && first) {
                    sheetHeader.StartOffset = (float)s.tick;
                }

                if (s.noteData != null) {
                    var ib = MuseDashCompatibility.ConvertIBMSCode(s.noteData.ibms_id);
                    var tick_hit = (float)s.configData.time;
                    var tick_show = tick_hit - ((tick_hit - ((float)s.showTick - sheetHeader.StartOffset)) / (double)s.dt);

                    PathwaySide pathwayType = PathwaySide.Both;

                    switch (s.noteData.pathway) {
                        case 1:
                            pathwayType = PathwaySide.Top;
                            break;
                        case 0:
                            pathwayType = PathwaySide.Bottom;
                            break;
                    }

                    if (ib.code == IBMSCode.LongPress) {
                        if (!LongPresses.ContainsKey(s.configData.id))
                            LongPresses[s.configData.id] = [];

                        LongPresses[s.configData.id].Add(s);

                        continue;
                    }

                    //Switch case for entity type
                    EntityType type = EntityType.Unknown;
                    EventType? Event = null;

                    bool IsBoss = false;

                    switch (ib.code) {
                        case IBMSCode.SmallNormal:
                        case IBMSCode.SmallUp:
                        case IBMSCode.SmallDown:
                        case IBMSCode.Medium1Normal:
                        case IBMSCode.Medium2Normal:
                        case IBMSCode.Medium1Down:
                        case IBMSCode.Medium2Down:
                        case IBMSCode.Medium1Up:
                        case IBMSCode.Medium2Up:
                        case IBMSCode.Large1:
                        case IBMSCode.Large2:
                            type = EntityType.Single;
                            break;
                        case IBMSCode.Gemini:
                            type = EntityType.Double;
                            break;

                        case IBMSCode.Music:
                            type = EntityType.Score;
                            break;

                        case IBMSCode.Hammer:
                            type = EntityType.Hammer;
                            break;

                        case IBMSCode.Mul:
                            type = EntityType.Masher;
                            break;

                        case IBMSCode.Block:
                            type = EntityType.Gear;
                            break;

                        case IBMSCode.Ghost:
                            type = EntityType.Ghost;
                            break;

                        case IBMSCode.Raider:
                            type = EntityType.Raider;
                            break;

                        case IBMSCode.BossAttack1:
                        case IBMSCode.BossAttack2_1:
                        case IBMSCode.BossAttack2_2:
                            type = EntityType.Single;
                            IsBoss = true;
                            break;
                        case IBMSCode.BossBlock:
                            type = EntityType.Gear;
                            IsBoss = true;
                            break;

                        case IBMSCode.BossIn:
                            Event = EventType.BossIn;
                            break;
                        case IBMSCode.BossOut:
                            Event = EventType.BossOut;
                            break;
                        case IBMSCode.BossNear2:
                            Event = EventType.BossSingleHit;
                            break;
                        case IBMSCode.BossMul2:
                            Event = EventType.BossMasher;
                            break;

                        case IBMSCode.Hp:
                            type = EntityType.Heart;
                            break;

                        default:
                            Console.WriteLine("WARNING: An unidentified IBMS code with no compatibility translation definition was found during MD -> CD conversion.");
                            Console.WriteLine($"IBMS Code: {s.noteData.ibms_id} (as int: {(int)ib.code}, as name-definition: {ib.name})");
                            Console.WriteLine($"At time {tick_hit}, length of {s.configData.length}");
                            Console.WriteLine($"DataObjID: {s.objId}");
                            Console.WriteLine($"ConfigID: {s.configData.id}");
                            Console.WriteLine($"NoteID: {s.noteData.id}");
                            Console.WriteLine("");
                            ConsoleSystem.Print($"IBMS code with no translation: {s.noteData.ibms_id} (stored in enum as {ib.name})");
                            break;
                    }

                    if (Event != null) {
                        SheetEvent sheetEvent = new();
                        sheetEvent.Type = Event.Value;
                        sheetEvent.Time = tick_hit;
                        sheetEvent.Length = (double)s.configData.length;

                        if (s.noteData.damage > 0)
                            sheetEvent.Damage = s.noteData.damage;
                        if (s.noteData.fever > 0)
                            sheetEvent.Fever = s.noteData.fever;
                        if (s.noteData.score > 0)
                            sheetEvent.Score = s.noteData.score;

                        sheet.Events.Add(sheetEvent);
                    }
                    else {
                        //Switch case for direction type
                        EntityEnterDirection dir = EntityEnterDirection.RightSide;
                        switch (ib.code) {
                            case IBMSCode.SmallDown:
                            case IBMSCode.Medium1Down:
                            case IBMSCode.Medium2Down:
                            case IBMSCode.Hammer:
                                dir = EntityEnterDirection.TopDown;
                                break;

                            case IBMSCode.SmallUp:
                            case IBMSCode.Medium1Up:
                            case IBMSCode.Medium2Up:
                            case IBMSCode.HammerFlip:
                                dir = EntityEnterDirection.BottomUp;
                                break;
                        }

                        if (type != EntityType.Unknown) {
                            SheetEntity ent = new SheetEntity();
                            ent.Type = type;
                            ent.Pathway = pathwayType;
                            ent.EnterDirection = dir;
                            ent.HitTime = tick_hit;
                            ent.ShowTime = tick_show;

                            ent.Fever = s.noteData.fever;
                            ent.Damage = s.noteData.damage;
                            ent.Length = (double)s.configData.length;
                            ent.Score = s.noteData.score;

                            ent.RelatedToBoss = IsBoss;

                            sheet.Entities.Add(ent);
                        }
                        else {
                            
                        }
                    }
                }
                first = false;
            }

            foreach (var kvp in LongPresses) {
                int key = kvp.Key;
                List<MusicData> musicDatas = kvp.Value;
                musicDatas.Sort((x, y) => x.configData.id.CompareTo(y.configData.id));

                var firstItem = musicDatas.First();
                var lastItem = musicDatas.Last();

                double length = 0;
                if (firstItem != lastItem)
                    length = ((double)lastItem.tick + sheetHeader.StartOffset) - ((double)firstItem.tick + sheetHeader.StartOffset);

                var HitTime = (double)firstItem.configData.time;
                var ShowTime = (float)firstItem.showTick - sheetHeader.StartOffset;
                var transform1 = (HitTime - ShowTime) / (double)firstItem.dt;
                ShowTime = HitTime - transform1;

                sheet.Entities.Add(new SheetEntity() {
                    Type = EntityType.SustainBeam,
                    Pathway = firstItem.noteData.pathway == 0 ? PathwaySide.Bottom : PathwaySide.Top,
                    EnterDirection = EntityEnterDirection.RightSide,
                    HitTime = HitTime,
                    ShowTime = ShowTime,
                    Fever = firstItem.noteData.fever,
                    Damage = 0,
                    Length = (float)length,
                    Score = firstItem.noteData.score
                });
            }

            sheet.Header = sheetHeader;
            sheet.Entities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

            Console.WriteLine($"STOPWATCH: ConvertAssetBundleToDashSheet: Translated Muse Dash level to DashSheet in {measureFunctionTime.Elapsed.TotalSeconds} seconds");
            return sheet;
        }
    }
}
