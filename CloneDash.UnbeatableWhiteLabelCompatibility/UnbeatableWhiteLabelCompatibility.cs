using AssetStudio;
using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Core;
using Nucleus.Util;
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;
using SixLabors.ImageSharp.Drawing;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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

public class BeatmapIndexBeatmap(BeatmapIndexSong song) : ISongChart
{
	[JsonPropertyName("textAsset")] public PPtr<TextAsset> TextAsset { get; set; } = null!;
	[JsonPropertyName("difficulty")] public string Difficulty { get; set; } = null!;

	MD1_GamemodeData? GamemodeData;

	public SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		return new() {
			ChartAuthors = "D-Cell Games",
			Difficulty = "",
			DifficultyName = Difficulty,
			Color = new Nucleus.Common.Types.Color(255, 255, 255),
			GamemodeName = "Muse Dash 1",
			ReturnedLanguage = HumanLanguage.English,
		};
	}

	public IGamemodeDescriptor GetGamemode() {
		return GamemodeMod.GetGamemode("gamemode/musedash1/standard")!;
	}

	public object GetGamemodeData() {
		if (GamemodeData == null) {
			// Produce the gamemode data
			if (!TextAsset.TryGet(out var textAsset))
				return null!;

			Beatmap beatmap = BeatmapDecoder.Decode(new MemoryStream(textAsset.GetRawData()));
			GamemodeData = new MD1_GamemodeData();
			// todo

		}

		return GamemodeData;
	}

	public ISong GetSong() => song;
}

public class BeatmapIndexSong : ISong
{
	[JsonPropertyName("name")] public string Name { get; set; } = null!;
	[JsonPropertyName("stageScene")] public string StageScene { get; set; } = null!;
	[JsonPropertyName("beatmaps")] public BeatmapIndexBeatmap[] Beatmaps { get; set; } = null!;

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
		return null;
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
}

[MarkForStaticConstruction]
public static partial class UnbeatableWhiteLabelCompatibility
{
	public static BeatmapIndex BeatmapIndex;
	static AssetsManager Assets;
	static string InstallDir;
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

		MonoBehaviour mb_beatmapIndex = GetObjectByName<MonoBehaviour>("BeatmapIndex")!;
		var data = mb_beatmapIndex.ToType(mb_beatmapIndex.ConvertToTypeTree(Assemblies));

		BeatmapIndex = new();
		BeatmapIndex.Name = (string)data["m_Name"]!;

		object[] songs = (object[])data["songs"]!;
		BeatmapIndex.Songs = new BeatmapIndexSong[songs.Length];
		int i = 0;
		foreach (var songObj in songs) {
			OrderedDictionary songDict = (OrderedDictionary)songObj;
			BeatmapIndexSong song = BeatmapIndex.Songs[i] = new BeatmapIndexSong();
			song.Name = (string)songDict["name"]!;
			song.StageScene = (string)songDict["stageScene"]!;

			object[] beatmaps = (object[])songDict["beatmaps"]!;
			song.Beatmaps = new BeatmapIndexBeatmap[beatmaps.Length];
			int j = 0;
			foreach (var beatmapObj in beatmaps) {
				OrderedDictionary beatmapDict = (OrderedDictionary)beatmapObj;
				BeatmapIndexBeatmap beatmap = song.Beatmaps[j] = new BeatmapIndexBeatmap(song);
				beatmap.Difficulty = (string)beatmapDict["difficulty"]!;

				OrderedDictionary textAssetPtrRaw = (OrderedDictionary)beatmapDict["textAsset"]!;
				beatmap.TextAsset = new PPtr<TextAsset>((int)textAssetPtrRaw["m_FileID"]!, (long)textAssetPtrRaw["m_PathID"]!, mb_beatmapIndex.assetsFile);
				j++;
			}
			i++;
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
