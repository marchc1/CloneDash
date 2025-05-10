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
using CloneDash.Compatibility.MuseDash;
using CloneDash.Modding.Descriptors;
using Nucleus.Models;
using System.Buffers;
using System.Runtime.InteropServices;
using Nucleus.Types;
using Nucleus.Files;

namespace CloneDash.Compatibility.MuseDash
{
	public class MDAtlasRegion
	{
		public string Name;
		public int X;
		public int Y;
		public int Width;
		public int Height;
		public int OriginalWidth;
		public int OriginalHeight;
		public int OffsetX;
		public int OffsetY;
	}
	public class MDAtlas(Dictionary<string, MDAtlasRegion> regions)
	{
		public Dictionary<string, MDAtlasRegion> Regions => regions;
	}
	public class ParametersReader(Dictionary<string, string[]> kvps)
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

		public static List<CharacterConfigData> Characters { get; set; }
		public static List<CharacterLocalizationData> CharactersEN { get; set; }
		public static List<NoteConfigData> NoteDataManager { get; set; }

		public static string BuildTarget { get; private set; }
		public static string StandalonePlatform { get; private set; }
		public static string[] StreamingFiles { get; private set; }
		public static UnitySearchPath StreamingAssets { get; private set; }

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


			Albums = Filesystem.ReadJSON<List<MuseDashAlbum>>("musedash", "Assets/Static Resources/Data/Configs/others/albums.json");
			Albums.RemoveAll(x => x.JsonName == "");

			ConcurrentBag<MuseDashSong> workSongs = [];

