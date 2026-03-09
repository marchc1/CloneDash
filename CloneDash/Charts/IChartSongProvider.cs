using CloneDash.Data;

namespace CloneDash.Charts;

/// <summary>
/// A producer of source states and chart songs
/// </summary>
public interface IChartSongProvider {
	ReadOnlySpan<char> GetName();
	IChartSongSourceState NewState();

	public ChartSong? FindByName(ReadOnlySpan<char> name);
	public IEnumerable<string> GetAvailable();
}

