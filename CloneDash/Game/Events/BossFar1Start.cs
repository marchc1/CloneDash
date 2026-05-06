namespace CloneDash.Game.Events;

public class BossFar1Start(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1Start();
	}
}
