using CloneDash.Scenes;

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
			if (Level.As<DashGameLevel>().Pathway == this.Pathway) {
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

		private float xoffset;

		public override void DetermineAnimationPlayback() {
			if (Model == null) return;

			if (Dead) {
				Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(Pathway));
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			Position = new(xoffset, 450);
			base.DetermineAnimationPlayback();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<DashGameLevel>();
			var scene = level.Scene;

			switch (Variant) {
				case EntityVariant.BossHitFast:
				case EntityVariant.BossHitSlow:
				case EntityVariant.BossMash:
					break;
				default:
					var model = scene.GetEnemyModel(this).Instantiate();

					if (model != null)
						Model = model;

					string animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
					SetShowTimeViaLength(showtime);

					ApproachAnimation = Model.Data.FindAnimation(animationName);
					GreatHitAnimation = Model.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Great));
					PerfectHitAnimation = Model.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
					Scale = new(level.GlobalScale);
					SetMountBoneIfApplicable(scene.GetHPMount(this));
					break;
			}
		}
	}
}