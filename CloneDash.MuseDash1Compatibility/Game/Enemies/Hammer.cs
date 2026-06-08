using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Entities
{
	public class Hammer : DashEnemy
	{
		public Hammer() : base(MuseDash1EntityType.Hammer) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
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

		public override void Initialize() {
			base.Initialize();
		}
		float whenDidHammerHit = -1;
		public override void OnReset() {
			base.OnReset();
		}
		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			GetGameLevel().SetEnemyPosition(this);
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