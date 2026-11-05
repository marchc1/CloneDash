using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Scenes;

namespace CloneDash.Game;

public class MuseDash1SceneHitsoundOutput(MuseDash1ScenesComponent scenes) : IHitsoundOutput
{
	public void Play(HitsoundCue cue, int hits) {
		if (cue.TryGetEnum(out SceneSound sound) && scenes.HasActiveScene(out var scene))
			scene.PlaySound(sound, hits);
	}
}
