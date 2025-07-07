namespace CloneDash.Game.Events;

public class BossInEvent(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.In();
	}
}