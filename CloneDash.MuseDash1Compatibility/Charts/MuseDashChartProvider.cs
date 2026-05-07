using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using System.Xml.Linq;

namespace CloneDash.Charts;

public class MuseDashChartSource : BaseContiguousSongSource
{
	public MuseDashChartSource() : base(MuseDash1Compatibility.Songs) {

	}
}

public class MuseDashChartProvider : IChartSongProvider
{
	public ISong? FindByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in MuseDash1Compatibility.Songs) {
			if (name.Equals(song.BaseName, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<string> GetAvailable() {
		foreach (var song in MuseDash1Compatibility.Songs)
			yield return song.BaseName;
	}

	public ReadOnlySpan<char> GetName() => "Muse Dash";

	public ISongSourceState NewState() => new MuseDashChartSource();
}
