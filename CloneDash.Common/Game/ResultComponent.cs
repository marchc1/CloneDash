using Nucleus.Types;

namespace CloneDash.Common.Game;

public class ResultComponent : IRhythmGameComponent, IRhythmGameThinker
{
	public Func<bool> CanClear = () => true;
	public Func<bool> IsFailed = () => false;

	readonly GameEventManager events;
	readonly RhythmGameLevel level;
	readonly MusicComponent music;

	double DeathTime = -2000000;

	public ResultComponent(RhythmGameLevel level, MusicComponent music, GameEventManager events) {
		this.level = level;
		this.music = music;
		this.events = events;
		events.Listen<PlayerDeathGameEvent>(MarkDeath);
	}

	public bool Cleared { get; private set; }

	public bool Failed { get; private set; }

	public bool LastNoteHit { get; private set; } = false;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Input;

	public void MarkLastNoteHit() => LastNoteHit = true;

	public void Reset() {
		Cleared = false;
		Failed = false;
		LastNoteHit = false;
		DeathTime = -2000000d;
	}

	public void Think(FrameState frameState) {
		if (CanClear() && TryClear()) {
			level.EndFrame();
			return;
		}

		if (TryFail(IsFailed()))
			level.EndFrame();
	}

	public bool TryClear() {
		// A failed song can't also clear (e.g. it ends during the fail fade-in)
		if (Cleared || IsFailed() || !LastNoteHit || !music.IsValid() || !music.IsComplete)
			return false;

		Cleared = true;
		events.Fire(new SongClearedGameEvent());
		return true;
	}

	public bool TryFail(bool dead) {
		if (!dead)
			return false;

		if ((level.Curtime - DeathTime) > 5) {
			if (!Failed) {
				Failed = true;
				events.Fire(new SongFailedGameEvent());
			}
			// always return true if curtime - deathtime > 5
			return true;
		}
		return false;
	}

	void MarkDeath(ref PlayerDeathGameEvent ev) {
		DeathTime = level.Curtime;
	}
}
