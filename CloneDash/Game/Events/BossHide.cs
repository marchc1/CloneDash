namespace CloneDash.Game.Events;

public class BossHide(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Hide();
	}
}
