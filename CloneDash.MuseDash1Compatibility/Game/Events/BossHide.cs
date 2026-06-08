namespace CloneDash.Game.Events;
public class BossHide(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Hide();
	}
}
