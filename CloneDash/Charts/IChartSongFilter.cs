using CloneDash.Data;
using CloneDash.Menu.Searching;

namespace CloneDash.Charts;

public interface IChartSongFilter
{
	void PopulateFields(SongSearchDialog activeDialog);
	bool Test(ChartSong song);
}