using CloneDash.Characters;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.UnbeatableWhiteLabel;
using CloneDash.Game;
using CloneDash.Game.Logic;
using Nucleus;
using Nucleus.Entities;
using Nucleus.Types;

namespace CloneDash.Adapters.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelBeatMuseDash1Character(UnbeatableWhiteLabelBeatModel model, ICharacterDescriptor descriptor, RhythmGameLevel level, Conductor conductor, PathwaysComponent pathways, ISustainManager sustains)
	: ContentAdapter<UnbeatableWhiteLabelBeatModel, IMuseDash1CharacterInstance>(model), IMuseDash1CharacterInstance
{
	public const double JUMP_DURATION = 0.5;
	public const float SCALE = 225;

	const double MAX_DELTA = 0.1;
	const double QUAVER_FADE_TIME = 0.3;

	readonly UnbeatableWhiteLabelBeatIndividual beat = new(conductor, model, false, () => !(sustains.GetSustainState() & pathways.AirSidePathways).IsEmpty);
	readonly UnbeatableWhiteLabelBeatIndividual quaver = new(conductor, model, true, () => false);
	double lastTime = double.NaN;

	public ICharacterDescriptor GetCharacter() => descriptor;
	public double GetJumpDuration() => JUMP_DURATION;
	public IMuseDash1CharacterIndividual GetPrimary() => beat;
	public IMuseDash1CharacterIndividual GetSecondary() => quaver;

	public void Initialize() {
		level.Add(new UnbeatableWhiteLabelBeatEntity(quaver));
		level.Add(new UnbeatableWhiteLabelBeatEntity(beat));
		Logs.Warn("UNBEATABLE: Beat doesnt have a death, big press or transition animation; expect animation oddities");
	}

	public void Reset() {
		lastTime = double.NaN;
		beat.Reset();
		quaver.Reset();
	}

	public void Think() {
		double time = conductor.Time;
		double deltaTime = double.IsNaN(lastTime) ? 0 : Math.Clamp(time - lastTime, 0, MAX_DELTA);
		lastTime = time;

		beat.Update();
		quaver.Update();
		Source.Update(deltaTime);

		double sinceQuaver = time - quaver.LastAnimationTime;
		quaver.Alpha = (float)Math.Clamp(1 - (sinceQuaver - quaver.GetAnimationDuration()) / QUAVER_FADE_TIME, 0, 1);
	}
}

public class UnbeatableWhiteLabelBeatEntity(UnbeatableWhiteLabelBeatIndividual individual) : Entity
{
	public override void Render(FrameState frameState) => individual.Render();
}

public class UnbeatableWhiteLabelBeatIndividual(Conductor conductor, UnbeatableWhiteLabelBeatModel model, bool isQuaver, Func<bool> holdingAir) : IMuseDash1CharacterIndividual
{
	const string AIR_BLOCK = "BeatCombatAirBlockIntro";
	const string AIR_BLOCK_LOOP = "BeatCombatAirBlockLoop";
	const string FALL = "BeatCombatJump01Down";
	const string GROUND_BLOCK = "BeatCombatGroundBlockIntro";
	const string GROUND_BLOCK_LOOP = "BeatCombatGroundBlockLoop";
	const string JUMP = "BeatCombatJump01Up";
	const string QUAVER_BLOCK = "QuaverCombatAirBlockLoop";
	const string QUAVER_IDLE = "QuaverCombatIdle01";
	const string RUN = "BeatCombatRun01";

	static readonly string[] AirAttacks = ["BeatCombatAttack05", "BeatCombatAttack08", "BeatCombatAttack09", "BeatCombatAttack10"];
	static readonly string[] GroundAttacks = ["BeatCombatAttack01", "BeatCombatAttack02", "BeatCombatAttack03", "BeatCombatAttack04", "BeatCombatAttack06", "BeatCombatAttack07"];
	static readonly string[] Hurts = ["BeatCombatHurt01", "BeatCombatHurt02", "BeatCombatHurt03"];
	static readonly string[] QuaverAttacks = ["QuaverCombatAttack01", "QuaverCombatAttack02"];

	bool air;
	int attackVariation;
	double duration;
	bool holding;
	bool returnToRun;
	Vector2F position;
	float scale;
	string? state;

	public float Alpha { get; set; } = 1;
	public double LastAnimationTime { get; private set; } = double.NegativeInfinity;

	public float? GetAnimatedHeight() => null;

	public double GetAnimationDuration() => duration;

	public double GetTimeToAnimationEnd() => Math.Max(0, duration - (conductor.Time - LastAnimationTime));

	public bool IsInAir() => air && conductor.Time - LastAnimationTime < duration;

	public void PlayAnimation(CharacterAnimationType type) {
		if (isQuaver)
			PlayQuaver(type);
		else
			PlayBeat(type);
	}

	public void Render() {
		if (isQuaver) {
			if (Alpha > 0)
				model.RenderQuaver(position, scale * UnbeatableWhiteLabelBeatMuseDash1Character.SCALE, alpha: Alpha);
		}
		else
			model.RenderBeat(position, scale * UnbeatableWhiteLabelBeatMuseDash1Character.SCALE);
	}

