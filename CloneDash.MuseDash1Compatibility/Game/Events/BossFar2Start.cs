namespace CloneDash.Game.Events;

public class BossFar2Start(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far2Start();
	}
}
