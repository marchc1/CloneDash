using Nucleus.Core;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // SetModel("gear.glb", "Idle", true);
        }
        protected override void OnPass() {
            RewardPlayer();
        }
        public override void ChangePosition(ref Vector2F pos) {
            if(Pathway == PathwaySide.Bottom) {
                pos.Y += 58f;
            }
        }
        public override void Render(FrameState frameState) {
            Raylib_cs.Raylib.BeginScissorMode(0, 0, (int)frameState.WindowWidth, (int)(frameState.WindowHeight * 0.683f));
            base.Render(frameState);
            Graphics2D.ScissorRect();
        }
        public override void Build() {
			base.Build();
        }
    }
}
