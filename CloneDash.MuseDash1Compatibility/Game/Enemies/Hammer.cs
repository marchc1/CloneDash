using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Entities
{
	public class Hammer : DashEnemy
	{
		public Hammer(DashEnemyComponents components) : base(MuseDash1EntityType.Hammer, components) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwayId pathway, double distanceToHit) {
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
		float whenDidHammerHit = -1;
		public override void OnReset() {
			base.OnReset();
		}
		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			Components.Scenes.SetEnemyPosition(this);
			if (Dead) {
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}
			base.DetermineAnimationPlayback(visuals);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			BasicSetup(visuals);
		}
	}
}