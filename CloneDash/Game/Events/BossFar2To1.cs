namespace CloneDash.Game.Events;

public class BossFar2To1(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2To1();
	}
}
