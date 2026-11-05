namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public readonly record struct UnbeatableWhiteLabelJudgement(double Position, double HitTime, bool Miss, int TierOverride = -1, double BonusOverride = -1)
{
	public const double LENIENCY = 0.2;
	public const int TIER_COUNT = 5;

	public static readonly string[] TierNames = ["MISS", "BARELY", "OKAY", "GOOD", "GREAT", "PERFECT"];

	public double BaseScore => (double)Tier / TIER_COUNT;
	public double BonusScore => BonusOverride != -1 ? BonusOverride : Math.Max(0, LinearScale * TIER_COUNT + 1 - TIER_COUNT) * 0.5;
	public bool HasBonusScore => BonusScore > 0;
	public double LinearScale => Miss ? 0 : 1 - Math.Pow(Math.Abs(HitTime - Position) / LENIENCY, 1.5);
	public double Offset => Position - HitTime;
	public int Tier => TierOverride != -1 ? TierOverride : Miss ? 0 : (int)Math.Max(1, Math.Min(TIER_COUNT, 1 + Math.Floor(LinearScale * TIER_COUNT)));

	public static UnbeatableWhiteLabelJudgement Worst(UnbeatableWhiteLabelJudgement current, UnbeatableWhiteLabelJudgement other) => other.BaseScore < current.BaseScore ? other : current;
}
