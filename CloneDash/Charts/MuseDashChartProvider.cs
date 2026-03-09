using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using System.Xml.Linq;

namespace CloneDash.Charts;

public class MuseDashChartSource : BaseContiguousChartSongSource
{
	public MuseDashChartSource() : base(MuseDashCompatibility.Songs) {

	}
}

public class MuseDashChartProvider : IChartSongProvider
{
	public ChartSong? FindByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in MuseDashCompatibility.Songs) {
			if (name.Equals(song.BaseName, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<string> GetAvailable() {
		foreach (var song in MuseDashCompatibility.Songs)
			yield return song.BaseName;
	}

	public ReadOnlySpan<char> GetName() => "Muse Dash";

	public IChartSongSourceState NewState() => new MuseDashChartSource();
}
