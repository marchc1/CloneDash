using CloneDash.Common.Game;
using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelScoreComponent : IRhythmGameComponent
{
	const double ACCURACY_BASELINE = 0.6;
	const int COMBO_HITS_PER_TIER = 20;
	const int COMBO_TIER_COUNT = 4;

	readonly int[] tierCounts = new int[UnbeatableWhiteLabelJudgement.TIER_COUNT + 1];
	readonly int weightedNoteCount;
	double accuracyRaw;
	double baseScoreRaw;
	double bonusScoreRaw;

	public UnbeatableWhiteLabelScoreComponent(Beatmap beatmap) {
		int remaining = beatmap.GetSignificantNoteCount();
		for (int tier = 1; tier <= COMBO_TIER_COUNT; tier++) {
			if (remaining >= COMBO_HITS_PER_TIER) {
				weightedNoteCount += COMBO_HITS_PER_TIER * GetComboFactor(tier);
				remaining -= COMBO_HITS_PER_TIER;
				continue;
			}

			weightedNoteCount += remaining * GetComboFactor(tier);
			remaining = 0;
			break;
		}

		weightedNoteCount += remaining * GetComboFactor(COMBO_TIER_COUNT);
		weightedNoteCount = Math.Max(1, weightedNoteCount);
	}

	public double Accuracy { get; private set; } = 1;
	public double BaseScore { get; private set; }
	public double BonusScore { get; private set; }
	public int ComboCount { get; private set; }
	public int ComboFactor => GetComboFactor(ComboTier);
	public int ComboTier => (int)Math.Min(COMBO_TIER_COUNT, 1 + Math.Floor((double)ComboCount / COMBO_HITS_PER_TIER));
	public int HitCount { get; private set; }
	public int MissCount { get; private set; }
	public double TotalScore => BaseScore + BonusScore;

	public void Add(UnbeatableWhiteLabelJudgement judgement) {
		baseScoreRaw += ComboFactor * judgement.BaseScore;
		bonusScoreRaw += ComboFactor * judgement.BonusScore;

		if (judgement.Miss) {
			MissCount++;
			ComboCount = 0;
		}
		else {
			HitCount++;
			ComboCount++;
		}

		int tier = judgement.Tier;
		tierCounts[tier]++;
		accuracyRaw += tier == UnbeatableWhiteLabelJudgement.TIER_COUNT ? 1 : ACCURACY_BASELINE * tier / UnbeatableWhiteLabelJudgement.TIER_COUNT;

		BaseScore = baseScoreRaw / weightedNoteCount * 1000000;
		BonusScore = bonusScoreRaw / weightedNoteCount * 1000000;
		Accuracy = accuracyRaw / (HitCount + MissCount);
	}

	public int GetTierCount(int tier) => tierCounts[tier];

	public void Reset() {
		Array.Clear(tierCounts);
		accuracyRaw = 0;
		baseScoreRaw = 0;
		bonusScoreRaw = 0;
		Accuracy = 1;
		BaseScore = 0;
		BonusScore = 0;
		ComboCount = 0;
		HitCount = 0;
		MissCount = 0;
	}

	static int GetComboFactor(int tier) => 1 << (tier - 1);
}
