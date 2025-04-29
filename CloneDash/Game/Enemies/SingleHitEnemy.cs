using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class SingleHitEnemy : CD_BaseEnemy {
		public SingleHitEnemy() : base(EntityType.Single) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}
		protected override void OnHit(PathwaySide side) {
			base.OnHit(side);
			Kill();
		}

		protected override void OnMiss() {
			PunishPlayer();
			if (Level.As<CD_GameLevel>().Pathway == this.Pathway) {
				DamagePlayer();
			}
		}

		public override void ChangePosition(ref Vector2F pos) {

		}

		public override void Initialize() {
			base.Initialize();
		}

		public override void Render() {
			if (Model != null)
				base.Render();
		}

		protected override void OnFirstVisible() {
			base.OnFirstVisible();
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}

		private float xoffset;

		public override void DetermineAnimationPlayback() {
			if (Model == null) return;

			if (Dead) {
				Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(Pathway));
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}
			Position = new(xoffset, 450);
			base.DetermineAnimationPlayback();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			var model = (Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Boss2 => scene.BossEnemy2.GetModelFromPathway(Pathway),
				EntityVariant.Boss3 => scene.BossEnemy3.GetModelFromPathway(Pathway),

				EntityVariant.Small => scene.SmallEnemy.GetModelFromPathway(Pathway),

				EntityVariant.Medium1 => scene.MediumEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Medium2 => scene.MediumEnemy2.GetModelFromPathway(Pathway),

				EntityVariant.Large1 => scene.LargeEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Large2 => scene.LargeEnemy2.GetModelFromPathway(Pathway),

				_ => null
			})?.Instantiate();

			if (model != null)
				Model = model;

			double showtime = 0;
			string? animationName = Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1.GetAnimationString(Speed, out showtime),
				EntityVariant.Boss2 => scene.BossEnemy2.GetAnimationString(Speed, out showtime),
				EntityVariant.Boss3 => scene.BossEnemy3.GetAnimationString(Speed, out showtime),

				EntityVariant.Small => scene.SmallEnemy.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),

				EntityVariant.Medium1 => scene.MediumEnemy1.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),
				EntityVariant.Medium2 => scene.MediumEnemy2.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),

				EntityVariant.Large1 => scene.LargeEnemy1.GetAnimationString(Speed, out showtime),
				EntityVariant.Large2 => scene.LargeEnemy2.GetAnimationString(Speed, out showtime),

				_ => null
			};

			SceneDescriptor.IContainsGreatPerfectAndHPMount? greatPerfectHP = Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1,
				EntityVariant.Boss2 => scene.BossEnemy2,
				EntityVariant.Boss3 => scene.BossEnemy3,

				EntityVariant.Small => scene.SmallEnemy,

				EntityVariant.Medium1 => scene.MediumEnemy1,
				EntityVariant.Medium2 => scene.MediumEnemy2,

				EntityVariant.Large1 => scene.LargeEnemy1,
				EntityVariant.Large2 => scene.LargeEnemy2,

				_ => null
			};

			if (Model != null && greatPerfectHP != null && animationName != null) {
				ShowTime = HitTime - showtime;

				ApproachAnimation = Model.Data.FindAnimation(animationName);
				GreatHitAnimation = greatPerfectHP.FindGreatAnimation(Model);
				PerfectHitAnimation = greatPerfectHP.FindPerfectAnimation(Model);

				Scale = new(level.GlobalScale);
				SetMountBoneIfApplicable(greatPerfectHP);
			}
			else {
				
			}
		}
	}
}