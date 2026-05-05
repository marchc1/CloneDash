using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;

namespace CloneDash.Common.Game;

/// <summary>
/// A running instance of a gamemode.
/// </summary>
public interface IGame
{
	IGamemodeDescriptor GetGamemode();
	ISong GetSong();
	ISongChart GetSongChart();
	object GetGamemodeData();
	IConductor GetConductor();
}
