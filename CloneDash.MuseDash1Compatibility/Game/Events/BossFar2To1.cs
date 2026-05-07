namespace CloneDash.Game.Events;

public class BossFar2To1(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2To1();
	}
}
