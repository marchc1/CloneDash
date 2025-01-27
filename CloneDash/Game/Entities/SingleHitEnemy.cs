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


        protected override void OnHit(PathwaySide side) {
            Kill();
            postHitPhysics.Hit(NMath.Random.Vec2(new(-120, -210), new(-100, -180)), NMath.Random.Single(12.5f, 22.5f));
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

            postHitPhysics.PassthroughPosition(ref pos);
        }
        public override void Initialize() {
            base.Initialize();
            SetModel("singlehit.glb", "SingleHitIdle", true);
        }

        public override void Build() {
            HSV = new(Pathway == PathwaySide.Top ? 200 : 285, 1, 1);
            //Texture = RelatedToBoss ? TextureSystem.LoadTexture("boss_projectile.png") : TextureSystem.LoadTexture("fightable_ball.png");
        }
    }
}