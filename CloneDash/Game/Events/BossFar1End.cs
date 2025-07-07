namespace CloneDash.Game.Events;

public class BossFar1End(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1End();
	}
}
