using AssetStudio;
using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.Unity;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.Codecs;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Core;
using Nucleus.Util;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using OsuParsers.Enums.Beatmaps;
using SixLabors.ImageSharp.Drawing;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public enum UWLCompatLayerInitResult
{
	OK,
	SteamNotInstalled,
	UnbeatableNotInstalled,
	StreamingAssetsNotFound,
	NoteDataManagerNotFound,
	OperatingSystemNotCompatible
}

public enum Height
{
	None,
	Low,
	Mid,
	Top,
	Side
}
public enum Side
{
	None,
	Left,
	Right
}
public enum NoteType
{
	None,
	Default,
	Spam,
	Freestyle,
	Dodge,
	Double,
	Hold,
	Setpiece
}
public enum NoteSpeed
{
	Standard,
	Stepped,
	Fast
}
public static class HitObjectExt
{
	extension(HitObject self)
	{
		public HitSoundType hitSound => self.HitSound;
		public int time => (int)self.StartTime;
		public int type => (int)self.EndTime;
		public int laneNumber {
			get {
				return (int)(self.Position.X * 6 / 512 + 1);
			}
		}
		public bool IsCommand() {
			return self.laneNumber == 1 || self.laneNumber == 2;
		}

		public bool IsNormalNote() {
			return self.laneNumber == 3 || self.laneNumber == 4;
		}

		public bool IsFlip() {
			return self.laneNumber == 5;
		}

		public bool IsExtraNote() {
			return self.laneNumber == 6;
		}

		public bool IsAnyNote() {
			return self.IsNormalNote() || self.IsExtraNote();
		}

		public bool IsInstantType() {
			return (self.type & 129) == 1;
		}

		public bool IsHoldType() {
			return (self.type & 129) == 128;
		}

		public bool IsSpawnMid() {
			return self.Extras.SampleSet == SampleSet.Normal;
		}

		public bool IsHiding() {
			return self.IsNormalNote() && self.hitSound == HitSoundType.Clap && (self.IsInstantType() || self.IsHoldType());
		}

		public bool IsToggleCenter() {
			return self.IsFlip() && self.hitSound == HitSoundType.Whistle;
		}

		public bool IsCameraSwapImmediate() {
			return self.IsFlip() && self.hitSound == HitSoundType.Clap;
		}

		public Height GetNoteHeight() {
			switch (self.laneNumber) {
				case 1:
				case 2:
					throw new Exception(string.Format("Trying to get the height of an effect (lane {0}, time {1})", self.laneNumber, self.time));
				case 3:
					return Height.Top;
				case 4:
					return Height.Low;
				case 5:
					throw new Exception(string.Format("Trying to get the height of a side flip (lane {0}, time {1})", self.laneNumber, self.time));
				case 6:
					return Height.Mid;
				default:
					throw new Exception(string.Format("Trying to get the height of a hit object on unsupported lane {0} (time {1})", self.laneNumber, self.time));
			}
		}
		public NoteSpeed GetNoteSpeed() {
			int text = (int)self.Extras.AdditionSet;
			if (text == 1) {
				return NoteSpeed.Stepped;
			}
			if (!(text == 2)) {
				return NoteSpeed.Standard;
			}
			return NoteSpeed.Fast;
		}

