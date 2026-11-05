using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SpeedChange(DashEventComponents components, PathwaySet pathways, int speed) : DashEvent(components){
	public PathwaySet Pathways => pathways;
	public int Speed => speed;

	public override void Activate() {
		base.Activate();
	}
}
