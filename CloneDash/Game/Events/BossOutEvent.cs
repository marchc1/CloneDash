namespace CloneDash.Game.Events;

public class BossOutEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Out();
	}
}
