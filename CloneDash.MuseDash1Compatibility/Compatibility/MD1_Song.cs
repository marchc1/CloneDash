using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.Unity;
using CloneDash.Settings;
using CommunityToolkit.HighPerformance;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Types;
using OdinSerializer;
using Raylib_cs;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using static CloneDash.Compatibility.Unity.UnityAssetUtils;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using Texture2D = AssetStudio.Texture2D;

namespace CloneDash.Compatibility.MuseDash;

public class MuseDashSongInfoJSON
{
	[JsonPropertyName("uid")] public string UID { get; set; } = "";
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("author")] public string Author { get; set; } = "";
	[JsonPropertyName("bpm")] public string BPM { get; set; } = "";
	[JsonPropertyName("music")] public string Music { get; set; } = "";
	[JsonPropertyName("demo")] public string Demo { get; set; } = "";
	[JsonPropertyName("cover")] public string Cover { get; set; } = "";
	[JsonPropertyName("noteJson")] public string NoteJSON { get; set; } = "";
	[JsonPropertyName("scene")] public string Scene { get; set; } = "";
	[JsonPropertyName("levelDesigner")] public string LevelDesigner { get; set; } = "";

	[JsonPropertyName("difficulty1")] public string Difficulty1 { get; set; } = "";
	[JsonPropertyName("difficulty2")] public string Difficulty2 { get; set; } = "";
	[JsonPropertyName("difficulty3")] public string Difficulty3 { get; set; } = "";
	[JsonPropertyName("difficulty4")] public string Difficulty4 { get; set; } = "";
	[JsonPropertyName("difficulty5")] public string Difficulty5 { get; set; } = "";

	public void CloneInto(MuseDashSongInfoJSON into) {
		into.UID = UID;
		into.Name = Name;
		into.Author = Author;
		into.BPM = BPM;
		into.Music = Music;
		into.Demo = Demo;
		into.Cover = Cover;
		into.NoteJSON = NoteJSON;
		into.Scene = Scene;
		into.LevelDesigner = LevelDesigner;
		into.Difficulty1 = Difficulty1;
		into.Difficulty2 = Difficulty2;
		into.Difficulty3 = Difficulty3;
		into.Difficulty4 = Difficulty4;
		into.Difficulty5 = Difficulty5;
	}
}

public delegate void ChartCoverAvailableToMainThreadFn(MD1_SongCover? cover);

public class MD1_Song : ISong, IHasLowToHighDifficulties
{
	private bool __gotDemoTrack = false;
	private bool __gotCover = false;

	public MD1_SongInfo? Info;


	protected IAudioClip? AudioTrack;
	protected IAudioClip? DemoTrack;
	protected MD1_SongCover? CoverTexture;
	private readonly List<MD1_SongChart> Sheets = [];
	private readonly List<int> Difficulties = [];

	protected readonly object AsyncLock = new object();
	protected bool DeferringDemoToAsyncHandler;

	readonly ConcurrentDictionary<object, ChartCoverAvailableToMainThreadFn?> chartCoverCallbacks = [];

	public string GetDifficultyString1() => GetInfo()?.Difficulty1 ?? "";
	public string GetDifficultyString2() => GetInfo()?.Difficulty2 ?? "";
	public string GetDifficultyString3() => GetInfo()?.Difficulty3 ?? "";
	public string GetDifficultyString4() => GetInfo()?.Difficulty4 ?? "";
	public string GetDifficultyString5() => GetInfo()?.Difficulty5 ?? "";

	public bool TryGetDifficultyInteger(int i, out int d) => int.TryParse(GetDifficultyString(i), out d);
	public string GetDifficultyString(int i) => i switch {
		1 => GetDifficultyString1(),
		2 => GetDifficultyString2(),
		3 => GetDifficultyString3(),
		4 => GetDifficultyString4(),
		5 => GetDifficultyString5(),
		_ => ""
	};

	protected void Clear() {
		audiosystem.DestroyAudioClip(AudioTrack);
		AudioTrack = null;
		// Not clearing the demo since i dont want a sudden jump
		// DemoTrack?.Dispose(); DemoTrack = null;
		Info = null;
		__gotCover = false;
		CoverTexture = null;
		chartCoverCallbacks.Clear();
		Sheets.Clear();
	}

