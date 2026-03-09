using CloneDash.Data;
using CloneDash.Menu.Searching;
using Nucleus;

namespace CloneDash.Charts;

/// <summary>
/// Realtime source retriever. This interface has a state.
/// </summary>
public interface IChartSongSourceState
{
	/// <summary>
	/// The root song source object, likely self-referential. Can be used to revert back to a previous state.
	/// </summary>
	IChartSongSourceState GetRootSource();
	/// <summary>
	/// The previous song source object. If this is null, there is no path back to another source.
	/// </summary>
	IChartSongSourceState? GetParentSource();

	/// <summary>
	/// Produce a new song source from a search filter. The parent source will be this object.
	/// </summary>
	IChartSongSourceState ProduceNewSource(IChartSongFilter filter);

	/// <summary>
	/// The <see cref="ChartSong"/> at the index, relative to the internal counter, which is manipulated by <see cref="MoveLeft"/>/<see cref="MoveRight"/>.
	/// </summary>
	ChartSong? At(int i);

	/// <summary>
	/// Is the source busy retrieving new songs.
	/// </summary>
	bool IsBusy();

	/// <summary>
	/// Move left by one. In all likelihood, the callback will execute on the same call stack. But if the source has
	/// to asynchronously retrieve songs, then it will call later. However, the source MUST guarantee that this callback
	/// runs on the main thread (via <see cref="MainThread.RunASAP(Action, ThreadExecutionTime)"/>). The return result is whether
	/// this operation is even possible in the first place.
	/// </summary>
	ChartSongSourceMoveInit MoveLeft(ChartSongSourceMoveFinishFn? callback = null);
	/// <summary>
	/// Move right by one. In all likelihood, the callback will execute on the same call stack. But if the source has
	/// to asynchronously retrieve songs, then it will call later. However, the source MUST guarantee that this callback
	/// runs on the main thread (via <see cref="MainThread.RunASAP(Action, ThreadExecutionTime)"/>). The return result is whether
	/// this operation is even possible in the first place.
	/// </summary>
	ChartSongSourceMoveInit MoveRight(ChartSongSourceMoveFinishFn? callback = null);

	/// <summary>
	/// Selects a particular song.
	/// </summary>
	/// <returns>If the song is available in this source or not</returns>
	ChartSongSourceMoveInit Select(ChartSong? song, ChartSongSourceMoveFinishFn? callback = null);

	/// <summary>
	/// Returns if the source should wrap around or not 
	/// </summary>
	bool ShouldWrap();

	/// <summary>
	/// How many songs does this source hold? Do NOT use 
	/// </summary>
	int GetSongCount();
	IChartSongFilter NewFilter();
}
