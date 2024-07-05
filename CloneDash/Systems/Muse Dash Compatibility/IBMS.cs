using AssetStudio;
using CloneDash.Game;
using CloneDash.Game.Entities;
using CloneDash.Game.Enumerations;
using CloneDash.Game.Sheets;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using OdinSerializer;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using Texture2D = AssetStudio.Texture2D;

namespace CloneDash
{
    /// <summary>
    /// Muse Dash style level converter
    /// </summary>
    public static partial class MuseDashCompatibility
    {
        public static string NoteManagerAssetBundle { get; private set; } = "";
        private static Dictionary<string, List<string>> IBMSToDesc { get; set; } = new();
        private static Dictionary<string, NoteConfigData> IDToNote { get; set; } = new();
        private static Dictionary<string, NoteConfigData> IBMSToNote { get; set; } = new();
        private static Dictionary<string, NoteConfigData> UIDToNote { get; set; } = new();

        private static List<NoteConfigData> NoteDataManager { get; set; }

        public static string[] StreamingFiles { get; private set; }

        public class MusicConfigData
        {
            public int id;
            public decimal time;
            public string note_uid;
            public decimal length;
            public bool blood;
            public int pathway;
        }
        public class NoteConfigData
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
        public class MusicData
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
        public class SceneEvent
        {
            public string uid;
            public decimal time;
        }
        public class GameDialogArgs
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
        public class SerializationData
        {
            public int SerializedFormat = 0;
            public byte[] SerializedBytes;
        }
        public class StageInfo
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

        public class StageDemo
        {
            public MusicTrack Track { get; set; }
            public Raylib_cs.Texture2D Cover { get; set; }
        }

        public static StageDemo GetStageDemo(MuseDashSong song) {
            if (song.DemoObject != null)
                return song.DemoObject;

            StageDemo demo = new StageDemo();
            //demo.Track = LoadAssetEasyC<AudioClip, MusicTrack>(song.Demo.Replace("_demo", "") + "_assets_all");
            demo.Cover = song.GetCover();
            song.DemoObject = demo;

            return demo;
        }


        private static void FillInTheBlankAudio(MuseDashSong song, StageInfo stage) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();

            var audioclip = song.AssetsFile.assetsFileList[0].Objects.First(x => x.type == ClassIDType.AudioClip) as AudioClip;
            var audiodata = audioclip.m_AudioData.GetData();

            if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
                FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                bank.Samples[0].RebuildAsStandardFileFormat(out stage.MusicStream, out var fileExtension);
            }

