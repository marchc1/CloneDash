using CloneDash.Game.Entities;

namespace CloneDash.Game.Events
{
    public class BossMasher : MapEvent
    {
        public BossMasher(DashGame game) : base(game) {
            this.Offset = -0.95;
        }

        public override void Build() {
            AssociatedEntity = Game.GameplayManager.CreateEntity<Masher>();
            AssociatedEntity.Invisible = true;
            AssociatedEntity.HitTime = Time;
            AssociatedEntity.ShowTime = Time + this.Offset;
            AssociatedEntity.DamageTaken = Damage.HasValue ? Damage.Value : 0;
            AssociatedEntity.FeverGiven = Fever.HasValue ? Fever.Value : 0;
            AssociatedEntity.Length = Length;
            AssociatedEntity.ScoreGiven = Score.HasValue ? Score.Value : 0;
        }

        public override void OnCall(double time) {
            Game.Boss.WindupForMasherAttack(this);
        }
    }

}
