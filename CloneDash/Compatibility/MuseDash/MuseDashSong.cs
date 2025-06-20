using AssetStudio;

using CloneDash.Compatibility.Unity;
using CloneDash.Data;

using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;

using Newtonsoft.Json;

using Nucleus;
using Nucleus.Audio;

using OdinSerializer;

using Raylib_cs;

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
}
public class MuseDashSong : ChartSong
{
	private MuseDashSongInfoJSON __jsonInfo;
	public MuseDashSong(MuseDashSongInfoJSON info) {
		__jsonInfo = info;
		// Debug.Assert(info.Difficulty5 == "");
	}
	public static string? GetFixedFilename(string givenBase, string fileName, [NotNullWhen(true)] bool throwExp = true) {
		return
			MuseDashCompatibility.StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase)))
			?? MuseDashCompatibility.StreamingFiles.FirstOrDefault(x => x.Contains(fileName.Replace("{name}", givenBase.Replace("_music", ""))))
			?? (throwExp ? throw new Exception($"Tried to find {givenBase}, could not find a match even with fixes applied") : null);
	}
	public string GetAssetsFilepath() => GetFixedFilename(BaseName, "music_{name}_assets_all.bundle", true) ?? throw new Exception();
	public string? GetDemoFilepath() => GetFixedFilename(BaseName, "song_{name}_assets_all", false);

	public MuseDashAlbum Album { get; set; }


	public StageDemo? DemoObject { get; internal set; }

	[JsonIgnore]
	public string BaseName => GetInfo().Music.Substring(0, GetInfo().Music.Length - 6);
	public override string ToString() => $"{Name} by {Author}";


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
			else Logs.Warn($"CloneDash: MuseDashSong.LoadAssetFile could not generate a demo filepath for {Name}.");
		}
	}

	public MusicTrack? MusicTrackOverride { get; set; }

	public Dictionary<int, ChartSheet> DashSheetOverrides { get; set; } = [];

	protected override MusicTrack ProduceAudioTrack() {
		if (IValidatable.IsValid(AudioTrack))
			return AudioTrack;

		AudioClip audioclip = MuseDashCompatibility.StreamingAssets.FindAssetByName<AudioClip>(__jsonInfo.Music)!;
		return MuseDashCompatibility.GetMusic(EngineCore.Level, audioclip);
	}

	protected override MusicTrack? ProduceDemoTrack() {
		if (IValidatable.IsValid(DemoTrack))
			return DemoTrack;

		AudioClip? audioclip = MuseDashCompatibility.StreamingAssets.FindAssetByName<AudioClip>(__jsonInfo.Demo);
		if (audioclip == null) return null;
		return MuseDashCompatibility.GetMusic(EngineCore.Level, audioclip);
	}

	protected override ChartCover? ProduceCover() {
		if (CoverTexture != null)
			return CoverTexture;

		Texture2D? tex2D = MuseDashCompatibility.StreamingAssets.FindAssetByName<Texture2D>(__jsonInfo.Cover);
		if (tex2D == null) return null;

		var img = tex2D.ToRaylib();

		var tex = Raylib.LoadTextureFromImage(img);
		Raylib.GenTextureMipmaps(ref tex);
		Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_TRILINEAR);
		Raylib.UnloadImage(img);
		CoverTexture = new() {
			Texture = new(EngineCore.Level.Textures, tex, true),
			Flipped = true
		};

		return CoverTexture;
	}

	protected override ChartSheet ProduceSheet(int mapID) {
		if (DashSheetOverrides.TryGetValue(mapID, out ChartSheet? sheet))
			return sheet;

		LoadAssetFile(); Interlude.Spin();

		//MonoBehaviour map = (MonoBehaviour)AssetsFile.assetsFileList[0].Objects.First(x => x is MonoBehaviour mB && mB.m_Name.EndsWith($"_map{mapID}"));
		MonoBehaviour map = MuseDashCompatibility.StreamingAssets.LoadAsset<MonoBehaviour>($"Assets/Static Resources/Data/Configs/StageInfos/{__jsonInfo.NoteJSON}{mapID}.asset")!;
		var obj = map.ToType();
		var rawData = JsonConvert.SerializeObject(obj, Formatting.Indented); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		var rr = MuseDashCompatibility.InitializeCompatibilityLayer(); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		if (rr != MDCompatLayerInitResult.OK)
			throw new FileLoadException("InitializeCompatibilityLayer did not succeed!");

		StageInfo stage = JsonConvert.DeserializeObject<StageInfo>(rawData); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		stage.musicDatas = OdinSerializer.SerializationUtility.DeserializeValue<List<MusicData>>(stage.serializationData.SerializedBytes, DataFormat.Binary); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		MuseDashCompatibility.FillInTheBlankNotes(this, stage); Interlude.Spin(submessage: "Reading Muse Dash chart...");

		return MuseDashCompatibility.ConvertStageInfoToDashSheet(this, stage);
	}

	protected override ChartInfo? ProduceInfo() {
		List<string> SearchTags = [];
		SearchTags.AddRange(__jsonInfo.Name.Split(' '));
		ChartInfo info = new ChartInfo() {
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
}