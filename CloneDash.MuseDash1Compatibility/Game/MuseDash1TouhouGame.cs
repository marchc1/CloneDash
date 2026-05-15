using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using Nucleus;

namespace CloneDash.Game;

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1TouhouGamemode : IGamemodeDescriptor
{
	public ReadOnlySpan<char> GetUUID() => UUID;

	public IGame Load(ISongChart chart, in GameLoadGenericParameters parms) {
		var game = new MuseDash1TouhouGame(new((MD1_SongChart)chart) {
			Autoplay = parms.Autoplay,
			Measure = parms.StartMeasure ?? 0
		});
		EngineCore.LoadLevel(game);
		return game;
	}

	public static readonly string UUID = "gamemode/musedash1/touhou";
}

[MarkForStaticConstruction]
public partial class MuseDash1TouhouGame(DashGameParams gameParameters) : MuseDash1Game(gameParameters), IGame
{

}