	public bool IsLoadingDemoAsync {
		get {
			lock (AsyncLock) {
				return DeferringDemoToAsyncHandler && DemoTrack == null;
			}
		}
	}

	// Public facing methods for getting data
	public IAudioClip GetAudioTrack() {
		if (AudioTrack != null && IValidatable.IsValid(AudioTrack))
			return AudioTrack;

		AudioTrack = ProduceAudioTrack();
		AudioTrack.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		return AudioTrack;
	}

	public MD1_SongInfo? GetInfo() {
		if (Info != null)
			return Info;

		Info = ProduceInfo();
		return Info;
	}

	public IAudioClip? GetDemoTrack() {
		if (DeferringDemoToAsyncHandler) {
			lock (AsyncLock) {
				return DemoTrack;
			}
		}
		if (__gotDemoTrack == false && DemoTrack != null && IValidatable.IsValid(AudioTrack))
			return DemoTrack;

		DemoTrack = ProduceDemoTrack();
		DemoTrack?.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		__gotDemoTrack = true;
		return DemoTrack;
	}

	public MD1_SongCover? GetCoverWhenAvailable(object consumer) {
		MD1_SongCover? cover = null;
		GetCoverWhenAvailable(consumer, (c) => cover = c);
		return cover;
	}
	public void GetCoverWhenAvailable(object consumer, ChartCoverAvailableToMainThreadFn? fn) {
		if (CoverTexture != null) {
			if (fn != null)
				fn(CoverTexture);
			return;
		}

		if (chartCoverCallbacks.ContainsKey(consumer))
			return;

		int count = chartCoverCallbacks.Count;
		chartCoverCallbacks[consumer] = fn;
		if (count == 0)
			Task.Run(StartRetrievingCover);
	}

	private void StartRetrievingCover() {
		ProduceCover((cover) => {
			CoverTexture = cover;
			foreach (var callback in chartCoverCallbacks)
				if (callback.Value != null)
					callback.Value(cover);
			chartCoverCallbacks.Clear();
		});
	}

	public virtual bool ShouldReproduceSheet(int difficulty) => false;

	public List<MD1_SongChart> LoadSheets() {
		if (Sheets.Count != 0) return Sheets;

		for (int i = 0; i < 5; i++) {
			var difficulty = i + 1;
			var sheet = ProduceSheet(difficulty);
			if (sheet != null) {
				Sheets.Add(sheet);
				Difficulties.Add(difficulty);
			}
		}

		return Sheets;
	}

	public MD1_SongChart? GetSheet(int difficulty) {
		LoadSheets();

		for (int i = 0; i < Difficulties.Count; i++) {
			if (Difficulties[i] == difficulty)
				return Sheets[i];
		}

		return null;
	}

