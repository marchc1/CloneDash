namespace CloneDash.Game.Events;

public class BossOutEvent(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Out();
	}
}
