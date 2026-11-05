using CloneDash.Common.Game;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelPlayerVisualsComponent(Conductor conductor, UnbeatableWhiteLabelJudgeComponent judge, UnbeatableWhiteLabelPlayerComponent player, UnbeatableWhiteLabelStageComponent stage) : IRhythmGameComponent, IRhythmGameThinker
{
	const double HURT_TIME = 0.1;
	const double IDLE_RESET_TIME = 0.25;
	const double INTRO_TIME = -1;

	static readonly string[] HitAttacks = ["BeatCombatAttack05", "BeatCombatAttack01", "BeatCombatAttack02", "BeatCombatAttack03", "BeatCombatAttack08", "BeatCombatAttack09", "BeatCombatAttack10"];
	static readonly string[] HurtAnimations = ["BeatCombatHurt01", "BeatCombatHurt02", "BeatCombatHurt03"];
	static readonly string[] SwingAttacks = ["BeatCombatAttack01", "BeatCombatAttack02", "BeatCombatAttack03", "BeatCombatAttack04", "BeatCombatAttack05", "BeatCombatAttack06", "BeatCombatAttack07"];

	int attackVariation;
	UnbeatableWhiteLabelAnimatorState? beat;
	bool holdingLow;
	bool holdingTop;
	bool hurt;
	double idleResetTimer;
	bool introIdle = true;
	int jumpVariation;
	double lastTime = double.NaN;
	UnbeatableWhiteLabelAnimatorState? quaver;
	int quaverAnimationVersion;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public void Reset() {
		attackVariation = 0;
		beat?.Reset();
		holdingLow = false;
		holdingTop = false;
		hurt = false;
		idleResetTimer = 0;
		introIdle = true;
		jumpVariation = 0;
		lastTime = double.NaN;
		quaver?.Reset();
		quaverAnimationVersion = 0;
	}

	public void Think(FrameState frameState) {
		double time = conductor.Time;
		double deltaTime = double.IsNaN(lastTime) ? 0 : Math.Max(0, time - lastTime);
		lastTime = time;

		beat ??= new(UnbeatableWhiteLabelStageComponent.Find(stage.Player, "Beat")?.GetComponent<SceneAnimator>());
		quaver ??= new(UnbeatableWhiteLabelStageComponent.Find(stage.Assist, "Quaver")?.GetComponent<SceneAnimator>());

		UpdateBeat(beat, time, deltaTime);
		UpdateQuaver(quaver);
	}

	void UpdateBeat(UnbeatableWhiteLabelAnimatorState beat, double time, double deltaTime) {
		var state = player.State;
		bool holdingAny = state == UnbeatableWhiteLabelPlayerState.HoldingTop || state == UnbeatableWhiteLabelPlayerState.HoldingLow;

		if (holdingLow && state != UnbeatableWhiteLabelPlayerState.HoldingLow)
			holdingLow = false;
		if (holdingTop && state != UnbeatableWhiteLabelPlayerState.HoldingTop)
			holdingTop = false;

		if (judge.Position - judge.LastMissTime < HURT_TIME)
			hurt = true;
		else if (hurt) {
			attackVariation++;
			hurt = false;
		}
		else {
			if (state != UnbeatableWhiteLabelPlayerState.AtRest) {
				introIdle = false;
				idleResetTimer = IDLE_RESET_TIME;
			}

			if (state == UnbeatableWhiteLabelPlayerState.HoldingTop) {
				if (!holdingTop) {
					beat.Play("BeatCombatAirBlockIntro");
					holdingTop = true;
				}
			}
			else if (state == UnbeatableWhiteLabelPlayerState.HoldingLow) {
				if (!holdingLow) {
					beat.Play("BeatCombatGroundBlockIntro");
					holdingLow = true;
				}
			}
			else {
				if (state == UnbeatableWhiteLabelPlayerState.AtRest && !introIdle) {
					if (idleResetTimer <= 0)
						beat.Play("BeatCombatIdle01");
					else
						idleResetTimer -= deltaTime;
				}

				if (player.DidHitLastFrame(UnbeatableWhiteLabelPathways.Top) || (player.FrameWasHeld && !player.FrameHeldAny)) {
					beat.Play(HitAttacks[attackVariation % HitAttacks.Length]);
					attackVariation++;
				}
				else if (judge.PressedLow) {
					if (player.Height == UnbeatableWhiteLabelPathways.Low && player.FramePreviousHeight != UnbeatableWhiteLabelPathways.Low) {
						if (attackVariation % 2 == 0) {
							beat.Play("BeatCombatAttack04");
							if (attackVariation % 7 == 3)
								attackVariation++;
						}
						else {
							beat.Play("BeatCombatAttack02");
							if (attackVariation % 7 == 1)
								attackVariation++;
						}
					}
					else {
						beat.Play(SwingAttacks[attackVariation % SwingAttacks.Length]);
						attackVariation++;
					}
				}
			}

			if (!holdingAny) {
				bool landed = player.Height == UnbeatableWhiteLabelPathways.Low && player.FramePreviousHeight != UnbeatableWhiteLabelPathways.Low;
				if (state == UnbeatableWhiteLabelPlayerState.Airborne && player.Height != UnbeatableWhiteLabelPathways.Low && player.FramePreviousHeight == UnbeatableWhiteLabelPathways.Low && !player.DidHitLastFrame(UnbeatableWhiteLabelPathways.Top))
					beat.Play(jumpVariation % 2 == 0 ? "BeatCombatJump01Up" : "BeatCombatJump02Up");
				if (!judge.PressedLow && landed) {
					beat.Play(jumpVariation % 2 == 0 ? "BeatCombatJump01Land" : "BeatCombatJump02Land");
					jumpVariation++;
				}
			}
		}

		if (hurt && !holdingAny)
			beat.Play(HurtAnimations[attackVariation % HurtAnimations.Length]);

		if (introIdle && time >= INTRO_TIME) {
			introIdle = false;
			beat.Play("BeatCombatIntro");
		}
	}

	void UpdateQuaver(UnbeatableWhiteLabelAnimatorState quaver) {
		var assist = player.Quaver;
		if (assist.AnimationVersion == quaverAnimationVersion || assist.Animation == null)
			return;

		quaver.Play(assist.Animation, true);
		quaverAnimationVersion = assist.AnimationVersion;
	}
}