		public NoteType GetNoteType() {
			if (self.IsNormalNote()) {
				if (self.IsInstantType()) {
					if (self.HitSound == HitSoundType.Normal) {
						return NoteType.Default;
					}
					if (self.HitSound == HitSoundType.Whistle) {
						return NoteType.Dodge;
					}
					if (self.HitSound == HitSoundType.Clap) {
						return NoteType.Default;
					}
					if (self.HitSound == HitSoundType.Finish) {
						return NoteType.Setpiece;
					}
					throw new Exception(string.Format("Normal hit sound {0} does not correspond to any note type (lane {1}, time {2})", self.HitSound, self.laneNumber, self.time));
				}
				else {
					if (!self.IsHoldType()) {
						throw new Exception(string.Format("Unsupported hit object note of type {0} (lane {1}, time {2})", self.type, self.laneNumber, self.time));
					}
					if (self.HitSound == HitSoundType.Normal) {
						return NoteType.Hold;
					}
					if (self.HitSound == HitSoundType.Whistle) {
						return NoteType.Double;
					}
					if (self.HitSound == HitSoundType.Clap) {
						return NoteType.Hold;
					}
					throw new Exception(string.Format("Hold hit sound {0} does not correspond to any note type (lane {1}, time {2})", self.HitSound, self.laneNumber, self.time));
				}
			}
			else {
				if (!self.IsExtraNote()) {
					throw new Exception(string.Format("Unsupported hit object on lane {0} (time {1})", self.laneNumber, self.time));
				}
				if (self.IsInstantType()) {
					if (self.HitSound == HitSoundType.Normal) {
						return NoteType.Freestyle;
					}
					throw new Exception(string.Format("Normal hit sound {0} does not correspond to any extra note type (lane {1}, time {2})", self.HitSound, self.laneNumber, self.time));
				}
				else {
					if (!self.IsHoldType()) {
						throw new Exception(string.Format("Unsupported hit object extra note of type {0} (lane {1}, time {2})", self.type, self.laneNumber, self.time));
					}
					if (self.HitSound == HitSoundType.Finish) {
						return NoteType.Spam;
					}
					throw new Exception(string.Format("Hold hit sound {0} does not correspond to any extra note type (lane {1}, time {2})", self.HitSound, self.laneNumber, self.time));
				}
			}
		}
	}
}

public record struct Lane
{
	public static Lane Top(Side side) {
		return new Lane {
			height = Height.Top,
			side = side
		};
	}

	public static Lane Mid(Side side) {
		return new Lane {
			height = Height.Mid,
			side = side
		};
	}

	public static Lane Low(Side side) {
		return new Lane {
			height = Height.Low,
			side = side
		};
	}

	public Height height;
	public Side side;

	public static readonly Lane TopLeft = Lane.Top(Side.Left);
	public static readonly Lane MidLeft = Lane.Mid(Side.Left);
	public static readonly Lane LowLeft = Lane.Low(Side.Left);
	public static readonly Lane TopRight = Lane.Top(Side.Right);
	public static readonly Lane MidRight = Lane.Mid(Side.Right);
	public static readonly Lane LowRight = Lane.Low(Side.Right);
}

public class NoteInfo
{
	public Lane lane {
		get {
			return new Lane {
				height = this.height,
				side = this.side
			};
		}
	}

	public bool hasEndTime {
		get {
			return this.type == NoteType.Double || this.type == NoteType.Hold || this.type == NoteType.Spam;
		}
	}

	public NoteInfo(HitObject hitObject, Side side) {
		this.time = (float)hitObject.time;
		this.side = side;
		this.height = hitObject.GetNoteHeight();
		this.type = hitObject.GetNoteType();
		this.hiding = hitObject.IsHiding();
		this.speed = hitObject.GetNoteSpeed();
		this.spawnMid = hitObject.IsSpawnMid();
		this.extra = [$"{(int)hitObject.Extras.SampleSet}"]; // todo..
	}

	public float GetEndTime() {
		if (!this.hasEndTime)
			throw new Exception(string.Format("Cannot get an end time for note type {0}", this.type));

		return float.Parse(this.extra[0], CultureInfo.InvariantCulture);
	}

	public float time;
	public Side side;
	public Height height;
	public NoteType type;
	public NoteSpeed speed;
	public bool hiding;
	public bool spawnMid;
	public string[] extra;
}

public class BeatmapInfo(BeatmapIndexSong song) : ISongChart
{
	[JsonPropertyName("textAsset")] public PPtr<TextAsset> TextAsset { get; set; } = null!;
	[JsonPropertyName("difficulty")] public string Difficulty { get; set; } = null!;

	public Beatmap Beatmap;

	MD1_GamemodeData? GamemodeData;

