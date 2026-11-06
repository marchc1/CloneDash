using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelDoubleNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	const double LATE_SECOND_HIT = 0.05;

	UnbeatableWhiteLabelJudgement first;

	public double EndTime { get; } = info.GetEndTime() / 1000d;
	public bool Stunned { get; private set; }
	public override bool Travelling => !Stunned;

	public override void Judge() {
		if (Dead)
			return;

		if (Stunned)
			JudgeStunned();
		else if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			Hit();
			first = Judged(false);
			TargetTime = EndTime;
			CurrentPathway = UnbeatableWhiteLabelPathways.Opposite(CurrentPathway);
			Stunned = true;
		}
		else if (PastHitRange() && !IsPast) {
			CountMiss(Judged(true));
			if (PlayerCanHit())
				Destroyed = true;
			IsPast = true;
		}
	}

	public override void OnReset() {
		base.OnReset();
		first = default;
		Stunned = false;
	}

	void JudgeStunned() {
		var judge = Components.Judge;

		if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			Hit();
			UnbeatableWhiteLabelJudgement second = new((judge.Position - EndTime) * 0.66 + EndTime, EndTime, false);
			CountHit(UnbeatableWhiteLabelJudgement.Worst(first, second), InitialPathway);
		}
		else if (PastHitRange(TargetTime + LATE_SECOND_HIT)) {
			CountMiss(Judged(true));
			Destroyed = true;
		}
		else if (IsSwungAt()) {
			Hit();
			CountHit(Judged(false) with { TierOverride = 1, BonusOverride = 0 }, InitialPathway);
		}
	}
}
