// TODO: this file is WAY too bloated!!!

using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Data;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;

using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;

using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Localization;
using Nucleus.Common.Models;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Util;

using Raylib_cs;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Color = Nucleus.Common.Types.Color;
using ImageFormat = Nucleus.Common.Graphics.ImageFormat;
using Material = AssetStudio.Material;
using Texture2D = AssetStudio.Texture2D;

namespace CloneDash.Compatibility.MuseDash
{
	public class MDAtlasPage : IModelAtlasPage, IDisposable
	{
		public string Name;
		public bool StraightAlpha;
		public Raylib.ImageRef Texture;
		public ITexture? GpuTexture;
		public List<MDAtlasRegion> Regions = [];
		public ParametersReader Parameters = new ParametersReader([]);

		public void Dispose() {
			((IDisposable)Texture).Dispose();
		}

		internal void CheckSizing() {
			var texWidth = Parameters.Read<int>("size", 0);
			var texHeight = Parameters.Read<int>("size", 1);

			if (texWidth != Texture.Width || texHeight != Texture.Height) {
				float scaleX = (float)Texture.Width / texWidth;
				float scaleY = (float)Texture.Height / texHeight;

				foreach (var region in Regions)
					region.ApplyScale(scaleX, scaleY);
			}
		}

		public int GetRegionCount() => Regions.Count;
		public IModelAtlasRegion? GetRegion(int index) => (index >= 0 && index < Regions.Count) ? Regions[index] : null;
		public IModelAtlasRegion? GetRegionByName(ReadOnlySpan<char> name, int index = -1) {
			for (int i = 0, c = Regions.Count; i < c; i++)
				if (Regions[i].Name.AsSpan().SequenceEqual(name))
					return Regions[i];
			return null;
		}
		public ITexture GetTexture() => GpuTexture!;
		ReadOnlySpan<char> IModelAtlasPage.GetName() => Name;
		public bool SetName(ReadOnlySpan<char> name) { Name = new(name); return true; }
		public void GetSize(out int w, out int h) { w = GpuTexture?.GetWidth() ?? 0; h = GpuTexture?.GetHeight() ?? 0; }
		public ImageFormat GetFormat() => GpuTexture?.GetFormat() ?? ImageFormat.None;
		public void GetFilter(out TextureFilter min, out TextureFilter max) { min = TextureFilter.Bilinear; max = TextureFilter.Bilinear; }
		public bool SetFilter(TextureFilter min, TextureFilter max) => false;
		public TextureWrap GetWrap() => default;
		public bool SetWrap(TextureWrap wrapmode) => false;
		public bool GetPreMultipliedAlpha() => !StraightAlpha;
		public bool SetPreMultipliedAlpha(bool pma) { StraightAlpha = pma; return true; }
	}
	public class MDAtlasRegion : IModelAtlasRegion
	{
		public string Name;
		public MDAtlasPage Page;
		public ParametersReader Parameters = new ParametersReader([]);

		private int? _x, _y, _width, _height, _origW, _origH, _offsetX, _offsetY;

		public int Degrees {
			get {
				var degreesStr = Parameters.Read<string>("rotate")!;
				int degrees = degreesStr switch {
					"true" => 90,
					"false" => 0,
					_ => int.Parse(degreesStr)
				};

				return degrees;
			}
		}
		public int X => _x ?? Parameters.Read<int>("xy", 0);
		public int Y => _y ?? Parameters.Read<int>("xy", 1);
		public int Width => _width ?? Parameters.Read<int>("size", 0);
		public int Height => _height ?? Parameters.Read<int>("size", 1);
		public int OriginalWidth => _origW ?? Parameters.Read<int>("orig", 0);
		public int OriginalHeight => _origH ?? Parameters.Read<int>("orig", 1);
		public int OffsetX => _offsetX ?? Parameters.Read<int>("offset", 0);
		public int OffsetY => _offsetY ?? Parameters.Read<int>("offset", 1);

		internal void ApplyScale(float scaleX, float scaleY) {
			_x = (int)MathF.Round(Parameters.Read<int>("xy", 0) * scaleX);
			_y = (int)MathF.Round(Parameters.Read<int>("xy", 1) * scaleY);

			bool rotated = (Degrees % 180 == 90);
			if (rotated) {
				_width = (int)MathF.Round(Parameters.Read<int>("size", 0) * scaleY);
				_height = (int)MathF.Round(Parameters.Read<int>("size", 1) * scaleX);
			}
			else {
				_width = (int)MathF.Round(Parameters.Read<int>("size", 0) * scaleX);
				_height = (int)MathF.Round(Parameters.Read<int>("size", 1) * scaleY);
			}

			if (rotated) {
				_origW = (int)MathF.Round(Parameters.Read<int>("orig", 0) * scaleY);
				_origH = (int)MathF.Round(Parameters.Read<int>("orig", 1) * scaleX);
				_offsetX = (int)MathF.Round(Parameters.Read<int>("offset", 0) * scaleY);
				_offsetY = (int)MathF.Round(Parameters.Read<int>("offset", 1) * scaleX);
			}
			else {
				_origW = (int)MathF.Round(Parameters.Read<int>("orig", 0) * scaleX);
				_origH = (int)MathF.Round(Parameters.Read<int>("orig", 1) * scaleY);
				_offsetX = (int)MathF.Round(Parameters.Read<int>("offset", 0) * scaleX);
				_offsetY = (int)MathF.Round(Parameters.Read<int>("offset", 1) * scaleY);
			}
		}

		public ITexture GetTexture() => Page.GetTexture();
		public IModelAtlasPage GetPage() => Page;
		ReadOnlySpan<char> IModelAtlasRegion.GetName() => Name;
		public bool SetName(ReadOnlySpan<char> name) { Name = new(name); return true; }
		public int GetIndex() => -1;
		public bool SetIndex(int index) => true;
		public AtlasNameIndex GetNameIndex() => new(Name, -1);
		public bool SetNameIndex(AtlasNameIndex nameIndex) { Name = nameIndex.Name; return true; }

		public void GetBounds(out int x, out int y, out int w, out int h) {
			int deg = Degrees;
			x = X;
			y = Y;
			w = (deg % 180 == 90) ? Height : Width;
			h = (deg % 180 == 90) ? Width : Height;
		}

		public void GetOffsets(out int x, out int y, out int w, out int h) {
			x = OffsetX;
			y = (OriginalHeight - Height) - OffsetY;
			w = OriginalWidth;
			h = OriginalHeight;
		}

		public float GetRotation() => Degrees;

		public bool SetBounds(int x, int y, int w, int h) => false;
		public bool SetOffsets(int x, int y, int w, int h) => false;
		public bool SetRotation(float rot) => false;
	}
	public class MDAtlas(Dictionary<string, MDAtlasPage> pages, Dictionary<string, MDAtlasRegion> regions) : IRuntimeTextureAtlas
	{
		public Dictionary<string, MDAtlasPage> Pages => pages;
		public Dictionary<string, MDAtlasRegion> Regions => regions;
		private bool disposed;

		public ITextureAtlasEdit? Edit() => null;

		public IModelAtlasPage? GetPage(ReadOnlySpan<char> name) {
			foreach (var kvp in Pages)
				if (kvp.Key.AsSpan().SequenceEqual(name))
					return kvp.Value;
			return null;
		}

		public IModelAtlasRegion? GetRegion(ReadOnlySpan<char> name, int index = -1) {
			foreach (var kvp in Regions)
				if (kvp.Key.AsSpan().SequenceEqual(name))
					return kvp.Value;
			return null;
		}

		public void Dispose() {
			if (disposed) return;

			foreach (var page in Pages)
				page.Value.Dispose();

			pages = null;

			disposed = true;
		}
	}
	public class ParametersReader(Dictionary<string, string[]> kvps)
	{
		public void Add(string key, string[] values) => kvps.Add(key, values);
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
	[MarkForStaticConstruction]
	public static partial class MuseDash1Compatibility
	{
		public const uint MUSEDASH_APPID = 774171;
		public static string? WhereIsMuseDashInstalled { get; set; } = null;
		public static string? WhereIsMuseDashDataFolder { get; set; } = null;
		public static bool IsMuseDashInstalled => WhereIsMuseDashInstalled != null;

		public static string NoteManagerAssetBundle { get; private set; } = "";
		public static FrozenDictionary<string, List<string>> IBMSToDesc { get; private set; } = null!;
		public static FrozenDictionary<string, NoteConfigData> IDToNote { get; private set; } = null!;
		public static FrozenDictionary<string, NoteConfigData> IBMSToNote { get; private set; } = null!;
		public static FrozenDictionary<string, NoteConfigData> UIDToNote { get; private set; } = null!;

		public static List<BossConfigData> Bosses { get; set; } = [];
		public static List<CharacterConfigData> Characters { get; set; } = null!;
		public static List<CharacterLocalizationData> CharactersEN { get; set; } = [];
		public static List<CharacterLocalizationData> CharactersZHCN { get; set; } = [];
		public static List<CharacterLocalizationData> CharactersZHTW { get; set; } = [];
		public static List<FailBarrageConfigData> FailBarrages { get; set; } = [];
		public static List<FailBarrageConfigData> FailBarragesEN { get; set; } = [];
		public static List<CharacterLocalizationData> CharactersJP { get; set; } = [];
		public static List<CharacterLocalizationData> CharactersKO { get; set; } = [];
		public static List<NoteConfigData> NoteDataManager { get; set; } = null!;

		public static string BuildTarget { get; private set; }
		public static string StandalonePlatform { get; private set; }
		public static string[] StreamingFiles { get; private set; }
		public static UnitySearchPathV2 StreamingAssets { get; private set; }

		public static void FillInTheBlankNotes(MD1_Song song, StageInfo stage) {
			foreach (var md in stage.musicDatas) {
				if (md.noteData == null && md.configData.note_uid != null) {
					md.noteData = UIDToNote[md.configData.note_uid];
				}
			}
		}

		public ref struct IBMSCodeReturn
		{
			public IBMSCode Code;
			public ReadOnlySpan<char> Name;
		}

