using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class SingleHitEnemy : DashEnemy
	{
		public SingleHitEnemy(DashEnemyComponents components) : base(MuseDash1EntityType.Single, components) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}
		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			base.OnHit(pathway, distanceToHit);
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			PunishPlayer();
			GetStats().Miss(this);
			if (Components.Character.IsCharacterOn(Pathway)) {
				DamagePlayer();
			}
		}

		public override void Initialize() {
			base.Initialize();
		}

		public override void Render() {
			var visuals = GetActiveVisuals();
			if (visuals.Model != null)
				base.Render();
		}

		protected override void OnFirstVisible() {
			base.OnFirstVisible();
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}

		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (visuals.Model == null) return;

			if (Dead) {
				Components.Scenes.SetEnemyKilledPosition(this);
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}

			Components.Scenes.SetEnemyPosition(this);
			base.DetermineAnimationPlayback(visuals);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);


			var scene = visuals.Scene;

			switch (Variant) {
				case EntityVariant.BossHitFast:
				case EntityVariant.BossHitSlow:
					break;
				default:
					var model = scene.GetEnemyModel(this)?.Instantiate();

					if (model != null)
						visuals.Model = model;

					double showtime = 1;
					string? animationName = scene?.GetEnemyApproachAnimation(this, out showtime);
					visuals.SetShowTimeViaLength(showtime, HitTime);

					visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(animationName);
					visuals.GreatHitAnimation = visuals.Model?.Data.FindAnimation(scene?.GetEnemyHitAnimation(this, HitAnimationType.Great));
					visuals.PerfectHitAnimation = visuals.Model?.Data.FindAnimation(scene?.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
					SetMountBoneIfApplicable(visuals, scene?.GetHPMount(visuals));
					break;
			}
		}
	}
}