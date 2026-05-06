using CloneDash.Common.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;

namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public struct ChartSceneChange {
	public string SceneUID;
	public double Time;
	public string? Value;
}


/// <summary>
/// Previous was referred to as a "song chart sheet"
/// </summary>
public class MD1_SongChart : ISongChart
{
	public readonly MD1_Song Song;
	public string? Rating;
	public double StartOffset;
	public readonly List<MD1_SongChartEntity> Entities = [];
	public readonly List<MD1_SongChartEvent> Events = [];
	public readonly List<TempoChange> TempoChanges = [];
	public readonly List<TimeSignatureChange> TimeSignatureChanges = [];
	public string? InitialScene;
	public readonly List<ChartSceneChange> SceneChanges = [];

	public MD1_SongChart(MD1_Song song) => Song = song;

	public ISong GetSong() => Song;
	public IGamemodeDescriptor GetGamemode() {
		throw new NotImplementedException();
	}

	public object GetGamemodeData() => this;
	public SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		throw new NotImplementedException();
	}
}
