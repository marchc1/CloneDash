namespace CloneDash.Game.Events;

public class BossFar1End(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far1End();
	}
}
