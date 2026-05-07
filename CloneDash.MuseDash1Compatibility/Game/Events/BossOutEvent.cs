namespace CloneDash.Game.Events;

public class BossOutEvent(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Out();
	}
}
