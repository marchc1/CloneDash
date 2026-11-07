using CloneDash.Game;
using CloneDash.Game.Input;

namespace CloneDash;

public static class CDUtils
{
	public static int DetermineScoreMultiplied(float baseScore, bool inFever, int combo, bool perfect) {
		float comboRate = MathF.Min(combo / 10 * .1f, .5f) + 1;
		float judgementRate = (perfect ? 1f : .5f) + (inFever ? .5f : 0f);
		return (int)Math.Round(judgementRate * (baseScore * comboRate), MidpointRounding.AwayFromZero);
	}

	public static int DetermineScoreMultiplied(this MuseDash1RulesComponent rules, float baseScore, PollResult pollResult) => DetermineScoreMultiplied(baseScore, rules.Fever.InFever, rules.Combo.Value, pollResult.IsPerfect);
}
