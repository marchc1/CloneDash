namespace CloneDash.Game.Statistics;

public enum CD_StatisticsImpressiveness
{
	Failed = 0,
	Cleared = 1 << 0,
	FullCombo = 1 << 1 | Cleared,
	AllPerfect = 1 << 2 | FullCombo
}
