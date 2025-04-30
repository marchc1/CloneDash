namespace CloneDash.Game.Events;

public class BossInEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.In();
	}
}