	public SongMetadata FetchMetadata() => FetchMetadata(HumanLanguage.GetCurrentLanguage()); 
	public SongMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		if (__jsonInfoLanguages.TryGetValue(desiredLanguage, out MuseDashSongInfoJSON? languageInfo))
			return new() {
				Name = languageInfo.Name ?? __jsonInfo.Name,
				Author = languageInfo.Author ?? __jsonInfo.Author
			};
		else
			return new() {
				Name = __jsonInfo.Name,
				Author = __jsonInfo.Author
			};
	}

	public IReadOnlyList<ISongChart> GetCharts() => LoadSheets();
	public bool IsAsynchronouslyLoading() => DeferringDemoToAsyncHandler;
	public void WaitForAsynchronousLoad(OnAsynchronousLoadingCompleteFn callback) => throw new NotImplementedException();

	public IAudioClip? GetDemoAudio() {
		return GetDemoTrack();
	}
	public SongCoverInfo GetCoverTexture() {
		GetCoverWhenAvailable(this, null);
		return CoverTexture == null ? default : new() {
			Texture = CoverTexture.Texture,
			Flipped = CoverTexture.Flipped
		};
	}

	public ReadOnlySpan<char> GetUUID() => $"song/musedash1/{Info?.Music}";

	~MD1_Song() {
		MainThread.RunASAP(() => {
			if (__gotCover && CoverTexture != null)
				Raylib.UnloadTexture(CoverTexture.Texture);

			if (AudioTrack != null) audiosystem.DestroyAudioClip(AudioTrack);
			if (DemoTrack != null) audiosystem.DestroyAudioClip(DemoTrack);
		});
	}

	private readonly MuseDashSongInfoJSON __jsonInfo = new();
	private readonly Dictionary<HumanLanguage, MuseDashSongInfoJSON> __jsonInfoLanguages = [];

	public void AddBaseJSONInfo(MuseDashSongInfoJSON baseInfo) {
		baseInfo.CloneInto(__jsonInfo);
	}

	public void AddLocalizedJSONInfo(HumanLanguage lang, string? name, string? author) {
		__jsonInfoLanguages[lang] = new() {
			Name = name!,
			Author = author!
		};
	}

	public static string? GetFixedFilename(string givenBase, string fileName, [NotNullWhen(true)] bool throwExp = true) {
		return
			MuseDash1Compatibility.StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase)))
			?? MuseDash1Compatibility.StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase.Replace("_music", ""))))
			?? (throwExp ? throw new Exception($"Tried to find {givenBase}, could not find a match even with fixes applied") : null);
	}
	public string GetAssetsFilepath() => GetFixedFilename(BaseName, "music_{name}_assets_all.bundle", true) ?? throw new Exception();
	public string? GetDemoFilepath() => GetFixedFilename(BaseName, "song_{name}_assets_all", false);

	public MuseDash1Album Album { get; set; }

	[JsonIgnore]
	public string BaseName => GetInfo()!.Music.Substring(0, GetInfo()!.Music.Length - 6);
	public override string ToString() {
		SongMetadata metadata = FetchMetadata();
		return $"{metadata.Name} by {metadata.Author}";
	}


	public AssetsManager AssetsFile { get; private set; } = null;
	public AssetsManager DemoFile { get; private set; } = null;

	public bool Unmanaged { get; set; } = false;

	private void LoadAssetFile() {
		if (Unmanaged) return;
		if (AssetsFile == null) {
			AssetsFile = new();
			string filepath = GetAssetsFilepath();
			AssetsFile.LoadFiles(filepath);
		}
		if (DemoFile == null) {
			string? filepath = GetDemoFilepath();
			if (filepath != null) {
				DemoFile = new();
				DemoFile.LoadFiles(filepath);
			}
			else Logs.Warn($"CloneDash: MuseDashSong.LoadAssetFile could not generate a demo filepath for {__jsonInfo.Name}.");
		}
	}

	public IAudioClip? MusicTrackOverride { get; set; }
	public Dictionary<int, MD1_SongChart> DashSheetOverrides { get; set; } = [];

	protected virtual IAudioClip ProduceAudioTrack() {
		if (IValidatable.IsValid(AudioTrack))
			return AudioTrack;

		AudioClip audioclip = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AudioClip>(__jsonInfo.Music)!;
		return MuseDash1Compatibility.GetMusic(EngineCore.Level, audioclip)!;
	}

	protected virtual IAudioClip? ProduceDemoTrack() {
		if (IValidatable.IsValid(DemoTrack))
			return DemoTrack;

		AudioClip? audioclip = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AudioClip>(__jsonInfo.Demo);
		if (audioclip == null) return null;
		return MuseDash1Compatibility.GetMusic(EngineCore.Level, audioclip);
	}

	protected virtual void ProduceCover(ChartCoverAvailableToMainThreadFn callback) {
		if (IValidatable.IsValid(CoverTexture)) {
			callback(CoverTexture);
			return;
		}

		// var start = new Stopwatch();
		// start.Start();
		Texture2D? tex2D = MuseDash1Compatibility.StreamingAssets.FindAssetByName<Texture2D>(__jsonInfo.Cover);
		if (tex2D == null) {
			callback(null);
			return;
		}

		var img = tex2D.ToRaylib();
		// start.Stop();
		// Logs.Debug($"Took {start.Elapsed.TotalMilliseconds} milliseconds to load cover of size {img.Width}x{img.Height} on task thread");

		MainThread.RunASAP(() => {
			// var start = new Stopwatch();
			// start.Start();

			var tex = Raylib.LoadTextureFromImage(img);
			Raylib.GenTextureMipmaps(ref tex);
			Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
			Raylib.UnloadImage(img);
			CoverTexture = new() {
				Texture = new(EngineCore.Level.Textures, tex, true),
				Flipped = true
			};
			// start.Stop();
			// Logs.Debug($"Took {start.Elapsed.TotalMilliseconds} milliseconds to load cover of size {img.Width}x{img.Height} on main thread");

			callback(CoverTexture);
		});
	}

	protected virtual MD1_SongChart? ProduceSheet(int mapID) {
		if (DashSheetOverrides.TryGetValue(mapID, out MD1_SongChart? sheet))
			return sheet;

		LoadAssetFile();
		Interlude.Spin();

		MD1_SongChart chart = new MD1_SongChart(this, mapID);
		return chart;
	}

	/// <summary>
	/// Called from charts only!!!
	/// </summary>
	public virtual MD1_GamemodeData? ProduceGamemodeData(MD1_SongChart chart, int mapID) {
		//MonoBehaviour map = (MonoBehaviour)AssetsFile.assetsFileList[0].Objects.First(x => x is MonoBehaviour mB && mB.m_Name.EndsWith($"_map{mapID}"));
		MonoBehaviour? map = MuseDash1Compatibility.StreamingAssets.LoadAsset<MonoBehaviour>($"Assets/Static Resources/Data/Configs/StageInfos/{__jsonInfo.NoteJSON}{mapID}.asset").GetResult();
		if (map == null)
			return null;

		var obj = map.ToType();
		var rawData = JsonConvert.SerializeObject(obj, Formatting.Indented); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		var rr = MuseDash1Compatibility.InitializeCompatibilityLayer(); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		if (rr != MD1CompatLayerInitResult.OK)
			throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

		StageInfo? stage = JsonConvert.DeserializeObject<StageInfo>(rawData);
		Interlude.Spin(submessage: "Reading Muse Dash chart...");
		if (stage == null) {
			Logs.Warn("Failed to parse JSON into a StageInfo!");
			return null;
		}

		stage.musicDatas = OdinSerializer.SerializationUtility.DeserializeValue<List<MusicData>>(stage.serializationData.SerializedBytes, DataFormat.Binary); Interlude.Spin(submessage: "Reading Muse Dash chart...");
		MuseDash1Compatibility.FillInTheBlankNotes(this, stage); Interlude.Spin(submessage: "Reading Muse Dash chart...");
		return MuseDash1Compatibility.ConvertStageInfoToMD1GamemodeData(this, stage);
	}

	protected virtual MD1_SongInfo? ProduceInfo() {
		List<string> SearchTags = [];

		SearchTags.AddRange(__jsonInfo.Name.Split(' '));
		MD1_SongInfo info = new MD1_SongInfo() {
			BPM = __jsonInfo.BPM,
			Music = __jsonInfo.Music,
			LevelDesigners = [__jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner, __jsonInfo.LevelDesigner],
			Difficulty1 = __jsonInfo.Difficulty1,
			Difficulty2 = __jsonInfo.Difficulty2,
			Difficulty3 = __jsonInfo.Difficulty3,
			Difficulty4 = __jsonInfo.Difficulty4,
			Difficulty5 = __jsonInfo.Difficulty5,
			Scene = __jsonInfo.Scene,
			SearchTags = SearchTags.ToArray()
		};

		return info;
	}

	int IHasLowToHighDifficulties.GetLowestDifficulty() => throw new NotImplementedException();
	int IHasLowToHighDifficulties.GetHighestDifficulty() => throw new NotImplementedException();
	int IHasLowToHighDifficulties.GetDifficultyCount() => Difficulties.Count;
	bool IHasLowToHighDifficulties.GetDifficulties(Span<int> difficulties) {
		return Difficulties.AsSpan().TryCopyTo(difficulties);
	}

	public IEnumerable<MuseDashSongInfoJSON> GetAvailableInfo() {
		if (__jsonInfo != null)
			yield return __jsonInfo;
		foreach (var info in __jsonInfoLanguages)
			yield return info.Value;
	}
}