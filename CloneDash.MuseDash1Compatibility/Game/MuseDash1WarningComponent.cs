using CloneDash.Common.Game;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1WarningComponent(MuseDash1ChartComponent chart, Conductor conductor, GameEventManager events, MuseDash1RulesComponent rules) : IRhythmGameComponent, IRhythmGameThinker
{
	public const double MAX_WARNING_TIME = 1.2;

	readonly List<DashEnemy> Warnings = [];

	int nextWarning;

	public static double GetWarningTime(DashEnemy enemy) => enemy.GetVisualHitTime() - Math.Clamp(enemy.Dt, 0, MAX_WARNING_TIME);

	public void Load() {
		foreach (var enemy in chart.EnemyManager.GetAll())
			if (enemy.Warns)
				Warnings.Add(enemy);
		Warnings.Sort((a, b) => GetWarningTime(a).CompareTo(GetWarningTime(b)));
	}

	public void Reset() => nextWarning = 0;

	public void SeekTo(double time) {
		while (nextWarning < Warnings.Count && GetWarningTime(Warnings[nextWarning]) < time)
			nextWarning++;
	}

	public void Think(FrameState frameState) {
		while (nextWarning < Warnings.Count && GetWarningTime(Warnings[nextWarning]) <= conductor.Time) {
			var enemy = Warnings[nextWarning++];
			if (!rules.IsDead())
				events.Fire(new WarningStartGameEvent(enemy));
		}
	}
}
