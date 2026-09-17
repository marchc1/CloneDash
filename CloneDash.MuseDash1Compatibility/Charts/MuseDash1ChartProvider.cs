using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using System.Xml.Linq;

namespace CloneDash.Charts;

public class MuseDash1ChartSource : BaseContiguousSongSource
{
	public MuseDash1ChartSource(BaseContiguousChartSongFilter? filter = null, ISongSourceState? parent = null) : base(MuseDash1Compatibility.Songs, filter, parent) {

	}

	public override ISongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not BaseContiguousChartSongFilter contigFilter)
			throw new InvalidCastException("Invalid contigFilter");
		return new MuseDash1ChartSource(contigFilter, this.GetRootSource());
	}
}

[MarkForStaticConstruction]
public class MuseDash1ChartProvider : IChartSongProvider
{
	public static readonly ConVar md1_lastsong = new ConVar(nameof(md1_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar md1_lastfilter = new ConVar(nameof(md1_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public IChartSongFilter? GetSavedFilter() => md1_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(md1_lastfilter.GetString()));
	public ISong? GetSavedSong() => md1_lastsong.GetString().IsEmpty ? null : MuseDash1Compatibility.Songs.FirstOrDefault(x => x.GetUUID().Equals(md1_lastsong.GetString(), StringComparison.InvariantCultureIgnoreCase));
	public void UpdateSavedFilter(IChartSongFilter? filter) => md1_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => md1_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUUID());

	public ISong? FindSongByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in MuseDash1Compatibility.Songs) {
			if (name.Equals(song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public IEnumerable<ISong> GetAvailableSongs() {
		foreach (var song in MuseDash1Compatibility.Songs)
			yield return song;
	}

	public ReadOnlySpan<char> GetName() => "Muse Dash 1";
	public ISongSourceState NewState() => new MuseDash1ChartSource();

	public int GetSortIndex() => int.MinValue;
	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Play Muse Dash Charts",
		Description = "Play base-game charts",
		Hue = 200,
		Icon = "icons/play.png"
	};
	public bool IsEnabled() => true;
}
