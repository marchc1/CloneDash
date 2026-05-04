using CloneDash.Game;
using CloneDash.Game.Input;

namespace CloneDash;

public static class CDUtils
{
	public static double DetermineScoreMultiplied(double baseScore, bool inFever, int combo, double accuracy) {
		if (combo <= 9) baseScore *= 1.0;
		else if (combo <= 19) baseScore *= 1.1;
		else if (combo <= 29) baseScore *= 1.2;
		else if (combo <= 39) baseScore *= 1.3;
		else if (combo <= 49) baseScore *= 1.4;
		else baseScore *= 1.5;

		accuracy = Math.Abs(accuracy);

		if (inFever)
			baseScore *= 1.5f;

		if (accuracy >= 25)
			baseScore *= (inFever ? 0.66666666666 : .5);

		return Math.Round(baseScore);
	}

	public static double DetermineScoreMultiplied(this IDashGame game, double baseScore, in PollResult pollResult) => DetermineScoreMultiplied(baseScore, game.IsInFever(), game.GetCurrentCombo(), pollResult.DistanceToHit);
}
