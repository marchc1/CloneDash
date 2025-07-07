namespace CloneDash.Game.Events;

public class BossFar2End(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2End();
	}
}
