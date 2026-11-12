using CloneDash.Common.Game;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelNoteRules(ComboComponent combo, HealthComponent health) : INoteRules<UnbeatableWhiteLabelNote>, IRhythmGameComponent
{
	const float DAMAGE = 10;
	const float REGAIN = 5;
	const int SWINGS_PER_REGAIN = 5;

	int swingStreak;

	public int MaxCombo { get; private set; }

	public bool CanAffectPlayer(UnbeatableWhiteLabelNote note) => true;

	public void Damage(UnbeatableWhiteLabelNote note) {
		health.Damage(DAMAGE);
	}

	public void Kill(UnbeatableWhiteLabelNote note) {
		if (note.Judgement.Tier <= 1)
			return;

		combo.Add();
		MaxCombo = Math.Max(MaxCombo, combo.Value);
	}

	public void Punish(UnbeatableWhiteLabelNote note) {
		combo.Break();
		swingStreak = 0;
	}

	public void Reset() {
		swingStreak = 0;
		MaxCombo = 0;
	}

	public void Reward(UnbeatableWhiteLabelNote note) { }

	public void Swing() {
		if (swingStreak < SWINGS_PER_REGAIN) {
			swingStreak++;
			return;
		}

		health.Heal(REGAIN);
		swingStreak = 0;
	}
}
