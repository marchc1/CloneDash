namespace CloneDash.Game.Entities
{
	public class Hammer : DashEnemy
	{
		public Hammer() : base(EntityType.Hammer) {
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
			if (Level.As<DashGameLevel>().Pathway == this.Pathway) {
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
		public override void DetermineAnimationPlayback() {
			Position = new(0, 450);
			if (Dead) {
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}
			base.DetermineAnimationPlayback();
		}
		public override void Build() {
			base.Build();
			BasicSetup();
		}
	}
}