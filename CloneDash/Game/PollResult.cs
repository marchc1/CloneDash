using CloneDash.Game.Entities;

namespace CloneDash.Game
{
    public struct PollResult
    {
        public bool Hit;
        public CD_BaseMEntity HitEntity;
        public double DistanceToHit;
        public string Greatness;

        public static readonly PollResult Empty = new PollResult() { Hit = false };

        public static PollResult Create(CD_BaseMEntity hitEntity, double distanceToHit, string greatness) {
            PollResult result = new PollResult();
            result.Hit = true;
            result.HitEntity = hitEntity;
            result.DistanceToHit = distanceToHit;
            result.Greatness = greatness;
            return result;
        }
    }
}
