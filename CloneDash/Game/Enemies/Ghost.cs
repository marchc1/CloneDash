namespace CloneDash.Game.Entities
{
	public class Ghost : DashEnemy
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
				Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(Pathway));
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			Position = new(0, 450);
			base.DetermineAnimationPlayback();
		}

		public override void Build() {
			base.Build();
			BasicSetup();
		}
	}
}