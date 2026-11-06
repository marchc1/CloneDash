namespace CloneDash.Game.Events;

public class BossInEvent(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.In();
	}
}