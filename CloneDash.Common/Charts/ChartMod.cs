using CloneDash.Common.Compatibility;
using CloneDash.Common.Songs;
using Nucleus;

namespace CloneDash.Charts;

[MarkForStaticConstruction]
public sealed class ChartMod : ModStore<IChartSongProvider, ISong>
{
	public static ChartMod Instance => ModStores.Get<ChartMod>();

	static ChartMod() {
		_ = Instance;
	}

	public ChartMod() : base("Songs", "song") { }
}
