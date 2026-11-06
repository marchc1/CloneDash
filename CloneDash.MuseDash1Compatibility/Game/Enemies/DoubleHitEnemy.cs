using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Entities
{
	public class DoubleHitEnemy : DashEnemy
	{
		public DoubleHitEnemy(DashEnemyComponents components) : base(MuseDash1EntityType.Double, components) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}
		public override void OnReset() {
			base.OnReset();
		}
		public override void Initialize() {
			base.Initialize();
		}

		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			DamagePlayer();
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
