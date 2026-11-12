namespace CloneDash.Common.Game;

public class HealthComponent(IConductor conductor, GameEventManager events) : IRhythmGameComponent
{
	public double BonusGranted = 1;
	public double BonusThreshold;
	public int BonusTimes;
	public double DyingSeconds;
	public double IFrameLength = 1.25;
	public float Max;

	int BonusesGiven;
	bool DyingUsed;
	double DyingUntil = -200000;
	double LastIFrameGivenTime = -200000;

	public bool Dead { get; private set; }

	public bool Dying => conductor.GetTime() < DyingUntil;

	/// <summary>
	/// Currently in an invincibility frame?
	/// </summary>
	public bool InIFrame => TimeSinceLastIFrame < IFrameLength;

	public bool Invincible => InIFrame || Dying;

	/// <summary>
	/// Time since the last invincibility frame was given
	/// </summary>
	public double TimeSinceLastIFrame => conductor.GetTime() - LastIFrameGivenTime;

	/// <summary>
	/// Current health of the player<br></br>
	/// Default: 250
	/// </summary>
	public float Value { get; private set; }

	public bool Damage(float damage) {
		if (Invincible)
			return false;

		Value = Math.Clamp(Value - damage, 0, Max);
		SetIFrameTime();
		if (Value <= 0)
			TriggerDeath();
		return true;
	}

	/// <summary>
	/// Drains health from the player, triggering death if the player has died, but does not call any other events unlike Damage
	/// </summary>
	/// <param name="damage"></param>
	/// <param name="responsible"></param>
	public void Drain(double damage) {
		if (Dying)
			return;

		Value = Math.Clamp(Value - (float)damage, 0, Max);
		if (Value <= 0)
			TriggerDeath();
	}

	public void Heal(float health) {
		if (BonusesGiven < BonusTimes && Value < BonusThreshold) {
			health = (float)(health * BonusGranted);
			BonusesGiven++;
		}

		if (DyingUsed && !Dying && health > 0)
			DyingUsed = false;

		Value = Math.Clamp(Value + health, 0, Max);
		if (health > 0)
			events.Fire(new PlayerHealedGameEvent(health));
	}

	public void Reset() {
		Value = Max;
		LastIFrameGivenTime = -10000d;
		Dead = false;
		BonusesGiven = 0;
		DyingUsed = false;
		DyingUntil = -200000;
	}

	/// <summary>
	/// Gives the player an invincibility frame. No checks are performed here
	/// </summary>
	public void SetIFrameTime() {
		LastIFrameGivenTime = conductor.GetTime();
	}

	public void SetIFrameTime(double length) {
		LastIFrameGivenTime = Math.Max(LastIFrameGivenTime, conductor.GetTime() + length - IFrameLength);
	}

	void TriggerDeath() {
		if (Dead) return;
		if (Value <= 0) {
			if (DyingSeconds > 0 && !DyingUsed) {
				Value = 1;
				DyingUsed = true;
				DyingUntil = conductor.GetTime() + DyingSeconds;
				return;
			}

			Dead = true;
			events.Fire(new PlayerDeathGameEvent());
		}
	}
}
