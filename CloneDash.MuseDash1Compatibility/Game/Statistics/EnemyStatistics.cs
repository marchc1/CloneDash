using Nucleus;

namespace CloneDash.Game.Statistics;

public class EnemyStatistics
{
	const double EARLY_LATE_THRESHOLD = 0.025;

	public DashEnemy Enemy;
	public EnemyStatisticsState State;
	public EnemyStatisticsAccuracy Accuracy;

	public void Reset() {
		State = EnemyStatisticsState.InPlay;
		Accuracy = EnemyStatisticsAccuracy.NotApplicable;
	}

	public EnemyStatistics(DashEnemy enemy) {
		Enemy = enemy;
		Reset();
	}

	public void Great() {
		Accuracy = EnemyStatisticsAccuracy.NotApplicable;
		State = EnemyStatisticsState.Great;
	}

	public void Miss() {
		Accuracy = EnemyStatisticsAccuracy.NotApplicable;
		State = EnemyStatisticsState.Missed;
	}

	public void Pass() {
		Accuracy = EnemyStatisticsAccuracy.NotApplicable;
		State = EnemyStatisticsState.Passed;
	}

	public EnemyStatisticsAccuracy Hit(double hitTime) {
		Accuracy = hitTime < -EARLY_LATE_THRESHOLD
					? EnemyStatisticsAccuracy.Late
					: hitTime > EARLY_LATE_THRESHOLD
						? EnemyStatisticsAccuracy.Early
						: EnemyStatisticsAccuracy.Precise;

		double preperfect = -Enemy.PrePerfectRange, postperfect = Enemy.PostPerfectRange;
		State = NMath.InRange(hitTime, preperfect, postperfect) ? EnemyStatisticsState.Perfect : EnemyStatisticsState.Great;

		return Accuracy;
	}
}
