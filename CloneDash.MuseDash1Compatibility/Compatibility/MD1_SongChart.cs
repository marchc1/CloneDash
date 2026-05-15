using CloneDash.Common.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Nucleus.Common.Types;

namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public struct ChartSceneChange
{
	public string SceneUID;
	public double Time;
	public string? Value;
}

public class MD1_GamemodeData
{
	public readonly List<MD1_SongChartEntity> Entities = [];
	public readonly List<MD1_SongChartEvent> Events = [];
	public readonly List<TempoChange> TempoChanges = [];
	public readonly List<TimeSignatureChange> TimeSignatureChanges = [];
	public readonly List<ChartSceneChange> SceneChanges = [];
	public string? InitialScene;
	public double StartOffset;
}

/// <summary>
/// Previous was referred to as a "song chart sheet"
/// </summary>
public class MD1_SongChart : ISongChart
{
	public readonly MD1_Song Song;
	public string? Rating;
	public MuseDashDifficulty Difficulty;
	public MD1_GamemodeData GamemodeData = null!;

	public MD1_SongChart(MD1_Song song, int difficultyID) {
		Song = song;
		Rating = song.GetDifficultyString(difficultyID);
		Difficulty = (MuseDashDifficulty)difficultyID;
	}
	public ISong GetSong() => Song;
	public IGamemodeDescriptor GetGamemode() {
		if (Difficulty == MuseDashDifficulty.Touhou)
			return GamemodeMod.GetGamemode(MuseDash1TouhouGamemode.UUID)!;
		else
			return GamemodeMod.GetGamemode(MuseDash1Gamemode.UUID)!;
	}
	public object GetGamemodeData() => (GamemodeData ?? Song.ProduceGamemodeData(this, (int)Difficulty))
									?? throw new Exception("uninitialized gamemode data");

	public SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		var designers = Song.GetInfo()?.LevelDesigners;
		var difficultyIndex = ((int)Difficulty) - 1;
		bool overflow = difficultyIndex >= designers?.Length;

		return new SongChartMetadata() {
			GamemodeName = "Muse Dash 1",
			ChartAuthors = overflow ? "N/A" : designers?[((int)Difficulty) - 1] ?? "N/A",
			ReturnedLanguage = desiredLanguage, // todo: language
			Difficulty = $"{Rating}",
			DifficultyName = Difficulty switch {
				MuseDashDifficulty.Easy => "Easy",
				MuseDashDifficulty.Hard => "Hard",
				MuseDashDifficulty.Master => "Master",
				MuseDashDifficulty.Supreme => "Supreme",
				MuseDashDifficulty.Touhou => "Touhou",
				_ => throw new Exception($"Unsupported difficulty level '{Difficulty}'")
			},
			Color = Difficulty switch {
				MuseDashDifficulty.Easy => new Color(88, 199, 76, 60),
				MuseDashDifficulty.Hard => new Color(109, 196, 199, 60),
				MuseDashDifficulty.Master => new Color(188, 95, 184, 60),
				MuseDashDifficulty.Supreme => new Color(199, 35, 35, 60),
				MuseDashDifficulty.Touhou => new Color(109, 103, 194, 60),
				_ => new(50, 50, 50)
			},
		};
	}
}
