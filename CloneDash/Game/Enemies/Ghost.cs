namespace CloneDash.Game.Entities
{
	public class Ghost : CD_BaseEnemy
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

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			Model = scene.Ghost.GetModelFromPathway(Pathway).Instantiate();

			var animationName = scene.Ghost.GetAnimationString(Speed, out var showtime);
			SetShowTimeViaLength(showtime);

			ApproachAnimation = Model.Data.FindAnimation(animationName);
			GreatHitAnimation = scene.Ghost.FindGreatAnimation(Model);
			PerfectHitAnimation = scene.Ghost.FindPerfectAnimation(Model);

			Scale = new(level.GlobalScale);
		}
	}
}