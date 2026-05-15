using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus.Engine;

namespace CloneDash.Game.Entities
{
	public class Raider : DashEnemy
	{
		public Raider() : base(MuseDash1EntityType.Raider) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}


		protected override void OnHit(PathwaySide side, double distanceToHit) {
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

		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}
			GetGameLevel().SetEnemyPosition(this);
			base.DetermineAnimationPlayback(visuals);
		}

		public override void Initialize() {
			base.Initialize();
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			BasicSetup(visuals);
			SetMountBoneIfApplicable(visuals, visuals.Scene.GetHPMount(visuals)!);
		}
	}
}