using CloneDash.Game.Entities;

namespace CloneDash.Game.Events
{
    public enum EventType {
        BossIn,
        BossOut,
        BossSingleHit,
        BossMasher
    }
    public abstract class MapEvent
    {
        public static Dictionary<EventType, Type> TypeConvert = new() {
            { EventType.BossIn, typeof(BossInEvent) },
            { EventType.BossOut, typeof(BossOutEvent) },
            { EventType.BossSingleHit, typeof(BossSingleHit) },
            { EventType.BossMasher, typeof(BossMasher) }
        };
        public static MapEvent CreateFromType(DashGame game, EventType type) {
            return (MapEvent)Activator.CreateInstance(TypeConvert[type], game);
        }

        /// <summary>
        /// The current game the event belongs to
        /// </summary>
        public DashGame Game { get; private set; }

        /// <summary>
        /// The event type.
        /// </summary>
        public EventType Type { get; private set; }

        /// <summary>
        /// Offset in seconds for when the event should be raised
        /// </summary>
        public double Offset { get; set; } = 0;

        /// <summary>
        /// The associated entity with this event
        /// </summary>
        public MapEntity? AssociatedEntity { get; set; }

        /// <summary>
        /// Has the event been called yet?
        /// </summary>
        public bool Called { get; private set; } = false;

        /// <summary>
        /// In seconds, the time the event will be raised
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// How long does this event last?
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// What happens when this event is raised?
        /// </summary>
        /// <param name="time"></param>
        public abstract void OnCall(double time);

        /// <summary>
        /// Should this event be raised now?<br></br>
        /// Takes into account both when the event occurs and if it's already been called
        /// </summary>
        /// <param name="Curtime"></param>
        /// <returns></returns>
        public bool ShouldCall(double Curtime) => !Called && Curtime >= Time + Offset;

        /// <summary>
        /// Calls the event, if it hasn't been called
        /// </summary>
        public void Call() {
            if (Called)
                return;

            OnCall(Game.Conductor.Time);
            Called = true;
        }
        /// <summary>
        /// Calls the event, both if it hasn't been called and if the time has elapsed to call the event
        /// </summary>
        public void TryCall() {
            if (Called)
                return;

            var t = Game.Conductor.Time;
            if (ShouldCall(t))
                Call();
        }
        /// <summary>
        /// Called after the event properties are set by the parsing system. Optional method
        /// </summary>
        public virtual void Build() { }
        /// <summary>
        /// Returns (Time + Offset) -> Time as a 0 -> 1 value for animation
        /// </summary>
        public double OffsetToTime => DashMath.Remap(Game.Conductor.Time, Time + Offset, Time, 0, 1);

        /// <summary>
        /// Event damage.
        /// </summary>
        public int? Damage { get; set; }
        /// <summary>
        /// Event fever.
        /// </summary>
        public int? Fever { get; set; }
        /// <summary>
        /// Event score.
        /// </summary>
        public int? Score { get; set; }

        private MapEvent() { }

        public MapEvent(DashGame game) {
            this.Game = game;
        }
    }
}
