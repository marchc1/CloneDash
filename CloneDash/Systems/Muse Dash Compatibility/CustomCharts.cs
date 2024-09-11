using CloneDash.Game.Sheets;
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
        private static void BuildMap(MuseDashSong song, ZipArchiveEntry? entry, int lvl) {
            if (entry == null) return;

            DashSheet sheet = new DashSheet();
        }
        public static MuseDashSong GetCustomAlbumsSong(string filepath) {
            var archive = ZipFile.Open(filepath, ZipArchiveMode.Read);
            var info = JsonConvert.DeserializeObject<CustomChartInfoJSON>(GetString(archive, "info.json")) ?? throw new Exception("Bad info.json!");

            var song = new MuseDashSong();
            song.Unmanaged = true;
            song.Author = info.author;
            song.Name = info.name;
            song.Music = "music.ogg";

            List<string> parts = [];
            if (info.levelDesigner != "?") parts.Add(info.levelDesigner);
            if (info.levelDesigner1 != "?") parts.Add(info.levelDesigner1);
            if (info.levelDesigner2 != "?") parts.Add(info.levelDesigner2);
            if (info.levelDesigner3 != "?") parts.Add(info.levelDesigner3);
            if (info.levelDesigner4 != "?") parts.Add(info.levelDesigner4);
            song.LevelDesigner = string.Join(", ", parts);

            song.Difficulty1 = info.difficulty1;
            song.Difficulty2 = info.difficulty2;
            song.Difficulty3 = info.difficulty3;
            song.Difficulty4 = info.difficulty4;

            song.Scene = info.scene;
            song.BPM = info.bpm;

            // Read and load demo audio
            var demoBytes = GetByteArray(archive, "demo.ogg");
            song.DemoTrackOverride = EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);

            // Read and load demo cover
            var coverBytes = GetByteArray(archive, "cover.png");
            var img = Raylib.LoadImageFromMemory(".png", coverBytes);
            var tex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            song.CoverTextureOverride = tex;

            // Read and load main audio
            var musicBytes = GetByteArray(archive, "demo.ogg");
            song.MusicTrackOverride = EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);

            var map1 = archive.Entries.FirstOrDefault(x => x.Name == "map1.bms");
            var map2 = archive.Entries.FirstOrDefault(x => x.Name == "map1.bms");
            var map3 = archive.Entries.FirstOrDefault(x => x.Name == "map1.bms");
            var map4 = archive.Entries.FirstOrDefault(x => x.Name == "map1.bms");

            BuildMap(song, map1, 1);
            BuildMap(song, map2, 2);
            BuildMap(song, map3, 3);
            BuildMap(song, map4, 4);

            return song;
        }
    }
}