			var res = Parallel.ForEach(Albums, (album) => {
				var songs = Filesystem.ReadJSON<List<MuseDashSongInfoJSON>>("musedash", $"Assets/Static Resources/Data/Configs/others/{album.JsonName}.json");
				var songsEN = Filesystem.ReadJSON<__musedashSong[]>("musedash", $"Assets/Static Resources/Data/Configs/english/{album.JsonName}_English.json");

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


		public static MDAtlas PopulateModelDataTextures(ModelData modelData, string atlasPath) {
			var atlasAsset = StreamingAssets.LoadAsset<TextAsset>(atlasPath);
			using var ms = new MemoryStream(atlasAsset.m_Script);
			using var sr = new StreamReader(ms);

			var text = sr.ReadToEnd();
			var lines = text.Replace("\r", "").Split('\n');

			string? pageName = null;
			Dictionary<string, string[]> pageParameters = [];
			bool donePageParameters = false;

			Dictionary<string, ParametersReader> regions = [];
			Dictionary<string, MDAtlasRegion> ogRegions = [];
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
			var character_image = atlasAsset.assetsFile.Objects.First(x => x is Texture2D texture && texture.m_Name == Path.GetFileNameWithoutExtension(pageName)) as Texture2D;
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

				ogRegions[regionKVP.Key] = new() {
					Name = regionKVP.Key,
					X = x,
					Y = y,
					Width = width,
					Height = height,
					OriginalWidth = originalWidth,
					OriginalHeight = originalHeight,
					OffsetX = offsetX,
					OffsetY = offsetY
				};

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

			return new MDAtlas(ogRegions);
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

public static class MuseDashModelConverter
{

	public const byte ATTACHMENT_REGION = 0;
	public const byte ATTACHMENT_BOUNDING_BOX = 1;
	public const byte ATTACHMENT_MESH = 2;
	public const byte ATTACHMENT_LINKED_MESH = 3;
	public const byte ATTACHMENT_PATH = 4;
	public const byte ATTACHMENT_POINT = 5;
	public const byte ATTACHMENT_CLIPPING = 6;

	public const byte BLEND_MODE_NORMAL = 0;
	public const byte BLEND_MODE_ADDITIVE = 1;
	public const byte BLEND_MODE_MULTIPLY = 2;
	public const byte BLEND_MODE_SCREEN = 3;

	public const byte CURVE_LINEAR = 0;
	public const byte CURVE_STEPPED = 1;
	public const byte CURVE_BEZIER = 2;

	public const byte BONE_ROTATE = 0;
	public const byte BONE_TRANSLATE = 1;
	public const byte BONE_SCALE = 2;
	public const byte BONE_SHEAR = 3;

	public const byte TRANSFORM_NORMAL = 0;
	public const byte TRANSFORM_ONLY_TRANSLATION = 1;
	public const byte TRANSFORM_NO_ROTATION_OR_REFLECTION = 2;
	public const byte TRANSFORM_NO_SCALE = 3;
	public const byte TRANSFORM_NO_SCALE_OR_REFLECTION = 4;

	public const byte SLOT_ATTACHMENT = 0;
	public const byte SLOT_COLOR = 1;
	public const byte SLOT_TWO_COLOR = 2;

	public const byte PATH_POSITION = 0;
	public const byte PATH_SPACING = 1;
	public const byte PATH_MIX = 2;

	public const byte PATH_POSITION_FIXED = 0;
	public const byte PATH_POSITION_PERCENT = 1;

	public const byte PATH_SPACING_LENGTH = 0;
	public const byte PATH_SPACING_FIXED = 1;
	public const byte PATH_SPACING_PERCENT = 2;

	public const byte PATH_ROTATE_TANGENT = 0;
	public const byte PATH_ROTATE_CHAIN = 1;
	public const byte PATH_ROTATE_CHAIN_SCALE = 2;

	public static bool MD_ReadBoolean(this MemoryStream asset) => asset.ReadByte() == 1;
	public static byte MD_ReadByte(this MemoryStream asset) => (byte)asset.ReadByte();
	public static short MD_ReadShort(this MemoryStream asset) => (short)((asset.ReadByte() << 8) | asset.ReadByte());
	public static int MD_ReadInt(this MemoryStream asset) => (asset.ReadByte() << 24) | (asset.ReadByte() << 16) | (asset.ReadByte() << 8) | asset.ReadByte();
	public static float MD_ReadFloat(this MemoryStream asset) => MemoryMarshal.Cast<int, float>(stackalloc int[1] { MD_ReadInt(asset) })[0];
	public static int MD_ReadVarInt(this MemoryStream asset, bool positive) {
		int b = asset.ReadByte();
		int result = b & 0x7F;

		if ((b & 0x80) != 0) {
			b = asset.ReadByte();
			result |= (b & 0x7F) << 7;

			if ((b & 0x80) != 0) {
				b = asset.ReadByte();
				result |= (b & 0x7F) << 14;

				if ((b & 0x80) != 0) {
					b = asset.ReadByte();
					result |= (b & 0x7F) << 21;

					if ((b & 0x80) != 0)
						result |= (asset.ReadByte() & 0x7F) << 28;
				}
			}
		}
		if (positive) return result;
		return (result >> 1) ^ -(result & 1);
	}
	public static string MD_ReadString(this MemoryStream asset, int? maxLength = null) => MD_ReadNullableString(asset, maxLength) ?? throw new NullReferenceException("String was null.");
	public static string? MD_ReadNullableString(this MemoryStream asset, int? maxLength = null) {
		int count = MD_ReadVarInt(asset, true);
		if (count <= 0) return null;
		if (count == 1) return "";
		count--;

		if (maxLength.HasValue && count >= maxLength) count = maxLength.Value - 1;
		if (count < (1 << 12)) {
			Span<byte> strdata = stackalloc byte[count];
			for (int i = 0; i < count; i++)
				strdata[i] = MD_ReadByte(asset);

			return Encoding.UTF8.GetString(strdata);
		}
		else {
			byte[] strdata = ArrayPool<byte>.Shared.Rent(count);
			for (int i = 0; i < count; i++)
				strdata[i] = MD_ReadByte(asset);

			string ret = Encoding.UTF8.GetString(strdata);
			ArrayPool<byte>.Shared.Return(strdata);
			return ret;
		}
	}

	public static string? MD_ReadRefString(this MemoryStream asset, string[] refStrings) {
		int index = MD_ReadVarInt(asset, true);
		return index == 0 ? null : refStrings[index - 1];
	}

	public static Color? MD_ReadNullableColor(this MemoryStream asset) {
		int rgba = asset.MD_ReadInt();
		return new() {
			R = (byte)((rgba & 0xff000000) >>> 24),
			G = (byte)((rgba & 0x00ff0000) >>> 16),
			B = (byte)((rgba & 0x0000ff00) >>> 8),
			A = (byte)(rgba & 0x000000ff)
		};
	}

	public static Color MD_ReadColor(this MemoryStream asset) => MD_ReadNullableColor(asset) ?? throw new NullReferenceException();

	private class stringLineReader(string[] lines, int index)
	{
		public string? ReadLine() {
			if (index >= lines.Length)
				return null;

			index++;
			return lines[index - 1];
		}

		public void BackLine() => index = index <= 0 ? 0 : index - 1;
	}

	private static bool MD_ReadRegionName(stringLineReader atlasReader, out string key) {
		var line = atlasReader.ReadLine();
		if (line == null) {
			key = null;
			return false;
		}

		key = line.Trim();
		return true;
	}
	private static bool MD_ReadAtlasLine(stringLineReader atlasReader, out string key, out string? out1, out string? out2, out string? out3, out string? out4) {
		var line = atlasReader.ReadLine();
		if (line == null) {
			key = null;
			out1 = out2 = out3 = out4 = null;
			return false;
		}
		var colon = line.IndexOf(':');
		if (colon == -1) {
			key = line;
			out1 = out2 = out3 = out4 = null;
			atlasReader.BackLine();
			return false;
		}

		key = line.Substring(0, colon).Trim();
		string[] pieces = line.Substring(colon + 1).Split(',');
		out1 = pieces.Length > 0 ? pieces[0].Trim() : null;
		out2 = pieces.Length > 1 ? pieces[1].Trim() : null;
		out3 = pieces.Length > 2 ? pieces[2].Trim() : null;
		out4 = pieces.Length > 3 ? pieces[3].Trim() : null;

		return true;
	}

	/// <summary>
	/// Convert a Muse Dash model from Unity assets into a Nucleus Model4System compatible model, or at least tries to.
	/// </summary>
	/// <param name="skeletonTA"></param>
	/// <param name="atlas"></param>
	/// <param name="image"></param>
	/// <returns></returns>
	public static ModelData ConvertMuseDashModelData(ModelData nucleusModelData, TextAsset skeletonTA, MDAtlas mdatlas) {
		using (MemoryStream skeleton = new(skeletonTA.m_Script))
			//using (Raylib.ImageRef image = new(imageT2D.ToRaylib())) 
			{
			string hash = skeleton.MD_ReadString();
			string version = skeleton.MD_ReadString();

			skeleton.MD_ReadFloat(); // x
			skeleton.MD_ReadFloat(); // y
			skeleton.MD_ReadFloat(); // width
			skeleton.MD_ReadFloat(); // height

			bool nonessential = skeleton.MD_ReadBoolean();
			bool hadAudio = false;
			if (nonessential) {
				skeleton.MD_ReadFloat();

				skeleton.MD_ReadNullableString(); // images
				hadAudio = !string.IsNullOrWhiteSpace(skeleton.MD_ReadNullableString()); // audio
			}

			string[] refStrings = new string[skeleton.MD_ReadVarInt(true)];
			for (int i = 0, c = refStrings.Length; i < c; i++) {
				refStrings[i] = skeleton.MD_ReadString();
			}

			nucleusModelData.Name = skeletonTA.m_Name;

			for (int i = 0, bones = skeleton.MD_ReadVarInt(true); i < bones; i++) {
				BoneData boneData = new BoneData();

				boneData.Name = skeleton.MD_ReadString();
				boneData.Index = i;
				if (i != 0)
					boneData.Parent = nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)];
				boneData.Rotation = skeleton.MD_ReadFloat();
				boneData.Position = new(skeleton.MD_ReadFloat(), skeleton.MD_ReadFloat());
				boneData.Scale = new(skeleton.MD_ReadFloat(), skeleton.MD_ReadFloat());
				boneData.Shear = new(skeleton.MD_ReadFloat(), skeleton.MD_ReadFloat());
				boneData.Length = skeleton.MD_ReadFloat();
				boneData.TransformMode = skeleton.MD_ReadByte() switch {
					TRANSFORM_NORMAL => TransformMode.Normal,
					TRANSFORM_ONLY_TRANSLATION => TransformMode.OnlyTranslation,
					TRANSFORM_NO_ROTATION_OR_REFLECTION => TransformMode.NoRotationOrReflection,
					TRANSFORM_NO_SCALE => TransformMode.NoScale,
					TRANSFORM_NO_SCALE_OR_REFLECTION => TransformMode.NoScaleOrReflection
				};
				skeleton.MD_ReadBoolean();
				if (nonessential)
					skeleton.MD_ReadInt();

				nucleusModelData.BoneDatas.Add(boneData);
			}

			for (int i = 0, slots = skeleton.MD_ReadVarInt(true); i < slots; i++) {
				SlotData slotData = new SlotData();

				slotData.Name = skeleton.MD_ReadString();
				slotData.Index = i;
				slotData.BoneData = nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)];
				slotData.Color = skeleton.MD_ReadColor();
				slotData.DarkColor = skeleton.MD_ReadNullableColor();
				slotData.Attachment = skeleton.MD_ReadRefString(refStrings);
				slotData.BlendMode = skeleton.MD_ReadByte() switch {
					BLEND_MODE_ADDITIVE => Nucleus.Models.BlendMode.Additive,
					BLEND_MODE_MULTIPLY => Nucleus.Models.BlendMode.Multiply,
					BLEND_MODE_NORMAL => Nucleus.Models.BlendMode.Normal,
					BLEND_MODE_SCREEN => Nucleus.Models.BlendMode.Screen
				};

				nucleusModelData.SlotDatas.Add(slotData);
			}

			for (int i = 0, iks = skeleton.MD_ReadVarInt(true); i < iks; i++) {
				skeleton.MD_ReadString();
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadBoolean();
				for (int i2 = 0, n = skeleton.MD_ReadVarInt(true); i2 < n; i2++)
					skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.ReadByte();
				skeleton.MD_ReadBoolean();
				skeleton.MD_ReadBoolean();
				skeleton.MD_ReadBoolean();
			}

			for (int i = 0, transforms = skeleton.MD_ReadVarInt(true); i < transforms; i++) {
				skeleton.MD_ReadString();
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadBoolean();
				for (int i2 = 0, n = skeleton.MD_ReadVarInt(true); i2 < n; i2++)
					skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadBoolean();
				skeleton.MD_ReadBoolean();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
			}

			for (int i = 0, paths = skeleton.MD_ReadVarInt(true); i < paths; i++) {
				skeleton.MD_ReadString();
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadBoolean();
				for (int i2 = 0, n = skeleton.MD_ReadVarInt(true); i2 < n; i2++)
					skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
			}

			nucleusModelData.DefaultSkin = MD_ReadSkin(skeleton, nucleusModelData, mdatlas, refStrings, true, nonessential);
			nucleusModelData.Skins.Add(nucleusModelData.DefaultSkin);

			for (int i = 0, skins = skeleton.MD_ReadVarInt(true); i < skins; i++)
				nucleusModelData.Skins.Add(MD_ReadSkin(skeleton, nucleusModelData, mdatlas, refStrings, false, nonessential));

			for (int i = 0, events = skeleton.MD_ReadVarInt(true); i < events; i++) {
				skeleton.MD_ReadRefString(refStrings);
				skeleton.MD_ReadVarInt(false);
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadNullableString();
				skeleton.MD_ReadNullableString();
				if (hadAudio) {
					skeleton.MD_ReadFloat();
					skeleton.MD_ReadFloat();
				}
			}

			for (int i = 0, animations = skeleton.MD_ReadVarInt(true); i < animations; i++) {
				MD_ReadAnimation(skeleton, nucleusModelData, refStrings, nonessential, hadAudio);
			}

			return nucleusModelData;
		}
	}
	private static void MD_ReadCurve(MemoryStream skeleton, int frame, int frames, out byte curveType, out float c1, out float c2, out float c3, out float c4) {
		if (frame >= frames - 1) {
			curveType = 0;
			c1 = c2 = c3 = c4 = 0;
			return;
		}

		curveType = skeleton.MD_ReadByte();
		if (curveType != CURVE_BEZIER)
			c1 = c2 = c3 = c4 = 0;
		else {
			c1 = skeleton.MD_ReadFloat();
			c2 = skeleton.MD_ReadFloat();
			c3 = skeleton.MD_ReadFloat();
			c4 = skeleton.MD_ReadFloat();
		}
	}

