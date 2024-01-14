namespace CloneDash.Game.Events
{
    public class BossOutEvent : MapEvent
    {
        public BossOutEvent(DashGame game) : base(game) { }
        public override void OnCall(double time) {
            Game.Boss.Exit(time);
        }
    }
}
