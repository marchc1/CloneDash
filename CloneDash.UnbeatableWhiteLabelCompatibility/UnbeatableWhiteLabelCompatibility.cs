using AssetStudio;
using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Compatibility;
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
using System.Buffers.Binary;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

[MarkForStaticConstruction]
public static partial class UnbeatableWhiteLabelCompatibility
{
	static BeatmapIndex beatmapIndex;
	public static BeatmapIndex BeatmapIndex {
		get {
			WaitForInitialization();
			return beatmapIndex;
		}
	}
	static AssetsManager Assets;
	static string InstallDir;

	static readonly Dictionary<string, IAudioClip> BgmClips = [];
	static readonly Dictionary<string, IAudioClip> MasterClips = [];
	static readonly Dictionary<string, IAudioClip> SfxClips = [];
	static bool BgmLoaded, MasterLoaded, SfxLoaded;

	static AssemblyLoader Assemblies;
	[MemberNotNull(nameof(Assets))]
	[MemberNotNull(nameof(Assemblies))]
	public static AppStatus LightInitialize() {
#if COMPILED_WINDOWS
		return INIT_WINDOWS();
#else
			return AppStatus.OperatingSystemNotCompatible;
#endif
	}
	const uint UNBEATABLE_WHITELABEL_APPID = 1290490;

	class lookupEntry
	{
		public required SerializedFile SerializedFile;
		public required AssetStudio.Object Object;
	}
	static readonly Dictionary<long, lookupEntry> PathIDLUT = [];
	static readonly HashSet<SerializedFile> ReadAssetsFiles = [];

	static Task<AppStatus>? initializeTask;

	public static void BeginInitializeCompatibilityLayer() => initializeTask ??= Task.Run(InitializeCompatibilityLayer);

	static void WaitForInitialization() => initializeTask?.GetAwaiter().GetResult();

