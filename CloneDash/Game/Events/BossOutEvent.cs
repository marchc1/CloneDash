namespace CloneDash.Game.Events;

public class BossOutEvent(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Out();
	}
}
