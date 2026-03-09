namespace CloneDash.Charts;

public abstract class BaseChartSongSource
{
	protected IChartSongSourceState Root = null!;
	protected IChartSongSourceState? Parent;

	public IChartSongSourceState GetRootSource() => Root;
	public IChartSongSourceState? GetParentSource() => Parent;
}
