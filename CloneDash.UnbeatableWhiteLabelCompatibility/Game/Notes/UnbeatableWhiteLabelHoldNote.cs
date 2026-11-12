using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelHoldNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	const double RELEASE_GRACE = 0.075;

	UnbeatableWhiteLabelJudgement start;
	double releaseGrace = RELEASE_GRACE;

	public double EndTime { get; } = info.GetEndTime() / 1000d;
	public bool Holding { get; private set; }
	public override bool Travelling => !Holding;

	public override double? GetAutoplayTime() => Holding ? null : base.GetAutoplayTime();

	public override void Judge() {
		if (Dead)
			return;

		if (Holding)
			JudgeHolding();
		else if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			Hit();
			Holding = true;
			Components.Player.StartHold(this);
			start = Judged(false);
			TargetTime = EndTime;
		}
		else if (PastHitRange()) {
			CountMiss(Judged(true));
			Destroyed = true;
		}
	}

	public override void OnReset() {
		base.OnReset();
		start = default;
		releaseGrace = RELEASE_GRACE;
		Holding = false;
	}

	void JudgeHolding() {
		var judge = Components.Judge;

		if (judge.IsHeldAt(CurrentPathway)) {
			if (!PastHitRange(EndTime))
				return;

			Components.Player.StopHold(this);
			CountMiss(new(judge.Position, EndTime, true));
			Destroyed = true;
		}
		else if (WithinHitRange(EndTime)) {
			UnbeatableWhiteLabelJudgement end = new((judge.Position - EndTime) * 0.66 + EndTime, EndTime, false);
			Components.Player.StopHold(this);
			Holding = false;
			CountHit(UnbeatableWhiteLabelJudgement.Worst(start, end));
		}
		else if (releaseGrace > 0)
			releaseGrace -= judge.DeltaTime;
		else {
			Components.Player.StopHold(this);
			CountMiss(new(judge.Position, EndTime, true));
			Destroyed = true;
		}
	}
}
