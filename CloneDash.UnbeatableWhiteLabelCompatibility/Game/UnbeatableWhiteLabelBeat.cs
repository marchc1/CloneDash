namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelBeat : UnbeatableWhiteLabelCharacter
{
	public void Update(UnbeatableWhiteLabelPlayerComponent player, UnbeatableWhiteLabelJudgeComponent judge, float deltaTime) {
		var quaver = player.Quaver;

		switch (State) {
			case UnbeatableWhiteLabelPlayerState.AtRest:
				player.CheckAttack(judge, true, true);
				if (SideChanged) {
					MoveToRest(0.02f);
					SideChanged = false;
				}
				if (StateTimer > 0) {
					StateTimer -= deltaTime;
					if (StateTimer <= 0)
						MoveToRest(0.03f);
				}
				if (!HeldAny && quaver.HeldAny) {
					HeldNote = quaver.HeldNote;
					quaver.HeldNote = null;
					Hold(HeldNote!.CurrentPathway, true);
				}
				break;
			case UnbeatableWhiteLabelPlayerState.Airborne:
				player.CheckAttack(judge, true, false);
				UpdateFalling(deltaTime);
				break;
			case UnbeatableWhiteLabelPlayerState.Attacking:
				player.CheckAttack(judge, false, true);
				if (StateTimer > 0) {
					StateTimer -= deltaTime;
					if (StateTimer <= 0)
						SetState(UnbeatableWhiteLabelPlayerState.AtRest, 0.3f);
				}
				break;
			case UnbeatableWhiteLabelPlayerState.AttackingWide:
				player.CheckAttack(judge, false, false);
				UpdateFalling(deltaTime);
				break;
			case UnbeatableWhiteLabelPlayerState.HoldingTop:
				if (!HeldTop) {
					if (!player.CheckLowAttack(judge, true)) {
						SetState(UnbeatableWhiteLabelPlayerState.Airborne);
						FallSpeed = JUMP_SPEED / 2;
					}
				}
				else {
					Hold(UnbeatableWhiteLabelPathways.Top);
					if (judge.PressedLow)
						quaver.LowAttack();
				}
				break;
			case UnbeatableWhiteLabelPlayerState.HoldingLow:
				if (!HeldLow) {
					if (!player.CheckTopAttack(judge, true))
						SetState(UnbeatableWhiteLabelPlayerState.AtRest, 0.3f);
				}
				else {
					Hold(UnbeatableWhiteLabelPathways.Low);
					if (judge.PressedTop)
						quaver.TopAttack();
				}
				break;
		}
	}
}
