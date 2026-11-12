using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game;

namespace CloneDash;

public static class CDUtils
{
	public static int DetermineFever(float fever, bool perfect) => (int)Math.Round((double)(fever * (perfect ? 1f : .5f)));

	public static int DetermineScoreMultiplied(float baseScore, bool inFever, int combo, bool perfect, in MuseDash1GameplayQuirks quirks, bool bossAttack, bool hidden) {
		int comboSteps = (int)Math.Floor((double)((float)combo / 100f / .1f));
		float comboRate = MathF.Min(comboSteps * .1f, .5f);
		float judgementRate = perfect ? quirks.PerfectScoreExtra : .5f;
		float feverRate = 1f;
		if (inFever) {
			feverRate = quirks.FeverScoreRate;
			judgementRate += .5f;
		}
		float bossRate = bossAttack ? quirks.BossAttackScoreRate : 1f;
		float hideRate = hidden ? quirks.HideNoteRate : 1f;
		return (int)Math.Round((double)((quirks.ScoreExtraRate + judgementRate + feverRate + bossRate + hideRate - 4f) * baseScore * (comboRate + 1f)), 1);
	}

	public static int DetermineScoreMultiplied(this MuseDash1RulesComponent rules, DashEnemy note, bool perfect, double time)
		=> DetermineScoreMultiplied(note.ScoreGiven, rules.InFeverAt(time), rules.Combo.Value, perfect, in rules.Quirks, note.IsBossAttack, note.Type == MuseDash1EntityType.Ghost);
}
