namespace CloneDash.Game.Events;

public class BossFar2End(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Far2End();
	}
}