		/// <summary>
		/// Converts a Muse Dash IBMS ID (base-36) into the <see cref="IBMSCode"/> it represents
		/// </summary>
		/// <param name="ibms_id"></param>
		/// <returns></returns>
		public static IBMSCodeReturn ConvertIBMSCode(ReadOnlySpan<char> ibms_id) {
			int decValue = Base36StringToNumber(ibms_id);
			IBMSCode code = (IBMSCode)decValue;
			ReadOnlySpan<char> name = IBMSCodeToName(code);

			return new IBMSCodeReturn() { Code = code, Name = name };
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
		public static int Base36StringToNumber(ReadOnlySpan<char> s) {
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
		public static ReadOnlySpan<char> NumberToBase36String(int num, Span<char> writeBuffer) {
			if (num == 0) {
				writeBuffer[^1] = '0';
				return writeBuffer[^1..];
			}

			int charPtr = writeBuffer.Length;

			while (num > 0) {
				int remainder = num % 36;
				char c = (remainder < 10) ? (char)(remainder + '0') : (char)(remainder - 10 + 'A');
				writeBuffer[--charPtr] = c;
				num /= 36;
			}

			return writeBuffer[charPtr..];
		}

		/// <summary>
		/// Dumps the <see cref="IBMSCode"/> enumeration to console
		/// </summary>
		[ConCommand(Name: "ibmscodes", Help: "Dumps the IBMSCode enum to console.")]
		public static void DumpIBMSCodes() {
			Span<char> tempWrite = stackalloc char[13];
			foreach (var mbr in Enum.GetValuesAsUnderlyingType(typeof(IBMSCode)))
				Logs.Print($"{Enum.GetName(typeof(IBMSCode), mbr)}: {mbr} ({NumberToBase36String((int)mbr, tempWrite)})");
		}

		static readonly Stopwatch measureFunctionTime = new();
		/// <summary>
		/// Converts a Muse Dash unity asset bundle into a <see cref="DashSheet"/> for use in a <see cref="DashGame"/>
		/// </summary>
		/// <param name="bundlename"></param>
		/// <returns></returns>
		public static MD1_GamemodeData ConvertStageInfoToMD1GamemodeData(MD1_Song song, StageInfo MDinfo, IEnumerable<TempoChange>? tempoChanges = null, IEnumerable<TimeSignatureChange>? timeSignatureChanges = null, PathwayLayout? layout = null) {
			measureFunctionTime.Reset();
			measureFunctionTime.Start();
			MD1_GamemodeData gamemodeData = new();
			if (layout != null)
				gamemodeData.Layout = layout;

			var topPathway = gamemodeData.Layout.PathwayAt(0);
			var bottomPathway = gamemodeData.Layout.PathwayAt(gamemodeData.Layout.Count - 1);

			gamemodeData.InitialScene = "scene/musedash1/" + MDinfo.scene;
			gamemodeData.SceneChanges.AddRange(MDinfo.sceneEvents.Select(x => new ChartSceneChange() {
				SceneUID = "scene/musedash1/scene_" + x.uid.Split("SceneEvent/")[1],
				Time = Convert.ToDouble(x.time),
				Value = x.value
			}));

			gamemodeData.TempoChanges.Add(new(0, 0, MDinfo.bpm));
			if (tempoChanges != null)
				gamemodeData.TempoChanges.AddRange(tempoChanges);

			bool first = true;
			Dictionary<int, List<MusicData>> LongPresses = new();
			HashSet<string> WarnedIBMSPresses = new();
			foreach (var s in MDinfo.musicDatas) {
				Interlude.Spin(submessage: "Reading Muse Dash chart...");
				if (s.noteData == null && first) {
					gamemodeData.StartOffset = (float)s.tick;
				}

				if (s.noteData != null) {
					var ib = MuseDash1Compatibility.ConvertIBMSCode(s.noteData.ibms_id);
					var tick_hit = (double)s.configData.time;
					var tick_show = tick_hit - ((tick_hit - ((double)s.showTick - gamemodeData.StartOffset)) / (double)s.dt);

					PathwaySet pathwayType = PathwaySet.Any;

					switch (s.noteData.pathway) {
						case 1:
							pathwayType = topPathway.AsSet;
							break;
						case 0:
							pathwayType = bottomPathway.AsSet;
							break;
					}

					if (s.configData.pathway_name != null && gamemodeData.Layout.Find(s.configData.pathway_name) is PathwayId namedPathway)
						pathwayType = namedPathway.AsSet;

					var blood = s.configData.blood;

					if (s.isLongPressStart) {
						gamemodeData.Entities.Add(new MD1_SongChartEntity {
							Type = MuseDash1EntityType.SustainBeam,
							Pathway = pathwayType,
							EnterDirection = EntityEnterDirection.RightSide,
							HitTime = tick_hit,
							ShowTime = tick_show,
							Dt = (double)s.dt,
							Layer = s.configData.layer,
							Curve = s.configData.curve,
							LeftGreatRange = (double)s.noteData.left_great_range,
							LeftPerfectRange = (double)s.noteData.left_perfect_range,
							RightGreatRange = (double)s.noteData.right_great_range,
							RightPerfectRange = (double)s.noteData.right_perfect_range,

							Fever = s.noteData.fever,
							Damage = s.noteData.damage,
							Length = (double)s.configData.length,
							Score = s.noteData.score,
							Speed = s.noteData.speed,

							RelatedToBoss = false,
							DebuggingInfo = $"ib.code: {ib.Code}"
						});

						continue;
					}

					//Switch case for entity type
					bool isBoss = ib.Code switch {
						IBMSCode.BossBlock
						or IBMSCode.BossAttack1 or IBMSCode.BossAttack2_1 or IBMSCode.BossAttack2_2
						=> true,
						_ => false
					};

					EventType eventType = ib.Code switch {
						IBMSCode.BossIn => EventType.BossIn,
						IBMSCode.BossOut => EventType.BossOut,

						IBMSCode.BossNear1 => EventType.BossSingleHit,
						IBMSCode.BossNear2 => EventType.BossSingleHit,
						IBMSCode.BossMul1 => EventType.BossMasher,
						IBMSCode.BossMul2 => EventType.BossMasherEnd,

						IBMSCode.BossFar1Start => EventType.BossFar1Start,
						IBMSCode.BossFar1End => EventType.BossFar1End,
						IBMSCode.BossFar1To2 => EventType.BossFar1To2,
						IBMSCode.BossFar2Start => EventType.BossFar2Start,
						IBMSCode.BossFar2End => EventType.BossFar2End,
						IBMSCode.BossFar2To1 => EventType.BossFar2To1,

						IBMSCode.BossShow => EventType.BossShow,
						IBMSCode.BossHide => EventType.BossHide,

						IBMSCode.AirSpeed1 => EventType.AirSpeed1,
						IBMSCode.AirSpeed2 => EventType.AirSpeed2,
						IBMSCode.AirSpeed3 => EventType.AirSpeed3,
						IBMSCode.RoadSpeed1 => EventType.GroundSpeed1,
						IBMSCode.RoadSpeed2 => EventType.GroundSpeed2,
						IBMSCode.RoadSpeed3 => EventType.GroundSpeed3,
						IBMSCode.DoubleSpeed1 => EventType.DoubleSpeed1,
						IBMSCode.DoubleSpeed2 => EventType.DoubleSpeed2,
						IBMSCode.DoubleSpeed3 => EventType.DoubleSpeed3,

						IBMSCode.ToggleScene1 => EventType.SceneChange,
						IBMSCode.ToggleScene2 => EventType.SceneChange,
						IBMSCode.ToggleScene3 => EventType.SceneChange,
						IBMSCode.ToggleScene4 => EventType.SceneChange,
						IBMSCode.ToggleScene5 => EventType.SceneChange,
						IBMSCode.ToggleScene6 => EventType.SceneChange,
						IBMSCode.ToggleScene7 => EventType.SceneChange,
						IBMSCode.ToggleScene8 => EventType.SceneChange,
						IBMSCode.ToggleScene9 => EventType.SceneChange,
						IBMSCode.ToggleScene10 => EventType.SceneChange,

						IBMSCode.SceneShow => EventType.ShowBackground,
						IBMSCode.SceneHide => EventType.HideBackground,

						IBMSCode.NoteShow => EventType.ShowNotes,
						IBMSCode.NoteHide => EventType.HideNotes,

						IBMSCode.CanvasUpScroll => EventType.ScreenScrollUp,
						IBMSCode.CanvasDownScroll => EventType.ScreenScrollDown,
						IBMSCode.CanvasScrollOver => EventType.ScreenScrollEnd,
						IBMSCode.RandomWave => EventType.ScanlinesOn,
						IBMSCode.RandomWaveOver => EventType.ScanlinesOff,
						IBMSCode.RgbSplit => EventType.ChromaticAberrationOn,
						IBMSCode.RgbSplitOver => EventType.ChromaticAberrationOff,
						IBMSCode.ShadowEdgeIn => EventType.VignetteOn,
						IBMSCode.ShadowEdgeOut => EventType.VignetteOff,
						IBMSCode.OldTv => EventType.TVStaticOn,
						IBMSCode.OldTvOver => EventType.TVStaticOff,
						IBMSCode.FlashStart => EventType.FlashbangStart,
						IBMSCode.FlashHigh => EventType.FlashbangHigh,
						IBMSCode.FlashEnd => EventType.FlashbangEnd,
						IBMSCode.NoteFreeze => EventType.NoteFreeze,
						IBMSCode.NoteUnfreeze => EventType.NoteUnfreeze,
						IBMSCode.BgFreeze => EventType.BgFreeze,
						IBMSCode.BgUnfreeze => EventType.BgUnfreeze,
						IBMSCode.PixelStart => EventType.MosaicStart,
						IBMSCode.PixelEnd => EventType.MosaicEnd,
						IBMSCode.GrayScaleStart => EventType.SepiaStart,
						IBMSCode.GrayScaleEnd => EventType.SepiaEnd,
						IBMSCode.FocusLinesBlack => EventType.FocusLinesBlack,
						IBMSCode.FocusLinesWhite => EventType.FocusLinesWhite,
						IBMSCode.FocusLinesOff => EventType.FocusLinesOff,
						IBMSCode.FilmGrainOn => EventType.FilmGrainOn,
						IBMSCode.FilmGrainOff => EventType.FilmGrainOff,
						IBMSCode.AutoPlayOn => EventType.AutoPlayOn,
						IBMSCode.AutoPlayOff => EventType.AutoPlayOff,
						IBMSCode.FlashbangColorWhite => EventType.FlashbangColorWhite,
						IBMSCode.FlashbangColorBlack => EventType.FlashbangColorBlack,
						IBMSCode.FlashbangColorRed => EventType.FlashbangColorRed,
						IBMSCode.FlashbangColorGreen => EventType.FlashbangColorGreen,
						IBMSCode.FlashbangColorBlue => EventType.FlashbangColorBlue,
						IBMSCode.FlashbangColorCyan => EventType.FlashbangColorCyan,
						IBMSCode.FlashbangColorMagenta => EventType.FlashbangColorMagenta,
						IBMSCode.FlashbangColorYellow => EventType.FlashbangColorYellow,

						_ => EventType.NotApplicable
					};

					if (eventType != EventType.NotApplicable) {
						if (eventType == EventType.SceneChange) {
							gamemodeData.SceneChanges.Add(new ChartSceneChange() {
								SceneUID = "scene/musedash1/scene_" + MuseDash1Compatibility.GetSceneIndexFromIBMSCode(ib.Code).ToString().PadLeft(2, '0'),
								Time = tick_hit,
								Value = null
							});
						}
						else {
							MD1_SongChartEvent ChartEvent = new() {
								Type = eventType,
								Time = tick_hit,
								Length = (double)s.configData.length,
								Dt = (double)s.dt,
								LeftGreatRange = (double)s.noteData.left_great_range,
								LeftPerfectRange = (double)s.noteData.left_perfect_range,
								RightGreatRange = (double)s.noteData.right_great_range,
								RightPerfectRange = (double)s.noteData.right_perfect_range,
								Speed = s.noteData.speed
							};

							if (s.noteData.damage > 0)
								ChartEvent.Damage = s.noteData.damage;
							if (s.noteData.fever > 0)
								ChartEvent.Fever = s.noteData.fever;
							if (s.noteData.score > 0)
								ChartEvent.Score = s.noteData.score;

							ChartEvent.BossAction = s.noteData.boss_action;
							gamemodeData.Events.Add(ChartEvent);
						}
					}
					else {
						MuseDash1EntityType entityType = ib.Code switch {
							IBMSCode.SmallNormal or IBMSCode.SmallUp or IBMSCode.SmallDown
							or IBMSCode.Medium1Normal or IBMSCode.Medium2Normal or IBMSCode.Medium1Down or IBMSCode.Medium2Down or IBMSCode.Medium1Up or IBMSCode.Medium2Up
							or IBMSCode.Large1 or IBMSCode.Large2
							or IBMSCode.BossAttack1 or IBMSCode.BossAttack2_1 or IBMSCode.BossAttack2_2
								=> MuseDash1EntityType.Single,

							IBMSCode.Gemini => MuseDash1EntityType.Double,
							IBMSCode.Hammer or IBMSCode.HammerFlip => MuseDash1EntityType.Hammer,
							IBMSCode.Mul => MuseDash1EntityType.Masher,
							IBMSCode.BossBlock or IBMSCode.Block => MuseDash1EntityType.Gear,
							IBMSCode.Ghost => MuseDash1EntityType.Ghost,
							IBMSCode.Raider or IBMSCode.RaiderFlip => MuseDash1EntityType.Raider,
							IBMSCode.Music => MuseDash1EntityType.Score,
							IBMSCode.Hp => MuseDash1EntityType.Heart,

							IBMSCode.Touhou_MediumBullet => MuseDash1EntityType.Single,
							IBMSCode.Touhou_MediumBulletUp => MuseDash1EntityType.Single,
							IBMSCode.Touhou_MediumBulletDown => MuseDash1EntityType.Single,
							IBMSCode.Touhou_MediumBulletLaneshift => MuseDash1EntityType.Single,
							IBMSCode.Touhou_SmallBullet => MuseDash1EntityType.Single,
							IBMSCode.Touhou_SmallBulletUp => MuseDash1EntityType.Single,
							IBMSCode.Touhou_SmallBulletDown => MuseDash1EntityType.Single,
							IBMSCode.Touhou_SmallBulletLaneshift => MuseDash1EntityType.Single,
							IBMSCode.Touhou_LargeBullet => MuseDash1EntityType.Single,
							IBMSCode.Touhou_LargeBulletUp => MuseDash1EntityType.Single,
							IBMSCode.Touhou_LargeBulletDown => MuseDash1EntityType.Single,
							IBMSCode.Touhou_LargeBulletLaneshift => MuseDash1EntityType.Single,
							IBMSCode.Touhou_BossBullet1 => MuseDash1EntityType.Single,
							IBMSCode.Touhou_BossBullet1Laneshift => MuseDash1EntityType.Single,
							IBMSCode.Touhou_BossBullet2 => MuseDash1EntityType.Single,
							IBMSCode.Touhou_BossBullet2Laneshift => MuseDash1EntityType.Single,

							_ => 0
						};
						var health = ib.Code == IBMSCode.Hp ? MD1_SongChartEntity.DEFAULT_HP : 0;

						if (entityType != 0) {
							EntityVariant variant = ib.Code switch {
								IBMSCode.SmallNormal or IBMSCode.SmallUp or IBMSCode.SmallDown => EntityVariant.Small,
								IBMSCode.Medium1Normal or IBMSCode.Medium1Down or IBMSCode.Medium1Up => EntityVariant.Medium1,
								IBMSCode.Medium2Normal or IBMSCode.Medium2Down or IBMSCode.Medium2Up => EntityVariant.Medium2,
								IBMSCode.Large1 => EntityVariant.Large1,
								IBMSCode.Large2 => EntityVariant.Large2,

								IBMSCode.BossAttack1 => EntityVariant.Boss1,
								IBMSCode.BossAttack2_1 => EntityVariant.Boss2,
								IBMSCode.BossAttack2_2 => EntityVariant.Boss3,

								IBMSCode.Touhou_MediumBullet => EntityVariant.Medium1,
								IBMSCode.Touhou_MediumBulletUp => EntityVariant.Medium1,
								IBMSCode.Touhou_MediumBulletDown => EntityVariant.Medium1,
								IBMSCode.Touhou_MediumBulletLaneshift => EntityVariant.Medium1,
								IBMSCode.Touhou_SmallBullet => EntityVariant.Small,
								IBMSCode.Touhou_SmallBulletUp => EntityVariant.Small,
								IBMSCode.Touhou_SmallBulletDown => EntityVariant.Small,
								IBMSCode.Touhou_SmallBulletLaneshift => EntityVariant.Small,
								IBMSCode.Touhou_LargeBullet => EntityVariant.Large1,
								IBMSCode.Touhou_LargeBulletUp => EntityVariant.Large1,
								IBMSCode.Touhou_LargeBulletDown => EntityVariant.Large1,
								IBMSCode.Touhou_LargeBulletLaneshift => EntityVariant.Large1,
								IBMSCode.Touhou_BossBullet1 => EntityVariant.Boss1,
								IBMSCode.Touhou_BossBullet1Laneshift => EntityVariant.Boss1,
								IBMSCode.Touhou_BossBullet2 => EntityVariant.Boss2,
								IBMSCode.Touhou_BossBullet2Laneshift => EntityVariant.Boss2,

								// :/
								IBMSCode.BossBlock => s.noteData.IsPhase2BossGear() ? EntityVariant.Boss2 : EntityVariant.Boss1,

								_ => EntityVariant.NotApplicable
							};

							bool flipped = ib.Code == IBMSCode.HammerFlip || ib.Code == IBMSCode.RaiderFlip;

							EntityEnterDirection dir = ib.Code switch {
								IBMSCode.SmallDown or IBMSCode.Medium1Down or IBMSCode.Medium2Down or IBMSCode.Hammer => EntityEnterDirection.TopDown,
								IBMSCode.SmallUp or IBMSCode.Medium1Up or IBMSCode.Medium2Up or IBMSCode.HammerFlip => EntityEnterDirection.BottomUp,
								_ => EntityEnterDirection.RightSide
							};

							gamemodeData.Entities.Add(new MD1_SongChartEntity {
								Type = entityType,
								Variant = variant,
								Pathway = pathwayType,
								EnterDirection = dir,
								HitTime = tick_hit,
								ShowTime = tick_show,
								Dt = (double)s.dt,
								Layer = s.configData.layer,
								Flipped = flipped,
								LeftGreatRange = (double)s.noteData.left_great_range,
								LeftPerfectRange = (double)s.noteData.left_perfect_range,
								RightGreatRange = (double)s.noteData.right_great_range,
								RightPerfectRange = (double)s.noteData.right_perfect_range,

								Fever = s.noteData.fever,
								Damage = s.noteData.damage,
								Length = (double)s.configData.length,
								Score = s.noteData.score,
								Speed = s.noteData.speed,
								Health = health,
								Blood = blood,

								RelatedToBoss = isBoss,
								BossAction = s.noteData.boss_action,
								DebuggingInfo = $"ib.code: {ib.Code}"
							});
						}
						else if (WarnedIBMSPresses.Add(s.noteData.ibms_id)) {
							Logs.Warn("WARNING: An unidentified IBMS code with no compatibility translation definition was found during MD -> CD conversion.");
							Logs.Info($"IBMS Code: {s.noteData.ibms_id} (as int: {(int)ib.Code}, as name-definition: {ib.Code})");
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

			gamemodeData.Entities.Sort(static (x, y) => x.HitTime.CompareTo(y.HitTime));
			Interlude.Spin(submessage: "Reading Muse Dash chart...");

			Logs.Info($"STOPWATCH: ConvertAssetBundleToDashSheet: Translated Muse Dash level to DashSheet in {measureFunctionTime.Elapsed.TotalSeconds} seconds");
			return gamemodeData;
		}

		public static int GetSceneIndexFromIBMSString(string ibms_id) => GetSceneIndexFromIBMSCode((IBMSCode)Base36StringToNumber(ibms_id));
		private static int GetSceneIndexFromIBMSCode(IBMSCode code) => code switch {
			IBMSCode.ToggleScene1 => 1,
			IBMSCode.ToggleScene2 => 2,
			IBMSCode.ToggleScene3 => 3,
			IBMSCode.ToggleScene4 => 4,
			IBMSCode.ToggleScene5 => 5,
			IBMSCode.ToggleScene6 => 6,
			IBMSCode.ToggleScene7 => 7,
			IBMSCode.ToggleScene8 => 8,
			IBMSCode.ToggleScene9 => 9,
			IBMSCode.ToggleScene10 => 10,
			_ => 1
		};


		public static List<MuseDash1Album> Albums { get; private set; } = [];
		public static List<MD1_Song> Songs { get; private set; }

		private struct __musedashSong
		{
			public string name;
			public string author;
		}

		private struct __musedashAlbumJSON
		{
			public List<MuseDashSongInfoJSON> songs;
			public __musedashSong[]? songsEN, songsCN_S, songsCN_T, songsJP, songsKO;
		}

		static __musedashAlbumJSON[] albumJSONs = [];

		public static void ReadDashStructures() {
			Albums = ReadJSON<List<MuseDash1Album>>("Assets/Static Resources/Data/Configs/others/albums.json")!;
			Albums.RemoveAll(x => x.JsonName == "");

			var workAlbumJSONs = new __musedashAlbumJSON[Albums.Count];
			using (StaticSequentialProfiler.StartStackFrame("Parallel Process Dash Structures"))
				Parallel.For(0, Albums.Count, (i) => {
					var album = Albums[i];
					// var songs_raw = filesystem.ReadAllText($"Assets/Static Resources/Data/Configs/others/{album.JsonName}.json");
					// var songsEN_raw = filesystem.ReadAllText($"Assets/Static Resources/Data/Configs/english/{album.JsonName}_English.json");
					workAlbumJSONs[i] = new() {
						songs = ReadJSON<List<MuseDashSongInfoJSON>>($"Assets/Static Resources/Data/Configs/others/{album.JsonName}.json")!,
						songsEN = ReadJSON<__musedashSong[]>($"Assets/Static Resources/Data/Configs/english/{album.JsonName}_English.json"),
						songsCN_S = ReadJSON<__musedashSong[]>($"Assets/Static Resources/Data/Configs/chineses/{album.JsonName}_ChineseS.json"),
						songsCN_T = ReadJSON<__musedashSong[]>($"Assets/Static Resources/Data/Configs/chineset/{album.JsonName}_ChineseT.json"),
						songsJP = ReadJSON<__musedashSong[]>($"Assets/Static Resources/Data/Configs/japanese/{album.JsonName}_Japanese.json"),
						songsKO = ReadJSON<__musedashSong[]>($"Assets/Static Resources/Data/Configs/korean/{album.JsonName}_Korean.json")
					};
				});

			albumJSONs = workAlbumJSONs;
		}

		public static void BuildDashStructures() {
			Stopwatch s = new Stopwatch();
			s.Start();

			ConcurrentBag<MD1_Song> workSongs = [];
			ConcurrentBag<HashSet<char>> workCharCodepoints = [];
			using (StaticSequentialProfiler.StartStackFrame("Parallel Process Dash Structures"))
				Parallel.ForEach(albumJSONs, (albumJSON) => {
					var songs = albumJSON.songs;
					var songsEN = albumJSON.songsEN;
					var songsCN_S = albumJSON.songsCN_S;
					var songsCN_T = albumJSON.songsCN_T;
					var songsJP = albumJSON.songsJP;
					var songsKO = albumJSON.songsKO;

					var songsFinal = new MD1_Song[songs.Count];

					for (int i = 0; i < songs.Count; i++) {
						PatchSong(songs, i);
						var song = new MD1_Song();
						song.AddBaseJSONInfo(songs[i]);

						if (songsEN != null)
							song.AddLocalizedInfo(ILocalize.English, songsEN[i].name, songsEN[i].author);
						if (songsCN_S != null)
							song.AddLocalizedInfo(ILocalize.ChineseSimplified, songsCN_S[i].name, songsCN_S[i].author);
						if (songsCN_T != null)
							song.AddLocalizedInfo(ILocalize.ChineseTraditional, songsCN_T[i].name, songsCN_T[i].author);
						if (songsJP != null)
							song.AddLocalizedInfo(ILocalize.Japanese, songsJP[i].name, songsJP[i].author);
						if (songsKO != null)
							song.AddLocalizedInfo(ILocalize.Korean, songsKO[i].name, songsKO[i].author);

						workSongs.Add(song);
					}
				});

			albumJSONs = [];
			Songs = [.. workSongs];
			Songs.Sort((x, y) => x.FetchMetadata().Name.CompareTo(y.FetchMetadata().Name, StringComparison.InvariantCulture));
#if I_AM_LAZY_I_WANT_TO_KNOW_THIS_NUMBER
			Logs.Info($"total songs: {Songs.Count}, charts: {Songs.Sum(x => {
				if (x.GetInfo() == null)
					return 0;
				int count = 0;
				count += !string.IsNullOrEmpty(x.Info!.Difficulty1) ? 1 : 0;
				count += !string.IsNullOrEmpty(x.Info!.Difficulty2) ? 1 : 0;
				count += !string.IsNullOrEmpty(x.Info!.Difficulty3) ? 1 : 0;
				count += !string.IsNullOrEmpty(x.Info!.Difficulty4) ? 1 : 0;
				count += !string.IsNullOrEmpty(x.Info!.Difficulty5) ? 1 : 0;
				return count;
			})}");
#endif
		}

		const string CHAOS_UID = "33-4";
		const string CHAOS_GLITCH_UID = "33-12";
		private static void PatchSong(List<MuseDashSongInfoJSON> songs, int i) {
			switch (songs[i].UID) {
				case CHAOS_GLITCH_UID: // "CHAOS (Glitch)" by Aesir points to seemingly non-existant demo assets #18
					songs[i].Cover = "chaos_cover";
					songs[i].Demo = "chaos_demo";
					break;
			}
		}

		private enum MDAtlasBuildStep
		{
			Waiting,
			ReadyForPage,
			ReadingPage,
			ReadyForRegion,
			ReadingRegion
		}
		private class AtlasBuilder()
		{
			MDAtlas atlas = new MDAtlas([], []);

			MDAtlasPage? workingPage = null;
			MDAtlasRegion? workingRegion = null;

			public MDAtlasPage WorkingPage => workingPage ?? throw new Exception("Haven't built a page yet!");
			public MDAtlasRegion WorkingRegion => workingRegion ?? throw new Exception("Haven't built a region yet!");

			public MDAtlas Atlas() => atlas;
			public MDAtlasPage StartPage(string name) {
				workingPage = new MDAtlasPage();
				workingPage.Name = name;

				atlas.Pages[name] = workingPage;

				return workingPage;
			}

			public MDAtlasRegion StartRegion(string name) {
				workingRegion = new MDAtlasRegion();
				workingRegion.Name = name;

				workingRegion.Page = workingPage!;
				workingPage!.Regions.Add(workingRegion);

				atlas.Regions[name] = workingRegion;

				return workingRegion;
			}
		}

		private static bool tryReadProperty(string line, [NotNullWhen(true)] out string? key, [NotNullWhen(true)] out string[]? values) {
			int colon = line.IndexOf(':');

			if (colon == -1) {
				key = null;
				values = null;
				return false;
			}

			key = line.Substring(0, colon).Trim();
			values = line.Substring(colon + 1).Trim().Split(',');
			return true;
		}

		public static byte[] GetSoundBytes(Level level, AudioClip audioclip, out string? fileExtension) {
			fileExtension = null;

			if (audioclip == null) return null;

			var audiodata = audioclip.m_AudioData.GetData();

			if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
				FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
				bank.Samples[0].RebuildAsStandardFileFormat(out var at, out fileExtension);
				return at!;
			}

			throw new NotImplementedException();
		}
		public static IAudioClip? GetSound(Level level, AudioClip clip) {
			using MemoryStream ms = new(GetSoundBytes(level, clip, out _));
			return audiosystem.CreateStreamAudioClip(ms, $"UnityAudioClip:{clip.m_PathID}/{clip.m_Name}");
		}
		public static IAudioClip? GetMusic(Level level, AudioClip clip) {
			using MemoryStream ms = new(GetSoundBytes(level, clip, out _));
			return audiosystem.CreateStreamAudioClip(ms, $"UnityAudioClip:{clip.m_PathID}/{clip.m_Name}");
		}

		public static MDAtlas PopulateModelDataTextures(ModelData modelData, TextAsset? atlasAsset, Texture2D[] images, Material[] materials, bool uploadTextures = true) {
			using var ms = new MemoryStream(atlasAsset?.m_Script ?? []);
			using var sr = new StreamReader(ms);

			var text = sr.ReadToEnd();
			var lines = text.Replace("\r", "").Split('\n');

			AtlasBuilder atlasBuilder = new AtlasBuilder();
			// Some atlases start right at the page name
			MDAtlasBuildStep buildStep = MDAtlasBuildStep.ReadyForPage;

			foreach (var dirtyLine in lines) {
				var line = dirtyLine.Trim();

				if (string.IsNullOrEmpty(line)) {
					buildStep = MDAtlasBuildStep.ReadyForPage;
					continue;
				}

				switch (buildStep) {
					case MDAtlasBuildStep.ReadyForPage:
						var imageName = Path.ChangeExtension(line, null);
						var index = images.IndexOf(x => x != null && x.m_Name == imageName);
						// some materials have no texture (miku_name, empty_spi)
						atlasBuilder.StartPage(line).Texture = new(index >= 0 ? images[index].ToRaylib() : default);
						buildStep = MDAtlasBuildStep.ReadingPage;

						if (index >= 0)
							atlasBuilder.WorkingPage.StraightAlpha =
								((string)materials[index].ToType()["m_ShaderKeywords"]!).Contains("_STRAIGHT_ALPHA_INPUT");
						break;
					case MDAtlasBuildStep.ReadingPage: {
							if (!tryReadProperty(line, out var key, out var values))
								goto case MDAtlasBuildStep.ReadyForRegion;
							else {
								var page = atlasBuilder.WorkingPage;
								page.Parameters.Add(key, values);
							}
						}
						break;
					case MDAtlasBuildStep.ReadyForRegion: {
							atlasBuilder.StartRegion(line);
							buildStep = MDAtlasBuildStep.ReadingRegion;
						}
						break;
					case MDAtlasBuildStep.ReadingRegion: {
							if (!tryReadProperty(line, out var key, out var values))
								goto case MDAtlasBuildStep.ReadyForRegion;
							else {
								var region = atlasBuilder.WorkingRegion;
								region.Parameters.Add(key, values);
							}
						}
						break;
				}
			}

			MDAtlas atlas = atlasBuilder.Atlas();

			foreach (var pageKVP in atlas.Pages) {
				var page = pageKVP.Value;
				if (page.Texture.Width == 0) {
					Logs.Warn($"Atlas page '{page.Name}' has no texture in the game data, using a blank one.");
					page.Texture = new(Image.GenImageColor(
						Math.Max(1, page.Parameters.Read<int>("size", 0)),
						Math.Max(1, page.Parameters.Read<int>("size", 1)),
						Color.Blank));
				}
				page.CheckSizing();
			}

			if (uploadTextures)
				UploadAtlasTextures(atlas);

			modelData.TextureAtlas = atlas;
			modelData.SetupAttachments();
			return atlas;
		}

		public static void UploadModelDataTextures(ModelData modelData) {
			if (modelData.TextureAtlas is MDAtlas atlas)
				UploadAtlasTextures(atlas);
		}

		static void UploadAtlasTextures(MDAtlas atlas) {
			foreach (var page in atlas.Pages.Values) {
				if (page.GpuTexture != null)
					continue;

				page.GpuTexture = textures.CreateTexture(page.Texture);
				page.GpuTexture.SetFilter(TextureFilter.Bilinear);
				page.GpuTexture.SetWrap(TextureWrap.Clamp);
				page.GpuTexture.AddPublicFlags(PublicTextureFlags.RequiresFlippedV); // TODO: Do the OSX assets ship differently?
			}
		}

		public static IAudioClip? LoadSoundFromName(Level level, string audioName) {
			if (audioName == null) return null;

			var audioclip = StreamingAssets.FindAssetByName<AudioClip>(audioName);
			if (audioclip == null) throw new FileNotFoundException();

			return LoadSound(audioclip);
		}

		public static IAudioClip? LoadSound(AudioClip audioclip) {
			string audioName = audioclip.m_Name;
			var audiodata = audioclip.m_AudioData.GetData();

			if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
				FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
				bank.Samples[0].RebuildAsStandardFileFormat(out var at, out var fileExtension);
				if (at == null)
					return null;
				using var into = new MemoryStream(at);

				return audiosystem.CreateStreamAudioClip(into, $"MDAsset:{audioName}");
			}

			throw new NotImplementedException();
		}

		public static byte[]? RebuildSoundFromName(string audioName, out string clipName) {
			var audioclip = StreamingAssets.FindAssetByName<AudioClip>(audioName);
			if (audioclip == null) throw new FileNotFoundException();

			clipName = audioclip.m_Name;
			var audiodata = audioclip.m_AudioData.GetData();
			if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
				FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
				bank.Samples[0].RebuildAsStandardFileFormat(out var at, out _);
				return at;
			}

			throw new NotImplementedException();
		}

		public static IAudioClip? CreateSound(byte[]? rebuilt, string clipName) {
			if (rebuilt == null)
				return null;

			using var into = new MemoryStream(rebuilt);
			return audiosystem.CreateStreamAudioClip(into, $"MDAsset:{clipName}");
		}

		public static IAudioClip? LoadMusicFromName(Level level, string? audioName) {
			if (audioName == null) return null;
			var audioclip = StreamingAssets.FindAssetByName<AudioClip>(audioName);
			if (audioclip == null) throw new FileNotFoundException();

			var audiodata = audioclip.m_AudioData.GetData();

			if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
				FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
				bank.Samples[0].RebuildAsStandardFileFormat(out var at, out var fileExtension);
				if (at == null)
					return null;
				using var into = new MemoryStream(at);

				return audiosystem.CreateStreamAudioClip(into, $"MDAsset:{audioName}");
			}

			throw new NotImplementedException();
		}

