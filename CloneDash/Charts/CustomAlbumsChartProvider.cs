using CloneDash.Common.Songs;
using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.MuseDash;
using Nucleus.Files;
using System.Xml.Linq;

namespace CloneDash.Charts;

public class CustomAlbumsChartSource : BaseContiguousSongSource
{
	public CustomAlbumsChartSource() : base(GetCustomSongs()) {

	}

	static List<ISong>? songs;
	private static IReadOnlyList<ISong> GetCustomSongs() {
		if (songs != null)
			return songs;

		var directory = Path.Combine(MuseDash1Compatibility.WhereIsMuseDashInstalled!, "Custom_Albums");
		if (!Directory.Exists(directory))
			return [];

		songs = new List<ISong>();
		foreach (var song in Directory.GetFiles(directory)) {
			try{
				var custom = new CustomAlbumsCompatibility.MD1_CustomChartsSong(song);
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

	public ReadOnlySpan<char> GetName() => "Custom Albums";
	public ISongSourceState NewState() => new CustomAlbumsChartSource();
}
