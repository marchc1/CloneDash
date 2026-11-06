namespace CloneDash.Common.Game;

public abstract class ScrollModifier
{
	public virtual TimeRemap? GetRemap(IScrollable note) => null;

	public virtual double GetSpeedFactor(IScrollable note) => 1;
}
