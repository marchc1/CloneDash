namespace CloneDash.Game.Events;

public class BossFar1To2(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1To2();
	}
}
