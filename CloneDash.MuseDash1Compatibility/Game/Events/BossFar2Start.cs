namespace CloneDash.Game.Events;

public class BossFar2Start(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2Start();
	}
}
