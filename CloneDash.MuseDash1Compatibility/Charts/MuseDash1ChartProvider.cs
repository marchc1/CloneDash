using CloneDash.Common;
using CloneDash.Common.Compatibility;
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
public class MuseDash1ChartProvider() : ModProvider<ISong>("Muse Dash 1", "musedash1"), IChartSongProvider
{
	public static readonly ConVar md1_lastsong = new ConVar(nameof(md1_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar md1_lastfilter = new ConVar(nameof(md1_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public IChartSongFilter? GetSavedFilter() => md1_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(md1_lastfilter.GetString()));
	public ISong? GetSavedSong() => ChartMod.Instance.GetDescriptorByUID(md1_lastsong.GetString());
	public void UpdateSavedFilter(IChartSongFilter? filter) => md1_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => md1_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUniqueID());

	public ISong? FindSongByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in MuseDash1Compatibility.Songs) {
			if (name.Equals(song.FetchMetadata().Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var song in MuseDash1Compatibility.Songs)
			yield return song.GetID();
	}

	public override ISong? FindByID(ReadOnlySpan<char> id) {
		foreach (var song in MuseDash1Compatibility.Songs)
			if (id.Equals(song.GetID(), StringComparison.Ordinal))
				return song;
		return null;
	}

	public ISongSourceState NewState() => new MuseDash1ChartSource();

	public override int GetSortIndex() => int.MinValue;
	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Play Muse Dash Charts",
		Description = "Play base-game charts",
		Hue = 200,
		Icon = "icons/play.png"
	};
}
