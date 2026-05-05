using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SpeedChange(DashGameLevel game, PathwaySide side, int speed) : DashEvent(game){
	public PathwaySide Side => side;
	public int Speed => speed;

	public override void Activate() {
		base.Activate();
		Game.SetPathwaySpeed(Side, Speed);
	}
}