		internal static MD1_Song? FindSong(ReadOnlySpan<char> md_level) {
			foreach (var song in Songs) {
				if (song.BaseName.Equals(md_level, StringComparison.InvariantCulture))
					return song;
			}
			return null;
		}

		static ThreadLocal<char[]> tempSearchBuffers = new ThreadLocal<char[]>(() => new char[512]);
		internal static IEnumerable<MD1_Song> FindSimilarSongs(ReadOnlySpan<char> md_level) {
			char[] buffer = tempSearchBuffers.Value!;
			md_level.CopyTo(buffer);
			int length = md_level.Length;
			return _FindSimilarSongs(buffer, length);
		}
		// If we don't do this we get instance of type cannot be preserved across await or yield boundary issues
		static IEnumerable<MD1_Song> _FindSimilarSongs(char[] buffer, int length) {
			foreach (var song in Songs) {
				if (song.BaseName.Contains(buffer.AsSpan()[..length], StringComparison.OrdinalIgnoreCase))
					yield return song;
			}
		}

		internal static IEnumerable<MD1_Song> FindSongsStartingWith(ReadOnlySpan<char> md_level) {
			char[] buffer = tempSearchBuffers.Value!;
			md_level.CopyTo(buffer);
			int length = md_level.Length;
			return _FindSongsStartingWith(buffer, length);
		}
		// If we don't do this we get instance of type cannot be preserved across await or yield boundary issues
		static IEnumerable<MD1_Song> _FindSongsStartingWith(char[] buffer, int length) {
			foreach (var song in Songs) {
				if (song.BaseName.StartsWith(buffer.AsSpan()[..length], StringComparison.OrdinalIgnoreCase))
					yield return song;
			}
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

[MarkForStaticConstruction]
/// <summary>
/// What the last conversion kept and dropped
/// </summary>
public class MD1ConversionStats
{
	// Converted
	public int IkConstraints;
	public int TransformConstraints;
	public int PathConstraints;
	public int IkTimelines;
	public int TransformTimelines;
	public int PathTimelines;
	public int DeformTimelines, DeformKeys;
	public int Events, EventKeys;
	public int Paths;
	public int LinkedMeshes;
	public int TwoColorTimelines;

	// Dropped
	public int BoundingBoxes;
	public int Points;
	/// <summary>
	/// Deforms on attachments that didn't convert
	/// </summary>
	public int UnresolvedDeforms;
	public int UnresolvedLinkedMeshes;
}

public static class MuseDash1ModelConverter
{
	public static MD1ConversionStats LastConversionStats { get; private set; } = new();


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

	/// <summary>
	/// Tint black color, 0x00RRGGBB. -1 = none
	/// </summary>
	public static Color? MD_ReadDarkColor(this MemoryStream asset) {
		int rgb = asset.MD_ReadInt();
		if (rgb == -1) return null;
		return new((byte)((rgb >> 16) & 0xff), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff), (byte)255);
	}

	/// <summary>
	/// Convert a Muse Dash model from Unity assets into a Nucleus Model4System compatible model, or at least tries to.
	/// </summary>
	/// <param name="skeletonTA"></param>
	/// <param name="atlas"></param>
	/// <param name="image"></param>
	/// <returns></returns>
	public static ModelData ConvertMuseDashModelData(ModelData nucleusModelData, TextAsset skeletonTA, MDAtlas mdatlas) {
		LastConversionStats = new();
		using (MemoryStream skeleton = new(MD1_SpineJson.IsJson(skeletonTA.m_Script) ? MD1_SpineJson.ToBinary(skeletonTA.m_Script) : skeletonTA.m_Script))
			//using (Raylib.ImageRef image = new(imageT2D.ToRaylib())) 
			{
			string hash = skeleton.MD_ReadString();
			string version = skeleton.MD_ReadString();

			skeleton.MD_ReadFloat(); // x
			skeleton.MD_ReadFloat(); // y
			skeleton.MD_ReadFloat(); // width
			skeleton.MD_ReadFloat(); // height

			bool nonessential = skeleton.MD_ReadBoolean();
			if (nonessential) {
				skeleton.MD_ReadFloat();

				skeleton.MD_ReadNullableString(); // images
				skeleton.MD_ReadNullableString(); // audio
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
				boneData.SkinRequired = skeleton.MD_ReadBoolean();
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
				slotData.DarkColor = MD_ReadDarkColor(skeleton);
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
				IkConstraintData ik = new();
				ik.Name = skeleton.MD_ReadString();
				ik.Order = skeleton.MD_ReadVarInt(true);
				ik.SkinRequired = skeleton.MD_ReadBoolean();
				MD_ReadConstraintBones(skeleton, nucleusModelData, ik);
				ik.Target = nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)];
				ik.Mix = skeleton.MD_ReadFloat();
				ik.Softness = skeleton.MD_ReadFloat();
				ik.BendDirection = (sbyte)skeleton.MD_ReadByte();
				ik.Compress = skeleton.MD_ReadBoolean();
				ik.Stretch = skeleton.MD_ReadBoolean();
				ik.Uniform = skeleton.MD_ReadBoolean();

				nucleusModelData.IkConstraints.Add(ik);
				LastConversionStats.IkConstraints++;
			}

			for (int i = 0, transforms = skeleton.MD_ReadVarInt(true); i < transforms; i++) {
				TransformConstraintData transform = new();
				transform.Name = skeleton.MD_ReadString();
				transform.Order = skeleton.MD_ReadVarInt(true);
				transform.SkinRequired = skeleton.MD_ReadBoolean();
				MD_ReadConstraintBones(skeleton, nucleusModelData, transform);
				transform.Target = nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)];
				transform.Local = skeleton.MD_ReadBoolean();
				transform.Relative = skeleton.MD_ReadBoolean();
				transform.OffsetRotation = skeleton.MD_ReadFloat();
				transform.OffsetX = skeleton.MD_ReadFloat();
				transform.OffsetY = skeleton.MD_ReadFloat();
				transform.OffsetScaleX = skeleton.MD_ReadFloat();
				transform.OffsetScaleY = skeleton.MD_ReadFloat();
				transform.OffsetShearY = skeleton.MD_ReadFloat();
				transform.RotateMix = skeleton.MD_ReadFloat();
				transform.TranslateMix = skeleton.MD_ReadFloat();
				transform.ScaleMix = skeleton.MD_ReadFloat();
				transform.ShearMix = skeleton.MD_ReadFloat();

