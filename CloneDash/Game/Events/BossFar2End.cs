namespace CloneDash.Game.Events;

public class BossFar2End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2End();
	}
}
