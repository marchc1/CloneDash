using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class SingleHitEnemy : CD_BaseEnemy
	{
		public SingleHitEnemy() : base(EntityType.Single) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
			firstTimeVisible = true;
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

		public override void ChangePosition(ref Vector2F pos) {

		}

		public override void Initialize() {
			base.Initialize();
		}
		
		public override void Render() {
			base.Render();
		}

		private bool firstTimeVisible = true;
		public override void PostThink(FrameState frameState) {
			if (!Visible) return;
			if (AnimationTime <= 0) return;

			if (firstTimeVisible) {
				firstTimeVisible = false;
				if (Variant.IsBoss()) {
					SendSignal(GetGameLevel().Boss, EntitySignalType.FirstAppearance);
				}
			}

			base.Think(frameState);
		}

		private float xoffset;

		public override void DetermineAnimationPlayback() {
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

			Model = (Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Boss2 => scene.BossEnemy2.GetModelFromPathway(Pathway),
				EntityVariant.Boss3 => scene.BossEnemy3.GetModelFromPathway(Pathway),

				EntityVariant.Small => scene.SmallEnemy.GetModelFromPathway(Pathway),

				EntityVariant.Medium1 => scene.MediumEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Medium2 => scene.MediumEnemy2.GetModelFromPathway(Pathway),

				EntityVariant.Large1 => scene.LargeEnemy1.GetModelFromPathway(Pathway),
				EntityVariant.Large2 => scene.LargeEnemy2.GetModelFromPathway(Pathway),

				_ => scene.SmallEnemy.GetModelFromPathway(Pathway) // default to small if something broke I guess
			}).Instantiate();

			double showtime = 0;
			var animationName = Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1.GetAnimationString(Speed, out showtime),
				EntityVariant.Boss2 => scene.BossEnemy2.GetAnimationString(Speed, out showtime),
				EntityVariant.Boss3 => scene.BossEnemy3.GetAnimationString(Speed, out showtime),

				EntityVariant.Small => scene.SmallEnemy.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),

				EntityVariant.Medium1 => scene.MediumEnemy1.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),
				EntityVariant.Medium2 => scene.MediumEnemy2.GetAnimationString(Speed, EnterDirection, out showtime, out xoffset),

				EntityVariant.Large1 => scene.LargeEnemy1.GetAnimationString(Speed, out showtime),
				EntityVariant.Large2 => scene.LargeEnemy2.GetAnimationString(Speed, out showtime),

				_ => throw new Exception("Can't handle that case...")
			};

			SceneDescriptor.IContainsGreatPerfect greatPerfect = Variant switch {
				EntityVariant.Boss1 => scene.BossEnemy1,
				EntityVariant.Boss2 => scene.BossEnemy2,
				EntityVariant.Boss3 => scene.BossEnemy3,

				EntityVariant.Small => scene.SmallEnemy,

				EntityVariant.Medium1 => scene.MediumEnemy1,
				EntityVariant.Medium2 => scene.MediumEnemy2,

				EntityVariant.Large1 => scene.LargeEnemy1,
				EntityVariant.Large2 => scene.LargeEnemy2,

				_ => throw new Exception("Can't handle that case...")
			};

			ShowTime = HitTime - showtime;

			ApproachAnimation = Model.Data.FindAnimation(animationName);
			GreatHitAnimation = greatPerfect.FindGreatAnimation(Model);
			PerfectHitAnimation = greatPerfect.FindPerfectAnimation(Model);

			Scale = new(level.GlobalScale);
		}
	}
}