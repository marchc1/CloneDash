using CloneDash.Common.Songs;

namespace CloneDash.Charts;

/// <summary>
/// A producer of source states and chart songs
/// </summary>
public interface IChartSongProvider {
	public struct NavigationButtonInstructions{
		public string Name;
		public string Description;
		public string Icon;
		public float Hue;
	}


	ReadOnlySpan<char> GetName();
	ISongSourceState NewState();

	int GetSortIndex();

	ISong? FindByName(ReadOnlySpan<char> name);
	IEnumerable<string> GetAvailable();

	bool IsEnabled();

	/// <summary>
	/// Returns the song that the user last was on. This is saved on a per-source basis. This only applies to offline sources,
	/// as the MDMC API doesn't have a way (and I don't expect it to have a way) to reverse, say, a song UUID -> an index given song parameters.
	/// </summary>
	/// <returns></returns>
	ISong? SavedSong();

	/// <summary>
	/// Returns the last filter the user had. This is saved on a per-source basis. This applies to offline and online sources.
	/// </summary>
	/// <returns></returns>
	IChartSongFilter? SavedFilter();

	void UpdateSavedSong(ISong? selectedSong);
	void UpdateSavedFilter(IChartSongFilter? filter);
	NavigationButtonInstructions GetNavigationButtonInstructions();
}

