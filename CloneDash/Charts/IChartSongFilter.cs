using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Menu.Searching;

namespace CloneDash.Charts;

public interface IChartSongFilter
{
	void PopulateFields(SongSearchDialog activeDialog);
	bool Test(ISong song);
}