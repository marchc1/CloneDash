using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using Nucleus;

namespace CloneDash.Game;

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1TouhouGamemode(ICloneDashModProvider provider) : MuseDash1Gamemode(provider)
{
	public override string GetName() => "Muse Dash 1 (Touhou)";
	public override string GetID() => "touhou";

	public const string UUID = "gamemode/musedash1/touhou";
}

[MarkForStaticConstruction]
public partial class MuseDash1TouhouGame(DashGameParams gameParameters) : MuseDash1Game(gameParameters)
{
	public override DashEnemy? LoadEntity(MD1_SongChartEntity ChartEntity) {
		DashEnemy? enemy = base.LoadEntity(ChartEntity);
		if (enemy == null) return null;

		// flip it for touhou mode
		if (enemy.Interactivity == EntityInteractivity.Hit && enemy.Type == MuseDash1EntityType.Single)
			enemy.Interactivity = EntityInteractivity.Avoid;

		return enemy;
	}
}