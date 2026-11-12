using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelDefaultNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : UnbeatableWhiteLabelNote(info, components)
{
	public override void Judge() {
		if (Dead)
			return;

		if (WithinHitRange()) {
			if (!IsSwungAt())
				return;

			CountHit(Judged(false));
		}
		else if (PastHitRange() && !IsPast) {
			CountMiss(Judged(true));
			if (PlayerCanHit())
				Destroyed = true;
			IsPast = true;
		}
	}
}
