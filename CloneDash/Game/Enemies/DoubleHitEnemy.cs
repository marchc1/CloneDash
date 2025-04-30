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
        }

        protected override void OnMiss() {
            DamagePlayer();
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

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			var sceneDescription = scene.DoubleEnemy;

			Model = sceneDescription.GetModelFromPathway(Pathway).Instantiate();

			string animationName = sceneDescription.GetAnimationString(Speed, out var showtime);
			ShowTime = HitTime - showtime;

			ApproachAnimation = Model.Data.FindAnimation(animationName);
			GreatHitAnimation =   sceneDescription.FindGreatAnimation(Model);
			PerfectHitAnimation = sceneDescription.FindPerfectAnimation(Model);

			Scale = new(level.GlobalScale);
		}
	}
}
