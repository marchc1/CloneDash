using AssetStudio;
using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Compatibility.Valve;
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
using OdinSerializer;
using SixLabors.ImageSharp.Drawing;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

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
	public static AppStatus LightInitialize() {
#if COMPILED_WINDOWS
		return INIT_WINDOWS();
#else
			return UWLCompatLayerInitResult.OperatingSystemNotCompatible;
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

	public static AppStatus InitializeCompatibilityLayer() {
		AppStatus result;
		if ((result = LightInitialize()) != AppStatus.OK) {
			return result; // unbeatable is optional
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
		return AppStatus.OK;
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

	public static readonly ConVar unbeatablewhitelabel_enabled = new(nameof(unbeatablewhitelabel_enabled), "0", FCvar.Saved, "Enables/disables UNBEATABLE [white label] compatibility. Requires a restart to take effect.");
	public static bool IsEnabled() => unbeatablewhitelabel_enabled.GetBool();
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
	public IChartSongFilter? GetSavedFilter() => ubwl_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(ubwl_lastfilter.GetString()));
	public ISong? GetSavedSong() => ubwl_lastsong.GetString().IsEmpty ? null : UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs.FirstOrDefault(x => x.GetUUID().Equals(ubwl_lastsong.GetString(), StringComparison.InvariantCultureIgnoreCase));
	public void UpdateSavedFilter(IChartSongFilter? filter) => ubwl_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => ubwl_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUUID());

	public ISong? FindSongByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs) {
			if (name.Equals(song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<ISong> GetAvailableSongs() {
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs)
			yield return song;
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
	public bool IsEnabled() => UnbeatableWhiteLabelCompatibility.unbeatablewhitelabel_enabled.GetBool();
}
