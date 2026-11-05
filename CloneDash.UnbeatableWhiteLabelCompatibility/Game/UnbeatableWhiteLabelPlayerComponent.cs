using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelPlayerComponent : IRhythmGameComponent
{
	const float TWIN_LENIENCY = 0.05f;

	readonly HashSet<PathwayId> frameHitPathways = [];
	readonly HashSet<PathwayId> hitPathways = [];
	bool lowAttackFirst = true;
	PathwayId twinPathway;
	UnbeatableWhiteLabelTwinKind twinKind;
	float twinTimer = -1;

	public UnbeatableWhiteLabelBeat Beat { get; } = new();
	public bool CameraCentered { get; set; }
	public bool FrameHeldAny { get; private set; }
	public PathwayId FramePreviousHeight { get; private set; } = UnbeatableWhiteLabelPathways.Low;
	public bool FrameWasHeld { get; private set; }
	public UnbeatableWhiteLabelHoldNote? HeldNote => Beat.HeldNote;
	public PathwayId Height => Beat.Height;
	public PathwayId PreviousHeight { get; private set; } = UnbeatableWhiteLabelPathways.Low;
	public UnbeatableWhiteLabelAssist Quaver { get; } = new();
	public UnbeatableWhiteLabelHoldNote? SecondHeldNote => Quaver.HeldNote;
	public Side Side => Beat.Side;
	public UnbeatableWhiteLabelPlayerState State => Beat.State;

	public void BeginFrame(UnbeatableWhiteLabelJudgeComponent judge) {
		if (judge.PressedLow)
			Beat.Height = UnbeatableWhiteLabelPathways.Low;
		else if (judge.PressedTop)
			Beat.Height = UnbeatableWhiteLabelPathways.Top;
	}

	public void ChangeSide(Side side) {
		Beat.ChangeSide(side);
		Quaver.ChangeSide(side);
	}

	public bool DidHitAt(PathwayId pathway) => hitPathways.Contains(pathway);

	public bool DidHitLastFrame(PathwayId pathway) => frameHitPathways.Contains(pathway);

	public void EndFrame() {
		frameHitPathways.Clear();
		frameHitPathways.UnionWith(hitPathways);
		FramePreviousHeight = PreviousHeight;
		FrameWasHeld = FrameHeldAny;
		FrameHeldAny = Beat.HeldAny;

		PreviousHeight = Beat.Height;
		hitPathways.Clear();
	}

	public void MarkHit(PathwayId pathway) => hitPathways.Add(pathway);

	public void Reset() {
		frameHitPathways.Clear();
		hitPathways.Clear();
		lowAttackFirst = true;
		ResetTwinTimer();
		Beat.Reset(1);
		CameraCentered = false;
		FrameHeldAny = false;
		FramePreviousHeight = UnbeatableWhiteLabelPathways.Low;
		FrameWasHeld = false;
		PreviousHeight = UnbeatableWhiteLabelPathways.Low;
		Quaver.Reset(0);
	}

	public void StartHold(UnbeatableWhiteLabelHoldNote note) {
		if (!Beat.HeldAny)
			Beat.HeldNote = note;
		else
			Quaver.HeldNote = note;
	}

	public void StopHold(UnbeatableWhiteLabelHoldNote note) {
		if (Beat.HeldAny && Beat.HeldNote == note)
			Beat.HeldNote = null;
		else if (Quaver.HeldAny && Quaver.HeldNote == note)
			Quaver.HeldNote = null;
	}

	public void UpdateFrame(UnbeatableWhiteLabelJudgeComponent judge, float deltaTime) {
		if (twinTimer > 0) {
			twinTimer -= deltaTime;
			if (twinTimer <= 0) {
				if (twinKind == UnbeatableWhiteLabelTwinKind.Twin) {
					if (twinPathway == UnbeatableWhiteLabelPathways.Top)
						Beat.Jump();
					else if (twinPathway == UnbeatableWhiteLabelPathways.Low)
						Beat.Attack();
				}
				else if (twinKind == UnbeatableWhiteLabelTwinKind.Hold)
					Beat.Hold(twinPathway);
				ResetTwinTimer();
			}
		}

		Beat.Update(this, judge, deltaTime);
		Quaver.Update(deltaTime);
		Beat.UpdateMotion(deltaTime);
		Quaver.UpdateMotion(deltaTime);
	}

	internal void CheckAttack(UnbeatableWhiteLabelJudgeComponent judge, bool allowEmptyAttack, bool allowEmptyJump) {
		if (lowAttackFirst) {
			CheckLowAttack(judge, allowEmptyAttack);
			CheckTopAttack(judge, allowEmptyJump);
		}
		else {
			CheckTopAttack(judge, allowEmptyJump);
			CheckLowAttack(judge, allowEmptyAttack);
		}
	}

	internal bool CheckLowAttack(UnbeatableWhiteLabelJudgeComponent judge, bool allowEmptyAttack) {
		if (!judge.PressedLow)
			return false;

		switch (judge.UpcomingTwins()) {
			case UnbeatableWhiteLabelTwinKind.Twin:
				if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Top) {
					Beat.AttackWide();
					ResetTwinTimer();
				}
				else
					SetTwinTimer(UnbeatableWhiteLabelPathways.Low, UnbeatableWhiteLabelTwinKind.Twin);
				return true;
			case UnbeatableWhiteLabelTwinKind.Hold:
				if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Top) {
					if (Beat.HeldNote != null && Beat.HeldNote.CurrentPathway != UnbeatableWhiteLabelPathways.Low)
						(Beat.HeldNote, Quaver.HeldNote) = (Quaver.HeldNote, Beat.HeldNote);
					Beat.Hold(UnbeatableWhiteLabelPathways.Low);
					Quaver.TopAttack();
					lowAttackFirst = !lowAttackFirst;
					ResetTwinTimer();
				}
				else
					SetTwinTimer(UnbeatableWhiteLabelPathways.Low, UnbeatableWhiteLabelTwinKind.Hold);
				return true;
		}

		if (Quaver.HeldLow)
			Quaver.LowAttack();
		if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Top) {
			Beat.AttackWide();
			ResetTwinTimer();
			return true;
		}
		if (Beat.HeldLow) {
			Beat.Hold(UnbeatableWhiteLabelPathways.Low);
			return true;
		}
		if (DidHitAt(UnbeatableWhiteLabelPathways.Low) || allowEmptyAttack) {
			Beat.Attack();
			return true;
		}
		return false;
	}

	internal bool CheckTopAttack(UnbeatableWhiteLabelJudgeComponent judge, bool allowEmptyJump) {
		if (!judge.PressedTop)
			return false;

		switch (judge.UpcomingTwins()) {
			case UnbeatableWhiteLabelTwinKind.Twin:
				if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Low) {
					Beat.AttackWide();
					ResetTwinTimer();
				}
				else
					SetTwinTimer(UnbeatableWhiteLabelPathways.Top, UnbeatableWhiteLabelTwinKind.Twin);
				return true;
			case UnbeatableWhiteLabelTwinKind.Hold:
				if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Low) {
					if (Beat.HeldNote != null && Beat.HeldNote.CurrentPathway != UnbeatableWhiteLabelPathways.Top)
						(Beat.HeldNote, Quaver.HeldNote) = (Quaver.HeldNote, Beat.HeldNote);
					Beat.Hold(UnbeatableWhiteLabelPathways.Top);
					Quaver.LowAttack();
					lowAttackFirst = !lowAttackFirst;
					ResetTwinTimer();
				}
				else
					SetTwinTimer(UnbeatableWhiteLabelPathways.Top, UnbeatableWhiteLabelTwinKind.Hold);
				return true;
		}

		if (Quaver.HeldTop)
			Quaver.TopAttack();
		if (twinTimer > 0 && twinPathway == UnbeatableWhiteLabelPathways.Low) {
			Beat.AttackWide();
			ResetTwinTimer();
			return true;
		}
		if (Beat.HeldTop) {
			Beat.Hold(UnbeatableWhiteLabelPathways.Top);
			return true;
		}
		if (DidHitAt(UnbeatableWhiteLabelPathways.Top) || allowEmptyJump) {
			Beat.Jump();
			return true;
		}
		return false;
	}

	void ResetTwinTimer() {
		twinPathway = UnbeatableWhiteLabelPathways.Mid;
		twinKind = UnbeatableWhiteLabelTwinKind.None;
		twinTimer = -1;
	}

	void SetTwinTimer(PathwayId pathway, UnbeatableWhiteLabelTwinKind kind) {
		twinPathway = pathway;
		twinKind = kind;
		twinTimer = TWIN_LENIENCY;
	}
}
