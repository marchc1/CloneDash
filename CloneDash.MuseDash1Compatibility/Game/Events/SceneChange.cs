using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SceneChange(MuseDash1Game game, int sceneArrayIdx) : DashEvent(game)
{
	public readonly int ArrayIdx = sceneArrayIdx;
	public override void Activate() {
		base.Activate();
		Game.SetScene(ArrayIdx);
	}
}
