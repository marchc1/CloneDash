using CloneDash.Compatibility.Unity;
using System.Numerics;

namespace CloneDash.Scenes;

public class MuseDash1Scrollable(float scrollLength, float anchor, float angle, float speedFactor, bool isGroup, bool enabled) : SceneComponent
{
	public readonly float Anchor = anchor;
	public readonly float Angle = angle;
	public readonly bool Enabled = enabled;
	public readonly bool IsGroup = isGroup;
	public readonly float ScrollLength = scrollLength;
	public readonly float SpeedFactor = speedFactor;

	public float Progress;

	bool cloned;

	Vector2 Direction => new(MathF.Cos(Angle * MathF.PI / 180), MathF.Sin(Angle * MathF.PI / 180));

	public override void Awake() {
		if (!IsGroup || cloned)
			return;

		cloned = true;
		Transform.GetWorldScale(out float scale, out _);
		var offset = Direction * scale * ScrollLength;
		foreach (var child in Transform.Children.ToArray())
			foreach (int sign in (ReadOnlySpan<int>)[1, -1]) {
				var clone = SceneObject.Instantiate(child.Object, Transform);
				clone.Transform.LocalX = child.LocalX + sign * offset.X;
				clone.Transform.LocalY = child.LocalY + sign * offset.Y;
			}
	}

	public override SceneComponent? Clone() => new MuseDash1Scrollable(ScrollLength, Anchor, Angle, SpeedFactor, IsGroup, Enabled);

	public Vector2 GetOffset() => Enabled ? Direction * ((Anchor - 1 + Progress) * ScrollLength) : Vector2.Zero;
}

public class MuseDash1ScrollableGear : SceneComponent
{
	public readonly List<MuseDash1Scrollable> Scrollables = [];

	public override void Awake() {
		Scrollables.Clear();
		Collect(Object);
	}

	public override SceneComponent? Clone() => new MuseDash1ScrollableGear();

	void Collect(SceneObject obj) {
		if (!obj.Active)
			return;

		Scrollables.AddRange(obj.GetComponents<MuseDash1Scrollable>());
		foreach (var child in obj.Transform.Children)
			Collect(child.Object);
	}
}

public class MuseDash1SceneScrollableControl(float stepPerSecond) : SceneComponent
{
	public readonly float StepPerSecond = stepPerSecond;

	public float SpeedMultiplier = 1;

	MuseDash1ScrollableGear? gear;
	float tick;

	public override void Awake() => gear = Object.GetComponent<MuseDash1ScrollableGear>();

	public override SceneComponent? Clone() => new MuseDash1SceneScrollableControl(StepPerSecond);

	public void ResetScrollProgress() => tick = 0;

	public void Update(float dt) {
		tick += dt;
		if (gear == null || !UnitySimScene.IsActiveInHierarchy(Object))
			return;

		foreach (var scrollable in gear.Scrollables) {
			float step = SpeedMultiplier * StepPerSecond * tick * scrollable.SpeedFactor;
			float progress = Math.Clamp(MathF.Abs(step) % scrollable.ScrollLength / scrollable.ScrollLength, 0, 1) * MathF.Sign(step);
			if (progress < 0)
				progress += 1;
			scrollable.Progress = Math.Clamp(progress, 0, 1);
		}
	}
}

public class MuseDash1OffsetGroup(bool ignoreZ) : SceneComponent
{
	public readonly bool IgnoreZ = ignoreZ;

	Vector3 origin;

	public override void Awake() {
		Transform.GetWorldPosition(out float x, out float y, out float z);
		origin = new(x, y, z);
	}

	public override SceneComponent? Clone() => new MuseDash1OffsetGroup(IgnoreZ);

	public void Update() {
		var offset = Vector2.Zero;
		foreach (var scrollable in Object.GetComponents<MuseDash1Scrollable>())
			offset += scrollable.GetOffset();

		Transform.GetWorldPosition(out _, out _, out float z);
		Transform.SetWorldPosition(origin.X + offset.X, origin.Y + offset.Y, IgnoreZ ? z : origin.Z);
	}
}

