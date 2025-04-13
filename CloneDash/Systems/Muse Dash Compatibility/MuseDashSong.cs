using AssetStudio;
using CloneDash.Data;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using OdinSerializer;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Quic;
using System.Text.Json.Serialization;
using Color = Raylib_cs.Color;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using Texture2D = AssetStudio.Texture2D;

namespace CloneDash
{
	public static partial class MuseDashCompatibility
    {
        public class MuseDashSongInfoJSON {
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
            [JsonPropertyName("difficulty5")] public string Difficulty5 { get; set; } = "";
        }
        public class MuseDashSong : ChartSong
        {
            private MuseDashSongInfoJSON __jsonInfo;
            public MuseDashSong(MuseDashSongInfoJSON info) {
                __jsonInfo = info;
				// Debug.Assert(info.Difficulty5 == "");
            }
            public static string? GetFixedFilename(string givenBase, string fileName, [NotNullWhen(true)] bool throwExp = true) {
                return
                    StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase)))
                    ?? StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase.Replace("_music", ""))))
                    ?? (throwExp ? throw new Exception($"Tried to find {givenBase}, could not find a match even with fixes applied") : null);
            }
            public string GetAssetsFilepath() => GetFixedFilename(BaseName, "music_{name}_assets_all.bundle", true) ?? throw new Exception();
            public string? GetDemoFilepath() => GetFixedFilename(BaseName, "song_{name}_assets_all", false);

            public MuseDashAlbum Album { get; set; }

            
            public StageDemo? DemoObject { get; internal set; }

            [JsonIgnore]
            public string BaseName => GetInfo().Music.Substring(0, GetInfo().Music.Length - 6);
            public override string ToString() => $"{Name} by {Author}";


            public AssetsManager AssetsFile { get; private set; } = null;
            public AssetsManager DemoFile { get; private set; } = null;

            public bool Unmanaged { get; set; } = false;

            private void LoadAssetFile() {
                if (Unmanaged) return;
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
					else Logs.Warn($"CloneDash: MuseDashSong.LoadAssetFile could not generate a demo filepath for {Name}.");
                }
            }

            public MusicTrack? MusicTrackOverride { get; set; }

            public Dictionary<int, ChartSheet> DashSheetOverrides { get; set; } = [];

            protected override MusicTrack ProduceAudioTrack() {
                LoadAssetFile();
                if (IValidatable.IsValid(AudioTrack))
                    return AudioTrack;
                if (AssetsFile == null)
                    return null;

                var audioclip = AssetsFile.assetsFileList[0].Objects.First(x => x.type == ClassIDType.AudioClip) as AudioClip;
                var audiodata = audioclip.m_AudioData.GetData();

                if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
                    FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                    bank.Samples[0].RebuildAsStandardFileFormat(out var at, out var fileExtension);
                    AudioTrack = EngineCore.Level.Sounds.LoadMusicFromMemory(at);
                    return AudioTrack;
                }
                throw new Exception("ProduceAudioTrack: Could not load audio track");
            }

            protected override MusicTrack? ProduceDemoTrack() {
                LoadAssetFile();
                if (IValidatable.IsValid(DemoTrack))
                    return DemoTrack;
                if (DemoFile == null)
                    return null;

                AudioClip audioClip = (AudioClip)DemoFile.assetsFileList[0].Objects.First(x => x is AudioClip audio && audio.m_Name.EndsWith("_demo"));  //.Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
                byte[] musicStream;
                var audiodata = audioClip.m_AudioData.GetData();

                if (audioClip.m_Type == FMODSoundType.UNKNOWN) {
                    FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
                    bank.Samples[0].RebuildAsStandardFileFormat(out musicStream, out var fileExtension);

                    DemoTrack = EngineCore.Level.Sounds.LoadMusicFromMemory(musicStream);
                    return DemoTrack;
                }

                throw new Exception("ProduceDemoTrack: Could not load demo track");
            }

            protected override ChartCover? ProduceCover() {
				if (DemoFile == null) return null;

				LoadAssetFile();
                if (CoverTexture != null)
                    return CoverTexture;

                Texture2D tex2D = (Texture2D)DemoFile.assetsFileList[0].Objects.First(x => x is Texture2D tex2D && tex2D.m_Name.EndsWith("_cover"));  //.Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
				
				var imgData = tex2D.image_data.GetData();
				var img = imgData.ToImage(tex2D.m_Width, tex2D.m_Height, tex2D.m_TextureFormat switch {
					TextureFormat.DXT5 => PixelFormat.PIXELFORMAT_COMPRESSED_DXT5_RGBA
				}, tex2D.m_MipCount);
                var tex = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                CoverTexture = new() {
                    Texture = tex
                };

                return CoverTexture;
            }

            protected override ChartSheet ProduceSheet(int mapID) {
                if (DashSheetOverrides.TryGetValue(mapID, out ChartSheet? sheet))
                    return sheet;

				LoadAssetFile();

				MonoBehaviour map = (MonoBehaviour)AssetsFile.assetsFileList[0].Objects.First(x => x is MonoBehaviour mB && mB.m_Name.EndsWith($"_map{mapID}"));
                var obj = map.ToType();
                var rawData = JsonConvert.SerializeObject(obj, Formatting.Indented);

                var rr = InitializeCompatibilityLayer();
                if (rr != MDCompatLayerInitResult.OK)
                    throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

                StageInfo stage = JsonConvert.DeserializeObject<StageInfo>(rawData);
                stage.musicDatas = OdinSerializer.SerializationUtility.DeserializeValue<List<MusicData>>(stage.serializationData.SerializedBytes, DataFormat.Binary);

                FillInTheBlankNotes(this, stage);

                return ConvertStageInfoToDashSheet(this, stage);
            }

            protected override ChartInfo? ProduceInfo() {
                List<string> SearchTags = [];
                SearchTags.AddRange(__jsonInfo.Name.Split(' '));
                ChartInfo info = new ChartInfo() {
                    BPM = decimal.TryParse(__jsonInfo.BPM, out var bpm) ? bpm : 0,
                    Music = __jsonInfo.Music,
                    LevelDesigners = [__jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner],
                    Difficulty1 = __jsonInfo.Difficulty1,
                    Difficulty2 = __jsonInfo.Difficulty2,
                    Difficulty3 = __jsonInfo.Difficulty3,
                    Difficulty4 = __jsonInfo.Difficulty4,
                    Difficulty5 = __jsonInfo.Difficulty5,
                    Scene = __jsonInfo.Scene,
                    SearchTags = SearchTags.ToArray()
                };

                return info;
            }
        }
    }
}