	private static KeyframeInterpolation interp(byte b) => b switch {
		CURVE_STEPPED => KeyframeInterpolation.Constant,
		CURVE_LINEAR => KeyframeInterpolation.Linear,
		CURVE_BEZIER => KeyframeInterpolation.Bezier
	};
	private static KeyframeHandle<float>? interpKFH(byte b, float c1, float c2) {
		if (b != CURVE_BEZIER) return null;

		return new() { HandleType = KeyframeHandleType.AutoClamped, Value = c2, Time = c1 };
	}


	private static void fillKeyframe(FCurve<float> curve, float time, float value, byte ct, float c1, float c2, float c3, float c4) {
		curve.AddKeyframe(new(time, value) { Interpolation = interp(ct), RightHandle = interpKFH(ct, c1, c2), LeftHandle = interpKFH(ct, c3, c4) });
	}

	private static void fixCurve(FCurve<float> curve) {

	}

	private static void MD_ReadAnimation(MemoryStream skeleton, ModelData nucleusModelData, string[] refStrings, bool nonessential, bool hadAudio) {
		Nucleus.Models.Runtime.Animation animation = new Nucleus.Models.Runtime.Animation();
		animation.Name = skeleton.MD_ReadString();
		nucleusModelData.Animations.Add(animation);

		for (int slotI = 0, slots = skeleton.MD_ReadVarInt(true); slotI < slots; slotI++) {
			int slotIndex = skeleton.MD_ReadVarInt(true);

			for (int timelineI = 0, timelines = skeleton.MD_ReadVarInt(true); timelineI < timelines; timelineI++) {
				int type = skeleton.MD_ReadByte();
				int frames = skeleton.MD_ReadVarInt(true);

				switch (type) {
					case SLOT_ATTACHMENT: {
							ActiveAttachmentTimeline tl = new ActiveAttachmentTimeline();
							tl.SlotIndex = slotIndex;
							tl.NewCurves();

							for (int frame = 0; frame < frames; frame++)
								tl.Curve(0).AddKeyframe(new(skeleton.MD_ReadFloat(), skeleton.MD_ReadRefString(refStrings)));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
					case SLOT_COLOR: {
							SlotColor4Timeline tl = new SlotColor4Timeline();
							tl.SlotIndex = slotIndex;
							tl.NewCurves();

							float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var color = skeleton.MD_ReadColor();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

								fillKeyframe(tl.Curve(0), time, color.R * 100, curveType, c1, c2, lc3, lc4);
								fillKeyframe(tl.Curve(1), time, color.G * 100, curveType, c1, c2, lc3, lc4);
								fillKeyframe(tl.Curve(2), time, color.B * 100, curveType, c1, c2, lc3, lc4);
								fillKeyframe(tl.Curve(3), time, color.A * 100, curveType, c1, c2, lc3, lc4);

								lc3 = c3;
								lc4 = c4;
							}
							fixCurve(tl.Curve(0));
							fixCurve(tl.Curve(1));
							fixCurve(tl.Curve(2));
							fixCurve(tl.Curve(3));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
					case SLOT_TWO_COLOR: {
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var light = skeleton.MD_ReadInt();
								var dark = skeleton.MD_ReadInt();
								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);
							}
						}
						break;
				}
			}
		}
		for (int boneI = 0, bones = skeleton.MD_ReadVarInt(true); boneI < bones; boneI++) {
			int boneIndex = skeleton.MD_ReadVarInt(true);

			for (int timelineI = 0, timelines = skeleton.MD_ReadVarInt(true); timelineI < timelines; timelineI++) {
				int type = skeleton.MD_ReadByte();
				int frames = skeleton.MD_ReadVarInt(true);

				switch (type) {
					case BONE_ROTATE: {
							RotationTimeline tl = new RotationTimeline();
							tl.BoneIndex = boneIndex;
							tl.NewCurves();

							float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var value = skeleton.MD_ReadFloat();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);
								var overlap = 16384 - (int)(16384.4999f - value / 360);
								value -= overlap * 360;
								fillKeyframe(tl.Curve(0), time, value, curveType, c1, c2, lc3, lc4);
								lc3 = c3;
								lc4 = c4;
							}
							fixCurve(tl.Curve(0));
							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
					case BONE_TRANSLATE:
					case BONE_SCALE:
					case BONE_SHEAR: {
							DuoBoneFloatPropertyTimeline tl = type switch {
								BONE_TRANSLATE => new TranslateTimeline(),
								BONE_SCALE => new ScaleTimeline(),
								BONE_SHEAR => new ShearTimeline(),
								_ => throw new Exception()
							};

							tl.BoneIndex = boneIndex;
							tl.NewCurves();

							float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var x = skeleton.MD_ReadFloat();
								var y = skeleton.MD_ReadFloat();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

								fillKeyframe(tl.Curve(0), time, x, curveType, c1, c2, lc3, lc4);
								fillKeyframe(tl.Curve(1), time, y, curveType, c1, c2, lc3, lc4);
								lc3 = c3;
								lc4 = c4;
							}

							fixCurve(tl.Curve(0));
							fixCurve(tl.Curve(1));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
				}
			}
		}
		for (int ikI = 0, iks = skeleton.MD_ReadVarInt(true); ikI < iks; ikI++) {
			int ikIndex = skeleton.MD_ReadVarInt(true);
			int frames = skeleton.MD_ReadVarInt(true);

			for (int frame = 0; frame < frames; frame++) {
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadByte();
				skeleton.MD_ReadBoolean();
				skeleton.MD_ReadBoolean();
				MD_ReadCurve(skeleton, frame, frames, out _, out _, out _, out _, out _);
			}
		}

		for (int trI = 0, transforms = skeleton.MD_ReadVarInt(true); trI < transforms; trI++) {
			throw new Exception();
		}

		for (int pathI = 0, paths = skeleton.MD_ReadVarInt(true); pathI < paths; pathI++) {
			throw new Exception();
		}

		for (int deformI = 0, deforms = skeleton.MD_ReadVarInt(true); deformI < deforms; deformI++) {
			Skin skin = nucleusModelData.Skins[skeleton.MD_ReadVarInt(true)];
			for (int i2 = 0, c2 = skeleton.MD_ReadVarInt(true); i2 < c2; i2++) {
				int slotIndex = skeleton.MD_ReadVarInt(true);
				for (int i3 = 0, c3 = skeleton.MD_ReadVarInt(true); i3 < c3; i3++) {
					skeleton.MD_ReadRefString(refStrings);
					int frames = skeleton.MD_ReadVarInt(true);

					for (int frame = 0; frame < frames; frame++) {
						skeleton.MD_ReadFloat();
						var end = skeleton.MD_ReadVarInt(true);
						if (end != 0) {
							int start = skeleton.MD_ReadVarInt(true);
							end += start;
							for (int i4 = start; i4 < end; i4++)
								skeleton.MD_ReadFloat();
						}

						MD_ReadCurve(skeleton, frame, frames, out _, out _, out _, out _, out _);
					}
				}
			}
		}

		for (int drawI = 0, drawOrder = skeleton.MD_ReadVarInt(true); drawI < drawOrder; drawI++) {
			skeleton.MD_ReadFloat();
			int count = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < count; i2++) {
				skeleton.MD_ReadVarInt(true);
				skeleton.MD_ReadVarInt(true);
			}
		}

		for (int eventIndex = 0, events = skeleton.MD_ReadVarInt(true); eventIndex < events; eventIndex++) {
			skeleton.MD_ReadFloat();
			int count = skeleton.MD_ReadVarInt(true);
			skeleton.MD_ReadVarInt(false);
			skeleton.MD_ReadFloat();

			if (skeleton.MD_ReadBoolean()) skeleton.MD_ReadString();
			if (hadAudio) {
				skeleton.MD_ReadFloat();
				skeleton.MD_ReadFloat();
			}
		}
	}
	private static AttachmentVertex[] MD_ReadVertices(MemoryStream skeleton, int vertices, out bool weighted) {
		weighted = skeleton.MD_ReadBoolean();
		AttachmentVertex[] returnVertices = new AttachmentVertex[vertices];
		if (!weighted)
			for (int i = 0; i < vertices; i++)
				returnVertices[i] = new AttachmentVertex() { X = skeleton.MD_ReadFloat(), Y = skeleton.MD_ReadFloat() };
		else
			for (int i = 0; i < vertices; i++) {
				int bones = skeleton.MD_ReadVarInt(true);
				AttachmentVertex vertex = new AttachmentVertex();
				vertex.Weights = new AttachmentWeight[bones];
				for (int b = 0; b < bones; b++) {
					int boneIndex = skeleton.MD_ReadVarInt(true);
					float bindPosX = skeleton.MD_ReadFloat();
					float bindPosY = skeleton.MD_ReadFloat();
					float weight = skeleton.MD_ReadFloat();

					vertex.Weights[b] = new AttachmentWeight(boneIndex, weight, new(bindPosX, bindPosY));
					vertex.X += bindPosX * weight;
					vertex.Y += bindPosY * weight;
				}

				returnVertices[i] = vertex;
			}

		return returnVertices;
	}

	private static Skin MD_ReadSkin(MemoryStream skeleton, ModelData nucleusModelData, MDAtlas mdatlas, string[] refStrings, bool def, bool nonessential) {
		Skin skin = new Skin();
		int slots = 0;

		if (def) {
			slots = skeleton.MD_ReadVarInt(true);
			if (slots == 0)
				return null;

			skin.Name = "default";
		}
		else {
			skin.Name = skeleton.MD_ReadRefString(refStrings);

			var bonesCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < bonesCount; i2++)
				skeleton.MD_ReadVarInt(true);

			var ikCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < ikCount; i2++)
				skeleton.MD_ReadVarInt(true);

			var transformCOunt = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < transformCOunt; i2++)
				skeleton.MD_ReadVarInt(true);

			var pathCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < pathCount; i2++)
				skeleton.MD_ReadVarInt(true);

			slots = skeleton.MD_ReadVarInt(true);
		}
		for (int i = 0; i < slots; i++) {
			var slotIndex = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0, attachments = skeleton.MD_ReadVarInt(true); i2 < attachments; i2++) {
				string? attachmentName = skeleton.MD_ReadRefString(refStrings);
				string? name = skeleton.MD_ReadRefString(refStrings);
				if (name == null) name = attachmentName;

				Attachment? attachment = null;
				var type = skeleton.MD_ReadByte();
				//Logs.Info($"Type: {type}, name: {name}");
				switch (type) {
					case ATTACHMENT_REGION: {
							string path = skeleton.MD_ReadRefString(refStrings) ?? name;
							float rotation = skeleton.MD_ReadFloat();
							float x = skeleton.MD_ReadFloat();
							float y = skeleton.MD_ReadFloat();
							float scaleX = skeleton.MD_ReadFloat();
							float scaleY = skeleton.MD_ReadFloat();
							float width = skeleton.MD_ReadFloat();
							float height = skeleton.MD_ReadFloat();
							Color color = skeleton.MD_ReadColor();
							RegionAttachment region = new();

							region.Name = name;
							region.Path = path;
							region.Color = color;
							region.Position = new(x, y);
							region.Rotation = rotation;

							var mdRegion = mdatlas.Regions.First(x => x.Value.Name == region.Path).Value;
							var wM = width / mdRegion.OriginalWidth;
							var hM = height / mdRegion.OriginalHeight;
							region.Scale = new(scaleX * wM, scaleY * hM);
							attachment = region;
						}
						break;
					case ATTACHMENT_BOUNDING_BOX: {
							MD_ReadVertices(skeleton, skeleton.MD_ReadVarInt(true), out _);
							if (nonessential)
								skeleton.MD_ReadInt();
						}
						break;
					case ATTACHMENT_MESH: {
							string path = skeleton.MD_ReadRefString(refStrings) ?? name;
							Color color = skeleton.MD_ReadColor();

							var vertexCount = skeleton.MD_ReadVarInt(true);
							float[] uArray = new float[vertexCount];
							float[] vArray = new float[vertexCount];
							for (int uv = 0; uv < vertexCount; uv++) {
								uArray[uv] = skeleton.MD_ReadFloat(); 
								vArray[uv] = 1 - skeleton.MD_ReadFloat();
							}

							int triangleCount = skeleton.MD_ReadVarInt(true) / 3;
							AttachmentTriangle[] triangles = new AttachmentTriangle[triangleCount];
							for (int tri = 0; tri < triangles.Length; tri++) {
								triangles[tri] = new() {
									V1 = skeleton.MD_ReadShort(),
									V2 = skeleton.MD_ReadShort(),
									V3 = skeleton.MD_ReadShort()
								};
							}

							AttachmentVertex[] vertices = MD_ReadVertices(skeleton, vertexCount, out bool weighted);

							var hullCount = skeleton.MD_ReadVarInt(true);
							if (nonessential) {
								var edgeCount = skeleton.MD_ReadVarInt(true);
								for (int edge = 0; edge < edgeCount; edge++)
									skeleton.MD_ReadShort();

								skeleton.MD_ReadFloat();
								skeleton.MD_ReadFloat();
							}

							MeshAttachment mesh = new();
							mesh.Name = name;
							mesh.Path = path;
							mesh.Color = color;
							mesh.Vertices = vertices;
							mesh.Triangles = triangles;
							mesh.Position = new(0);
							mesh.Rotation = 0;
							mesh.Scale = new(1);
							for (int i3 = 0; i3 < vertexCount; i3++) {
								mesh.Vertices[i3].U = uArray[i3];
								mesh.Vertices[i3].V = vArray[i3];
							}

							attachment = mesh;
						}
						break;
					case ATTACHMENT_LINKED_MESH: {
							skeleton.MD_ReadRefString(refStrings);
							skeleton.MD_ReadInt();
							skeleton.MD_ReadRefString(refStrings);
							skeleton.MD_ReadRefString(refStrings);
							skeleton.MD_ReadBoolean();
							if (nonessential) {
								skeleton.MD_ReadFloat();
								skeleton.MD_ReadFloat();
							}
						}
						break;
					case ATTACHMENT_PATH: {
							skeleton.MD_ReadBoolean();
							skeleton.MD_ReadBoolean();
							int c = skeleton.MD_ReadVarInt(true);
							MD_ReadVertices(skeleton, c, out _);
							for (int _ = 0; _ < c / 3; _++)
								skeleton.MD_ReadFloat();
							if (nonessential)
								skeleton.MD_ReadInt();
						}
						break;
					case ATTACHMENT_POINT: {
							skeleton.MD_ReadFloat();
							skeleton.MD_ReadFloat();
							skeleton.MD_ReadFloat();
							if (nonessential)
								skeleton.MD_ReadInt();
						}
						break;
					case ATTACHMENT_CLIPPING: {
							var endSlot = skeleton.MD_ReadVarInt(true);
							var vertices = MD_ReadVertices(skeleton, skeleton.MD_ReadVarInt(true), out _);
							if (nonessential)
								skeleton.MD_ReadInt();

							ClippingAttachment clip = new ClippingAttachment();
							clip.EndSlot = nucleusModelData.SlotDatas[endSlot].Name;
							clip.Vertices = vertices;

							attachment = clip;
						}
						break;
				}

				if (attachment != null)
					skin.SetAttachment(slotIndex, name, attachment);
			}
		}
		return skin;
	}

	// EVERYTHING in Clone Dash should probably go through these methods, or at least those that use in-game/menu assets.
	// This lets musedash overrides work

	public static ModelData MD_GetModelData(this Level level, string objectName) {
		ModelData md_data = new ModelData();
		var prefab = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>(objectName);

		/*MuseDashModelConverter.ConvertMuseDashModelData(
			md_data,
			pathID, 
			MuseDashCompatibility.PopulateModelDataTextures(md_data, pathID)
		);*/

		return md_data;
	}
}