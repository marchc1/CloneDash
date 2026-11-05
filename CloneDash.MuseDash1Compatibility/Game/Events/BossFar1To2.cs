namespace CloneDash.Game.Events;

public class BossFar1To2(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far1To2();
	}
}
