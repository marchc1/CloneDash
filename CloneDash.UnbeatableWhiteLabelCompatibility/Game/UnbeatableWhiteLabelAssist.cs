namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelAssist : UnbeatableWhiteLabelCharacter
{
	const float FADE_IN_TIME = 0.15f;
	const float FADE_OUT_TIME = 0.3f;

	int attackVariation;

	public string? Animation { get; private set; }
	public int AnimationVersion { get; private set; }

	public void LowAttack() {
		FadeTo(1, FADE_IN_TIME);
		if (HeldLow) {
			PlayAnimation("QuaverCombatAirBlockLoop");
			Hold(UnbeatableWhiteLabelPathways.Low);
			return;
		}

		PlayAttack();
		Attack();
	}

	public override void Reset(float alpha) {
		base.Reset(alpha);
		attackVariation = 0;
		Animation = null;
		AnimationVersion = 0;
	}

	public void TopAttack() {
		FadeTo(1, FADE_IN_TIME);
		if (HeldTop) {
			PlayAnimation("QuaverCombatAirBlockLoop");
			Hold(UnbeatableWhiteLabelPathways.Top);
			return;
		}

		PlayAttack();
		Jump();
	}

	public void Update(float deltaTime) {
		switch (State) {
			case UnbeatableWhiteLabelPlayerState.AtRest:
				if (SideChanged) {
					MoveToRest(0.02f);
					SideChanged = false;
				}
				if (StateTimer >= 0) {
					StateTimer -= deltaTime;
					if (StateTimer <= 0) {
						MoveToRest(0.03f);
						FadeTo(0, FADE_OUT_TIME);
					}
				}
				break;
			case UnbeatableWhiteLabelPlayerState.Airborne:
			case UnbeatableWhiteLabelPlayerState.AttackingWide:
				UpdateFalling(deltaTime);
				break;
			case UnbeatableWhiteLabelPlayerState.Attacking:
				if (StateTimer > 0) {
					StateTimer -= deltaTime;
					if (StateTimer <= 0)
						SetState(UnbeatableWhiteLabelPlayerState.AtRest, 0.3f);
				}
				break;
			case UnbeatableWhiteLabelPlayerState.HoldingTop:
				if (!HeldTop && !HeldLow) {
					PlayAttack();
					SetState(UnbeatableWhiteLabelPlayerState.Airborne);
					FallSpeed = JUMP_SPEED / 2;
					FadeTo(0, FADE_OUT_TIME);
				}
				break;
			case UnbeatableWhiteLabelPlayerState.HoldingLow:
				if (!HeldLow && !HeldTop) {
					PlayAttack();
					SetState(UnbeatableWhiteLabelPlayerState.AtRest, 0.3f);
				}
				break;
		}
	}

	void PlayAnimation(string animation) {
		Animation = animation;
		AnimationVersion++;
	}

	void PlayAttack() {
		PlayAnimation(attackVariation % 2 == 0 ? "QuaverCombatAttack01" : "QuaverCombatAttack02");
		attackVariation++;
	}
}
