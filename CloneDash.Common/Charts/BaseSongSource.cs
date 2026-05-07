namespace CloneDash.Charts;

public abstract class BaseSongSource
{
	protected ISongSourceState Root = null!;
	protected ISongSourceState? Parent;

	public ISongSourceState GetRootSource() => Root;
	public ISongSourceState? GetParentSource() => Parent;
}
