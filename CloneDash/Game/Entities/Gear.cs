using CloneDash.Modding.Descriptors;
using Nucleus.Types;
namespace CloneDash.Game.Entities
{
    public class Gear : CD_BaseEnemy
    {
        public Gear() : base(EntityType.Gear) {
            Interactivity = EntityInteractivity.Avoid;
            DoesDamagePlayer = true;
        }

        public override void Initialize() {
            base.Initialize();
        }

        protected override void OnPass() {
            RewardPlayer();
        }

		public override void DetermineAnimationPlayback() {
			Position = new(0, 450);
			base.DetermineAnimationPlayback();
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}

		public override void Build() {
			base.Build();

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			Model = (Variant switch {
				EntityVariant.Boss1 or EntityVariant.Boss2 => scene.BossGears.GetModelFromPathway(Pathway),
				_ => scene.Gears.GetModelFromPathway(Pathway)
			}).Instantiate();

			double showtime = 0;
			var animationName = Variant switch {
				EntityVariant.Boss1 or EntityVariant.Boss2 => scene.BossGears.GetAnimationString(Pathway, Speed, out showtime),
				_ => scene.Gears.GetAnimationString(Pathway, Speed, out showtime)
			};

			ShowTime = HitTime - showtime;

			ApproachAnimation = Model.Data.FindAnimation(animationName);

			Scale = new(level.GlobalScale);
		}
	}
}
