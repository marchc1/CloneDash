using CloneDash.Common.Compatibility;
using CloneDash.Common.Songs;
using CloneDash.Common.UI.Searching;
using CloneDash.Compatibility.MDMC;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Menu.Searching;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using System.Reflection;
using static CloneDash.CustomAlbumsCompatibility.CustomAlbums.CustomAlbumsCompatibility;

namespace CloneDash.Charts;

[MarkForStaticConstruction]

public class MDMCChartProvider() : ModProvider<ISong>("MDMC", "mdmc"), IChartSongProvider
{
	public static readonly ConVar mdmc_lastfilter = new ConVar(nameof(mdmc_lastfilter), "", FCvar.Saved, "The last selected song filter");

	public ISong? FindSongByName(ReadOnlySpan<char> name) => null; // Cannot poll for this
	public override IEnumerable<string> GetAvailableIDs() { yield break; } // Cannot poll for this
	public override ISong? FindByID(ReadOnlySpan<char> id) => null; // Cannot poll for this
	public ISongSourceState NewState() => new MDMCChartSongSourceState();

	public IChartSongFilter? GetSavedFilter() => mdmc_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<MDMCChartFilter>(new(mdmc_lastfilter.GetString()));
	public ISong? GetSavedSong() => null;

	public void UpdateSavedFilter(IChartSongFilter? filter) => mdmc_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((MDMCChartFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) { }

	public override int GetSortIndex() => 4000;

	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Browse mdmc.moe Charts",
		Description = "Download new charts from the Muse Dash Modding Community.",
		Hue = 340,
		Icon = "icons/globe-hemisphere-west.png"
	};
}

public class MDMCChartFilter(MDMCChartFilter? baseFilter) : BaseContiguousChartSongFilter(baseFilter)
{
	public MDMCWebAPI.Sort Sort = baseFilter?.Sort ?? MDMCWebAPI.Sort.Likes;
	public MDMCWebAPI.SortOrder SortOrder = baseFilter?.SortOrder ?? MDMCWebAPI.SortOrder.Descending;
	public bool RankedOnly = baseFilter?.RankedOnly ?? false;

	public override void PopulateFields(SongSearchDialog dialog) {
		base.PopulateFields(dialog);

		dialog.EnumInput(nameof(Sort), "Sort By", Sort);
		dialog.EnumInput(nameof(SortOrder), "Sort Order", SortOrder);
		dialog.BoolInput(nameof(RankedOnly), "Ranked Only", RankedOnly);
	}
}

public class MDMCChartSongSourceState : BaseSongSource, ISongSourceState
{
	public MDMCChartSongSourceState(MDMCChartFilter? filter = null) {
		Root = this;
		this.filter = filter;
		isBusy = true;
		FetchAsync();
	}

	readonly Dictionary<string, MD1_CustomChartsSong> SongCache = [];
	readonly List<MD1_CustomChartsSong> Songs = [];
	int totalSongs;
	bool isBusy;
	int pointer = 0;
	readonly MDMCChartFilter? filter;
	private MD1_CustomChartsSong AddChartSelector(in MDMCChart chart, bool addToList) {
		if (SongCache.TryGetValue(chart.ID, out var song))
			return song;
		SongCache[chart.ID] = song = new MD1_CustomChartsSong(chart);
		if (addToList)
			Songs.Add(song);
		return song;
	}

	public int Index(ISong? song) {
		if (song == null)
			return -1;

		if (Songs.Count <= 0)
			return -1;

		if (Songs.Count == 1)
			return 0;

		ISong? ret;
		for (int i = 0; i < Songs.Count; i++)
			if ((ret = Songs[i]) == song)
				return i;

		return -1;
	}


	public ISong? At(int i) {
		var absPtr = pointer + i;
		if (absPtr < 0) return null;
		if (absPtr >= Songs.Count) return null;

		return Songs[absPtr];
	}

	public int GetSongCount() => totalSongs;

	public bool IsBusy() => isBusy;

	static readonly ChartSongSourceMoveInit NotExecuted = new() { OperationExecuted = false, ImmediatelyAvailable = false };
	static readonly ChartSongSourceMoveInit Executed = new() { OperationExecuted = true, ImmediatelyAvailable = true };

	/// <summary>
	/// Moves the selection one chart to the left, if there is one.
	/// </summary>
	public ChartSongSourceMoveInit MoveLeft(ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy() || pointer <= 0) {
			return NotExecuted;
		}

		pointer--;
		callback?.Invoke(new() { OperationExecuted = true, Movement = -1 });
		return Executed;
	}

	public ChartSongSourceMoveInit MoveRight(ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy() || pointer + 1 >= Songs.Count) {
			return NotExecuted;
		}

		pointer++;
		callback?.Invoke(new() { OperationExecuted = true, Movement = 1 });

		// Prefetch the next page in the background once the right edge of the visible discs nears the end
		if (!fetching && Songs.Count < totalSongs && pointer + SongSelector.VisibleDiscs / 2 >= Songs.Count - 1) {
			FetchAsync();
		}

		return Executed;
	}

	private class mdmcChartsWithCount
	{
		[JsonProperty("charts")] public MDMCChart[] Charts = null!;
		[JsonProperty("total")] public int Count;
	}

	bool fetching;
	int nextPage = 1;
	/// <summary>
	/// Requests the next page of charts from mdmc.moe and appends them (skipping any already listed) when it arrives.
	/// </summary>
	private void FetchAsync() {
		fetching = true;
		MDMCWebAPI.SearchCharts(filter?.Query, filter?.Sort ?? MDMCWebAPI.Sort.Likes, filter?.SortOrder ?? MDMCWebAPI.SortOrder.Descending, nextPage++, filter?.RankedOnly ?? false)
			.Then(resp => {
				mdmcChartsWithCount charts = resp.FromJSON<mdmcChartsWithCount>() ?? throw new Exception("Parsing failure");

				// Pages can overlap if rankings shift between requests; AddChartSelector skips charts it has already seen
				foreach (var chart in charts.Charts)
					AddChartSelector(chart, true);

				// empty page = end, even if total disagrees
				totalSongs = charts.Charts.Length == 0 ? Songs.Count : charts.Count;
				fetching = false;
				isBusy = false;
			});
	}

	public IChartSongFilter NewFilter() {
		return new MDMCChartFilter(filter);
	}

	public ISongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not MDMCChartFilter mdmcFilter)
			throw new InvalidCastException();

		return new MDMCChartSongSourceState(mdmcFilter);
	}

	public ChartSongSourceMoveInit Select(ISong? selectSong, ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy()) {
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false
			};
		}

		int idx = -1;
		for (int i = 0; i < Songs.Count; i++) {
			var song = Songs[i];
			if (song == selectSong) {
				idx = i;
				break;
			}
		}

		if (idx == -1)
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false,
			};

		isBusy = true;
		{
			var delta = idx - pointer;
			pointer = idx;
			callback?.Invoke(new() {
				OperationExecuted = true,
				Movement = delta
			});
		}
		isBusy = false;

		return new ChartSongSourceMoveInit {
			OperationExecuted = true,
			ImmediatelyAvailable = true
		};
	}

	public bool ShouldWrap() => false;
}