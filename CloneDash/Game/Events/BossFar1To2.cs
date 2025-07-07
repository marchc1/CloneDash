namespace CloneDash.Game.Events;

public class BossFar1To2(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1To2();
	}
}
