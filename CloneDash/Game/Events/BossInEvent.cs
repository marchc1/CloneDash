namespace CloneDash.Game.Events
{
    public class BossInEvent : MapEvent
    {
        public BossInEvent(DashGame game) : base(game) { }
        public override void OnCall(double time) {
            Game.Boss.Enter(time);
        }
    }
}