	public SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		return new() {
			ChartAuthors = "D-Cell Games",
			Difficulty = "1",
			DifficultyName = Difficulty,
			Color = new Nucleus.Common.Types.Color(115, 55, 55),
			GamemodeName = "Muse Dash 1",
			ReturnedLanguage = HumanLanguage.English,
		};
	}

	public IGamemodeDescriptor GetGamemode() {
		return GamemodeMod.GetGamemode("gamemode/musedash1/standard")!;
	}

	public object GetGamemodeData() {
		if (GamemodeData == null) {
			GamemodeData = new MD1_GamemodeData();

			if (!TextAsset.TryGet(out var textAsset))
				return null!;

			Beatmap = BeatmapDecoder.Decode(new MemoryStream(textAsset.m_Script));

			GamemodeData.InitialScene = "scene_01";

			foreach (var hitObjectInfo in Beatmap.HitObjects) {
				if (hitObjectInfo.IsFlip()) {
					continue;
				}
				else if (hitObjectInfo.IsAnyNote()) {
					NoteInfo noteInfo = new NoteInfo(hitObjectInfo, Side.Right);
					MuseDash1EntityType entityType = noteInfo.type switch {
						NoteType.Default => MuseDash1EntityType.Single,
						NoteType.Spam => MuseDash1EntityType.Masher,
						NoteType.Freestyle => MuseDash1EntityType.Masher,
						NoteType.Dodge => MuseDash1EntityType.Gear,
						NoteType.Double => MuseDash1EntityType.Double,
						NoteType.Hold => MuseDash1EntityType.SustainBeam,
						NoteType.Setpiece => MuseDash1EntityType.Raider,
						_ => 0
					};
					GamemodeData.Entities.Add(new() {
						Type = entityType,
						Damage = 30,
						EnterDirection = EntityEnterDirection.RightSide,
						Blood = false,
						Fever = 10,
						Flipped = false,
						HitTime = noteInfo.time,
						ShowTime = noteInfo.time - noteInfo.speed switch {
							NoteSpeed.Standard => 1,
							NoteSpeed.Fast => 0.5,
							_ => 1
						},
						Length = noteInfo.hasEndTime ? noteInfo.GetEndTime() - noteInfo.time : 0,
						Pathway = noteInfo.height switch { Height.Low => PathwaySide.Bottom, Height.Mid => PathwaySide.Bottom, Height.Top => PathwaySide.Top, _ => PathwaySide.Bottom },
						Speed = noteInfo.speed switch {
							NoteSpeed.Standard => 1,
							NoteSpeed.Fast => 3,
							_ => 1
						},
						Score = 100
					});
				}
				else if (hitObjectInfo.IsCommand()) {
					// todo
				}
			}
		}

		return GamemodeData;
	}

	public ISong GetSong() => song;

	public IAudioClip GetAudioTrack() => AudioClip;

	public IAudioClip AudioClip = null!;

	public int GetRatingNumber() => 5; // todo
}

public class BeatmapIndexSong : ISong
{
	[JsonPropertyName("name")] public string Name { get; set; } = null!;
	[JsonPropertyName("stageScene")] public string StageScene { get; set; } = null!;
	[JsonPropertyName("beatmaps")] public BeatmapInfo[] Beatmaps { get; set; } = null!;

	public IAudioClip? PreviewClip;

	public SongMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		return new() {
			Name = Name,
			Author = "D-Cell Games"
		};
	}

	public IReadOnlyList<ISongChart> GetCharts() {
		return Beatmaps;
	}

	public SongCoverInfo GetCoverTexture() {
		return new() {
			// todo
		};
	}

	public IAudioClip? GetDemoAudio() {
		return PreviewClip;
	}

	public ReadOnlySpan<char> GetUUID() {
		return $"song/unbeatable_whitelabel/{Name.ToLower()}";
	}

	public bool IsAsynchronouslyLoading() {
		return false;
	}

	public void WaitForAsynchronousLoad(OnAsynchronousLoadingCompleteFn callback) => throw new NotImplementedException();
}

public class BeatmapIndex
{
	[JsonPropertyName("m_Name")] public string Name { get; set; } = null!;
	[JsonPropertyName("songs")] public BeatmapIndexSong[] Songs { get; set; } = null!;
	public readonly Dictionary<string, BeatmapIndexSong> SongDict = [];
}

[MarkForStaticConstruction]
public static partial class UnbeatableWhiteLabelCompatibility
{
	public static BeatmapIndex BeatmapIndex;
	static AssetsManager Assets;
	static string InstallDir;
	static FmodSoundBank[] Bgm;
	static FmodSoundBank[] Master;
	static FmodSoundBank[] Sfx;

	static readonly Dictionary<string, IAudioClip> BgmClips = [];
	static readonly Dictionary<string, IAudioClip> MasterClips = [];
	static readonly Dictionary<string, IAudioClip> SfxClips = [];

	static AssemblyLoader Assemblies;
	[MemberNotNull(nameof(Assets))]
	[MemberNotNull(nameof(Assemblies))]
	public static UWLCompatLayerInitResult LightInitialize() {
#if COMPILED_WINDOWS
		return INIT_WINDOWS();
#else
			return MD1CompatLayerInitResult.OperatingSystemNotCompatible;
#endif
	}
	const uint UNBEATABLE_WHITELABEL_APPID = 1290490;

