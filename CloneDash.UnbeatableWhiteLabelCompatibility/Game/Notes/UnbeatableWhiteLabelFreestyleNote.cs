using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelFreestyleNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	readonly List<UnbeatableWhiteLabelFreestyleNote> children = [];
	int hitChildren;
	UnbeatableWhiteLabelJudgement worst;

	public IReadOnlyList<UnbeatableWhiteLabelFreestyleNote> Children => children;
	public UnbeatableWhiteLabelFreestyleNote? NextChild => hitChildren < children.Count ? children[hitChildren] : null;
	public UnbeatableWhiteLabelFreestyleNote? Parent { get; private set; }
	public bool Stunned { get; private set; }
	public override bool Travelling => !Stunned;

	public void AddChild(UnbeatableWhiteLabelFreestyleNote child) {
		children.Add(child);
		child.Parent = this;
	}

	public override double? GetAutoplayTime() => Dead || Parent != null ? null : Stunned ? NextChild?.TargetTime : TargetTime;

	public override void Judge() {
		if (Dead)
			return;

		if (Parent != null)
			JudgeChild();
		else if (Stunned)
			JudgeStunned();
		else if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			Hit();
			if (children.Count == 0) {
				CountHit(Judged(false));
				return;
			}

			worst = Judged(false);
			Stunned = true;
		}
		else if (PastHitRange()) {
			CountMiss(Judged(true));
			DestroyGroup(this);
		}
	}

	public override void OnReset() {
		base.OnReset();
		hitChildren = 0;
		worst = default;
		Stunned = false;
	}

	static void DestroyGroup(UnbeatableWhiteLabelFreestyleNote leader) {
		foreach (var child in leader.children)
			child.Destroyed = true;
		leader.Destroyed = true;
	}

	void JudgeChild() {
		if (Parent!.Dead || Parent.Destroyed) {
			Destroyed = true;
			return;
		}

		if (!PastHitRange())
			return;

		CountMiss(Judged(true));
		DestroyGroup(Parent);
	}

	void JudgeStunned() {
		var child = NextChild;

		if (child != null && child.WithinHitRange()) {
			if (!IsSwungAt())
				return;

			var judge = Components.Judge;
			UnbeatableWhiteLabelJudgement judgement = new((judge.Position - child.HitTime) * 0.66 + child.HitTime, child.HitTime, false);
			worst = UnbeatableWhiteLabelJudgement.Worst(worst, judgement);
			child.Destroyed = true;
			hitChildren++;

			if (NextChild == null)
				CountHit(worst);
			else
				Hit();
		}
		else if (child == null || child.PastHitRange()) {
			CountMiss(Judged(true));
			DestroyGroup(this);
		}
	}
}
