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
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.In), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
	}
	public void Out() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Out), false);
	}
	public void SingleHit() {

	}
	public void Masher() {

	}
	public void Far1Start() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From0To1), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby1), true);
	}
	public void Far1End() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From1To0), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
	}
	public void Far1To2() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From1To2), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby2), true);
	}
	public void Far2Start() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From0To2), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby2), true);
	}
	public void Far2End() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From2To0), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
	}
	public void Far2To1() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.From2To1), false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby1), true);
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
							Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.CloseAttackSlow), false);
							Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
							break;
						case EntityVariant.BossHitFast:
							Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.CloseAttackFast), false);
							Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
							break;
						default:
							Animations.SetAnimation(ANIMATION_CHANNEL_FIRE, scene.GetBossAnimation(she), false);
							Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, she.Variant switch {
								EntityVariant.Boss1 => scene.GetBossAnimation(BossAnimationType.Standby1),
								EntityVariant.Boss2 => scene.GetBossAnimation(BossAnimationType.Standby2),
								EntityVariant.Boss3 => scene.GetBossAnimation(BossAnimationType.Standby2),
							}, true);
							break;
					}
				}

				if (signalType == EntitySignalType.FirstHit && (she.Variant == EntityVariant.BossHitSlow || she.Variant == EntityVariant.BossHitFast)) {
					Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Hurt), false);
					Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
				}
				break;
			case Masher me:
				if (me.Variant.IsBoss()) {
					switch (signalType) {
						case EntitySignalType.FirstAppearance:
							Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.MultiAttack), false);
							Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby0), true);
							break;
						case EntitySignalType.FirstHit:
						case EntitySignalType.HitAgain:

							break;
						case EntitySignalType.MashOver:

							break;
					}
				}
				break;
			case Gear ge: {
					Animations.SetAnimation(ANIMATION_CHANNEL_FIRE, scene.GetBossAnimation(ge), false);
					Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.GetBossAnimation(BossAnimationType.Standby1), true);
				}
				break;
		}
	}

	public override void Render() {
		if (!Visible) return;
		if (Model == null) return;

		if (!Level.Paused) __anim.AddDeltaTime(Level.CurtimeDelta);

		__anim.Apply(Model);
		Model.Position = Position;
		Model.Scale = Scale;

		Rlgl.DrawRenderBatchActive();
		Model.Render();
		Rlgl.DrawRenderBatchActive();
	}

	public override void Build() {
		base.Build();
		Model = GetGameLevel().Scene.GetEnemyModel(this).Instantiate();
		Animations = new Nucleus.Models.Runtime.AnimationHandler(Model);


		Model.SetToSetupPose();
	}
	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}
}