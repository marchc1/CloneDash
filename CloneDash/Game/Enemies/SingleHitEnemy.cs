using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class SingleHitEnemy : DashEnemy
	{
		public SingleHitEnemy() : base(EntityType.Single) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}
		protected override void OnHit(PathwaySide side, double distanceToHit) {
			base.OnHit(side, distanceToHit);
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			PunishPlayer();
			GetStats().Miss(this);
			if (Level.As<MuseDash1Game>().Pathway == this.Pathway) {
				DamagePlayer();
			}
		}

		public override void Initialize() {
			base.Initialize();
		}

		public override void Render() {
			if (Model != null)
				base.Render();
		}

		protected override void OnFirstVisible() {
			base.OnFirstVisible();
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}

		public override void DetermineAnimationPlayback() {
			if (Model == null) return;

			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			GetGameLevel().SetEnemyPosition(this);
			base.DetermineAnimationPlayback();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<MuseDash1Game>();
			var scene = level.Scene;

			switch (Variant) {
				case EntityVariant.BossHitFast:
				case EntityVariant.BossHitSlow:
					break;
				default:
					var model = scene.GetEnemyModel(this)?.Instantiate();

					if (model != null)
						Model = model;

					double showtime = 1;
					string? animationName = scene?.GetEnemyApproachAnimation(this, out showtime);
					SetShowTimeViaLength(showtime);

					ApproachAnimation = Model?.Data.FindAnimation(animationName);
					GreatHitAnimation = Model?.Data.FindAnimation(scene?.GetEnemyHitAnimation(this, HitAnimationType.Great));
					PerfectHitAnimation = Model?.Data.FindAnimation(scene?.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
					SetMountBoneIfApplicable(scene?.GetHPMount(this));
					break;
			}
		}
	}
}