	class lookupEntry
	{
		public required SerializedFile SerializedFile;
		public required AssetStudio.Object Object;
	}
	static readonly Dictionary<long, lookupEntry> PathIDLUT = [];
	static readonly Dictionary<ulong, lookupEntry> NameLUT = [];

	static UnbeatableWhiteLabelCompatibility() {
		if (LightInitialize() != UWLCompatLayerInitResult.OK) {
			return; // unbeatable is optional
		}

		// Build the LUT
		foreach (var serializedFile in Assets!.AssetsFileList) {
			foreach (var obj in serializedFile.Objects) {
				lookupEntry entry = new() { SerializedFile = serializedFile, Object = obj };
				PathIDLUT[obj.m_PathID] = entry;
				NameLUT[obj.GetUnityName().Hash(false)] = entry;
			}
		}

		Bgm = LoadFsbMagically(System.IO.File.ReadAllBytes(System.IO.Path.Combine(InstallDir, "UNBEATABLE [white label]_Data", "StreamingAssets", "BGM.bank")));
		Master = LoadFsbMagically(System.IO.File.ReadAllBytes(System.IO.Path.Combine(InstallDir, "UNBEATABLE [white label]_Data", "StreamingAssets", "Master.bank")));
		Sfx = LoadFsbMagically(System.IO.File.ReadAllBytes(System.IO.Path.Combine(InstallDir, "UNBEATABLE [white label]_Data", "StreamingAssets", "SFX.bank")));
		BuildNucleusClips(Bgm, BgmClips);
		BuildNucleusClips(Master, MasterClips);
		BuildNucleusClips(Sfx, SfxClips);

		MonoBehaviour mb_beatmapIndex = GetObjectByName<MonoBehaviour>("BeatmapIndex")!;
		var data = mb_beatmapIndex.ToType(mb_beatmapIndex.ConvertToTypeTree(Assemblies));

		BeatmapIndex = new();
		BeatmapIndex.Name = (string)data["m_Name"]!;

		object[] songs = (object[])data["songs"]!;
		BeatmapIndex.Songs = new BeatmapIndexSong[songs.Length];
		int i = 0;
		foreach (var songObj in songs) {
			OrderedDictionary songDict = (OrderedDictionary)songObj;
			string songName = (string)songDict["name"]!;
			if (songName == null || !Songs.TryGetValue(songName, out var musicName))
				continue;

			BeatmapIndexSong song = BeatmapIndex.Songs[i] = new BeatmapIndexSong();
			song.Name = songName;
			BeatmapIndex.SongDict[song.Name] = song;
			song.StageScene = (string)songDict["stageScene"]!;

			object[] beatmaps = (object[])songDict["beatmaps"]!;
			song.Beatmaps = new BeatmapInfo[beatmaps.Length];

			BgmClips.TryGetValue(musicName.SongPreview, out song.PreviewClip);

			int j = 0;
			foreach (var beatmapObj in beatmaps) {
				OrderedDictionary beatmapDict = (OrderedDictionary)beatmapObj;
				BeatmapInfo beatmap = song.Beatmaps[j] = new BeatmapInfo(song);
				beatmap.Difficulty = (string)beatmapDict["difficulty"]!;

				OrderedDictionary textAssetPtrRaw = (OrderedDictionary)beatmapDict["textAsset"]!;
				beatmap.TextAsset = new PPtr<TextAsset>((int)textAssetPtrRaw["m_FileID"]!, (long)textAssetPtrRaw["m_PathID"]!, mb_beatmapIndex.assetsFile);
				j++;
				MasterClips.TryGetValue(musicName.SongAudio, out beatmap.AudioClip!);
			}
			i++;
		}
		BeatmapIndex.Songs = BeatmapIndex.Songs.Where(x => x != null).ToArray();
	}

	readonly struct SongMusicInfo(string preview, string audio)
	{
		public readonly string SongPreview = preview;
		public readonly string SongAudio = audio;
	}

