using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Entities
{
	public class Ghost : DashEnemy
	{
		public Ghost(DashEnemyComponents components) : base(MuseDash1EntityType.Ghost, components) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = false;
			DoesPunishPlayer = false;
		}

		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			base.OnMiss();
			GetStats().Miss(this);
		}

		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
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
			BasicSetup(visuals);
		}
	}
}