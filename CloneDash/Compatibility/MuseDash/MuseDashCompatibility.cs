using AssetStudio;
using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.Unity;
using CloneDash.Data;
using CloneDash.Game;
using Nucleus;
using Nucleus.Models.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Raylib_cs;
using Texture2D = AssetStudio.Texture2D;
using Color = Raylib_cs.Color;
using Nucleus.Audio;
using Nucleus.Engine;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp;

namespace CloneDash.Compatibility.MuseDash
{
	/// <summary>
	/// Muse Dash style level converter
	/// </summary>
	public static partial class MuseDashCompatibility
	{
		public const uint MUSEDASH_APPID = 774171;
		public static string? WhereIsMuseDashInstalled { get; set; } = null;
		public static string? WhereIsMuseDashDataFolder { get; set; } = null;
		public static bool IsMuseDashInstalled => WhereIsMuseDashInstalled != null;

		public static string NoteManagerAssetBundle { get; private set; } = "";
		public static Dictionary<string, List<string>> IBMSToDesc { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> IDToNote { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> IBMSToNote { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> UIDToNote { get; private set; } = new();

		public static List<NoteConfigData> NoteDataManager { get; set; }

		public static string BuildTarget { get; private set; }
		public static string StandalonePlatform { get; private set; }
		public static string[] StreamingFiles { get; private set; }

		public static void FillInTheBlankNotes(MuseDashSong song, StageInfo stage) {
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
		public static ChartSheet ConvertStageInfoToDashSheet(ChartSong song, StageInfo MDinfo, IEnumerable<TempoChange>? tempoChanges = null) {
			Stopwatch measureFunctionTime = Stopwatch.StartNew();

			ChartSheet sheet = new(song);
			sheet.Rating = song.Difficulty(MDinfo.difficulty);

			sheet.TempoChanges.Add(new(0, 0, MDinfo.bpm));
			if (tempoChanges != null)
				sheet.TempoChanges.AddRange(tempoChanges);

			bool first = true;
			Dictionary<int, List<MusicData>> LongPresses = new();
			HashSet<string> WarnedIBMSPresses = new();
			foreach (var s in MDinfo.musicDatas) {
				Interlude.Spin(submessage: "Reading Muse Dash chart...");
				if (s.noteData == null && first) {
					sheet.StartOffset = (float)s.tick;
				}

				if (s.noteData != null) {
					var ib = MuseDashCompatibility.ConvertIBMSCode(s.noteData.ibms_id);
					var tick_hit = (float)s.configData.time;
					var tick_show = tick_hit - ((tick_hit - ((float)s.showTick - sheet.StartOffset)) / (double)s.dt);

					PathwaySide pathwayType = PathwaySide.Both;

					switch (s.noteData.pathway) {
						case 1:
							pathwayType = PathwaySide.Top;
							break;
						case 0:
							pathwayType = PathwaySide.Bottom;
							break;
					}

					var blood = s.configData.blood;

					if (s.isLongPressStart) {
						ChartEntity press = new ChartEntity();
						press.Type = EntityType.SustainBeam;
						press.Pathway = pathwayType;
						press.EnterDirection = EntityEnterDirection.RightSide;
						press.HitTime = tick_hit;
						press.ShowTime = tick_show;

						press.Fever = s.noteData.fever;
						press.Damage = s.noteData.damage;
						press.Length = (double)s.configData.length;
						press.Score = s.noteData.score;

						press.RelatedToBoss = false;
						press.DebuggingInfo = $"ib.code: {ib.code}";
						sheet.Entities.Add(press);

						continue;
					}

					//Switch case for entity type
					bool isBoss = ib.code switch {
						IBMSCode.BossBlock
						or IBMSCode.BossAttack1 or IBMSCode.BossAttack2_1 or IBMSCode.BossAttack2_2
						=> true,
						_ => false
					};

					EventType eventType = ib.code switch {
						IBMSCode.BossIn => EventType.BossIn,
						IBMSCode.BossOut => EventType.BossOut,

						IBMSCode.BossNear2 => EventType.BossSingleHit,
						IBMSCode.BossMul2 => EventType.BossMasher,

						IBMSCode.BossFar1Start => EventType.BossFar1Start,
						IBMSCode.BossFar1End => EventType.BossFar1End,
						IBMSCode.BossFar1To2 => EventType.BossFar1To2,
						IBMSCode.BossFar2Start => EventType.BossFar2Start,
						IBMSCode.BossFar2End => EventType.BossFar2End,
						IBMSCode.BossFar2To1 => EventType.BossFar2To1,

						IBMSCode.BossHide => EventType.BossHide,

						_ => EventType.NotApplicable
					};

					if (eventType != EventType.NotApplicable) {
						ChartEvent ChartEvent = new();
						ChartEvent.Type = eventType;
						ChartEvent.Time = tick_hit;
						ChartEvent.Length = (double)s.configData.length;

						if (s.noteData.damage > 0)
							ChartEvent.Damage = s.noteData.damage;
						if (s.noteData.fever > 0)
							ChartEvent.Fever = s.noteData.fever;
						if (s.noteData.score > 0)
							ChartEvent.Score = s.noteData.score;

						ChartEvent.BossAction = s.noteData.boss_action;
						sheet.Events.Add(ChartEvent);
					}
					else {
						EntityType entityType = ib.code switch {
							IBMSCode.SmallNormal or IBMSCode.SmallUp or IBMSCode.SmallDown
							or IBMSCode.Medium1Normal or IBMSCode.Medium2Normal or IBMSCode.Medium1Down or IBMSCode.Medium2Down or IBMSCode.Medium1Up or IBMSCode.Medium2Up
							or IBMSCode.Large1 or IBMSCode.Large2
							or IBMSCode.BossAttack1 or IBMSCode.BossAttack2_1 or IBMSCode.BossAttack2_2
								=> EntityType.Single,

							IBMSCode.Gemini => EntityType.Double,
							IBMSCode.Hammer or IBMSCode.HammerFlip => EntityType.Hammer,
							IBMSCode.Mul => EntityType.Masher,
							IBMSCode.BossBlock or IBMSCode.Block => EntityType.Gear,
							IBMSCode.Ghost => EntityType.Ghost,
							IBMSCode.Raider or IBMSCode.RaiderFlip => EntityType.Raider,
							IBMSCode.Music => EntityType.Score,
							IBMSCode.Hp => EntityType.Heart,

							_ => EntityType.Unknown
						};
						var health = ib.code == IBMSCode.Hp ? ChartEntity.DEFAULT_HP : 0;

						if (entityType != EntityType.Unknown) {

							EntityVariant variant = ib.code switch {
								IBMSCode.SmallNormal or IBMSCode.SmallUp or IBMSCode.SmallDown => EntityVariant.Small,
								IBMSCode.Medium1Normal or IBMSCode.Medium1Down or IBMSCode.Medium1Up => EntityVariant.Medium1,
								IBMSCode.Medium2Normal or IBMSCode.Medium2Down or IBMSCode.Medium2Up => EntityVariant.Medium2,
								IBMSCode.Large1 => EntityVariant.Large1,
								IBMSCode.Large2 => EntityVariant.Large2,

								IBMSCode.BossAttack1 => EntityVariant.Boss1,
								IBMSCode.BossAttack2_1 => EntityVariant.Boss2,
								IBMSCode.BossAttack2_2 => EntityVariant.Boss3,

								// :/
								IBMSCode.BossBlock => s.noteData.IsPhase2BossGear() ? EntityVariant.Boss2 : EntityVariant.Boss1,

								_ => EntityVariant.NotApplicable
							};

							bool flipped = ib.code == IBMSCode.HammerFlip || ib.code == IBMSCode.RaiderFlip;

							EntityEnterDirection dir = ib.code switch {
								IBMSCode.SmallDown or IBMSCode.Medium1Down or IBMSCode.Medium2Down or IBMSCode.Hammer => EntityEnterDirection.TopDown,
								IBMSCode.SmallUp or IBMSCode.Medium1Up or IBMSCode.Medium2Up or IBMSCode.HammerFlip => EntityEnterDirection.BottomUp,
								_ => EntityEnterDirection.RightSide
							};

							ChartEntity ent = new ChartEntity();
							ent.Type = entityType;
							ent.Variant = variant;
							ent.Pathway = pathwayType;
							ent.EnterDirection = dir;
							ent.HitTime = tick_hit;
							ent.ShowTime = tick_show;
							ent.Flipped = flipped;

							ent.Fever = s.noteData.fever;
							ent.Damage = s.noteData.damage;
							ent.Length = (double)s.configData.length;
							ent.Score = s.noteData.score;
							ent.Speed = s.noteData.speed;
							ent.Health = health;

							ent.Blood = blood;

							ent.RelatedToBoss = isBoss;
							ent.DebuggingInfo = $"ib.code: {ib.code}";
							sheet.Entities.Add(ent);
						}
						else if (WarnedIBMSPresses.Add(s.noteData.ibms_id)) {
							Logs.Warn("WARNING: An unidentified IBMS code with no compatibility translation definition was found during MD -> CD conversion.");
							Logs.Info($"IBMS Code: {s.noteData.ibms_id} (as int: {(int)ib.code}, as name-definition: {ib.name})");
							Logs.Info($"At time {tick_hit}, length of {s.configData.length}");
							Logs.Info($"DataObjID: {s.objId}");
							Logs.Info($"ConfigID: {s.configData.id}");
							Logs.Info($"NoteID: {s.noteData.id}");
							Logs.Info("");
						}

					}
				}
				first = false;
			}

			sheet.Entities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));
			Interlude.Spin(submessage: "Reading Muse Dash chart...");

			Console.WriteLine($"STOPWATCH: ConvertAssetBundleToDashSheet: Translated Muse Dash level to DashSheet in {measureFunctionTime.Elapsed.TotalSeconds} seconds");
			return sheet;
		}


		public static List<MuseDashAlbum> Albums { get; private set; } = [];
		public static List<MuseDashSong> Songs { get; private set; }



		private struct __musedashSong
		{
			public string name;
			public string author;
		}

		public static void BuildDashStructures() {
			Stopwatch s = new Stopwatch();
			s.Start();

			Albums = UnityAssetUtils.LoadAssetEasyC<TextAsset, List<MuseDashAlbum>>(StreamingFiles, "config_others_assets_albums_");
			Albums.RemoveAll(x => x.JsonName == "");

			ConcurrentBag<MuseDashSong> workSongs = [];

			var res = Parallel.ForEach(Albums, (album) => {
				var songs = UnityAssetUtils.LoadAssetEasyC<TextAsset, List<MuseDashSongInfoJSON>>(StreamingFiles, $"config_others_assets_{album.JsonName.ToLower()}_");
				var songsEN = UnityAssetUtils.LoadAssetEasyC<TextAsset, __musedashSong[]>(StreamingFiles, $"config_others_assets_{album.JsonName.ToLower()}_");

				var songsFinal = new MuseDashSong[songs.Count];

				for (int i = 0; i < songs.Count; i++) {
					workSongs.Add(new MuseDashSong(songs[i]) {
						Name = songsEN[i].name,
						Author = songsEN[i].author,
						Album = album
					});
				}
			});

			Songs = workSongs.ToList();

			Songs.Sort((x, y) => x.Name.CompareTo(y.Name));
		}

		private class ParametersReader(Dictionary<string, string[]> kvps)
		{
			public T? Read<T>(string key) where T : IParsable<T> => Read<T>(key, 0);
			public T? Read<T>(string key, int index) where T : IParsable<T> {
				return kvps.TryGetValue(key, out string[]? pieces)
					? T.TryParse(pieces[index], null, out T? res)
						? res
						: default
					: default;
			}
		}

		public static void SeparateBundlePathCombination(string combined, out string bundle, out string path) {
			string[] pieces = combined.Split('/');
			if (pieces.Length != 2) throw new Exception("Expected two pieces; defining the bundle name search, then the path name search (ie. bundleName/pathSearch, without any extensions)");
			bundle = pieces[0];
			path = pieces[1];
		}

		public static MusicTrack GenerateMusicTrack(Level level, string bundleName_pathName) {
			SeparateBundlePathCombination(bundleName_pathName, out var bundle, out var path);
			return GenerateMusicTrack(level, bundle, path);
		}
		public static MusicTrack GenerateMusicTrack(Level level, string bundleName, string musicName) {
			var bundle = MuseDashCompatibility.StreamingFiles.FirstOrDefault(x => x.Contains($"{bundleName}"));
			AssetsManager manager = new AssetsManager();
			manager.LoadFiles(bundle);
			var obj = manager.assetsFileList[0];
			var audioClip = obj.Objects.First(x => x is AudioClip ta && ta.m_Name.Contains($"{musicName}")) as AudioClip;

			byte[] musicStream;
			var audiodata = audioClip.m_AudioData.GetData();

			if (audioClip.m_Type == FMODSoundType.UNKNOWN) {
				FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
				bank.Samples[0].RebuildAsStandardFileFormat(out musicStream, out var fileExtension);

				var track = EngineCore.Level.Sounds.LoadMusicFromMemory(musicStream);
				track.Paused = false;
				return track;
			}

			throw new Exception();
		}

		public static void PopulateModelDataTextures(ModelData modelData, string bundleName_pathName) {
			SeparateBundlePathCombination(bundleName_pathName, out var bundle, out var path);
			PopulateModelDataTextures(modelData, bundle, path);
		}
		public static void PopulateModelDataTextures(ModelData modelData, string bundleName, string atlasName) {
			var bundle = MuseDashCompatibility.StreamingFiles.FirstOrDefault(x => x.Contains($"{bundleName}"));
			AssetsManager manager = new AssetsManager();
			manager.LoadFiles(bundle);
			var obj = manager.assetsFileList[0];
			var character_atlas = obj.Objects.First(x => x is TextAsset ta && ta.m_Name == $"{atlasName}.atlas") as TextAsset;

			using var ms = new MemoryStream(character_atlas.m_Script);
			using var sr = new StreamReader(ms);

			var text = sr.ReadToEnd();
			var lines = text.Replace("\r", "").Split('\n');

			string? pageName = null;
			Dictionary<string, string[]> pageParameters = [];
			bool donePageParameters = false;

			Dictionary<string, ParametersReader> regions = [];
			string workingRegion = null;
			Dictionary<string, string[]> workingRegionParameters = [];

			foreach (var curline in lines) {
				var line = curline.Trim();
				if (string.IsNullOrEmpty(line)) continue;

				bool runLineSearch = false;

				if (pageName == null)
					pageName = line;
				else if (!donePageParameters) {
					int colon = line.IndexOf(':');
					if (colon == -1) {
						donePageParameters = true;
						runLineSearch = true;
					}
					else {
						var pieces = line.Substring(colon + 1).Split(',');
						for (int i = 0; i < pieces.Length; i++) pieces[i] = pieces[i].Trim();
						pageParameters[line.Substring(0, colon)] = pieces;
					}
				}
				else runLineSearch = true;

				if (runLineSearch) {
					int colon = line.IndexOf(':');
					if (colon == -1) {
						if (workingRegion != null) {
							regions[workingRegion] = new(workingRegionParameters);
							workingRegionParameters = [];
						}
						workingRegion = line;
					}
					else {
						var pieces = line.Substring(colon + 1).Split(',');
						for (int i = 0; i < pieces.Length; i++) pieces[i] = pieces[i].Trim();
						workingRegionParameters[line.Substring(0, colon)] = pieces;
					}
				}
			}

			if (workingRegion != null && workingRegionParameters.Count > 0) {
				regions[workingRegion] = new(workingRegionParameters);
			}

			if (pageName == null) throw new NullReferenceException();
			var character_image = obj.Objects.First(x => x is Texture2D no && no.m_Name == Path.GetFileNameWithoutExtension(pageName)) as Texture2D;
			using var img = new Raylib_cs.Raylib.ImageRef(character_image!.ToRaylib(), flipV: true);

			modelData.TextureAtlas = new();

			var page = new ParametersReader(pageParameters);

			var texWidth = page.Read<int>("size", 0);
			var texHeight = page.Read<int>("size", 1);

			modelData.TextureAtlas.ClearTextures();

			foreach (var regionKVP in regions) {
				var region = regionKVP.Value;

				var degreesStr = region.Read<string>("rotate");
				int degrees = degreesStr switch {
					"true" => 90,
					"false" => 0,
					_ => int.Parse(degreesStr)
				};

				var x = region.Read<int>("xy", 0);
				var y = region.Read<int>("xy", 1);
				var width = region.Read<int>("size", 0);
				var height = region.Read<int>("size", 1);
				var originalWidth = region.Read<int>("orig", 0);
				var originalHeight = region.Read<int>("orig", 1);
				var offsetX = region.Read<int>("offset", 0);
				var offsetY = region.Read<int>("offset", 1);

				int screenspaceWidth = ((degrees % 180) == 90) ? height : width;
				int screenspaceHeight = ((degrees % 180) == 90) ? width : height;
				Image newImg = Raylib.GenImageColor(screenspaceWidth, screenspaceHeight, Color.Blank);

				Raylib.ImageDraw(ref newImg, img, new(x, y, screenspaceWidth, screenspaceHeight), new(0, 0, newImg.Width, newImg.Height), Color.White);
				if (degrees != 0)
					Raylib.ImageRotate(ref newImg, degrees);
				Raylib.ImageResizeCanvas(ref newImg, originalWidth, originalHeight, offsetX, (originalHeight - height) - offsetY, Color.Blank);
				modelData.TextureAtlas.AddTexture(regionKVP.Key, newImg);
			}

			HashSet<string> usedRegions = [];
			modelData.TextureAtlas.Validate();

			modelData.SetupAttachments();
		}
	}


	public enum MuseDashDifficulty
	{
		Unknown = 0,
		Easy = 1,
		Hard = 2,
		Master = 3,

		Supreme = 4,
		Touhou = 5
	}
}
