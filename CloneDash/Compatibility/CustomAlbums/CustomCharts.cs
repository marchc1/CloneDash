using CloneDash.Common.Data;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.MDMC;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.FileSystem;
using Nucleus.Files;
using Raylib_cs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;

namespace CloneDash.Compatibility.CustomAlbums
{
	public static partial class CustomAlbumsCompatibility
	{
		public class CustomChartInfoJSON
		{
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
			public string difficulty5 { get; set; } = "";
			public string hideBmsMode { get; set; } = "";
			public string hideBmsDifficulty { get; set; } = "";
			public string hideBmsMessage { get; set; } = "";
			public List<string> searchTags { get; set; } = [];
		}
		private static StreamReader GetStreamReader(SearchPath archive, string filename) {
			return new StreamReader(archive.Open(filename, FileAccess.Read, FileMode.Open) ?? throw new Exception($"Custom Charts: Could not create a read stream for {filename}"));
		}
		private static string GetString(SearchPath archive, string filename) {
			return GetStreamReader(archive, filename).ReadToEnd();
		}
		private static byte[] GetByteArray(SearchPath archive, string filename) {
			var stream = archive.Open(filename, FileAccess.Read, FileMode.Open);
			if (stream == null) {
				return [];
			}

			using (var mem = new MemoryStream()) {
				stream.CopyTo(mem);
				return mem.ToArray();
			}
		}

		public class MD1_CustomChartsSong : MD1_Song
		{
			public string? Filepath { get; private set; }
			public SearchPath? Archive { get; private set; }
			public MDMCChart WebChart;
			public bool UsesWebChart = false;

			public MD1_CustomChartsSong(in MDMCChart webChart) : base(null!) {
				WebChart = webChart;
				UsesWebChart = true;

				Name = webChart.TitleRomanized ?? webChart.Title;
				Author = webChart.Artist;
			}

			public MD1_CustomChartsSong(string filepath) : base(null!) {
				Filepath = filepath;
				string? ext = Path.GetExtension(filepath);
				switch (ext) {
					case ".mdm":
						Archive = new ZipArchiveSearchPath(filepath);
						break;

					case ".bms":
					case ".json":
					case "":
						Archive = new DiskSearchPath(ext == "" ? filepath : Path.GetDirectoryName(filepath) ?? throw new Exception("Wtf?"));
						break;

					default: throw new NotImplementedException("Custom Charts: Bad filetype for CustomChartsSong constructor!");
				}
			}

			public MD1_CustomChartsSong(string pathID, string path) : base(null!) {
				string? ext = Path.GetExtension(path);
				switch (ext) {
					case ".mdm":
						Archive = new ZipArchiveSearchPath(pathID, path);
						break;
					case ".bms":
					case ".json":
					case "":
						Archive = new DiskSearchPath(filesystem.FindSearchPath(pathID, path), path);
						break;
					default: throw new NotImplementedException("Custom Charts: Bad filetype for CustomChartsSong constructor!");
				}
			}

			~MD1_CustomChartsSong() {
				MainThread.RunASAP(() => {
					if (CoverTexture != null && Raylib.IsTextureReady(CoverTexture.Texture)) Raylib.UnloadTexture(CoverTexture.Texture);

				});
			}

			protected override IAudioClip? ProduceAudioTrack() {
				if (Archive != null) {
					var musicBytes = GetByteArray(Archive, "music.ogg");
					if (musicBytes.Length == 0) // Try to find mp3 instead
						musicBytes = GetByteArray(Archive, "music.mp3");
					if (musicBytes.Length == 0)
						throw new FileNotFoundException("Custom Charts: Music could not be found! (searched for music.ogg, music.mp3)");

					using MemoryStream ms = new MemoryStream(musicBytes);
					return audiosystem.CreateStreamAudioClip(ms);
				}
				else {
					return WebChart.GetMusicTrack(false); // this wont even run
				}
			}

