
using Nucleus;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class Hammer : CD_BaseEnemy
    {
        public Hammer() : base(EntityType.Hammer) {
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }

        protected override void OnHit(PathwaySide side) {
            Kill();
        }

        protected override void OnMiss() {
            PunishPlayer();
            if (Level.As<CD_GameLevel>().Pathway == this.Pathway) {
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
		public override void ChangePosition(ref Vector2F pos) {

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

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			Model = scene.Hammer.GetModelFromPathway(Pathway, Flipped).Instantiate();

			string animationName = scene.Hammer.GetAnimationString(Speed, out var showtime);
			ShowTime = HitTime - showtime;

			ApproachAnimation = Model.Data.FindAnimation(animationName);
			GreatHitAnimation = scene.Hammer.FindGreatAnimation(Model);
			PerfectHitAnimation = scene.Hammer.FindPerfectAnimation(Model);

			Scale = new(level.GlobalScale);
		}
    }
}