public class MuseDash1GCClip(int index, float updateInterval, float clipValue) : SceneComponent
{
	public const float CLIP_STEP = 6.7f;
	public const float MIN_SIZE = 0.268f;
	public const float SIZE_POWER = 20;

	public readonly int Index = index;
	public readonly float UpdateInterval = updateInterval;

	float clip;
	float passTime;
	SceneSpriteRenderer? renderer;
	float shownClip = clipValue;

	public override void Awake() {
		renderer = Object.GetComponent<SceneSpriteRenderer>();
		if (renderer == null)
			return;

		renderer.Color = Vector4.One;
		renderer.VerticalClip = shownClip;
	}

	public override SceneComponent? Clone() => new MuseDash1GCClip(Index, UpdateInterval, shownClip);

	public void Update(float dt, ReadOnlySpan<float> bandBuffer, bool visualizing) {
		if (!visualizing)
			clip = MIN_SIZE;
		else {
			passTime += dt;
			if (passTime >= UpdateInterval) {
				passTime = 0;
				Visualize(bandBuffer);
			}
		}

		shownClip = MathF.Max(clip, MIN_SIZE);
		renderer?.VerticalClip = shownClip;
	}

	static float Snap(float value) => MathF.Round(value * 100 / CLIP_STEP, MidpointRounding.ToEven) * CLIP_STEP / 100;

	void Visualize(ReadOnlySpan<float> bandBuffer) {
		if (Index < 0 || Index >= bandBuffer.Length)
			return;

		float own = bandBuffer[Index] * SIZE_POWER;
		float last = bandBuffer[^1] * SIZE_POWER;
		clip = last < own ? last / own : last > own ? own / last : own;
		clip = Snap(clip);
		clip = Snap(float.Lerp(shownClip, clip, Math.Clamp(UpdateInterval * SIZE_POWER, 0, 1)));
	}
}

public class MuseDash1MikuBossAnimator : SceneComponent
{
	public override SceneComponent? Clone() => new MuseDash1MikuBossAnimator();
}

public class MuseDash1BossActionEffect(long vfxObject, long bombEffect) : SceneComponent
{
	public const double BOMB_AFTER_CHANGE = 0.167;
	public const double BOMB_AFTER_ENTER = 0.67;
	public const double BOMB_AFTER_FAR_ATTACK = 0.33;
	public const string ENTER_STATE = "Play12BossEffect";

	public readonly long BombEffectID = bombEffect;
	public readonly long VfxObjectID = vfxObject;

	SceneObject? bomb;
	double bombAt = double.MaxValue;
	double time;
	SceneObject? vfx;

	public override void Awake() {
		vfx = Object.Scene.FindImportedObject(VfxObjectID);
		bomb = Object.Scene.FindImportedObject(BombEffectID);
	}

	public override SceneComponent? Clone() => new MuseDash1BossActionEffect(VfxObjectID, BombEffectID);

	public void Enter(bool firstEntrance) {
		if (vfx == null)
			return;

		vfx.Active = true;
		if (firstEntrance) {
			foreach (var animator in vfx.GetComponents<SceneAnimator>())
				animator.Play(ENTER_STATE);
			bombAt = double.MaxValue;
		}
		else
			bombAt = time + BOMB_AFTER_ENTER;
	}

	public void ActionChanged(string action) {
		double? delay = action switch {
			"boss_far_atk_2_start" => BOMB_AFTER_FAR_ATTACK,
			"boss_far_atk_1_end" or "boss_far_atk_2_end" or "atk_1_to_2" => BOMB_AFTER_CHANGE,
			_ => null
		};
		if (delay is double seconds)
			bombAt = time + seconds;
	}

	public void Update(float dt) {
		time += dt;
		if (time < bombAt)
			return;

		bombAt = double.MaxValue;
		if (bomb != null)
			Restart(bomb);
	}

	static void Restart(SceneObject obj) {
		foreach (var system in obj.GetComponents<SceneParticleSystem>())
			system.Restart();
		foreach (var child in obj.Transform.Children)
			Restart(child.Object);
	}
}
