
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
		public override void OnReset() {
			base.OnReset();
			whenDidHammerHit = -1;
		}
		public override void ChangePosition(ref Vector2F pos) {
            var level = Level.As<CD_GameLevel>();

            var pathwayY = Game.Pathway.ValueDependantOnPathway(Pathway, level.TopPathway.Position.Y, level.BottomPathway.Position.Y); // Pathway == PathwaySide.Top ? DashVars.UpPathway.Position.Y : DashVars.DownPathway.Position.Y;
            var timeToHit = (float)(HitTime - ShowTime);

            pos.Y = pathwayY;
        }

        public override void Build() {

        }
    }
}
