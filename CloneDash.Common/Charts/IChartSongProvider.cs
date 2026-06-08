using CloneDash.Common.Songs;

namespace CloneDash.Charts;

/// <summary>
/// A producer of source states and chart songs
/// </summary>
public interface IChartSongProvider {
	ReadOnlySpan<char> GetName();
	ISongSourceState NewState();

	public ISong? FindByName(ReadOnlySpan<char> name);
	public IEnumerable<string> GetAvailable();
}

