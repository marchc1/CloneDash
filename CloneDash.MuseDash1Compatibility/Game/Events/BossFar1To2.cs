namespace CloneDash.Game.Events;

public class BossFar1To2(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1To2();
	}
}
