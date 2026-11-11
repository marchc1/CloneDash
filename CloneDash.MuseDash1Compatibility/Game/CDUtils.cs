using CloneDash.Game;
using CloneDash.Game.Input;

namespace CloneDash;

public static class CDUtils
{
	public static int DetermineFever(float fever, bool perfect) => (int)Math.Round((double)(fever * (perfect ? 1f : .5f)));

	public static int DetermineScoreMultiplied(float baseScore, bool inFever, int combo, bool perfect) {
		int comboSteps = (int)Math.Floor((double)((float)combo / 100f / .1f));
		float comboRate = MathF.Min(comboSteps * .1f, .5f);
		float judgementRate = perfect ? 1f : .5f;
		if (inFever)
			judgementRate += .5f;
		return (int)Math.Round((double)(judgementRate * baseScore * (comboRate + 1f)), 1);
	}

	public static int DetermineScoreMultiplied(this MuseDash1RulesComponent rules, float baseScore, PollResult pollResult, double time) => DetermineScoreMultiplied(baseScore, rules.InFeverAt(time), rules.Combo.Value, pollResult.IsPerfect);
}
