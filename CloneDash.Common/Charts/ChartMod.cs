using CloneDash.Common.Compatibility;
using CloneDash.Common.Songs;
using Nucleus;

namespace CloneDash.Charts;

[MarkForStaticConstruction]
public sealed class ChartMod() : ModStore<IChartSongProvider, ISong>("Songs", "song")
{
	public static ChartMod Instance => ModStores.Get<ChartMod>();

	static ChartMod() {
		_ = Instance;
	}
}
