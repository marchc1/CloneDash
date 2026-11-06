using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game.Events;

public class SceneChange(DashEventComponents components, int sceneArrayIdx) : DashEvent(components)
{
	public readonly int ArrayIdx = sceneArrayIdx;
	bool shouldDebug;
	public override void Activate() {
		base.Activate();
		shouldDebug = Components.Scenes.SetScene(ArrayIdx);
	}
	public override string ToString() {
		return $"SceneChange -> {ArrayIdx}";
	}
	public override bool ShouldDebug() {
		return shouldDebug;
	}
}