	public void Reset() {
		attackVariation = 0;
		LastAnimationTime = double.NegativeInfinity;
		if (isQuaver) {
			Alpha = 0;
			state = QUAVER_IDLE;
			duration = 0;
			model.PlayQuaver(QUAVER_IDLE);
		}
		else
			Play(RUN, 0, false, false, false);
	}

	public void SetPos(Vector2F vector2F) => position = vector2F;

	public void SetScale(Vector2F value) => scale = value.X;

	public void Update() {
		if (isQuaver || state == null)
			return;

		double elapsed = conductor.Time - LastAnimationTime;
		if (state == JUMP && elapsed >= Length(JUMP))
			Swap(FALL);
		if (holding) {
			bool inAir = holdingAir();
			if (inAir && state is GROUND_BLOCK or GROUND_BLOCK_LOOP)
				Swap(AIR_BLOCK_LOOP);
			else if (!inAir && state is AIR_BLOCK or AIR_BLOCK_LOOP)
				Swap(GROUND_BLOCK_LOOP);
		}
		if (returnToRun && !holding && elapsed >= duration)
			Play(RUN, 0, false, false, false);
	}

	string Next(string[] clips) => clips[attackVariation++ % clips.Length];

	double Length(string clip) => isQuaver ? (model.QuaverAnimator?.GetStateLength(clip) ?? 0) : (model.BeatAnimator?.GetStateLength(clip) ?? 0);

	void Play(string clip, double length, bool inAir, bool hold, bool thenRun) {
		state = clip;
		air = inAir;
		holding = hold;
		returnToRun = thenRun;
		duration = length > 0 ? length : Length(clip);
		LastAnimationTime = conductor.Time;
		if (isQuaver)
			model.PlayQuaver(clip);
		else
			model.PlayBeat(clip);
	}

	void PlayBeat(CharacterAnimationType type) {
		switch (type) {
			case CharacterAnimationType.Run:
			case CharacterAnimationType.In:
				Play(RUN, 0, false, false, false);
				break;
			case CharacterAnimationType.Jump:
				Play(JUMP, UnbeatableWhiteLabelBeatMuseDash1Character.JUMP_DURATION, true, false, true);
				break;
			case CharacterAnimationType.JumpHit:
			case CharacterAnimationType.JumpHitGreat:
			case CharacterAnimationType.UpHit:
			case CharacterAnimationType.BigHit:
				Play(Next(AirAttacks), UnbeatableWhiteLabelBeatMuseDash1Character.JUMP_DURATION, true, false, true);
				break;
			case CharacterAnimationType.AttackMiss:
			case CharacterAnimationType.AttackGreat:
			case CharacterAnimationType.AttackPerfect:
			case CharacterAnimationType.DownHit:
				Play(Next(GroundAttacks), 0, false, false, true);
				break;
			case CharacterAnimationType.Hurt:
			case CharacterAnimationType.JumpHurt:
			case CharacterAnimationType.UpPressHurt:
				Play(Next(Hurts), 0, false, false, true);
				break;
			case CharacterAnimationType.Die:
				Play(Hurts[^1], 0, false, false, false);
				break;
			case CharacterAnimationType.UpPressStart:
			case CharacterAnimationType.UpPress:
			case CharacterAnimationType.PressAirToBig:
			case CharacterAnimationType.PressBigToAir:
			case CharacterAnimationType.PressHitToAir:
				Play(AIR_BLOCK, 0, false, true, false);
				break;
			case CharacterAnimationType.Press:
			case CharacterAnimationType.BigPress:
			case CharacterAnimationType.PressGroundToBig:
				Play(holdingAir() ? AIR_BLOCK : GROUND_BLOCK, 0, false, true, false);
				break;
			case CharacterAnimationType.DownPress:
			case CharacterAnimationType.PressBigToGround:
			case CharacterAnimationType.PressHitToGround:
				Play(GROUND_BLOCK, 0, false, true, false);
				break;
			case CharacterAnimationType.UpPressEnd:
				Play(FALL, 0, false, false, true);
				break;
		}
	}

	void PlayQuaver(CharacterAnimationType type) {
		switch (type) {
			case CharacterAnimationType.NotApplicable:
				break;
			case CharacterAnimationType.Press:
			case CharacterAnimationType.DownPress:
			case CharacterAnimationType.UpPressStart:
			case CharacterAnimationType.UpPress:
			case CharacterAnimationType.BigPress:
				Play(QUAVER_BLOCK, 0, false, true, false);
				break;
			case CharacterAnimationType.Jump:
			case CharacterAnimationType.JumpHit:
			case CharacterAnimationType.JumpHitGreat:
			case CharacterAnimationType.UpHit:
				Play(Next(QuaverAttacks), UnbeatableWhiteLabelBeatMuseDash1Character.JUMP_DURATION, true, false, false);
				break;
			default:
				Play(Next(QuaverAttacks), 0, false, false, false);
				break;
		}
	}

	void Swap(string clip) {
		state = clip;
		model.PlayBeat(clip);
	}
}
