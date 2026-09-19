using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SceneChange(MuseDash1Game game, int sceneArrayIdx) : DashEvent(game)
{
	public readonly int ArrayIdx = sceneArrayIdx;
	bool shouldDebug;
	public override void Activate() {
		base.Activate();
		shouldDebug = Game.SetScene(ArrayIdx);
	}
	public override string ToString() {
		return $"SceneChange -> {ArrayIdx}";
	}
	public override bool ShouldDebug() {
		return shouldDebug;
	}
}
