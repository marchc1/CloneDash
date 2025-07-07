namespace CloneDash.Game.Events;

public class BossFar2To1(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2To1();
	}
}