	public static AppStatus InitializeCompatibilityLayer() {
		AppStatus result;
		if ((result = LightInitialize()) != AppStatus.OK) {
			return result; // unbeatable is optional
		}

		MonoBehaviour mb_beatmapIndex = FindResource<MonoBehaviour>("beatmapindex")!;
		var data = mb_beatmapIndex.ToType(mb_beatmapIndex.ConvertToTypeTree(Assemblies));

		beatmapIndex = new();
		beatmapIndex.Name = (string)data["m_Name"]!;

		object[] songs = (object[])data["songs"]!;
		beatmapIndex.Songs = new BeatmapIndexSong[songs.Length];
		int i = 0;
		foreach (var songObj in songs) {
			OrderedDictionary songDict = (OrderedDictionary)songObj;
			string songName = (string)songDict["name"]!;
			if (songName == null || !Songs.TryGetValue(songName, out var musicName))
				continue;

			BeatmapIndexSong song = beatmapIndex.Songs[i] = new BeatmapIndexSong();
			song.Name = songName;
			beatmapIndex.SongDict[song.Name] = song;
			song.StageScene = (string)songDict["stageScene"]!;

			object[] beatmaps = (object[])songDict["beatmaps"]!;
			song.Beatmaps = new BeatmapInfo[beatmaps.Length];

			song.PreviewClipName = musicName.SongPreview;

			int j = 0;
			foreach (var beatmapObj in beatmaps) {
				OrderedDictionary beatmapDict = (OrderedDictionary)beatmapObj;
				BeatmapInfo beatmap = song.Beatmaps[j] = new BeatmapInfo(song);
				beatmap.Difficulty = (string)beatmapDict["difficulty"]!;

				OrderedDictionary textAssetPtrRaw = (OrderedDictionary)beatmapDict["textAsset"]!;
				beatmap.TextAsset = new PPtr<TextAsset>((int)textAssetPtrRaw["m_FileID"]!, (long)textAssetPtrRaw["m_PathID"]!, mb_beatmapIndex.assetsFile);
				j++;
				beatmap.AudioClipName = musicName.SongAudio;
			}
			i++;
		}
		beatmapIndex.Songs = beatmapIndex.Songs.Where(x => x != null).ToArray();
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
			if (fsbSpan.Length >= 0x18) {
				uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(fsbSpan[0x04..]) == 0 ? 0x40u : 0x3Cu;
				long fsbSize = headerSize + (long)BinaryPrimitives.ReadUInt32LittleEndian(fsbSpan[0x0C..]) + BinaryPrimitives.ReadUInt32LittleEndian(fsbSpan[0x10..]) + BinaryPrimitives.ReadUInt32LittleEndian(fsbSpan[0x14..]);
				if (fsbSize < fsbSpan.Length)
					fsbSpan = fsbSpan[..(int)fsbSize];
			}
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

	static SerializedFile? FindAssetsFile(string fileName) {
		foreach (var file in Assets.AssetsFileList)
			if (file.fileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
				return file;
		return null;
	}

	static SerializedFile? ReadAssetsFile(string fileName) {
		lock (ReadAssetsFiles) {
			SerializedFile? root = FindAssetsFile(fileName);
			if (root == null || ReadAssetsFiles.Contains(root))
				return root;

			List<SerializedFile> toRead = [];
			Stack<SerializedFile> pending = [];
			pending.Push(root);
			while (pending.TryPop(out var file)) {
				if (!ReadAssetsFiles.Add(file))
					continue;

				toRead.Add(file);
				foreach (var external in file.m_Externals)
					if (FindAssetsFile(external.fileName) is SerializedFile dependency)
						pending.Push(dependency);
			}

			Assets.ReadFiles(toRead);
			foreach (var file in toRead)
				foreach (var obj in file.Objects)
					PathIDLUT[obj.m_PathID] = new() { SerializedFile = file, Object = obj };

			return root;
		}
	}

	static T? FindResource<T>(string resourcePath) where T : AssetStudio.Object {
		SerializedFile? globalGameManagers = FindAssetsFile("globalgamemanagers");
		if (globalGameManagers == null)
			return null;

		foreach (var objectInfo in globalGameManagers.m_Objects) {
			if (objectInfo.classID != (int)ClassIDType.ResourceManager)
				continue;

			ResourceManager resourceManager = new(new ObjectReader(globalGameManagers.reader, globalGameManagers, objectInfo));
			foreach (var (path, pptr) in resourceManager.m_Container) {
				if (!path.Equals(resourcePath, StringComparison.OrdinalIgnoreCase))
					continue;

				ReadAssetsFile(pptr.m_FileID == 0 ? globalGameManagers.fileName : globalGameManagers.m_Externals[pptr.m_FileID - 1].fileName);
				if (pptr.TryGet(out T? resource))
					return resource;
			}
		}

		return null;
	}

	public static T? GetObjectByName<T>(string fileName, ReadOnlySpan<char> name) where T : AssetStudio.Object {
		WaitForInitialization();
		SerializedFile? file = ReadAssetsFile(fileName);
		if (file == null)
			return null;

		foreach (var obj in file.Objects)
			if (obj is T t && name.SequenceEqual(obj.GetUnityName()))
				return t;
		return null;
	}

	static Dictionary<string, IAudioClip> LoadClips(string bankFile, Dictionary<string, IAudioClip> clips, ref bool loaded) {
		WaitForInitialization();
		if (!loaded && InstallDir != null) {
			loaded = true;
			BuildNucleusClips(LoadFsbMagically(System.IO.File.ReadAllBytes(System.IO.Path.Combine(InstallDir, "UNBEATABLE [white label]_Data", "StreamingAssets", bankFile))), clips);
		}
		return clips;
	}

	public static IAudioClip? GetBgmClip(string name) => LoadClips("BGM.bank", BgmClips, ref BgmLoaded).TryGetValue(name, out var clip) ? clip : null;

	public static IAudioClip? GetMasterClip(string name) => LoadClips("Master.bank", MasterClips, ref MasterLoaded).TryGetValue(name, out var clip) ? clip : null;

	public static IAudioClip? GetSfxClip(string name) => LoadClips("SFX.bank", SfxClips, ref SfxLoaded).TryGetValue(name, out var clip) ? clip : null;

	public static T? GetObjectByPathID<T>(long pathID) where T : AssetStudio.Object {
		WaitForInitialization();
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
public class UnbeatableWhiteLabelChartProvider() : ModProvider<ISong>("UNBEATABLE: White Label", "unbeatable_whitelabel"), IChartSongProvider
{
	public static readonly ConVar ubwl_lastsong = new ConVar(nameof(ubwl_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar ubwl_lastfilter = new ConVar(nameof(ubwl_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public IChartSongFilter? GetSavedFilter() => ubwl_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(ubwl_lastfilter.GetString()));
	public ISong? GetSavedSong() => ChartMod.Instance.GetDescriptorByUID(ubwl_lastsong.GetString());
	public void UpdateSavedFilter(IChartSongFilter? filter) => ubwl_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => ubwl_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUUID());

	public ISong? FindSongByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs) {
			if (name.Equals(song.FetchMetadata().Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs)
			yield return song.GetID();
	}

	public override ISong? FindByID(ReadOnlySpan<char> id) {
		foreach (var song in UnbeatableWhiteLabelCompatibility.BeatmapIndex.Songs)
			if (id.Equals(song.GetID(), StringComparison.Ordinal))
				return song;
		return null;
	}

	public ISongSourceState NewState() => new UnbeatableWhiteLabelChartSource();

	public override int GetSortIndex() => 40000;
	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Play UNBEATABLE: White Label charts",
		Description = "Play a chart from UNBEATABLE: White Label",
		Hue = 348,
		Icon = "icons/play.png"
	};
	public override bool IsEnabled() => UnbeatableWhiteLabelCompatibility.unbeatablewhitelabel_enabled.GetBool();
}
