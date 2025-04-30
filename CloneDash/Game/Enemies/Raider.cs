using Nucleus;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using static Nucleus.NMath;

namespace CloneDash.Game.Entities
{
	public class Raider : CD_BaseEnemy
	{
		public Raider() : base(EntityType.Raider) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}


		protected override void OnHit(PathwaySide side, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			PunishPlayer();
			GetStats().Miss(this);
			if (Level.As<CD_GameLevel>().Pathway == this.Pathway) {
				DamagePlayer();
			}
		}

		public override void ChangePosition(ref Vector2F pos) {

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

		public override void Initialize() {
			base.Initialize();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			Model = scene.Raider.GetModelFromPathway(Pathway, Flipped).Instantiate();

			string animationName = scene.Raider.GetAnimationString(Speed, out var showtime);
			ShowTime = HitTime - showtime;

			ApproachAnimation = Model.Data.FindAnimation(animationName);
			GreatHitAnimation = scene.Raider.FindGreatAnimation(Model);
			PerfectHitAnimation = scene.Raider.FindPerfectAnimation(Model);

			Scale = new(level.GlobalScale);

			SetMountBoneIfApplicable(scene.Raider);
		}
	}
}