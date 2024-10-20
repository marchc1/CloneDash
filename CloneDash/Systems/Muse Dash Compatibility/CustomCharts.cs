
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.ManagedMemory;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using CloneDash.Data;
namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        public class CustomChartInfoJSON {
            public string name { get; set; } = "";
            public string name_romanized { get; set; } = "";
            public string author { get; set; } = "";
            public string bpm { get; set; } = "";
            public string scene { get; set; } = "";
            public string levelDesigner { get; set; } = "";
            public string levelDesigner1 { get; set; } = "";
            public string levelDesigner2 { get; set; } = "";
            public string levelDesigner3 { get; set; } = "";
            public string levelDesigner4 { get; set; } = "";
            public string difficulty1 { get; set; } = "";
            public string difficulty2 { get; set; } = "";
            public string difficulty3 { get; set; } = "";
            public string difficulty4 { get; set; } = "";
            public string hideBmsMode { get; set; } = "";
            public string hideBmsDifficulty { get; set; } = "";
            public string hideBmsMessage { get; set; } = "";
            public List<string> searchTags { get; set; } = [];
        }
        private static StreamReader GetStreamReader(ZipArchive archive, string filename) {
            return new StreamReader(archive.Entries.FirstOrDefault(x => x.Name == filename)?.Open() ?? throw new Exception($"Could not create a read stream for {filename}"));
        }
        private static string GetString(ZipArchive archive, string filename) {
            return GetStreamReader(archive, filename).ReadToEnd();
        }
        private static byte[] GetByteArray(ZipArchive archive, string filename) {
            var stream = archive.Entries.FirstOrDefault(x => x.Name == filename)?.Open() ?? throw new Exception($"Could not create a read stream for {filename}");
            using (var mem = new MemoryStream()) {
                stream.CopyTo(mem);
                return mem.ToArray();
            }
        }

        public class CustomChartsSong : ChartSong
        {
            public string Filepath { get; private set; }
            public ZipArchive Archive { get; private set; }
            public CustomChartsSong(string filepath) {
                Filepath = filepath;
                Archive = ZipFile.Open(filepath, ZipArchiveMode.Read);
                
            }
            protected override MusicTrack ProduceAudioTrack() {
                var demoBytes = GetByteArray(Archive, "music.ogg");
                return EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);
            }

            protected override ChartCover? ProduceCover() {
                var coverBytes = GetByteArray(Archive, "cover.png");
                var img = Raylib.LoadImageFromMemory(".png", coverBytes);
                var tex = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);

                return new() {
                    Texture = tex
                };
            }

            protected override MusicTrack? ProduceDemoTrack() {
                var demoBytes = GetByteArray(Archive, "demo.ogg");
                return EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);
            }

            protected override ChartInfo? ProduceInfo() {
                var info = JsonConvert.DeserializeObject<CustomChartInfoJSON>(GetString(Archive, "info.json")) ?? throw new Exception("Bad info.json!");
                Name = info.name;
                Author = info.author;
                ChartInfo ret = new() {
                    BPM = decimal.Parse(info.bpm),
                    LevelDesigners = [info.levelDesigner1, info.levelDesigner2, info.levelDesigner3, info.levelDesigner4],
                    Scene = info.scene,
                    SearchTags = info.searchTags.ToArray(),
                    Difficulty1 = info.difficulty1,
                    Difficulty2 = info.difficulty2,
                    Difficulty3 = info.difficulty3,
                    Difficulty4 = info.difficulty4,
                };

                return ret;
            }

            protected override ChartSheet ProduceSheet(int id) {
                var map = Archive.Entries.FirstOrDefault(x => x.Name == $"map{id}.bms");

                throw new Exception("BMS parsing is not yet implemented");
            }
        }
    }
}