				nucleusModelData.TransformConstraints.Add(transform);
				LastConversionStats.TransformConstraints++;
			}

			for (int i = 0, paths = skeleton.MD_ReadVarInt(true); i < paths; i++) {
				PathConstraintData path = new();
				path.Name = skeleton.MD_ReadString();
				path.Order = skeleton.MD_ReadVarInt(true);
				path.SkinRequired = skeleton.MD_ReadBoolean();
				MD_ReadConstraintBones(skeleton, nucleusModelData, path);
				path.Target = nucleusModelData.SlotDatas[skeleton.MD_ReadVarInt(true)];
				path.PositionMode = skeleton.MD_ReadVarInt(true) switch {
					PATH_POSITION_PERCENT => PositionMode.Percent,
					_ => PositionMode.Fixed
				};
				path.SpacingMode = skeleton.MD_ReadVarInt(true) switch {
					PATH_SPACING_FIXED => SpacingMode.Fixed,
					PATH_SPACING_PERCENT => SpacingMode.Percent,
					_ => SpacingMode.Length
				};
				path.RotateMode = skeleton.MD_ReadVarInt(true) switch {
					PATH_ROTATE_CHAIN => RotateMode.Chain,
					PATH_ROTATE_CHAIN_SCALE => RotateMode.ChainScale,
					_ => RotateMode.Tangent
				};
				// unscaled
				path.OffsetRotation = skeleton.MD_ReadFloat();
				path.Position = skeleton.MD_ReadFloat();
				path.Spacing = skeleton.MD_ReadFloat();
				path.RotateMix = skeleton.MD_ReadFloat();
				path.TranslateMix = skeleton.MD_ReadFloat();

				nucleusModelData.PathConstraints.Add(path);
				LastConversionStats.PathConstraints++;
			}

