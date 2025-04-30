using CloneDash.Game.Entities;
using Nucleus;
using Nucleus.Engine;

namespace CloneDash.Game.Input
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

		public bool IsPerfect {
			get {
				if (!Hit) return false;

				double distance = DistanceToHit;
				double pregreat = -HitEntity.PreGreatRange, postgreat = HitEntity.PostGreatRange;
				double preperfect = -HitEntity.PrePerfectRange, postperfect = HitEntity.PostPerfectRange;

				return NMath.InRange(distance, preperfect, postperfect);
			}
		}
		public bool IsAtLeastGreat {
			get {
				if (!Hit) return false;

				double distance = DistanceToHit;
				double pregreat = -HitEntity.PreGreatRange, postgreat = HitEntity.PostGreatRange;

				return NMath.InRange(distance, pregreat, postgreat);
			}
		}
	}
}