	static readonly Dictionary<string, SongMusicInfo> Songs = new() {
		{ "EMPTY DIARY",        new("empty diary", "audio") },
		{ "PROPERRHYTHM",       new("proper rhythm", "audio_12") },
		{ "Mirror",             new("mirror", "audio") },
		{ "FOREVER NOW",        new("forever now", "audio") },
		{ "FOREVER WHEN",       new("forever now train melody", "audio") }, // ??????????????
		{ "Waiting",            new("waiting", "audio") }
	};

	private static FmodSoundBank[] LoadFsbMagically(byte[] bank) {
		ReadOnlySpan<byte> fsb5Magic = "FSB5"u8;
		ReadOnlySpan<byte> currentSpan = bank;

		List<FmodSoundBank> soundBanks = [];

		while (true) {
			int index = currentSpan.IndexOf(fsb5Magic);

			if (index == -1) {
				break;
			}

			ReadOnlySpan<byte> fsbSpan = currentSpan[index..];
			soundBanks.Add(FsbLoader.LoadFsbFromByteArray(fsbSpan.ToArray()));
			currentSpan = currentSpan[(index + fsb5Magic.Length)..];
		}

		return soundBanks.ToArray();
	}

	private static void BuildNucleusClips(FmodSoundBank[] banks, Dictionary<string, IAudioClip> clips) {
		for (int h = 0; h < banks.Length; h++) {
			FmodSoundBank bank = banks[h];
			HashSet<string> uniqueStrs = [];
			for (int i = 0; i < bank.Samples.Count; i++) {
				var sample = bank.Samples[i];
				int add = 1;
				string sampleName = string.IsNullOrWhiteSpace(sample.Name) ? $"sample_{i}" : sample.Name;
				if (uniqueStrs.Contains(sampleName)) {
					string pickAnother;
					while (uniqueStrs.Contains(pickAnother = $"{sampleName}_{add}"))
						add++;
					sampleName = pickAnother;
				}
				uniqueStrs.Add(sampleName);

				// 3. Rebuild the data into standard format (.wav, .ogg, etc.)
				if (sample.RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension)) {
					using var into = new MemoryStream(dataBytes!);
					clips[sampleName] = audiosystem.CreateStreamAudioClip(into, $"UWLAsset:{sampleName}")!;
				}
			}
		}
	}

	public static T? GetObjectByName<T>(ReadOnlySpan<char> name) where T : AssetStudio.Object {
		if (!NameLUT.TryGetValue(name.Hash(false), out lookupEntry? entry))
			return null;
		return entry.Object is T t ? t : null;
	}

	public static T? GetObjectByPathID<T>(long pathID) where T : AssetStudio.Object {
		if (!PathIDLUT.TryGetValue(pathID, out lookupEntry? entry))
			return null;
		return entry.Object is T t ? t : null;
	}
}

public class UnbeatableWhiteLabelChartSource : BaseContiguousSongSource
{
	public UnbeatableWhiteLabelChartSource(BaseContiguousChartSongFilter? filter = null, ISongSourceState? parent = null) : base(UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs, filter, parent) {

	}

	public override ISongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not BaseContiguousChartSongFilter contigFilter)
			throw new InvalidCastException("Invalid contigFilter");
		return new UnbeatableWhiteLabelChartSource(contigFilter, this.GetRootSource());
	}
}

[MarkForStaticConstruction]
public class UnbeatableWhiteLabelChartProvider : IChartSongProvider
{
	public static readonly ConVar ubwl_lastsong = new ConVar(nameof(ubwl_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar ubwl_lastfilter = new ConVar(nameof(ubwl_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public IChartSongFilter? SavedFilter() => ubwl_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(ubwl_lastfilter.GetString()));
	public ISong? SavedSong() => ubwl_lastsong.GetString().IsEmpty ? null : UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs.FirstOrDefault(x => x.GetUUID().Equals(ubwl_lastsong.GetString(), StringComparison.InvariantCultureIgnoreCase));
	public void UpdateSavedFilter(IChartSongFilter? filter) => ubwl_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => ubwl_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUUID());

	public ISong? FindByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs) {
			if (name.Equals(song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<string> GetAvailable() {
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs)
			yield return song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name;
	}

	public ReadOnlySpan<char> GetName() => "Custom Albums";
	public ISongSourceState NewState() => new UnbeatableWhiteLabelChartSource();

	public int GetSortIndex() => 40000;
	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Play UNBEATABLE: White Label charts",
		Description = "Play a chart from UNBEATABLE: White Label",
		Hue = 348,
		Icon = "icons/play.png"
	};
}
