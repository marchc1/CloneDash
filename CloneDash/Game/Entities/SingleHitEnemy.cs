using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
    public class SingleHitEnemy : CD_BaseEnemy
    {
        private EnemyPostHitPhysicsController postHitPhysics;
        public SingleHitEnemy() : base(EntityType.Single) {
            postHitPhysics = new(this);
            Interactivity = EntityInteractivity.Hit;
        }

		public override void OnReset() {
			base.OnReset();
			postHitPhysics = new(this);
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
            var scrw = Raylib_cs.Raylib.GetScreenWidth();
            if (RelatedToBoss) {

            }
            else {
                var yOff = NMath.Ease.InCubic(Math.Clamp((float)NMath.Remap(pos.X, scrw / 2, scrw, 0f, 1), 0f, 1f));
                switch (EnterDirection) {
                    case EntityEnterDirection.RightSide:
                        // no modification occurs
                        break;

                    case EntityEnterDirection.TopDown:
                        pos.y = (float)NMath.Remap(yOff, 0, 1, pos.Y, (-Raylib_cs.Raylib.GetScreenHeight() / 2) * 2);
                        break;

                    case EntityEnterDirection.BottomUp:
                        pos.y = (float)NMath.Remap(yOff, 0, 1, pos.Y, (Raylib_cs.Raylib.GetScreenHeight() / 2) * 2);
                        break;
                }
            }
        }
        public override void Initialize() {
            base.Initialize();
        }
		// TODO: Rework how the base level determines visibility, then remove XPos as a factor at all
		protected override void OverrideModelPosition(ref Vector2F position) {
			base.OverrideModelPosition(ref position);
			position.X = 0;
			position.Y = 0;
		}
		protected override void OnVisible() {
			base.OnVisible();
		}

		public override void Build() {
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

				_ => scene.SmallEnemy.GetModelFromPathway(Pathway) // default to small if something broke
			}).Instantiate();
			Animations = new(Model);
			Scale = new(level.GlobalScale);

		}
    }
}