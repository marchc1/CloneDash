using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public enum UnbeatableWhiteLabelPlayerState
{
	AtRest,
	Airborne,
	Attacking,
	AttackingWide,
	HoldingTop,
	HoldingLow
}

public class UnbeatableWhiteLabelCharacter
{
	const float ATTACK_X = 1.2f;
	const float GRAVITY = 60f;
	const float HOLD_X = 1f;
	const float MID_JUMP_Y = UnbeatableWhiteLabelPathways.HEIGHT_OFFSET / 2 - 0.3f;
	const float TOP_JUMP_Y = UnbeatableWhiteLabelPathways.HEIGHT_OFFSET - 0.6f;

	protected const float JUMP_SPEED = 10f;

	float fadeDuration;
	float fadeElapsed;
	float fadeFrom;
	float fadeTo;
	float moveDuration;
	float moveElapsed;
	float moveFromX, moveFromY;
	bool moving;
	float moveToX, moveToY;

	public float Alpha { get; private set; } = 1;
	public UnbeatableWhiteLabelHoldNote? HeldNote { get; set; }
	public PathwayId Height { get; set; } = UnbeatableWhiteLabelPathways.Low;
	public Side Side { get; private set; } = Side.Right;
	public UnbeatableWhiteLabelPlayerState State { get; private set; }
	public float X { get; private set; } = UnbeatableWhiteLabelPathways.PLAYER_REST_X;
	public float Y { get; private set; }

	public bool Airborne => State == UnbeatableWhiteLabelPlayerState.Airborne;
	public bool HeldAny => HeldNote != null && !HeldNote.Dead;
	public bool HeldLow => HeldAny && HeldNote!.CurrentPathway == UnbeatableWhiteLabelPathways.Low;
	public bool HeldTop => HeldAny && HeldNote!.CurrentPathway == UnbeatableWhiteLabelPathways.Top;
	public bool HoldingAny => State == UnbeatableWhiteLabelPlayerState.HoldingTop || State == UnbeatableWhiteLabelPlayerState.HoldingLow;

	protected float FallSpeed { get; set; }
	protected float RestX => Side.Pick(-UnbeatableWhiteLabelPathways.PLAYER_REST_X, UnbeatableWhiteLabelPathways.PLAYER_REST_X);
	protected bool SideChanged { get; set; }
	protected float StateTimer { get; set; }

	public void Attack() {
		if (!Airborne)
			Place(RestX, 0);
		SetState(UnbeatableWhiteLabelPlayerState.Attacking, 0.03f);
		Height = UnbeatableWhiteLabelPathways.Low;
		MoveTo(Side.Pick(-ATTACK_X, ATTACK_X), 0, 0.03f);
	}

	public void AttackWide() {
		FallSpeed = JUMP_SPEED / 2;
		if (!Airborne) {
			SetState(UnbeatableWhiteLabelPlayerState.AttackingWide, 0.025f);
			MoveTo(RestX, MID_JUMP_Y, 0.025f);
		}
		else {
			SetState(UnbeatableWhiteLabelPlayerState.AttackingWide, 0.05f);
			Place(RestX, MID_JUMP_Y);
		}
	}

	public void ChangeSide(Side side) {
		Side = side;
		SideChanged = true;
	}

	public void FadeTo(float alpha, float duration) {
		fadeFrom = Alpha;
		fadeTo = alpha;
		fadeDuration = duration;
		fadeElapsed = 0;
	}

	public void Hold(PathwayId pathway, bool instant = false) {
		if (pathway == UnbeatableWhiteLabelPathways.Top) {
			MoveTo(Side.Pick(-HOLD_X, HOLD_X), TOP_JUMP_Y, instant ? 0 : 0.025f);
			SetState(UnbeatableWhiteLabelPlayerState.HoldingTop);
		}
		else if (pathway == UnbeatableWhiteLabelPathways.Low) {
			Place(RestX, 0);
			MoveTo(Side.Pick(-HOLD_X, HOLD_X), 0, instant ? 0 : 0.03f);
			SetState(UnbeatableWhiteLabelPlayerState.HoldingLow);
		}
	}

	public void Jump() {
		FallSpeed = JUMP_SPEED;
		if (!Airborne) {
			SetState(UnbeatableWhiteLabelPlayerState.Airborne, 0.025f);
			MoveTo(RestX, TOP_JUMP_Y, 0.025f);
		}
		else {
			SetState(UnbeatableWhiteLabelPlayerState.Airborne, 0.05f);
			Place(RestX, TOP_JUMP_Y);
		}
	}

	public virtual void Reset(float alpha) {
		Alpha = alpha;
		fadeDuration = 0;
		FallSpeed = 0;
		HeldNote = null;
		Height = UnbeatableWhiteLabelPathways.Low;
		moving = false;
		Side = Side.Right;
		SideChanged = false;
		State = UnbeatableWhiteLabelPlayerState.AtRest;
		StateTimer = 0;
		X = RestX;
		Y = 0;
	}

	public void UpdateMotion(float deltaTime) {
		if (moving) {
			moveElapsed += deltaTime;
			float t = moveDuration <= 0 ? 1 : Math.Min(1, moveElapsed / moveDuration);
			float eased = 1 - (1 - t) * (1 - t);
			X = moveFromX + (moveToX - moveFromX) * eased;
			Y = moveFromY + (moveToY - moveFromY) * eased;
			moving = t < 1;
		}

		if (fadeDuration > 0) {
			fadeElapsed += deltaTime;
			float t = Math.Min(1, fadeElapsed / fadeDuration);
			Alpha = fadeFrom + (fadeTo - fadeFrom) * (1 - (1 - t) * (1 - t));
			if (t >= 1)
				fadeDuration = 0;
		}
	}

	protected void MoveTo(float x, float y, float duration) {
		moveFromX = X;
		moveFromY = Y;
		moveToX = x;
		moveToY = y;
		moveDuration = duration;
		moveElapsed = 0;
		moving = true;
		if (duration <= 0)
			UpdateMotion(0);
	}

	protected void MoveToRest(float duration) => MoveTo(RestX, 0, duration);

	protected void Place(float x, float y) {
		moving = false;
		X = x;
		Y = y;
	}

	protected void SetState(UnbeatableWhiteLabelPlayerState state, float timer = 0) {
		State = state;
		StateTimer = timer;
	}

	protected void UpdateFalling(float deltaTime) {
		StateTimer += deltaTime;
		if (StateTimer <= 0.05f)
			return;

		Y += FallSpeed * deltaTime;
		FallSpeed -= GRAVITY * deltaTime;
		if (Y > 0)
			return;

		Y = 0;
		SetState(UnbeatableWhiteLabelPlayerState.AtRest);
		Height = UnbeatableWhiteLabelPathways.Low;
	}
}
