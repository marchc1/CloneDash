using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1RulesComponent : IRhythmGameThinker
{
	public readonly ComboComponent Combo;
	public readonly Conductor Conductor;
	public readonly FeverComponent Fever;
	public readonly HealthComponent Health;
	public readonly InputComponent Input;
	public readonly ResultComponent Results;
	public readonly ScoreComponent Score;

	readonly GameEventManager events;
	readonly RhythmGameLevel level;
	readonly MuseDash1GameplayQuirksComponent quirks;

	public MuseDash1RulesComponent(ComboComponent combo, Conductor conductor, GameEventManager events, FeverComponent fever, HealthComponent health, InputComponent input, RhythmGameLevel level, MuseDash1GameplayQuirksComponent quirks, ResultComponent results, ScoreComponent score) {
		Combo = combo;
		Conductor = conductor;
		Fever = fever;
		Health = health;
		Input = input;
		Results = results;
		Score = score;
		this.events = events;
		this.level = level;
		this.quirks = quirks;

		Conductor.CanSeek = () => !IsDead();
		input.CanPoll = () => !IsDead();
		results.IsFailed = () => IsDead();

		events.Listen<PauseRequestGameEvent>(GuardPause);
	}

	ref MuseDash1GameplayQuirks Quirks => ref quirks.Value;

	/// <summary>
	/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
	/// </summary>
	/// <param name="fever"></param>
	public void AddFever(float fever, DashEnemy? responsible = null) {
		if (Fever.InFever) return;

		double feverD = (double)fever;
		if (Quirks.FeverModifier != null) Quirks.FeverModifier.Invoke(ProduceSnapshot(responsible), ref feverD);
		fever = (float)feverD;

		Fever.Add(fever);
	}

	/// <summary>
	/// Adds to the players score.
	/// </summary>
	/// <param name="score"></param>
	public void AddScore(int score, DashEnemy? responsible = null) {
		double s = (double)score;
		if (responsible != null && responsible.Type != 0) s *= Quirks.ScoreMultipliers[responsible.Type.GetBit()];
		if (Quirks.ScoreModifier != null) Quirks.ScoreModifier.Invoke(ProduceSnapshot(responsible), ref s);
		score = (int)(float)s;

		Score.Add((int)(float)s);
		events.Fire(new ScoreChangedGameEvent(Score.Value));
	}

	public bool BreaksAvoids() => Quirks.BreaksAvoids;

	/// <summary>
	/// Damage the player.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="damage"></param>
	public void Damage(float damage, DashEnemy? responsible) {
		if (IsDead() || Fever.InFever && Quirks.InvincibleInFever)
			return;

		double damageD = (double)damage;
		if (Quirks.DamageModifier != null) Quirks.DamageModifier.Invoke(ProduceSnapshot(responsible), ref damageD);
		damage = (float)damageD;
		damage = Math.Max(0, damage);

		if (Quirks.ConsumeFeverBeforeHealth && !Health.Invincible)
			damage -= Fever.Consume(damage);

		bool hurt = Health.Damage(damage);

		Combo.Break();
		events.Fire(new PlayerDamagedGameEvent(hurt));
	}

	public bool IsDead() => Health.Dead && !Quirks.PlayableAfterDeath;

	public bool NeedsToHoldSustains() => !Quirks.AutoHoldsSustains;

	public void Think(FrameState frameState) {
		if (Input.State.ClickCount(MuseDashActions.Fever) > 0 && !level.Paused)
			Fever.TryEnter();

		// Health drain
		if (Quirks.HealthLossPerSecond != null && Conductor.GetTime() >= 0) {
			double loss = 0;
			Quirks.HealthLossPerSecond(ProduceSnapshot(), ref loss);
			Health.Drain(loss * Conductor.TimeDelta);
		}

		if (Quirks.FeverGainPerSecond != null && Conductor.GetTime() >= 0) {
			double gain = 0;
			Quirks.FeverGainPerSecond(ProduceSnapshot(), ref gain);
			Fever.Add((float)(gain * Conductor.TimeDelta));
		}
	}

	/// <summary>
	/// Produces a quirks snapshot for use with the current gameplay quirks
	/// </summary>
	protected MuseDash1GameplayQuirks.GameSnapshot ProduceSnapshot(DashEnemy? enemy = null, MuseDash1GameplayQuirks.Judgement judgement = 0) {
		MuseDash1GameplayQuirks.GameSnapshot snapshot = new() {
			Difficulty = level.GetSongChart().GetRatingNumber(),
			CurrentCombo = Combo.Value,
			CurrentScore = Score.Value,
			InFever = Fever.InFever,
			Health = Health.Value,
			MaxHealth = Health.Max,
			Judgement = judgement
		};

		if (enemy != null) {
			snapshot = snapshot with {
				EntityBlood = enemy.Blood ? 80 : 0,
				EntityType = enemy.Type,
				EntityWarning = enemy.Warns,
			};
		}

		return snapshot;
	}

	private void GuardPause(ref PauseRequestGameEvent ev) {
		if (ev.FromMenu && (Results.LastNoteHit || Conductor.Time < 0))
			ev.Cancel = true;
		if (IsDead())
			ev.Cancel = true;
	}
}
