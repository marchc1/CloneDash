namespace CloneDash.Game.Events;

public class BossFar2Start(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2Start();
	}
}
