using CloneDash.Common.Songs;

namespace CloneDash.Charts;

/// <summary>
/// A producer of source states and chart songs
/// </summary>
public interface IChartSongProvider
{
	public struct NavigationButtonInstructions
	{
		/// <summary>
		/// The buttons name
		/// </summary>
		public string Name;
		/// <summary>
		/// A short description for the button
		/// </summary>
		public string Description;
		/// <summary>
		/// Button icon
		/// </summary>
		public string Icon;
		/// <summary>
		/// The buttons hue
		/// </summary>
		public float Hue;
	}

	/// <summary>
	/// The name of the provider, this should be unique and should be a short name for the source of the songs.
	/// </summary>
	ReadOnlySpan<char> GetName();
	/// <summary>
	/// Produces a new <see cref="ISongSourceState"/> with no filters.
	/// </summary>
	ISongSourceState NewState();

	/// <summary>
	/// Find a song by its name.
	/// </summary>
	ISong? FindSongByName(ReadOnlySpan<char> name);

	/// <summary>
	/// Get all available songs.
	/// </summary>
	IEnumerable<ISong> GetAvailableSongs();

	/// <summary>
	/// Is this source available?
	/// </summary>
	bool IsEnabled();

	/// <summary>
	/// Returns the song that the user last was on. This is saved on a per-source basis. This only applies to offline sources,
	/// as the MDMC API doesn't have a way (and I don't expect it to have a way) to reverse, say, a song UUID -> an index given song parameters.
	/// </summary>
	ISong? GetSavedSong();
	/// <summary>
	/// Update the saved song.
	/// </summary>
	void UpdateSavedSong(ISong? selectedSong);


	/// <summary>
	/// Returns the last filter the user had. This is saved on a per-source basis. This applies to offline and online sources.
	/// </summary>
	IChartSongFilter? GetSavedFilter();
	/// <summary>
	/// Updated the saved filter.
	/// </summary>
	/// <param name="filter"></param>
	void UpdateSavedFilter(IChartSongFilter? filter);


	/// <summary>
	/// The sorting index used in the user interface.
	/// </summary>
	int GetSortIndex();

	/// <summary>
	/// Returns the navigation button instructions for the main menu user interface.
	/// </summary>
	NavigationButtonInstructions GetNavigationButtonInstructions();
}

