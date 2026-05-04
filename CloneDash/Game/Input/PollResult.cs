using Nucleus;

namespace CloneDash.Game.Input;


public struct PollParams {
	public int AmountOfTimesHit;
	public int HitsRemaining;
	public PathwaySide Pathway;
}

public struct PollResult
{
	/// <summary>
	/// Did this input hit something?
	/// </summary>
	public bool Hit;
	/// <summary>
	/// What did it hit?
	/// </summary>
	public BaseDashEnemy HitEntity;
	public double DistanceToHit;
	public string Greatness;

	public static readonly PollResult Empty = new PollResult() { Hit = false };

	public static PollResult Create(BaseDashEnemy hitEntity, double distanceToHit, string greatness) {
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
