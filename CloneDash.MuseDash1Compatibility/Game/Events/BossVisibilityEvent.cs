namespace CloneDash.Game.Events;

public class BossVisibilityEvent(DashEventComponents components, bool visible) : DashEvent(components)
{
	public readonly bool Visible = visible;
}