			protected override void ProduceCover(ChartCoverAvailableToMainThreadFn callback) {
				if (Archive != null) {
					var coverBytes = GetByteArray(Archive, "cover.png");
					Raylib.ImageRef img = new(".png", coverBytes);

					MainThread.RunASAP(() => {
						var tex = Raylib.LoadTextureFromImage(img);
						Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
						callback(new() {
							Texture = new Nucleus.ManagedMemory.Texture(EngineCore.Level.Textures, tex, true)
						});
						img.Dispose();
					});
				}
				else {
					WebChart.GetCoverAsTextureAsync((tex) => {
						if (tex == null) return;

						CoverTexture = new() {
							Texture = tex
						};
					});
				}
			}

			protected override IAudioClip? ProduceDemoTrack() {
				if (Archive != null) {
					var demoBytes = GetByteArray(Archive, "demo.ogg");
					if (demoBytes.Length == 0)
						demoBytes = GetByteArray(Archive, "demo.mp3");

					if (demoBytes.Length == 0)
						return null;

					using MemoryStream ms = new MemoryStream(demoBytes);
					return audiosystem.CreateStreamAudioClip(ms);
				}
				else {
					DeferringDemoToAsyncHandler = true;
					WebChart.GetMusicTrackAsync((demo) => {
						lock (AsyncLock) {
							DemoTrack = demo;
						}
					}, true);
					return null;
				}
			}

			bool corruptInfo = false;
			protected override MD1_SongInfo? ProduceInfo() {
				if (corruptInfo) return null;
				if (Archive != null) {
					CustomChartInfoJSON? info = null;
					try {
						info = JsonConvert.DeserializeObject<CustomChartInfoJSON>(GetString(Archive, "info.json")) ?? throw new Exception("Bad info.json!");
					}
					catch (Exception ex) {
						corruptInfo = true;
						Logs.Error($"Custom Charts: The CustomCharts SearchPath '{Archive.ToString()}' failed to produce info.json: {ex.Message}");
					}
					if (info == null)
						return null;

					Name = info.name;
					Author = info.author;
					MD1_SongInfo ret = new() {
						BPM = info.bpm,
						LevelDesigners = [info.levelDesigner1, info.levelDesigner2, info.levelDesigner3, info.levelDesigner4],
						Scene = info.scene,
						SearchTags = info.searchTags.ToArray(),
						Difficulty1 = info.difficulty1,
						Difficulty2 = info.difficulty2,
						Difficulty3 = info.difficulty3,
						Difficulty4 = info.difficulty4,
						Difficulty5 = info.difficulty5
					};

					return ret;
				}
				else {
					return new() {
						BPM = "",
						Difficulty1 = "",
						Difficulty2 = "",
						Difficulty3 = "",
						Difficulty4 = "",
						Difficulty5 = "",
						LevelDesigners = ["", "", "", "", ""],
						Music = "",
						Scene = "",
						SearchTags = [""]
					};
				}
			}

			private bool __downloading = false;

			public static string GetDownloadCachePath(string localPath) {
				var download = filesystem.GetSearchPathID("download").FirstOrDefault() as DiskSearchPath ?? throw new Exception("Cannot find download cache directory?");

				return Path.Combine(download.ResolveToAbsolute("charts"), $"{localPath}.mdm");
			}