            Logs.Info($"STOPWATCH: FillInTheBlankAudio: {measureFunctionTime.Elapsed.TotalSeconds} seconds");
        }
        private static void FillInTheBlankNotes(MuseDashSong song, StageInfo stage) {
            foreach (var md in stage.musicDatas) {
                if (md.noteData == null && md.configData.note_uid != null) {
                    md.noteData = UIDToNote[md.configData.note_uid];
                }
            }
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
        public static DashSheet ConvertStageInfoToDashSheet(StageInfo MDinfo) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();

            DashSheet sheet = new();

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
                            Logs.Warn("WARNING: An unidentified IBMS code with no compatibility translation definition was found during MD -> CD conversion.");
                            Logs.Info($"IBMS Code: {s.noteData.ibms_id} (as int: {(int)ib.code}, as name-definition: {ib.name})");
                            Logs.Info($"At time {tick_hit}, length of {s.configData.length}");
                            Logs.Info($"DataObjID: {s.objId}");
                            Logs.Info($"ConfigID: {s.configData.id}");
                            Logs.Info($"NoteID: {s.noteData.id}");
                            Console.WriteLine("");
                            //ConsoleSystem.Print($"IBMS code with no translation: {s.noteData.ibms_id} (stored in enum as {ib.name})");
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
                            ent.DebuggingInfo = $"ib.code: {ib.code}";
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

        public enum MuseDashDifficulty
        {
            Unknown = 0,
            Easy = 1,
            Normal = 2,
            Hard = 3,
            Hidden = 4
        }

        public class MuseDashAlbum
        {
            [JsonPropertyName("uid")] public string UID { get; set; } = "";
            [JsonPropertyName("title")] public string Title { get; set; } = "";
            [JsonPropertyName("tag")] public string Tag { get; set; } = "";
            [JsonPropertyName("jsonName")] public string JsonName { get; set; } = "";
            [JsonPropertyName("prefabsName")] public string PrefabsName { get; set; } = "";

            public List<MuseDashSong> Songs { get; set; } = [];

            public override string ToString() => $"{Title} [{Songs.Count} songs]";
        }

        public class MuseDashSong
        {

            public static string? GetFixedFilename(string givenBase, string fileName, [NotNullWhen(true)] bool throwExp = true) {
                return
                    StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase)))
                    ?? StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase.Replace("_music", ""))))
                    ?? (throwExp ? throw new Exception($"Tried to find {givenBase}, could not find a match even with fixes applied") : null);
            }
            public string GetAssetsFilepath() => GetFixedFilename(BaseName, "music_{name}_assets_all.bundle", true) ?? throw new Exception();
            public string? GetDemoFilepath() => GetFixedFilename(BaseName, "song_{name}_assets_all", false);

            public MuseDashAlbum Album { get; set; }

            [JsonPropertyName("uid")] public string UID { get; set; } = "";
            [JsonPropertyName("name")] public string Name { get; set; } = "";
            [JsonPropertyName("author")] public string Author { get; set; } = "";
            [JsonPropertyName("bpm")] public string BPM { get; set; } = "";
            [JsonPropertyName("music")] public string Music { get; set; } = "";
            [JsonPropertyName("demo")] public string Demo { get; set; } = "";
            [JsonPropertyName("cover")] public string Cover { get; set; } = "";
            [JsonPropertyName("noteJson")] public string NoteJSON { get; set; } = "";
            [JsonPropertyName("scene")] public string Scene { get; set; } = "";
            [JsonPropertyName("levelDesigner")] public string LevelDesigner { get; set; } = "";

            [JsonPropertyName("difficulty1")] public string Difficulty1 { get; set; } = "";
            [JsonPropertyName("difficulty2")] public string Difficulty2 { get; set; } = "";
            [JsonPropertyName("difficulty3")] public string Difficulty3 { get; set; } = "";
            [JsonPropertyName("difficulty4")] public string Difficulty4 { get; set; } = "";
            public StageDemo? DemoObject { get; internal set; }

            [JsonIgnore]
            public string BaseName => Music.Substring(0, Music.Length - 6);
            public override string ToString() => $"{Name} by {Author}";


            public AssetsManager AssetsFile { get; private set; } = null;
            public AssetsManager DemoFile { get; private set; } = null;

            private void LoadAssetFile() {
                if (AssetsFile == null) {
                    AssetsFile = new();
                    string filepath = GetAssetsFilepath();
                    AssetsFile.LoadFiles(filepath);
                }
                if (DemoFile == null) {
                    string? filepath = GetDemoFilepath();
                    if (filepath != null) {
                        DemoFile = new();
                        DemoFile.LoadFiles(filepath);
                    }
                }
            }

            private Raylib_cs.Texture2D? __cover;
            private MusicTrack? __demotrack;

            public Raylib_cs.Texture2D GetCover() {
                LoadAssetFile();
                if (__cover.HasValue)
                    return __cover.Value;

                Texture2D tex2D = (Texture2D)DemoFile.assetsFileList[0].Objects.First(x => x is Texture2D tex2D && tex2D.m_Name.EndsWith("_cover"));  //.Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
                var imgData = AssetStudio.Texture2DExtensions.ConvertToStream(tex2D, ImageFormat.Png, true).ToArray();
                var img = Raylib.LoadImageFromMemory(".png", imgData);
                var tex = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                __cover = tex;
                return tex;
            }

            public MusicTrack? GetDemoMusic() {
                LoadAssetFile();
                if (IValidatable.IsValid(__demotrack))
                    return __demotrack;
                if (DemoFile == null)
                    return null;

                AudioClip audioClip = (AudioClip)DemoFile.assetsFileList[0].Objects.First(x => x is AudioClip audio && audio.m_Name.EndsWith("_demo"));  //.Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
                byte[] musicStream;
                var audiodata = audioClip.m_AudioData.GetData();

                if (audioClip.m_Type == FMODSoundType.UNKNOWN) {
                    FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                    bank.Samples[0].RebuildAsStandardFileFormat(out musicStream, out var fileExtension);

                    __demotrack = EngineCore.Level.Sounds.LoadMusicFromMemory(musicStream);
                    return __demotrack;
                }

                throw new Exception("Something went wrong loading an AudioClip");
            }

            public DashSheet GetDashSheet(int mapID) {
                MonoBehaviour map = (MonoBehaviour)AssetsFile.assetsFileList[0].Objects.First(x => x is MonoBehaviour mB && mB.m_Name.EndsWith($"_map{mapID}"));
                var obj = map.ToType();
                var rawData = JsonConvert.SerializeObject(obj, Formatting.Indented);

                var rr = InitializeCompatibilityLayer();
                if (rr != MDCompatLayerInitResult.OK)
                    throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

                StageInfo stage = JsonConvert.DeserializeObject<StageInfo>(rawData);
                stage.musicDatas = OdinSerializer.SerializationUtility.DeserializeValue<List<MusicData>>(stage.serializationData.SerializedBytes, DataFormat.Binary);

                FillInTheBlankAudio(this, stage);
                FillInTheBlankNotes(this, stage);

                return ConvertStageInfoToDashSheet(stage);
            }
        }

        public static List<MuseDashAlbum> Albums { get; private set; } = [];
        public static List<MuseDashSong> Songs { get; private set; } = [];

        private static ClassIDType GetClassIDFromType(Type t) {
            switch (t.Name) {
                case "AudioClip": return ClassIDType.AudioClip;
                case "TextAsset": return ClassIDType.TextAsset;
                case "Texture2D": return ClassIDType.Texture2D;
            }
            return ClassIDType.UnknownType;
        }

        private static AssetType __internalLoadAsset<AssetType>(string query, bool regex = false) {
            AssetsManager manager = new();
            string? filepath = StreamingFiles.First(x => regex ? Regex.IsMatch(x, query) : x.Contains(query));
            if (filepath == null)
                throw new FileNotFoundException($"No file matched the regular expression/query for \"{query}\"");
            manager.LoadFiles(filepath);

            AssetType item = (AssetType)(object)manager.assetsFileList[0].Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
            if (item == null)
                throw new NotImplementedException($"Could not convert! Is there a type conversion definition for {typeof(AssetType).Name}?");

            return item;
        }


        public static ReturnStructure LoadAssetEasyS<AssetType, ReturnStructure>(string query, bool regex = false) where AssetType : class where ReturnStructure : struct {
            AssetType item = __internalLoadAsset<AssetType>(query, regex);

            switch (item) {
                case Texture2D texture2D:
                    var imgData = AssetStudio.Texture2DExtensions.ConvertToStream(texture2D, ImageFormat.Png, true).ToArray();
                    var img = Raylib.LoadImageFromMemory(".png", imgData);
                    var tex = Raylib.LoadTextureFromImage(img);
                    return (ReturnStructure)(object)tex;
                default:
                    throw new NotImplementedException($"There is not a struct ReturnStructure generator for {typeof(AssetType).Name}!");
            }
        }
        public static ReturnStructure LoadAssetEasyC<AssetType, ReturnStructure>(string query, bool regex = false) where AssetType : class where ReturnStructure : class {
            AssetType item = __internalLoadAsset<AssetType>(query, regex);

            switch (item) {
                case AudioClip audioClip:
                    if (typeof(ReturnStructure) != typeof(MusicTrack)) throw new NotImplementedException("AudioClip returns a MusicTrack and cannot return a different type.");

                    byte[] musicStream;
                    var audiodata = audioClip.m_AudioData.GetData();

                    if (audioClip.m_Type == FMODSoundType.UNKNOWN) {
                        FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                        bank.Samples[0].RebuildAsStandardFileFormat(out musicStream, out var fileExtension);

                        return EngineCore.Level.Sounds.LoadMusicFromMemory(musicStream) as ReturnStructure;
                    }

                    throw new Exception("Something went wrong loading an AudioClip");
                case TextAsset textAsset:
                    return JsonConvert.DeserializeObject<ReturnStructure>(Encoding.UTF8.GetString(textAsset.m_Script));
                default:
                    throw new NotImplementedException($"There is not a class ReturnStructure generator for {typeof(AssetType).Name}!");
            }
        }

        private struct __musedashSong
        {
            public string name;
            public string author;
        }

        public static void BuildDashStructures() {
            Stopwatch s = new Stopwatch();
            s.Start();

            Albums = LoadAssetEasyC<TextAsset, List<MuseDashAlbum>>("config_others_assets_albums_");
            Albums.RemoveAll(x => x.JsonName == "");

            foreach (var album in Albums) {
                var songs = LoadAssetEasyC<TextAsset, List<MuseDashSong>>($"config_others_assets_{album.JsonName.ToLower()}_");
                var songsEN = LoadAssetEasyC<TextAsset, __musedashSong[]>($"config_others_assets_{album.JsonName.ToLower()}_");

                for (int i = 0; i < songs.Count; i++) {
                    songs[i].Name = songsEN[i].name;
                    songs[i].Author = songsEN[i].author;
                    songs[i].Album = album;
                }

                Songs.AddRange(songs);
            }

            Songs.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }
}