			// Empty default skin comes back null and isn't listed.
			nucleusModelData.DefaultSkin = MD_ReadSkin(skeleton, nucleusModelData, mdatlas, refStrings, true, nonessential);
			if (nucleusModelData.DefaultSkin != null)
				nucleusModelData.Skins.Add(nucleusModelData.DefaultSkin);

			for (int i = 0, skins = skeleton.MD_ReadVarInt(true); i < skins; i++)
				nucleusModelData.Skins.Add(MD_ReadSkin(skeleton, nucleusModelData, mdatlas, refStrings, false, nonessential));

			// When all skins/attachments have been read, linked meshes need to be fully linked up
			nucleusModelData.ResolveLinkedMeshes();
			HashSet<Attachment> linkedSeen = [];
			foreach (var skin in nucleusModelData.Skins) {
				foreach (var entry in skin.Attachments.ToArray()) {
					if (entry.Value is not MeshAttachment { IsLinked: true } linked) continue;
					if (linked.ParentMesh != null) {
						if (linkedSeen.Add(linked))
							LastConversionStats.LinkedMeshes++;
						continue;
					}
					// no vertices without a parent
					skin.Attachments.Remove(entry.Key);
					LastConversionStats.UnresolvedLinkedMeshes++;
					Logs.Warn($"Linked mesh '{linked.Name}' couldn't find its parent '{linked.ParentKey}' (skin {linked.ParentSkin ?? "default"}), skipping it.");
				}
			}

