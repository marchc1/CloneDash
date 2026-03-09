using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using Nucleus.Files;
using System.Xml.Linq;

namespace CloneDash.Charts;

public class CustomAlbumsChartSource : BaseContiguousChartSongSource
{
	public CustomAlbumsChartSource() : base(GetCustomSongs()) {

	}

	static List<ChartSong>? songs;
	private static IReadOnlyList<ChartSong> GetCustomSongs() {
		if (songs != null)
			return songs;

		var directory = Path.Combine(MuseDashCompatibility.WhereIsMuseDashInstalled!, "Custom_Albums");
		if (!Directory.Exists(directory))
			return [];

		songs = new List<ChartSong>();
		foreach (var song in Directory.GetFiles(directory)) {
			try{
				var custom = new CustomAlbumsCompatibility.CustomChartsSong(song);
				songs.Add(custom);
			}
			catch {

			}
		}

		return songs;
	}
}

public class CustomAlbumsChartProvider : IChartSongProvider
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

	public ReadOnlySpan<char> GetName() => "Custom Albums";
	public IChartSongSourceState NewState() => new CustomAlbumsChartSource();
}
