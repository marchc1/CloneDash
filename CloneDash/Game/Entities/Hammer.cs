
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
            whenDidHammerHit = Level.CurtimeF;
        }

        protected override void OnMiss() {
            PunishPlayer();
            if (Level.As<CD_GameLevel>().Pathway == this.Pathway) {
                DamagePlayer();
            }
        }

        public override void Initialize() {
            base.Initialize();
            SetModel("hammer.glb");
        }
        float whenDidHammerHit = -1;
        public override void ChangePosition(ref Vector2F pos) {
            var level = Level.As<CD_GameLevel>();

            var pathwayY = Game.Pathway.ValueDependantOnPathway(Pathway, level.TopPathway.Position.Y, level.BottomPathway.Position.Y); // Pathway == PathwaySide.Top ? DashVars.UpPathway.Position.Y : DashVars.DownPathway.Position.Y;
            var timeToHit = (float)(HitTime - ShowTime);

            pos.Y = pathwayY;
            var bone = this.Model.GetBoneByName("Bone");

            bone.Rotation = new(0, 0, -(float)(whenDidHammerHit == -1 ? NMath.Remap(level.Conductor.Time, HitTime - 2, HitTime, 100, 0) : NMath.Remap(Level.CurtimeF - whenDidHammerHit, 0, 0.55f, 0, 100)));
        }

        public override void Build() {
            HSV = new(Pathway == PathwaySide.Top ? 200 : 285, 1, 1);
        }
    }
}
