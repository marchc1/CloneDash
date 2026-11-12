using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Game.Statistics;
using CloneDash.Scenes;
using Microsoft.Extensions.DependencyInjection;
using Nucleus;

namespace CloneDash.Game;

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1TouhouGamemode(ICloneDashModProvider provider) : MuseDash1Gamemode(provider)
{
	public override string GetName() => "Muse Dash 1 (Touhou)";
	public override string GetID() => "touhou";

	public override IEnumerable<GamemodeInput> GetInputs() => [];
	public override IEnumerable<GamemodeInputSwitch> GetInputSwitches() => [];

	protected override void AddChart(ServiceCollection parts, MD1_GamemodeData data) => parts.AddPartWith<MuseDash1ChartComponent, MuseDash1TouhouChartComponent>(data);

	public const string UUID = "gamemode/musedash1/touhou";
}

public class MuseDash1TouhouChartComponent(Conductor conductor, MD1_GamemodeData data, GameEventManager events, HitsoundComponent hitsounds, RhythmGameLevel level, ResultComponent results, MuseDash1ScenesComponent scenes, ScrollComponent scroll, StatisticsData stats) : MuseDash1ChartComponent(conductor, data, events, hitsounds, level, results, scenes, scroll, stats)
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