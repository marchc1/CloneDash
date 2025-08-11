using CloneDash.Compatibility.MDMC;
using CloneDash.Data;
using CloneDash.Game;

using Newtonsoft.Json;

using static CloneDash.Compatibility.CustomAlbums.CustomAlbumsCompatibility;

namespace CloneDash.Menu.Searching;

public class MDMCSearchFilter : SearchFilter
{
	public int Page = 0;
	public string Query = "";
	public MDMCWebAPI.Sort Sort = MDMCWebAPI.Sort.Likes;
	public MDMCWebAPI.SortOrder Order = MDMCWebAPI.SortOrder.Descending;
	public bool OnlyRanked = false;


	public override void Populate(SongSearchDialog dialog) {
		TextInput(dialog, nameof(Query), "Search Query", true, true);
		EnumInput(dialog, nameof(Sort), Sort);
		EnumInput(dialog, nameof(Order), Order);
        CheckboxInput(dialog, nameof(OnlyRanked), "Only ranked charts?");
	}

	public override Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog) {
		var menu = dialog.Level.As<MainMenuLevel>();
		Page = 0;
		dialog.Selector.ClearSongs();
		//PopulateMDMCCharts(dialog.Selector);
		return x => true;
	}

	private CustomChartsSong AddChartSelector(MDMCChart chart) {
		CustomChartsSong song = new CustomChartsSong(chart);
		return song;
	}

	private class mdmcChartsWithCount
	{
		[JsonProperty("charts")] public MDMCChart[] Charts;
		[JsonProperty("total")] public int Count;
	}

	internal void PopulateMDMCCharts(SongSelector selector) {
		Page++;

		MDMCWebAPI.SearchCharts(string.IsNullOrWhiteSpace(Query) ? null : Query, Sort, Order, Page, OnlyRanked).Then((resp) => {
			mdmcChartsWithCount charts = resp.FromJSON<mdmcChartsWithCount>() ?? throw new Exception("Parsing failure");

			if (charts.Count == 0) {
				selector.MarkNoMoreSongsLeft();
				return;
			}
			var songs = new List<CustomChartsSong>();

			foreach (MDMCChart chart in charts.Charts) {
				songs.Add(AddChartSelector(chart));
			}

			selector?.SetCount(charts.Count);
			if (string.IsNullOrWhiteSpace(Query)) {
				selector?.SetTotal(charts.Count);
			}
			selector?.AddSongs(songs);
			selector?.AcceptMoreSongs();
		});
	}
}
