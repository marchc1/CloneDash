namespace CloneDash.Game.Entities
{
    public class DoubleHitEnemy : CD_BaseEnemy
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
