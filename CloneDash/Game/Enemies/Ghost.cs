namespace CloneDash.Game.Entities
{
	public class Ghost : BaseDashEnemy
	{
		public Ghost() : base(EntityType.Ghost) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = false;
			DoesPunishPlayer = false;
		}

		protected override void OnHit(PathwaySide side, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			base.OnMiss();
			GetStats().Miss(this);
		}

		public override void DetermineAnimationPlayback() {
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
			BasicSetup();
		}
	}
}