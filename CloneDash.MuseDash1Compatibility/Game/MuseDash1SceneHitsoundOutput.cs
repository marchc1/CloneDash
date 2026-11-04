using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game;

public class MuseDash1SceneHitsoundOutput(MuseDash1Game game) : IHitsoundOutput
{
	public void Play(HitsoundCue cue, int hits) {
		if (cue.TryGetEnum(out SceneSound sound) && game.HasActiveScene(out var scene))
			scene.PlaySound(sound, hits);
	}
}