			for (int i = 0, events = skeleton.MD_ReadVarInt(true); i < events; i++) {
				EventData eventData = new();
				eventData.Name = skeleton.MD_ReadRefString(refStrings) ?? "";
				eventData.Int = skeleton.MD_ReadVarInt(false);
				eventData.Float = skeleton.MD_ReadFloat();
				eventData.String = skeleton.MD_ReadNullableString();
				eventData.AudioPath = skeleton.MD_ReadNullableString();
				if (eventData.AudioPath != null) {
					eventData.Volume = skeleton.MD_ReadFloat();
					eventData.Balance = skeleton.MD_ReadFloat();
				}

				nucleusModelData.Events.Add(eventData);
				LastConversionStats.Events++;
			}

			for (int i = 0, animations = skeleton.MD_ReadVarInt(true); i < animations; i++) {
				MD_ReadAnimation(skeleton, nucleusModelData, refStrings, nonessential);
			}

			return nucleusModelData;
		}
	}
	private static void MD_ReadConstraintBones(MemoryStream skeleton, ModelData nucleusModelData, ConstraintData constraint) {
		for (int i = 0, n = skeleton.MD_ReadVarInt(true); i < n; i++)
			constraint.Bones.Add(nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)]);
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


	private static void fillKeyframe(FCurve<float> curve, float time, float value, byte ct, float c1, float c2, byte prevCt, float c3, float c4) {
		var key = new Nucleus.Models.Keyframe<float>(time, value) {
			Interpolation = interp(ct),
			RightHandle = interpKFH(ct, c1, c2),
			LeftHandle = interpKFH(prevCt, c3, c4)
		};
		// Keys at the same time (a jump) would replace each other here.
		curve.Recompute();
		var keys = curve.Keyframes;
		if (keys.Count > 0 && key.Time - keys[^1].Time <= FCurve<float>.KEYFRAME_TIME_EPSILON) {
			var last = keys[^1];
			double moved = key.Time - 2 * FCurve<float>.KEYFRAME_TIME_EPSILON;
			if (keys.Count >= 2) {
				var previous = keys[keys.Count - 2];
				if (previous.Interpolation != KeyframeInterpolation.Constant && last.Time > previous.Time)
					last.Value = (float)(previous.Value + (last.Value - previous.Value) * (moved - previous.Time) / (last.Time - previous.Time));
			}
			last.Time = moved;
			keys[^1] = last;
		}
		curve.AddKeyframe(key);
	}

	private static void FixCurve(FCurve<float> curve, bool angles = false) =>
		LegacyCurves.ConvertNormalizedHandles(curve, angles);

	private static void MD_ReadAnimation(MemoryStream skeleton, ModelData nucleusModelData, string[] refStrings, bool nonessential) {
		Nucleus.Models.Runtime.Animation animation = new Nucleus.Models.Runtime.Animation();
		animation.Name = skeleton.MD_ReadNullableString() ?? "";
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
							tl.NewCurves(frames);

							for (int frame = 0; frame < frames; frame++)
								tl.Curve(0).AddKeyframe(new(skeleton.MD_ReadFloat(), skeleton.MD_ReadRefString(refStrings)));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
					case SLOT_COLOR: {
							SlotColor4Timeline tl = new SlotColor4Timeline();
							tl.SlotIndex = slotIndex;
							tl.NewCurves(frames);

							byte prevCt = CURVE_LINEAR; float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var color = skeleton.MD_ReadColor();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

								fillKeyframe(tl.Curve(0), time, color.R / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(tl.Curve(1), time, color.G / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(tl.Curve(2), time, color.B / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(tl.Curve(3), time, color.A / 255f, curveType, c1, c2, prevCt, lc3, lc4);

								prevCt = curveType;
								lc3 = c3;
								lc4 = c4;
							}
							FixCurve(tl.Curve(0));
							FixCurve(tl.Curve(1));
							FixCurve(tl.Curve(2));
							FixCurve(tl.Curve(3));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
					case SLOT_TWO_COLOR: {
							LastConversionStats.TwoColorTimelines++;
							SlotRGBA2Timeline timeline = new SlotRGBA2Timeline();
							timeline.SlotIndex = slotIndex;
							timeline.NewCurves(frames);

							byte prevCt = CURVE_LINEAR;
							float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var light = skeleton.MD_ReadColor();
								var dark = MD_ReadDarkColor(skeleton) ?? Color.Black;
								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

								fillKeyframe(timeline.Curve(0), time, light.R / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(1), time, light.G / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(2), time, light.B / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(3), time, light.A / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(4), time, dark.R / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(5), time, dark.G / 255f, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(timeline.Curve(6), time, dark.B / 255f, curveType, c1, c2, prevCt, lc3, lc4);

								prevCt = curveType;
								lc3 = c3;
								lc4 = c4;
							}
							for (int i = 0; i < 7; i++)
								FixCurve(timeline.Curve(i));

							animation.Timelines.Add(timeline);
							animation.Duration = Math.Max(animation.Duration, timeline.Curve(0)?.Last?.Time ?? 0);
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
							tl.NewCurves(frames);

							byte prevCt = CURVE_LINEAR; float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var value = skeleton.MD_ReadFloat();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);
								fillKeyframe(tl.Curve(0), time, value, curveType, c1, c2, prevCt, lc3, lc4);
								prevCt = curveType;
								lc3 = c3;
								lc4 = c4;
							}
							FixCurve(tl.Curve(0), true);
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
							tl.NewCurves(frames);

							byte prevCt = CURVE_LINEAR; float lc3 = 0, lc4 = 0;
							for (int frame = 0; frame < frames; frame++) {
								var time = skeleton.MD_ReadFloat();
								var x = skeleton.MD_ReadFloat();
								var y = skeleton.MD_ReadFloat();

								MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

								fillKeyframe(tl.Curve(0), time, x, curveType, c1, c2, prevCt, lc3, lc4);
								fillKeyframe(tl.Curve(1), time, y, curveType, c1, c2, prevCt, lc3, lc4);
								prevCt = curveType;
								lc3 = c3;
								lc4 = c4;
							}

							FixCurve(tl.Curve(0));
							FixCurve(tl.Curve(1));

							animation.Timelines.Add(tl);
							animation.Duration = Math.Max(animation.Duration, tl.Curve(0)?.Last?.Time ?? 0);
						}
						break;
				}
			}
		}
		for (int ikI = 0, iks = skeleton.MD_ReadVarInt(true); ikI < iks; ikI++) {
			IkConstraintTimeline timeline = new IkConstraintTimeline();
			timeline.ConstraintIndex = skeleton.MD_ReadVarInt(true);
			timeline.NewCurves();

			byte prevCt = CURVE_LINEAR;
			float lc3 = 0, lc4 = 0;
			for (int frame = 0, frames = skeleton.MD_ReadVarInt(true); frame < frames; frame++) {
				var time = skeleton.MD_ReadFloat();
				var mix = skeleton.MD_ReadFloat();
				var softness = skeleton.MD_ReadFloat();
				var bend = (sbyte)skeleton.MD_ReadByte();
				var compress = skeleton.MD_ReadBoolean();
				var stretch = skeleton.MD_ReadBoolean();
				MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

				fillKeyframe(timeline.Curve(0), time, mix, curveType, c1, c2, prevCt, lc3, lc4);
				fillKeyframe(timeline.Curve(1), time, softness, curveType, c1, c2, prevCt, lc3, lc4);
				// stepped
				fillKeyframe(timeline.Curve(2), time, bend, CURVE_STEPPED, 0, 0, CURVE_STEPPED, 0, 0);
				fillKeyframe(timeline.Curve(3), time, compress ? 1 : 0, CURVE_STEPPED, 0, 0, CURVE_STEPPED, 0, 0);
				fillKeyframe(timeline.Curve(4), time, stretch ? 1 : 0, CURVE_STEPPED, 0, 0, CURVE_STEPPED, 0, 0);

				prevCt = curveType;
				lc3 = c3;
				lc4 = c4;
			}
			FixCurve(timeline.Curve(0));
			FixCurve(timeline.Curve(1));

			animation.Timelines.Add(timeline);
			animation.Duration = Math.Max(animation.Duration, timeline.Curve(0)?.Last?.Time ?? 0);
			LastConversionStats.IkTimelines++;
		}

		for (int trI = 0, transforms = skeleton.MD_ReadVarInt(true); trI < transforms; trI++) {
			TransformConstraintTimeline timeline = new TransformConstraintTimeline();
			timeline.ConstraintIndex = skeleton.MD_ReadVarInt(true);
			timeline.NewCurves();

			byte prevCt = CURVE_LINEAR;
			float lc3 = 0, lc4 = 0;
			for (int frame = 0, frames = skeleton.MD_ReadVarInt(true); frame < frames; frame++) {
				var time = skeleton.MD_ReadFloat();
				var rotate = skeleton.MD_ReadFloat();
				var translate = skeleton.MD_ReadFloat();
				var scale = skeleton.MD_ReadFloat();
				var shear = skeleton.MD_ReadFloat();
				MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

				fillKeyframe(timeline.Curve(0), time, rotate, curveType, c1, c2, prevCt, lc3, lc4);
				fillKeyframe(timeline.Curve(1), time, translate, curveType, c1, c2, prevCt, lc3, lc4);
				fillKeyframe(timeline.Curve(2), time, scale, curveType, c1, c2, prevCt, lc3, lc4);
				fillKeyframe(timeline.Curve(3), time, shear, curveType, c1, c2, prevCt, lc3, lc4);

				prevCt = curveType;
				lc3 = c3;
				lc4 = c4;
			}
			FixCurve(timeline.Curve(0));
			FixCurve(timeline.Curve(1));
			FixCurve(timeline.Curve(2));
			FixCurve(timeline.Curve(3));

			animation.Timelines.Add(timeline);
			animation.Duration = Math.Max(animation.Duration, timeline.Curve(0)?.Last?.Time ?? 0);
			LastConversionStats.TransformTimelines++;
		}

		for (int pathI = 0, paths = skeleton.MD_ReadVarInt(true); pathI < paths; pathI++) {
			int pathIndex = skeleton.MD_ReadVarInt(true);
			for (int i = 0, c = skeleton.MD_ReadVarInt(true); i < c; i++) {
				int type = skeleton.MD_ReadByte(), frames = skeleton.MD_ReadVarInt(true);
				CurveTimeline<float> timeline = type switch {
					PATH_POSITION => new PathConstraintPositionTimeline() { ConstraintIndex = pathIndex },
					PATH_SPACING => new PathConstraintSpacingTimeline() { ConstraintIndex = pathIndex },
					_ => new PathConstraintMixTimeline() { ConstraintIndex = pathIndex }
				};
				timeline.NewCurves(frames);

				byte prevCt = CURVE_LINEAR;
				float lc3 = 0, lc4 = 0;
				for (int frame = 0; frame < frames; frame++) {
					var time = skeleton.MD_ReadFloat();
					var value = skeleton.MD_ReadFloat();
					var translate = type == PATH_MIX ? skeleton.MD_ReadFloat() : 0;
					MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);

					fillKeyframe(timeline.Curve(0), time, value, curveType, c1, c2, prevCt, lc3, lc4);
					if (type == PATH_MIX)
						fillKeyframe(timeline.Curve(1), time, translate, curveType, c1, c2, prevCt, lc3, lc4);

					prevCt = curveType;
					lc3 = c3;
					lc4 = c4;
				}
				FixCurve(timeline.Curve(0));
				if (type == PATH_MIX)
					FixCurve(timeline.Curve(1));

				animation.Timelines.Add(timeline);
				animation.Duration = Math.Max(animation.Duration, timeline.Curve(0)?.Last?.Time ?? 0);
				LastConversionStats.PathTimelines++;
			}
		}

		for (int deformI = 0, deforms = skeleton.MD_ReadVarInt(true); deformI < deforms; deformI++) {
			Skin skin = nucleusModelData.Skins[skeleton.MD_ReadVarInt(true)];
			for (int i2 = 0, deformSlots = skeleton.MD_ReadVarInt(true); i2 < deformSlots; i2++) {
				int slotIndex = skeleton.MD_ReadVarInt(true);
				for (int i3 = 0, deformAttachments = skeleton.MD_ReadVarInt(true); i3 < deformAttachments; i3++) {
					string attachmentName = skeleton.MD_ReadRefString(refStrings) ?? "";
					int frames = skeleton.MD_ReadVarInt(true);

					// Offsets from setup: one pair per weight when weighted, per vertex otherwise
					var vertices = (skin.GetAttachment(slotIndex, attachmentName) as VertexAttachment)?.GetVertices();
					int deformLength = 0;
					if (vertices != null) {
						foreach (var vertex in vertices)
							deformLength += (vertex.Weights?.Length ?? 0) > 0 ? vertex.Weights!.Length * 2 : 2;
					}

					DeformTimeline? timeline = null;
					if (vertices != null) {
						timeline = new DeformTimeline();
						timeline.SlotIndex = slotIndex;
						timeline.Skin = skin == nucleusModelData.DefaultSkin ? null : skin.Name;
						timeline.Attachment = attachmentName;
					}
					else {
						LastConversionStats.UnresolvedDeforms++;
						Logs.Warn($"Deform timeline in '{animation.Name}' points at '{attachmentName}' (slot {slotIndex}, skin {skin.Name}), which didn't convert, skipping it.");
					}

					byte prevCt = CURVE_LINEAR;
					float lc3 = 0, lc4 = 0;
					for (int frame = 0; frame < frames; frame++) {
						var time = skeleton.MD_ReadFloat();
						float[] offsets = new float[deformLength];
						var end = skeleton.MD_ReadVarInt(true);
						if (end != 0) {
							int start = skeleton.MD_ReadVarInt(true);
							end += start;
							for (int i4 = start; i4 < end; i4++) {
								var value = skeleton.MD_ReadFloat();
								if (i4 < deformLength)
									offsets[i4] = value;
							}
						}

						MD_ReadCurve(skeleton, frame, frames, out var curveType, out var c1, out var c2, out var c3, out var c4);
						if (timeline != null) {
							// index trick, key k = k
							fillKeyframe(timeline.Curve, time, frame, curveType, c1, c2, prevCt, lc3, lc4);
							timeline.Offsets.Add(offsets);
						}
						prevCt = curveType;
						lc3 = c3;
						lc4 = c4;
					}

					if (timeline == null || frames == 0) continue;
					FixCurve(timeline.Curve);
					animation.Timelines.Add(timeline);
					animation.Duration = Math.Max(animation.Duration, timeline.Curve.Last?.Time ?? 0);
					LastConversionStats.DeformTimelines++;
					LastConversionStats.DeformKeys += frames;
				}
			}
		}

		int drawOrderFrames = skeleton.MD_ReadVarInt(true);
		if (drawOrderFrames > 0) {
			int slotCount = nucleusModelData.SlotDatas.Count;
			DrawOrderTimeline drawTimeline = new();
			drawTimeline.NewCurves();

			for (int drawI = 0; drawI < drawOrderFrames; drawI++) {
				float time = skeleton.MD_ReadFloat();
				int offsetCount = skeleton.MD_ReadVarInt(true);

				int[] drawOrder = new int[slotCount];
				for (int i2 = 0; i2 < slotCount; i2++)
					drawOrder[i2] = -1;

				int[] unchanged = new int[slotCount - offsetCount];
				int originalIndex = 0, unchangedIndex = 0;

				for (int i2 = 0; i2 < offsetCount; i2++) {
					int slotIndex = skeleton.MD_ReadVarInt(true);
					int offset = skeleton.MD_ReadVarInt(true);

					while (originalIndex != slotIndex)
						unchanged[unchangedIndex++] = originalIndex++;

					drawOrder[originalIndex + offset] = originalIndex++;
				}

				while (originalIndex < slotCount)
					unchanged[unchangedIndex++] = originalIndex++;

				for (int i2 = slotCount - 1; i2 >= 0; i2--)
					if (drawOrder[i2] == -1)
						drawOrder[i2] = unchanged[--unchangedIndex];

				drawTimeline.Curve(0).AddKeyframe(new(time, drawOrder) { Interpolation = Nucleus.Models.KeyframeInterpolation.Constant });
			}

			animation.Timelines.Add(drawTimeline);
			animation.Duration = Math.Max(animation.Duration, drawTimeline.Curve(0)?.Last?.Time ?? 0);
		}

		int eventKeys = skeleton.MD_ReadVarInt(true);
		if (eventKeys > 0) {
			EventTimeline eventTimeline = new();
			for (int eventIndex = 0; eventIndex < eventKeys; eventIndex++) {
				float time = skeleton.MD_ReadFloat();
				EventData eventData = nucleusModelData.Events[skeleton.MD_ReadVarInt(true)];
				ModelEvent modelEvent = new() {
					Time = time,
					Data = eventData,
					Int = skeleton.MD_ReadVarInt(false),
					Float = skeleton.MD_ReadFloat(),
					Volume = eventData.Volume,
					Balance = eventData.Balance
				};
				modelEvent.String = skeleton.MD_ReadBoolean() ? skeleton.MD_ReadNullableString() : eventData.String;
				if (eventData.AudioPath != null) {
					modelEvent.Volume = skeleton.MD_ReadFloat();
					modelEvent.Balance = skeleton.MD_ReadFloat();
				}

				eventTimeline.Events.Add(modelEvent);
				LastConversionStats.EventKeys++;
			}

			animation.Timelines.Add(eventTimeline);
			animation.Duration = Math.Max(animation.Duration, eventTimeline.Events[eventTimeline.Events.Count - 1].Time);
		}

		foreach (var timeline in animation.Timelines.OfType<CurveTimeline<float>>())
			LegacyCurves.SplitOverruns(timeline.Curves);
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
				skin.Bones.Add(nucleusModelData.BoneDatas[skeleton.MD_ReadVarInt(true)]);

			var ikCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < ikCount; i2++)
				skin.Constraints.Add(nucleusModelData.IkConstraints[skeleton.MD_ReadVarInt(true)]);

			var transformCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < transformCount; i2++)
				skin.Constraints.Add(nucleusModelData.TransformConstraints[skeleton.MD_ReadVarInt(true)]);

			var pathCount = skeleton.MD_ReadVarInt(true);
			for (int i2 = 0; i2 < pathCount; i2++)
				skin.Constraints.Add(nucleusModelData.PathConstraints[skeleton.MD_ReadVarInt(true)]);

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
							// Atlas names are trimmed, the skeleton's keep trailing spaces
							string path = (skeleton.MD_ReadRefString(refStrings) ?? name)!.Trim();
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

							var mdRegion = mdatlas.Regions.FirstOrDefault(x => x.Value.Name == region.Path).Value;
							if (mdRegion == null) {
								Logs.Warn($"Region attachment '{name}' points at '{path}', which isn't in the atlas, skipping it.");
								break;
							}
							var wM = width / mdRegion.OriginalWidth;
							var hM = height / mdRegion.OriginalHeight;
							region.Scale = new(scaleX * wM, scaleY * hM);
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
							string path = (skeleton.MD_ReadRefString(refStrings) ?? name)!.Trim();

							Color color = skeleton.MD_ReadColor();

							var vertexCount = skeleton.MD_ReadVarInt(true);
							float[] uArray = new float[vertexCount];
							float[] vArray = new float[vertexCount];
							for (int uv = 0; uv < vertexCount; uv++) {
								uArray[uv] = skeleton.MD_ReadFloat();
								vArray[uv] = skeleton.MD_ReadFloat();
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
							mesh.Name = name!;
							mesh.Path = path;
							mesh.Color = color;
							mesh.Vertices = vertices;
							mesh.Triangles = triangles;
							mesh.HullCount = hullCount;
							mesh.Position = new(0);
							mesh.Rotation = 0;
							mesh.Scale = new(1);

							for (int i3 = 0; i3 < vertexCount; i3++) {
								mesh.Vertices[i3].U = uArray[i3];
								mesh.Vertices[i3].V = 1 - vArray[i3];
							}

							attachment = mesh;
						}
						break;
					case ATTACHMENT_LINKED_MESH: {
							var path = skeleton.MD_ReadRefString(refStrings);
							var color = skeleton.MD_ReadColor();
							var skinname = skeleton.MD_ReadRefString(refStrings);
							var parent = skeleton.MD_ReadRefString(refStrings);
							var deform = skeleton.MD_ReadBoolean();
							float width = 0;
							float height = 0;
							if (nonessential) {
								width = skeleton.MD_ReadFloat();
								height = skeleton.MD_ReadFloat();
							}

							path = (path ?? name)!.Trim();

							MeshAttachment meshAttachment = new();
							meshAttachment.Name = name!;
							meshAttachment.Path = path!;
							meshAttachment.Color = color;
							meshAttachment.Position = new(0);
							meshAttachment.Rotation = 0;
							meshAttachment.Scale = new(1);

							// parent gets hooked up once every skin is read
							meshAttachment.ParentKey = parent ?? "";
							meshAttachment.ParentSkin = skinname;
							meshAttachment.InheritDeform = deform;

							attachment = meshAttachment;
						}
						break;
					case ATTACHMENT_PATH: {
							PathAttachment pathAttachment = new();
							pathAttachment.Name = name!;
							pathAttachment.Closed = skeleton.MD_ReadBoolean();
							pathAttachment.ConstantSpeed = skeleton.MD_ReadBoolean();
							int vertexCount = skeleton.MD_ReadVarInt(true);
							pathAttachment.Vertices = MD_ReadVertices(skeleton, vertexCount, out _);
							pathAttachment.Lengths = new float[vertexCount / 3];
							for (int i3 = 0; i3 < pathAttachment.Lengths.Length; i3++)
								pathAttachment.Lengths[i3] = skeleton.MD_ReadFloat();
							if (nonessential)
								pathAttachment.Color = skeleton.MD_ReadColor();
							pathAttachment.Position = new(0);
							pathAttachment.Rotation = 0;
							pathAttachment.Scale = new(1);

							attachment = pathAttachment;
							LastConversionStats.Paths++;
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
							clip.Name = name!;
							clip.EndSlot = nucleusModelData.SlotDatas[endSlot].Name;
							clip.Vertices = vertices;

							attachment = clip;
						}
						break;
				}

				if (attachment != null)
					skin.SetAttachment(slotIndex, attachmentName, attachment);
				else if (type is ATTACHMENT_BOUNDING_BOX or ATTACHMENT_POINT) {
					// no runtime types for these yet
					switch (type) {
						case ATTACHMENT_BOUNDING_BOX: LastConversionStats.BoundingBoxes++; break;
						case ATTACHMENT_POINT: LastConversionStats.Points++; break;
					}
					Logs.Warn($"Ignoring unsupported attachment type ({type switch {
						ATTACHMENT_BOUNDING_BOX => "bounding box",
						ATTACHMENT_POINT => "point",
						_ => "???"
					}})");
				}
			}
		}
		return skin;
	}

	// EVERYTHING in Clone Dash should probably go through these methods, or at least those that use in-game/menu assets.
	// This lets musedash overrides work

	public static ModelData MD_GetModelData(this Level level, MonoBehaviour skeletonData, bool uploadTextures = true) {
		MonoBehaviourReader reader = new(skeletonData);
		TextAsset skeletonJSON = reader.Get<TextAsset>("skeletonJSON")!;
		// Placeholder skeletons can come without an atlas or texture.
		MonoBehaviour? atlasAsset = reader.GetList<MonoBehaviour>("atlasAssets")?.FirstOrDefault();
		MonoBehaviourReader? atlasAssets = atlasAsset == null ? null : new(atlasAsset);
		TextAsset? atlasFile = atlasAssets?.Get<TextAsset>("atlasFile");
		List<Material> materials = atlasAssets?.GetList<Material>("materials") ?? [];

		long[] texturePaths = new long[materials.Count];
		for (int i = 0; i < materials.Count; i++) {
			var mat = materials[i];
			texturePaths[i] = mat.m_SavedProperties.m_TexEnvs.FirstOrDefault().Value?.m_Texture.m_PathID ?? 0;
		}
		return MD_GetModelData(level, skeletonJSON, atlasFile, texturePaths, materials.ToArray(), uploadTextures);
	}

	public static ModelData MD_GetModelData(this Level level, long skeletonPath, long atlasPath, long[] texturePaths, Material[] materialsIn, bool uploadTextures = true) {
		var skeleton = MuseDash1Compatibility.StreamingAssets.FindAssetByPathID<TextAsset>(skeletonPath)!;
		var atlas = MuseDash1Compatibility.StreamingAssets.FindAssetByPathID<TextAsset>(atlasPath)!;

		return MD_GetModelData(level, skeleton, atlas, texturePaths, materialsIn, uploadTextures);
	}

	public static ModelData MD_GetModelData(this Level level, TextAsset skeleton, TextAsset? atlas, long[] texturePaths, Material[] materialsIn, bool uploadTextures = true) {
		ModelData md_data = new ModelData();
		// Only this shader draws dark colors, other models carry them unused.
		md_data.RenderTintBlack = materialsIn.Any(m => MuseDash1Compatibility.StreamingAssets
			.FindAssetByPathID<AssetStudio.Shader>(m.m_Shader.m_PathID)?.m_ParsedForm?.m_Name == "Spine/Skeleton Tint Black");

		Texture2D[] textures = new Texture2D[texturePaths.Length];
		for (int i = 0, c = texturePaths.Length; i < c; i++) {
			textures[i] = MuseDash1Compatibility.StreamingAssets.FindAssetByPathID<Texture2D>(texturePaths[i])!;
		}

		ConvertMuseDashModelData(
			md_data,
			skeleton,
			MuseDash1Compatibility.PopulateModelDataTextures(md_data, atlas, textures, materialsIn, uploadTextures)
		);

		var defaultSkin = md_data.DefaultSkin;
		// No default skin, merge into a new one
		if (defaultSkin == null && md_data.Skins.Count > 0) {
			defaultSkin = md_data.DefaultSkin = new Skin() { Name = "default" };
			md_data.Skins.Insert(0, defaultSkin);
		}
		switch ((MD1_HexieMode)md_hexiemode.GetInt()) {
			case MD1_HexieMode.Original:
				var origin = md_data.FindSkin("origin");
				if (origin != null)
					defaultSkin?.AddSkin(origin);
				break;
			case MD1_HexieMode.Hexie:
				var hexie = md_data.FindSkin("hexie");
				if (hexie != null)
					defaultSkin?.AddSkin(hexie);
				break;
		}

		// This feels kinda... ehh...
		// I will allow it for now, but in the future, we should figure out
		// what would merge these skins under normal conditions.
		if (defaultSkin != null)
			foreach (var skin in md_data.Skins) {
				if (skin == defaultSkin) continue;
				if (skin.Name is "origin" or "hexie") continue;
				defaultSkin.AddSkin(skin);
			}

		return md_data;
	}

	public static readonly ConVar md_hexiemode = new(nameof(md_hexiemode), "0", FCvar.Saved, "Enables/disables suggestive/revealing content.", 0, 1);
}

public enum MD1_HexieMode
{
	Original,
	Hexie
}