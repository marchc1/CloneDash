namespace CloneDash.Game.Entities
{
	public class DoubleHitEnemy : DashEnemy
	{
		public DoubleHitEnemy() : base(EntityType.Double) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}
		public override void OnReset() {
			base.OnReset();
		}
		public override void Initialize() {
			base.Initialize();
		}

		protected override void OnHit(PathwaySide side, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			DamagePlayer();
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
