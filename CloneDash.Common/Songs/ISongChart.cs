using CloneDash.Common.Gamemodes;
using Nucleus.Common.Audio;
using Nucleus.Common.Types;

namespace CloneDash.Common.Songs;

public struct SongChartMetadata
{
	/// <summary>
	/// The language this metadata is in
	/// </summary>
	public HumanLanguage ReturnedLanguage;

	/// <summary>
	/// The human-friendly difficulty ID of the chart. For example, Muse Dash has difficulties for "easy", "hard", "master", "hidden", etc.
	/// </summary>
	public string DifficultyID;
	/// <summary>
	/// The human-friendly difficulty of the chart. This is likely a number. But don't assume that it is one, it can be anything.
	/// </summary>
	public string Difficulty;
	/// <summary>
	/// The color of the chart. This is likely decided by the gamemode, and may be different depending on difficulty ID.
	/// </summary>
	public Color Color;
	/// <summary>
	/// The human-friendly gamemode name.
	/// </summary>
	public string GamemodeName;
	/// <summary>
	/// The human-friendly authors of the chart.
	/// </summary>
	public string ChartAuthors;
}

/// <summary>
/// A song chart is a generic combination of a few things:
/// <br/> 1. A target gamemode for this chart
/// <br/> 2. Gamemode-specific data for this chart
/// <br/> 3. Generic metadata
/// </summary>

public interface ISongChart
{
	/// <summary>
	/// The parent song
	/// </summary>
	ISong GetSong();
	/// <summary>
	/// The gamemode this chart targets
	/// </summary>
	IGamemodeDescriptor GetGamemode();
	/// <summary>
	/// The gamemode-specific data for this chart, which will be passed in
	/// when creating an instance of the gamemode.
	/// </summary>
	object GetGamemodeData();
	/// <summary>
	/// Gathers a bunch of chart metadata
	/// </summary>
	SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage);
}
