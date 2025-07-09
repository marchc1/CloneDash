namespace CloneDash.Game.Events;

public class BossFar2Start(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2Start();
	}
}
