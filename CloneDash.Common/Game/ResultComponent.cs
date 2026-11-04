using Nucleus.Engine;

namespace CloneDash.Common.Game;

public class ResultComponent
{
	readonly GameEventManager events;
	readonly Level level;
	readonly MusicComponent music;

	double DeathTime = -2000000;

	public ResultComponent(Level level, MusicComponent music, GameEventManager events) {
		this.level = level;
		this.music = music;
		this.events = events;
		events.Listen<PlayerDeathGameEvent>(MarkDeath);
	}

	public bool Cleared { get; private set; }

	public bool Failed { get; private set; }

	public bool LastNoteHit { get; private set; } = false;

	public void MarkLastNoteHit() => LastNoteHit = true;

	public void Reset() {
		Cleared = false;
		Failed = false;
		LastNoteHit = false;
		DeathTime = -2000000d;
	}

	public bool TryClear() {
		if (Cleared || !LastNoteHit || !music.IsValid() || !music.IsComplete)
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
