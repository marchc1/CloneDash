using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.MD1_Compat.Compatibility;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1FeverFXComponent : IRhythmGameComponent, IRhythmGamePreRenderer, IRhythmGameThinker
{
	readonly IMuseDash1FeverRuntime? FeverFX;
	readonly HitsoundComponent Hitsounds;
	readonly RhythmGameLevel level;

	public MuseDash1FeverFXComponent(ContentSlot<IMuseDash1FeverRuntime> slot, GameEventManager events, HitsoundComponent hitsounds, RhythmGameLevel level) {
		FeverFX = slot.Value;
		Hitsounds = hitsounds;
		this.level = level;

		events.Listen<FeverStartGameEvent>(EnterFever);
		events.Listen<FeverEndGameEvent>(ExitFever);
	}

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public void Activate() => FeverFX?.Activate();

	public void Load() => FeverFX?.Initialize();

	public void PreRender(FrameState frameState) => FeverFX?.Render();

	public void Reset() => FeverFX?.Reset();

	public void Think(FrameState frameState) => FeverFX?.Think();

	private void EnterFever(ref FeverStartGameEvent ev) {
		if (!level.IsSeeking) {
			FeverFX?.Activate();
			Hitsounds.Play(SceneSound.Fever, 0);
		}
	}

	private void ExitFever(ref FeverEndGameEvent ev) {
		FeverFX?.Cancel();
	}
}