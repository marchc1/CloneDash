namespace CloneDash.Game.Events;

public class BossFar1Start(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1Start();
	}
}
