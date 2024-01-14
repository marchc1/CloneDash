using CloneDash.Game.Entities;

namespace CloneDash.Game.Events
{
    public class BossSingleHit : MapEvent
    {
        public BossSingleHit(DashGame game) : base(game) {
            this.Offset = -0.95;
        }

        public override void Build() {
            AssociatedEntity = Game.GameplayManager.CreateEntity<SingleHitEnemy>();
            AssociatedEntity.Invisible = true;
            AssociatedEntity.HitTime = Time;
            AssociatedEntity.ShowTime = Time + this.Offset;
            AssociatedEntity.Pathway = PathwaySide.Both;
            AssociatedEntity.DamageTaken = Damage.HasValue ? Damage.Value : 0;
            AssociatedEntity.FeverGiven = Fever.HasValue ? Fever.Value : 0;
            AssociatedEntity.ScoreGiven = Score.HasValue ? Score.Value : 0;
        }

        public override void OnCall(double time) {
            Game.Boss.WindupForSingleAttack(this);
        }
    }

}
