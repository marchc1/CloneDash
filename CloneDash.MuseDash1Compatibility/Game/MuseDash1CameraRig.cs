using CloneDash.Common.Game;
using Nucleus;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game;

public class MuseDash1CameraRig : ICameraRig, IRhythmGameComponent, IRhythmGameThinker
{
	const float MASH_SIZE = 0.84f;
	const float MASH_X = -1.08f;
	const float SHAKE_OFFSET_X = -0.13f;
	const float SHAKE_OFFSET_Y = 0.1f;
	const float SHAKE_SIZE = 0.813f;
	const float SHAKE_STEP_TIME = 0.02f;
	const float VIEW_TIME = 0.083f;

	static readonly float[] ShakeSteps = [1, -0.1f, 0.3f, 0];

	readonly RhythmGameLevel level;

	Vector2F shakeFrom;
	Vector2F shakeOffset;
	int shakeStep = -1;
	float shakeTime;
	CameraTween size = new(1);
	CameraTween x = new(0);

	public MuseDash1CameraRig(GameEventManager events, RhythmGameLevel level) {
		this.level = level;

		events.Listen<MashEndGameEvent>(ZoomOut);
		events.Listen<MashHitGameEvent>(Shake);
		events.Listen<MashStartGameEvent>(ZoomIn);
	}

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public void CalcView2D(FrameState frameState, ref Camera2D cam) {
		var position = GetPosition();
		cam.Zoom = ((frameState.WindowHeight / 900) * 120) / size.Value;
		cam.Rotation = 0.0f;
		cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
		cam.Target = new(position.X, position.Y);

		//cam.Offset = new(frameState.WindowWidth * Game.Pathway.PATHWAY_LEFT_PERCENTAGE * .5f, frameState.WindowHeight * 0.5f);
		//cam.Target = cam.Offset;
	}

	public void Reset() {
		size.Set(1);
		x.Set(0);
		shakeStep = -1;
	}

	public void Think(FrameState frameState) {
		if (level.Paused)
			return;

		float dt = (float)globals.CurTimeDelta;
		size.Advance(dt);
		x.Advance(dt);
		if (shakeStep < 0)
			return;

		shakeTime += dt;
		while (shakeStep >= 0 && shakeTime >= SHAKE_STEP_TIME) {
			shakeTime -= SHAKE_STEP_TIME;
			shakeFrom = GetShakeTarget(shakeStep);
			if (++shakeStep >= ShakeSteps.Length)
				CompleteShake();
		}
	}

	void CompleteShake() {
		shakeStep = -1;
		x.Snap(MASH_X);
		size.Snap(SHAKE_SIZE);
	}

	Vector2F GetPosition() {
		if (shakeStep < 0)
			return new(x.Value, 0);

		float t = NMath.Ease.OutQuad(shakeTime / SHAKE_STEP_TIME);
		var target = GetShakeTarget(shakeStep);
		return new(shakeFrom.X + (target.X - shakeFrom.X) * t, shakeFrom.Y + (target.Y - shakeFrom.Y) * t);
	}

	Vector2F GetShakeTarget(int step) => new(MASH_X + shakeOffset.X * ShakeSteps[step], shakeOffset.Y * ShakeSteps[step]);

	void Shake(ref MashHitGameEvent ev) {
		if (level.IsSeeking)
			return;

		if (shakeStep >= 0)
			CompleteShake();

		shakeFrom = new(x.Value, 0);
		shakeOffset = new(SHAKE_OFFSET_X, (Random.Shared.NextSingle() * 2 - 1) * SHAKE_OFFSET_Y);
		shakeStep = 0;
		shakeTime = 0;
	}

	void ZoomIn(ref MashStartGameEvent ev) {
		if (level.IsSeeking)
			return;

		size.Start(MASH_SIZE, VIEW_TIME);
		x.Set(0);
		x.Start(MASH_X, VIEW_TIME);
	}

	void ZoomOut(ref MashEndGameEvent ev) {
		if (level.IsSeeking)
			return;

		if (shakeStep >= 0)
			CompleteShake();

		size.Set(MASH_SIZE);
		size.Start(1, VIEW_TIME);
		x.Set(MASH_X);
		x.Start(0, VIEW_TIME);
	}

	struct CameraTween(float value)
	{
		float current = value;
		float duration;
		float from = value;
		float time;
		float to = value;

		public readonly float Value => current;

		public void Advance(float dt) {
			if (time >= duration)
				return;

			time = Math.Min(time + dt, duration);
			current = from + (to - from) * NMath.Ease.OutQuad(time / duration);
		}

		public void Set(float value) {
			current = from = to = value;
			duration = time = 0;
		}

		public void Snap(float value) => current = value;

		public void Start(float value, float length) {
			from = current;
			to = value;
			duration = length;
			time = 0;
		}
	}
}
