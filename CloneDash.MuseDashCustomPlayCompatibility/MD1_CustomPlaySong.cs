using CloneDash.Charts;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Compatibility.MuseDash;
using CloneDash.CustomAlbumsCompatibility.CustomAlbums;
using CloneDash.MuseDashCustomPlayCompatibility.Mdcpmf;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Core;

namespace CloneDash.MuseDashCustomPlayCompatibility;

public class MD1_CustomPlaySong : MD1_Song
{
	public const string SCORE_FILENAME = "score.mdcpmf";

	public string Directory { get; }
	public string ScorePath { get; }
	public MdcpmfChart Chart { get; private set; }

	public MD1_CustomPlaySong(string path) : base() {
		if (System.IO.Directory.Exists(path)) {
			Directory = path;
			ScorePath = Path.Combine(path, SCORE_FILENAME);
		}
		else {
			ScorePath = path;
			Directory = Path.GetDirectoryName(path) ?? throw new FileNotFoundException("CustomPlay: bad chart path", path);
		}

		if (!File.Exists(ScorePath))
			throw new FileNotFoundException("CustomPlay: no score.mdcpmf", ScorePath);

		Chart = MdcpmfChart.FromFile(ScorePath);

		AddBaseJSONInfo(new() {
			Name = Chart.Title,
			Author = Chart.Artist,
		}, doNotLocalize: true);
	}

	public static bool IsCustomPlayPath(string path) {
		if (System.IO.Directory.Exists(path))
			return File.Exists(Path.Combine(path, SCORE_FILENAME));
		return Path.GetExtension(path).Equals(".mdcpmf", StringComparison.OrdinalIgnoreCase) && File.Exists(path);
	}

	public override string GetID() => ScorePath;
	public override ICloneDashModProvider GetProvider() => ChartMod.Instance.GetProviderByID(CustomPlayChartProvider.PROVIDER_ID)!;

	/// <summary>
	/// easy/hard/master/special -> Muse Dash difficulty slot
	/// </summary>
	public int DifficultySlot => Chart.Difficulty switch {
		"easy" => (int)MuseDashDifficulty.Easy,
		"hard" => (int)MuseDashDifficulty.Hard,
		"master" => (int)MuseDashDifficulty.Master,
		"special" => (int)MuseDashDifficulty.Supreme,
		_ => (int)MuseDashDifficulty.Master
	};

	string LevelString => Chart.Level is "" or "-1" ? "?" : Chart.Level;

	~MD1_CustomPlaySong() {
		MainThread.RunASAP(() => {
			if (CoverTexture?.Texture != null && CoverTexture.Texture.IsValid()) CoverTexture.Texture.Dispose();
		});
	}

	string? FindFile(params string[] names) {
		foreach (var name in names) {
			var path = Path.Combine(Directory, name);
			if (File.Exists(path))
				return path;
		}
		return null;
	}

	protected override IAudioClip ProduceAudioTrack() {
		var music = FindFile("music.ogg", "music.mp3", "music.wav")
			?? throw new FileNotFoundException("CustomPlay: music could not be found (no .ogg, .mp3, .wav)");

		using MemoryStream ms = new MemoryStream(File.ReadAllBytes(music));
		return audiosystem.CreateStreamAudioClip(ms) ?? throw new InvalidDataException($"CustomPlay: couldn't load '{music}'");
	}

	protected override IAudioClip? ProduceDemoTrack() {
		var music = FindFile("music.ogg");
		if (music == null)
			return null;

		var demo = CustomPlayDemo.Load(music, Chart.DemoStart, Chart.DemoEnd);
		if (demo == null)
			return null;

		return audiosystem.CreateDynamicAudioClip(demo.Render, demo.SampleRate, demo.Channels);
	}

	protected override void ProduceCover(ChartCoverAvailableToMainThreadFn callback) {
		var cover = FindFile("cover.png", "cover.jpg");
		if (cover == null) {
			callback(null);
			return;
		}

		var coverBytes = File.ReadAllBytes(cover);
		MainThread.RunASAP(() => {
			callback(new() {
				Texture = textures.CreateTexture(coverBytes)
			});
		});
	}

	protected override MD1_SongInfo? ProduceInfo() {
		string[] designers = ["", "", "", "", ""];
		designers[DifficultySlot - 1] = Chart.Designer;

		string difficulty(int slot) => slot == DifficultySlot ? LevelString : "";

		return new() {
			BPM = Chart.BPM.ToString(System.Globalization.CultureInfo.InvariantCulture),
			Scene = $"scene_{MdcpmfConverter.GetSceneIndex(Chart.Scene, out _):00}",
			LevelDesigners = designers,
			SearchTags = [Chart.Title, Chart.Artist, Chart.Designer, Chart.ID],
			Difficulty1 = difficulty(1),
			Difficulty2 = difficulty(2),
			Difficulty3 = difficulty(3),
			Difficulty4 = difficulty(4),
			Difficulty5 = difficulty(5),
		};
	}

	protected override MD1_SongChart? ProduceSheet(int id) {
		if (id != DifficultySlot)
			return null;
		return new MD1_SongChart(this, id);
	}

	public MD1_SongChart? GetChart() => GetSheet(DifficultySlot);

	public override MD1_GamemodeData? ProduceGamemodeData(MD1_SongChart chart, int mapID) {
		Interlude.Spin(submessage: "Reading CustomPlay chart...");

		Chart = MdcpmfChart.FromFile(ScorePath);
		var converted = MdcpmfConverter.Convert(Chart);
		MdcpmfConverter.LogReport(converted, Chart.Title);

		var stageInfo = BmsLoader.TransmuteData(converted.Notes, converted.Scene);
		stageInfo.mapName = Chart.Title;
		stageInfo.difficulty = mapID;
		stageInfo.scene = converted.Scene;
		stageInfo.bpm = (float)Chart.BPM;
		stageInfo.sceneEvents.AddRange(converted.SceneEvents);

		var data = MuseDash1Compatibility.ConvertStageInfoToMD1GamemodeData(this, stageInfo, converted.TempoChanges);
		foreach (var (layer, keys) in converted.TimeRemaps)
			data.LayerRemaps[layer] = new TimeRemap(keys);

		return data;
	}
}
