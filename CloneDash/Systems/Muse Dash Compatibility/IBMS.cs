using AssetStudio;
using CloneDash.Data;
using CloneDash.Game.Entities;
using CloneDash.Systems;
using CustomAlbums.Utilities;
using Nucleus;
using SpirV;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace CloneDash
{
	/// <summary>
	/// Muse Dash style level converter
	/// </summary>
	public static partial class MuseDashCompatibility
	{
		public static string NoteManagerAssetBundle { get; private set; } = "";
		public static Dictionary<string, List<string>> IBMSToDesc { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> IDToNote { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> IBMSToNote { get; private set; } = new();
		public static Dictionary<string, NoteConfigData> UIDToNote { get; private set; } = new();

		public static List<NoteConfigData> NoteDataManager { get; set; }

		public static string BuildTarget { get; private set; }
		public static string[] StreamingFiles { get; private set; }

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
		public static ChartSheet ConvertStageInfoToDashSheet(ChartSong song, StageInfo MDinfo) {
			Stopwatch measureFunctionTime = Stopwatch.StartNew();

			ChartSheet sheet = new(song);
			sheet.Rating = song.Difficulty(MDinfo.difficulty);

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
	}


	public enum MuseDashDifficulty
	{
		Unknown = 0,
		Easy = 1,
		Hard = 2,
		Master = 3,

		Hidden = 4,
		Touhou = 5
	}
}
