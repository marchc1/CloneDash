using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelDodgeNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	public override bool Killable => false;

	public override void Judge() {
		var player = Components.Player;

		if (WithinHitRange() && PlayerDodged()) {
			if (IsPast)
				return;

			Components.Judge.Dodge(CurrentPathway);
			CountHitQuiet();
			IsPast = true;
			return;
		}

		if (!PastHitRange())
			return;

		if (player.Height == CurrentPathway) {
			if (IsPast)
				return;

			CountMiss(Judged(true));
			Destroyed = true;
		}
		else if (!IsPast) {
			CountHitQuiet();
			IsPast = true;
		}
	}

	void CountHitQuiet() => Components.Judge.CountHit(this, Judged(false) with { TierOverride = UnbeatableWhiteLabelJudgement.TIER_COUNT, BonusOverride = 0 });

	bool PlayerDodged() => Components.Player.PreviousHeight == CurrentPathway && Components.Player.Height != CurrentPathway;
}
