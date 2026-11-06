namespace CloneDash.Game.Events;

public class BossFar1Start(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far1Start();
	}
}
