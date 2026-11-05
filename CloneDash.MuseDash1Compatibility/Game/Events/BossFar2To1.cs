namespace CloneDash.Game.Events;

public class BossFar2To1(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far2To1();
	}
}
