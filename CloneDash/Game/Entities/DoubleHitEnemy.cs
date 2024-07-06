using Nucleus;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Game.Entities
{
    public class DoubleHitEnemy : CD_BaseEnemy
    {
        private EnemyPostHitPhysicsController postHitPhysics;
        public DoubleHitEnemy() : base(EntityType.Double) {
            postHitPhysics = new(this);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }
        public override void Initialize() {
            base.Initialize();
            SetModel("doublehit.glb", "Idle", true);
            Model.HSV = new(37, 1.24f, 1);
        }

        public override void ChangePosition(ref Vector2F pos) {
            postHitPhysics.PassthroughPosition(ref pos);
        }

        protected override void OnHit(PathwaySide side) {
            Kill();
            postHitPhysics.Hit(NMath.Random.Vec2(new(-120, -210), new(-100, -180)), NMath.Random.Single(-22f, 22.5f));
        }

        protected override void OnMiss() {
            DamagePlayer();
        }
        bool thoughtBefore = false;
        public override void Think(FrameState frameState) {
            if (!thoughtBefore) {
                thoughtBefore = true;
                this.Rotation = Pathway == PathwaySide.Top ? new(0, 0, -180) : new(0, 0, 0);
            }
        }
        public override void Render(FrameState frameState) {
            var game = Level.As<CD_GameLevel>();
            var YPos = Game.Pathway.ValueDependantOnPathway(Pathway, game.TopPathway.Position.Y, game.BottomPathway.Position.Y);

            Model.Position = new(this.Position.X, this.Position.Y, 0);
            Model.Rotation = this.Rotation;
            Model.Render();

            if (Dead)
                return;
            for (int i = 64 - 1; i >= 0; i -= 4) {
                var size = Math.Abs(YPos)/ 4;
                Raylib.DrawCubeV(new((float)XPos, (float)YPos + ((size / 2) * (Pathway == PathwaySide.Top ? 1 : -1)), 10), new(i/1.4f, size, 10), new Color(255, 193, 92, 15));
            }
        }
    }
}
