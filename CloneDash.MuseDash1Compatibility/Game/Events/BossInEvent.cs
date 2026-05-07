namespace CloneDash.Game.Events;

public class BossInEvent(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.In();
	}
}