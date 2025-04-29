using Newtonsoft.Json;
using Nucleus;
using Raylib_cs;
using System.IO.Compression;
using CloneDash.Data;
using Nucleus.Audio;
using CloneDash.Systems.CustomCharts;
using CloneDash.Systems.CustomAlbums;
using Nucleus.Core;

namespace CloneDash
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
			return new StreamReader(archive.Open(filename, FileAccess.Read, FileMode.Open) ?? throw new Exception($"Could not create a read stream for {filename}"));
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

		public class CustomChartsSong : ChartSong
		{
			public string? Filepath { get; private set; }
			public SearchPath? Archive { get; private set; }
			public MDMCChart WebChart;
			public bool UsesWebChart = false;

			public CustomChartsSong(MDMCChart webChart) {
				WebChart = webChart;
				UsesWebChart = true;

				Name = webChart.TitleRomanized ?? webChart.Title;
				Author = webChart.Artist;
			}

			public CustomChartsSong(string filepath) {
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
					default: throw new NotImplementedException("Bad filetype for CustomChartsSong constructor!");
				}
			}

			public CustomChartsSong(string pathID, string path) {
				string? ext = Path.GetExtension(path);
				switch (ext) {
					case ".mdm":
						Archive = new ZipArchiveSearchPath(pathID, path);
						break;
					case ".bms":
					case ".json":
					case "":
						Archive = new DiskSearchPath(Filesystem.FindSearchPath(pathID, path), path);
						break;
					default: throw new NotImplementedException("Bad filetype for CustomChartsSong constructor!");
				}
			}

			~CustomChartsSong() {
				MainThread.RunASAP(() => {
					if (CoverTexture != null && Raylib.IsTextureReady(CoverTexture.Texture)) Raylib.UnloadTexture(CoverTexture.Texture);
				});
			}

			protected override MusicTrack ProduceAudioTrack() {
				if (Archive != null) {
					var demoBytes = GetByteArray(Archive, "music.ogg");
					return EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);
				}
				else {
					return WebChart.GetMusicTrack(false); // this wont even run
				}
			}

			protected override ChartCover? ProduceCover() {
				if (Archive != null) {
					var coverBytes = GetByteArray(Archive, "cover.png");
					using (Raylib.ImageRef img = new(".png", coverBytes)) {
						var tex = Raylib.LoadTextureFromImage(img);
						Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);

						return new() {
							Texture = new Nucleus.ManagedMemory.Texture(EngineCore.Level.Textures, tex, true)
						};
					}
				}
				else {
					DeferringCoverToAsyncHandler = true;

					WebChart.GetCoverAsTextureAsync((tex) => {
						if (tex == null) return;

						lock (AsyncLock) {
							CoverTexture = new() {
								Texture = tex
							};
						}
					});

					return null;
				}
			}

			protected override MusicTrack? ProduceDemoTrack() {
				if (Archive != null) {
					var demoBytes = GetByteArray(Archive, "demo.ogg");
					if (demoBytes.Length == 0)
						demoBytes = GetByteArray(Archive, "demo.mp3");

					if (demoBytes.Length == 0)
						return null;

					return EngineCore.Level.Sounds.LoadMusicFromMemory(demoBytes);
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

			protected override ChartInfo? ProduceInfo() {
				if (Archive != null) {
					var info = JsonConvert.DeserializeObject<CustomChartInfoJSON>(GetString(Archive, "info.json")) ?? throw new Exception("Bad info.json!");
					Name = info.name;
					Author = info.author;
					ChartInfo ret = new() {
						BPM = decimal.TryParse(info.bpm, out var bpmprs) ? bpmprs : 0,
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
						BPM = 0,
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
				var download = Filesystem.GetSearchPathID("download")[0] as DiskSearchPath ?? throw new Exception("Cannot find download cache directory?");

				return download.ResolveToAbsolute($"charts/{localPath}.mdm");
			}

			public void DownloadOrPullFromCache(Action<CustomChartsSong> complete) {
				if (Archive == null) {
					if (__downloading) {
						Logs.Error("Already downloading, please wait.");
						return;
					}
					// Ensure Archive is populated from either a download or a cache
					var filename = GetDownloadCachePath(WebChart.ID);
					__downloading = true;
					if (!File.Exists(filename)) {
						WebChart.DownloadTo(filename, (worked) => {
							System.Diagnostics.Debug.Assert(worked);
							if (worked) {
								// Invalidate everything
								Filepath = filename;
								Archive = new ZipArchiveSearchPath(filename);
								Clear();
								complete(this);
								Logs.Info($"Downloaded {WebChart.ID}.mdm");
							}
							else Logs.Warn($"Couldn't download {WebChart.ID}.mdm");
						});
						Logs.Info($"Downloading {WebChart.ID}.mdm..");
					}
					else {
						Logs.Info($"Already cached {WebChart.ID}.mdm");

						// Invalidate everything
						Filepath = filename;
						Archive = new ZipArchiveSearchPath(filename);
						Clear();
						complete?.Invoke(this);
					}
				}
				else {
					complete?.Invoke(this);
				}
			}

			protected override ChartSheet ProduceSheet(int id) {
				// DownloadOrPullFromCache();
				var map = Archive.Open($"map{id}.bms", FileAccess.Read, FileMode.Open);
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (map == null)
					throw new Exception("Bad map difficulty.");

				var bms = BmsLoader.Load(map, $"map{id}.bms");
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (bms == null) throw new Exception("BMS parsing exception");


				var stageInfo = BmsLoader.TransmuteData(bms);
				Interlude.Spin(submessage: "Reading Custom Albums chart...");

				// We should be able to pass the transmuted data into this and not have to re-invent the wheel just for customs!
				return MuseDashCompatibility.ConvertStageInfoToDashSheet(this, stageInfo);
			}
		}
	}
}