			public void DownloadOrPullFromCache(Action<MD1_CustomChartsSong> complete) {
				if (Archive == null) {
					if (__downloading) {
						Logs.Error("Already downloading, please wait.");
						return;
					}
					// Ensure Archive is populated from either a download or a cache
					var filename = GetDownloadCachePath(WebChart.ID);
					__downloading = true;
					var mdmFilename = $"{WebChart.ID}.mdm";
					var canStream = filesystem.CanOpen("charts", mdmFilename);
					if (!canStream) {
						WebChart.DownloadTo(filename, (worked) => {
							System.Diagnostics.Debug.Assert(worked);
							if (worked) {
								// Invalidate everything
								Filepath = filename;
								Archive = new ZipArchiveSearchPath("charts", mdmFilename);
								Clear();
								complete(this);
								Logs.Info($"Downloaded {mdmFilename}");
							}
							else Logs.Warn($"Couldn't download {mdmFilename}");
						});
						Logs.Info($"Downloading {mdmFilename}..");
					}
					else {
						Logs.Info($"Already cached {mdmFilename}");

						// Invalidate everything
						Filepath = filename;
						Archive = new ZipArchiveSearchPath("charts", mdmFilename);
						Clear();
						complete?.Invoke(this);
					}
				}
				else {
					complete?.Invoke(this);
				}
			}

			public MD1_SongChart LoadFromDiskBMS(string diskpath) {
				Archive = new DiskSearchPath(Path.GetDirectoryName(diskpath)!);
				var map = Archive.Open(Path.GetFileName(diskpath), FileAccess.Read, FileMode.Open);
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (map == null)
					throw new Exception("Bad map difficulty.");

				return loadFromStream(map);
			}

			private MD1_CustomAlbumsChart loadFromStream(Stream map, int difficulty = 0) {
				var bms = BmsLoader.Load(map, "", out var bpmChanges);
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (bms == null) throw new Exception("BMS parsing exception");
				var stageInfo = BmsLoader.TransmuteData(bms);
				stageInfo.mapName = Name;
				stageInfo.difficulty = difficulty;
				stageInfo.scene = bms.Info["GENRE"]?.GetValue<string>() ?? string.Empty;
				stageInfo.bpm = bms.Bpm;

				return new(this, difficulty, bpmChanges, bms, stageInfo);
			}

			protected override MD1_SongChart ProduceSheet(int id) {
				// DownloadOrPullFromCache();
				var map = Archive.Open($"map{id}.bms", FileAccess.Read, FileMode.Open);
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (map == null)
					throw new Exception("Bad map difficulty.");

				return loadFromStream(map, id);
			}

			public override MD1_GamemodeData? ProduceGamemodeData(MD1_SongChart chart, int mapID) {
				if (chart is not MD1_CustomAlbumsChart caChart)
					return null;

				var bms = caChart.bms;
				var bpmChanges = caChart.tempoChanges;
				var stageInfo = caChart.stageInfo;

				double lastTime = 0;
				double lastBeat = 0;
				double lastBPM = bms.Bpm;
				TempoChange[] newChanges = new TempoChange[bpmChanges.Count];
				for (int i = 0; i < bpmChanges.Count; i++) {
					var change = bpmChanges[i];
					double deltaBeats = change.Time - lastBeat;
					lastTime += deltaBeats * 60.0 * 4 / lastBPM;
					lastBeat = change.Time;
					lastBPM = change.BPM;
					newChanges[i] = new TempoChange(lastTime, change.Beat, lastBPM);
				}

				TimeSignatureChange[] newSignatureChanges = new TimeSignatureChange[bms.NotesPercent.Count];

				Interlude.Spin(submessage: "Reading Custom Albums chart...");

				// We should be able to pass the transmuted data into this and not have to re-invent the wheel just for customs!
				return MuseDashCompatibility.ConvertStageInfoToMD1GamemodeData(this, stageInfo, newChanges);
			}
		}
	}
}

public class MD1_CustomAlbumsChart : MD1_SongChart
{
	internal readonly List<TempoChange>? tempoChanges;
	internal readonly Bms bms;
	internal readonly StageInfo stageInfo;

	public MD1_CustomAlbumsChart(MD1_Song song, int difficultyID, List<TempoChange> tempoChanges, Bms bms, StageInfo stageInfo) : base(song, difficultyID) {
		this.tempoChanges = tempoChanges;
		this.bms = bms;
		this.stageInfo = stageInfo;
	}
}
