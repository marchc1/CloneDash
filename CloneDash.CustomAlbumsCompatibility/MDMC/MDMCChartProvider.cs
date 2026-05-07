using CloneDash.Common.Songs;
using CloneDash.Compatibility.MDMC;
using CloneDash.Menu.Searching;
using Newtonsoft.Json;
using Nucleus;
using System.Reflection;
using static CloneDash.CustomAlbumsCompatibility.CustomAlbums.CustomAlbumsCompatibility;

namespace CloneDash.Charts;

public class MDMCChartProvider : IChartSongProvider
{
	public ISong? FindByName(ReadOnlySpan<char> name) => null; // Cannot poll for this
	public IEnumerable<string> GetAvailable() { yield break; } // Cannot poll for this

	public ReadOnlySpan<char> GetName() => "MDMC";
	public ISongSourceState NewState() => new MDMCChartSongSourceState();
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
		FetchAsync(1, null);
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

	public ISong? At(int i) {
		var absPtr = pointer + i;
		if (absPtr < 0) return null;
		if (absPtr >= Songs.Count) return null;

		return Songs[absPtr];
	}

	public int GetSongCount() => totalSongs;

	public bool IsBusy() => isBusy;

	public ChartSongSourceMoveInit MoveLeft(ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy()) {
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false
			};
		}

		if (pointer <= 0) {
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false
			};
		}

		isBusy = true;
		{
			// Inquire if the chart is null, if it is, requery async
			if (Songs[pointer - 1] == null) {
				if (noMoreSongsLeft)
					return new ChartSongSourceMoveInit { ImmediatelyAvailable = false, OperationExecuted = false };
				return FetchAsync(MDMCWebAPI.ChartIdxToPageIdx(pointer - 1), callback);
			}
			else {
				pointer--;
				callback?.Invoke(new() {
					OperationExecuted = true,
					Movement = -1
				});
			}
		}
		isBusy = false;

		ChartSongSourceMoveInit res = new ChartSongSourceMoveInit();
		res.OperationExecuted = true;
		res.ImmediatelyAvailable = true;
		return res;
	}

	public ChartSongSourceMoveInit MoveRight(ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy()) {
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false
			};
		}

		isBusy = true;
		{
			// Inquire if the chart is null, if it is, requery async.
			// NOTE: We check halfway to ensure that the user will never see an empty disc
			var halfway = SongSelector.VisibleDiscs / 2;
			var hypotheticalMaxDisc = pointer + halfway;

			if (hypotheticalMaxDisc >= Songs.Count - 1 || Songs[hypotheticalMaxDisc] == null) {
				if (noMoreSongsLeft)
					return new ChartSongSourceMoveInit { ImmediatelyAvailable = false, OperationExecuted = false };
				return FetchAsync(MDMCWebAPI.ChartIdxToPageIdx(hypotheticalMaxDisc + 1), callback);
			}
			else {
				pointer++;
				callback?.Invoke(new() {
					OperationExecuted = true,
					Movement = 1
				});
			}
		}
		isBusy = false;

		ChartSongSourceMoveInit res = new ChartSongSourceMoveInit();
		res.OperationExecuted = true;
		res.ImmediatelyAvailable = true;
		return res;
	}



	private class mdmcChartsWithCount
	{
		[JsonProperty("charts")] public MDMCChart[] Charts = null!;
		[JsonProperty("total")] public int Count;
	}

	private ChartSongSourceMoveInit FetchAsync(int pageIdx, ChartSongSourceMoveFinishFn? callback) {
		MDMCWebAPI.SearchCharts(filter?.Query, filter?.Sort ?? MDMCWebAPI.Sort.Likes, filter?.SortOrder ?? MDMCWebAPI.SortOrder.Descending, pageIdx, filter?.RankedOnly ?? false)
			.Then(resp => {
				MainThread.RunASAP(() => {
					mdmcChartsWithCount charts = resp.FromJSON<mdmcChartsWithCount>() ?? throw new Exception("Parsing failure");

					if (charts.Count == 0) {
						MarkNoMoreSongsLeft();
						return;
					}

					var start = MDMCWebAPI.PageIdxToChartIdxStart(pageIdx);
					for (int i = start; i < start + MDMCWebAPI.MAX_CHARTS_PER_PAGE; i++) {
						ref readonly MDMCChart chart = ref charts.Charts[i - start];
						if (Songs.Count <= i)
							AddChartSelector(chart, true);
						else if (Songs[i] == null)
							Songs[i] = AddChartSelector(chart, false);
					}

					totalSongs = charts.Count;
					isBusy = false;
				});
			});

		return new ChartSongSourceMoveInit {
			ImmediatelyAvailable = false,
			OperationExecuted = true
		};
	}

	bool noMoreSongsLeft = false;
	private void MarkNoMoreSongsLeft() {
		noMoreSongsLeft = true;
		isBusy = false;
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