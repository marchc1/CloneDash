using CloneDash.Game.Enumerations;

namespace CloneDash.Game
{
    public struct SheetEvent
    {
        public float Time;
        public double Length;
        public EventType Type;

        public int? Damage;
        public int? Score;
        public int? Fever;
    }
}
