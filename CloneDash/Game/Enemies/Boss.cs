using CloneDash.Scenes;

using Nucleus;
using Nucleus.Engine;

using Raylib_cs;

namespace CloneDash.Game.Entities;

public class Boss : DashEnemy
{
	public Boss() : base(EntityType.Boss) {
		Interactivity = EntityInteractivity.Noninteractive;
		Visible = false;
	}
	public override void Initialize() {

	}

	public const int ANIMATION_CHANNEL_MAIN = 0;
	public const int ANIMATION_CHANNEL_FIRE = 1;
	public const int ANIMATION_CHANNEL_FIRE2 = 3;

	public void In() {
		var scene = GetGameLevel().Scene;
		Visible = true;
		scene.PlayBossAnimation(BossAnimationType.In, Animations);
	}
	public void Out() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.Out, Animations);
	}
	public void SingleHit() {

	}
	public void Masher() {

	}
	public void Far1Start() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From0To1, Animations);
	}
	public void Far1End() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From1To0, Animations);
	}
	public void Far1To2() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From1To2, Animations);
	}
	public void Far2Start() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From0To2, Animations);
	}
	public void Far2End() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From2To0, Animations);
	}
	public void Far2To1() {
		var scene = GetGameLevel().Scene;
		scene.PlayBossAnimation(BossAnimationType.From2To1, Animations);
	}
	public void Hide() {

	}

	public override void OnSignalReceived(DashModelEntity from, EntitySignalType signalType, object? data = null) {
		// If not visible, ignore the signal
		// Just so things don't get clogged up and a fire animation plays
		// when nothing is being fired.
		if (!Visible) return;
		var scene = GetGameLevel().Scene;

		switch (from) {
			case SingleHitEnemy she:
				// Confirm that this is boss related, and the first appearance
				if (she.Variant.IsBoss() && signalType == EntitySignalType.FirstAppearance) {
					// Figure out which animation to play.

					// Attack2 is defined with the same class as Attack1; less code typed out here
					// The JSON descriptor doesn't need to specify a whole object though for Attack2;
					// it can just specify a string and thats implicitly casted to the object type
					// during deserialization.

					switch (she.Variant) {
						case EntityVariant.BossHitSlow:
							scene.PlayBossAnimation(BossAnimationType.CloseAttackSlow, Animations);
							break;
						case EntityVariant.BossHitFast:
							scene.PlayBossAnimation(BossAnimationType.CloseAttackFast, Animations);
							break;
						default:
							scene.PlayBossAnimation(ANIMATION_CHANNEL_FIRE, she, Animations);
							break;
					}
				}

				if (signalType == EntitySignalType.Hit && (she.Variant == EntityVariant.BossHitSlow || she.Variant == EntityVariant.BossHitFast)) {
					scene.PlayBossAnimation(BossAnimationType.Hurt, Animations);
				}
				break;
			case Masher me:
				if (me.Variant.IsBoss()) {
					switch (signalType) {
						case EntitySignalType.FirstAppearance:
							scene.PlayBossAnimation(me.Variant == EntityVariant.BossMasher ? BossAnimationType.MultiAttack : BossAnimationType.MultiAttackEnd, Animations);
							break;
						case EntitySignalType.Hit:
							scene.PlayBossAnimation(BossAnimationType.MultiAttackHurt, Animations);
							break;
						case EntitySignalType.MashOver:
							scene.PlayBossAnimation(me.Variant == EntityVariant.BossMasher ? BossAnimationType.Hurt : BossAnimationType.MultiAttackHurtEnd, Animations);
							break;
					}
				}
				break;
			case Gear ge: {
					scene.PlayBossAnimation(ANIMATION_CHANNEL_FIRE, ge, Animations);
				}
				break;
		}
	}

	public override void Render() {
		if (!Visible) return;
		if (Model == null) return;

		if (!Level.Paused) __anim?.AddDeltaTime(Level.RendertimeDelta);

		__anim?.Apply(Model);
		Model.Position = Position;

		Rlgl.DrawRenderBatchActive();
		Model.Render();
		Rlgl.DrawRenderBatchActive();
	}

	public override void Build() {
		base.Build();
		Model = GetGameLevel().Scene.GetEnemyModel(this)?.Instantiate();
		if (Model != null) {
			Animations.SetModel(Model);
			Model.SetToSetupPose();
		}
	}
	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}
	public override void OnReset() {
		base.OnReset();
		Model?.SetToSetupPose();
		Visible = false;
		Animations?.ClearAllAnimation();
	}
}