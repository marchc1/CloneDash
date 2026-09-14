using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.CustomAlbums;
using CloneDash.Compatibility.MuseDash;
using FftSharp;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Files;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace CloneDash.CustomAlbumsCompatibility.CustomAlbums;

public class CustomAlbumsChartSource : BaseContiguousSongSource
{
	public CustomAlbumsChartSource(BaseContiguousChartSongFilter? filter = null, ISongSourceState? parent = null) :  base(GetCustomSongs(), filter, parent) {

	}

	public override ISongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not BaseContiguousChartSongFilter contigFilter)
			throw new InvalidCastException("Invalid contigFilter");
		return new CustomAlbumsChartSource(contigFilter, this.GetRootSource());
	}

	static List<ISong>? songs;
	internal static IReadOnlyList<ISong> GetCustomSongs() {
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

[MarkForStaticConstruction]
public class CustomAlbumsChartProvider : IChartSongProvider
{
	public static readonly ConVar cam_lastsong = new ConVar(nameof(cam_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar cam_lastfilter = new ConVar(nameof(cam_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public IChartSongFilter? SavedFilter() => cam_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(cam_lastfilter.GetString()));
	public ISong? SavedSong() => cam_lastsong.GetString().IsEmpty ? null : CustomAlbumsChartSource.GetCustomSongs().FirstOrDefault(x => x.GetUUID().Equals(cam_lastsong.GetString(), StringComparison.InvariantCultureIgnoreCase));
	public void UpdateSavedFilter(IChartSongFilter? filter) => cam_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => cam_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUUID());

	public ISong? FindByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in CustomAlbumsChartSource.GetCustomSongs()) {
			if (name.Equals(song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<string> GetAvailable() {
		foreach (var song in CustomAlbumsChartSource.GetCustomSongs())
			yield return song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name;
	}

	public ReadOnlySpan<char> GetName() => "Custom Albums";
	public ISongSourceState NewState() => new CustomAlbumsChartSource();
}
