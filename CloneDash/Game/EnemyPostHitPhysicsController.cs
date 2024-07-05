using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Game
{
    internal class EnemyPostHitPhysicsController(CD_BaseEnemy enemy)
    {
        private bool hit = false;

        private Vector2F pos = Vector2F.Zero;
        private Vector2F vel = Vector2F.Zero;
        private float ang = 0;
        private float angVel = 0;
        private float lastCurtime;

        public void Hit(Vector2F velocity, float angularVelocity) {
            if (hit) return;

            hit = true;
            this.pos = enemy.Position;
            this.vel = velocity;
            this.angVel = angularVelocity;
            lastCurtime = enemy.Level.CurtimeF;
        }

        public void PassthroughPosition(ref Vector2F vec) {
            if (hit) {
                var level = enemy.Level;
                var delta = (level.CurtimeF - lastCurtime) * 10;

                vel += new Vector2F(0, 100) * delta;
                pos += vel * delta;

                vec.X = pos.X;
                vec.Y = pos.Y;

                ang += angVel * delta;

                enemy.Rotation = new(0, 0, ang);

                lastCurtime = level.CurtimeF;
            }
        }
    }
}
