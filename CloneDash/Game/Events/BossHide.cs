namespace CloneDash.Game.Events;

public class BossHide(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Hide();
	}
}
