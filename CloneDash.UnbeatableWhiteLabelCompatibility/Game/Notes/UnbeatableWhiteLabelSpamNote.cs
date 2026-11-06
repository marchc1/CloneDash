using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelSpamNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	UnbeatableWhiteLabelJudgement start;

	public double EndTime { get; } = info.GetEndTime() / 1000d;
	public int Swings { get; private set; }
	public bool Stunned { get; private set; }
	public override bool Travelling => !Stunned;

	public override double? GetAutoplayTime() => Stunned ? null : base.GetAutoplayTime();

	public override void Judge() {
		if (Dead)
			return;

		if (Stunned)
			JudgeStunned();
		else if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			Hit();
			Swings++;
			start = Judged(false);
			TargetTime = EndTime;
			Stunned = true;
		}
		else if (PastHitRange()) {
			CountMiss(Judged(true));
			Destroyed = true;
		}
	}

	public override void OnReset() {
		base.OnReset();
		start = default;
		Swings = 0;
		Stunned = false;
	}

	void JudgeStunned() {
		var judge = Components.Judge;

		if (judge.Position >= EndTime) {
			CountHit(start);
			return;
		}

		TargetTime = judge.Position;
		if (!IsSwungAt())
			return;

		Hit();
		Swings++;
		if (WithinHitRange(EndTime))
			CountHit(start);
	}
}
