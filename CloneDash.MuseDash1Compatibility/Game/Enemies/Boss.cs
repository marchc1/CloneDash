using CloneDash.Common.Gamemodes.MuseDash.V1;
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
		Visible = true;
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.In, animations);
		}
	}
	public void Out() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.Out, animations);
		}
	}
	public void SingleHit() {

	}
	public void Masher() {

	}
	public void Far1Start() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From0To1, animations);
		}
	}
	public void Far1End() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From1To0, animations);
		}
	}
	public void Far1To2() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From1To2, animations);
		}
	}
	public void Far2Start() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From0To2, animations);
		}
	}
	public void Far2End() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From2To0, animations);
		}
	}
	public void Far2To1() {
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
			scene.PlayBossAnimation(BossAnimationType.From2To1, animations);
		}
	}
	public void Hide() {

	}

	public override void OnSignalReceived(DashEnemy? from, EntitySignalType signalType, object? data = null) {
		// If not visible, ignore the signal
		// Just so things don't get clogged up and a fire animation plays
		// when nothing is being fired.
		if (!Visible) return;

		if (signalType == EntitySignalType.SceneChange) {
			// Data will be a tuple of two scenes
			var sceneData = ((IMuseDash1SceneInstance? prev, IMuseDash1SceneInstance? now)?)data;
			if (sceneData != null) {
				IMuseDash1SceneInstance? prev = sceneData.Value.prev;
				IMuseDash1SceneInstance? now = sceneData.Value.now;

				// Patch up animations
				var prevVisuals = prev == null ? null : Visuals[prev.GetSceneArrayIndex()];
				var nowVisuals = now == null ? null : Visuals[now.GetSceneArrayIndex()];
				if (prevVisuals != null && nowVisuals != null) {
					nowVisuals.Animations.Channels[ANIMATION_CHANNEL_MAIN].Time = prevVisuals.Animations.Channels[ANIMATION_CHANNEL_MAIN].Time;
					nowVisuals.Animations.Channels[ANIMATION_CHANNEL_FIRE].Time = prevVisuals.Animations.Channels[ANIMATION_CHANNEL_FIRE].Time;
					nowVisuals.Animations.Channels[ANIMATION_CHANNEL_FIRE2].Time = prevVisuals.Animations.Channels[ANIMATION_CHANNEL_FIRE2].Time;
				}
			}

			return;
		}
		foreach (var visual in Visuals) {
			var scene = visual.Scene;
			var animations = visual.Animations;
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
								scene.PlayBossAnimation(BossAnimationType.CloseAttackSlow, animations);
								break;
							case EntityVariant.BossHitFast:
								scene.PlayBossAnimation(BossAnimationType.CloseAttackFast, animations);
								break;
							default:
								scene.PlayBossAnimation(ANIMATION_CHANNEL_FIRE, she, animations);
								break;
						}
					}

					if (signalType == EntitySignalType.Hit && (she.Variant == EntityVariant.BossHitSlow || she.Variant == EntityVariant.BossHitFast)) {
						scene.PlayBossAnimation(BossAnimationType.Hurt, animations);
					}
					break;
				case Masher me:
					if (me.Variant.IsBoss()) {
						switch (signalType) {
							case EntitySignalType.FirstAppearance:
								scene.PlayBossAnimation(me.Variant == EntityVariant.BossMasher ? BossAnimationType.MultiAttack : BossAnimationType.MultiAttackEnd, animations);
								break;
							case EntitySignalType.Hit:
								scene.PlayBossAnimation(BossAnimationType.MultiAttackHurt, animations);
								break;
							case EntitySignalType.MashOver:
								scene.PlayBossAnimation(me.Variant == EntityVariant.BossMasher ? BossAnimationType.Hurt : BossAnimationType.MultiAttackHurtEnd, animations);
								break;
						}
					}
					break;
				case Gear ge: {
						scene.PlayBossAnimation(ANIMATION_CHANNEL_FIRE, ge, animations);
					}
					break;
			}
		}
	}

	public override void Render() {
		if (!Visible) return;
		var visuals = GetActiveVisuals();

		if (visuals.Model == null) return;

		if (!Level.Paused) visuals.Animations?.AddDeltaTime(Level.RendertimeDelta);

		visuals.Animations?.Apply(visuals.Model);
		visuals.Model.Position = Position;
		visuals.Model.Scale = Scale;

		visuals.Model.Render();
	}

	public override void OnBuildVisuals(DashEnemyVisuals visuals) {
		base.OnBuildVisuals(visuals);
		visuals.Model = visuals.Scene.GetEnemyModel(this)?.Instantiate();
		if (visuals.Model != null) {
			visuals.Animations.SetModel(visuals.Model);
			visuals.Model.SetToSetupPose();
		}
	}

	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}

	public override void OnReset() {
		base.OnReset();
		Visible = false;
	}
}