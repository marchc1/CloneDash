using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SpeedChange(MuseDash1Game game, PathwaySet pathways, int speed) : DashEvent(game){
	public PathwaySet Pathways => pathways;
	public int Speed => speed;

	public override void Activate() {
		base.Activate();
	}
}
