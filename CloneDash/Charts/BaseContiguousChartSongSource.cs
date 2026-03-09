using CloneDash.Data;
using CloneDash.Menu.Searching;
using Nucleus;

namespace CloneDash.Charts;

/// <summary>
/// A base implementation where all songs are available at once
/// </summary>
public class BaseContiguousChartSongSource : BaseChartSongSource, IChartSongSourceState
{
	readonly IReadOnlyList<ChartSong> Songs;

	BaseContiguousChartSongFilter? Filter;
	public BaseContiguousChartSongSource(IReadOnlyList<ChartSong> songs, BaseContiguousChartSongFilter? filter = null, IChartSongSourceState? parent = null) {
		Filter = filter;
		Songs = filter == null ? [.. songs] : filter.Apply(songs);
		Parent = parent;
		Root = parent?.GetRootSource() ?? this;
	}

	public ChartSong? At(int i) {
		if (Songs.Count <= 0)
			return null;

		if (Songs.Count == 1)
			return Songs[0];

		var midx = NMath.Modulo(pointer + i, Songs.Count);
		return Songs[midx];
	}

	int pointer = 0;

	public int GetSongCount() => Songs.Count;
	bool isBusy;
	public bool IsBusy() => isBusy;
	public ChartSongSourceMoveInit MoveLeft(ChartSongSourceMoveFinishFn? callback = null) {
		if (IsBusy()) {
			return new ChartSongSourceMoveInit {
				OperationExecuted = false,
				ImmediatelyAvailable = false
			};
		}

		isBusy = true;
		{
			pointer--;
			callback?.Invoke(new() {
				OperationExecuted = true,
				Movement = -1
			});
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
			pointer++;
			callback?.Invoke(new() {
				OperationExecuted = true,
				Movement = 1
			});
		}
		isBusy = false;

		return new ChartSongSourceMoveInit {
			OperationExecuted = true,
			ImmediatelyAvailable = true
		};
	}
	public IChartSongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not BaseContiguousChartSongFilter contigFilter)
			throw new InvalidCastException("Invalid contigFilter");
		return new BaseContiguousChartSongSource(Songs, contigFilter, this.GetRootSource());
	}
	public ChartSongSourceMoveInit Select(ChartSong? selectSong, ChartSongSourceMoveFinishFn? callback = null) {
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
	public bool ShouldWrap() => true;

	public virtual IChartSongFilter NewFilter() {
		return new BaseContiguousChartSongFilter(Filter);
	}
}
