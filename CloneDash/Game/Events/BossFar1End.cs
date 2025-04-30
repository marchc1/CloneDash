namespace CloneDash.Game.Events;

public class BossFar1End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1End();
	}
}
