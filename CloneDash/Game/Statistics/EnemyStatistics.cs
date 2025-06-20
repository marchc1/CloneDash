using Nucleus;

namespace CloneDash.Game.Statistics;

public class EnemyStatistics
{
	public CD_BaseEnemy Enemy;
	public EnemyStatisticsState State;
	public EnemyStatisticsAccuracy Accuracy;

	public void Reset() {
		State = EnemyStatisticsState.InPlay;
		Accuracy = EnemyStatisticsAccuracy.NotApplicable;
	}

	public EnemyStatistics(CD_BaseEnemy enemy) {
		Enemy = enemy;
		Reset();
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
		var prePerfectRange = Enemy.PrePerfectRange;
		var postPerfectRange = Enemy.PostPerfectRange;

		Accuracy = hitTime > postPerfectRange / 2d
					? EnemyStatisticsAccuracy.Late
					: hitTime < -prePerfectRange / 2d
						? EnemyStatisticsAccuracy.Early
						: EnemyStatisticsAccuracy.Precise;

		double preperfect = -Enemy.PrePerfectRange, postperfect = Enemy.PostPerfectRange;
		State = NMath.InRange(hitTime, preperfect, postperfect) ? EnemyStatisticsState.Perfect : EnemyStatisticsState.Great;

		return Accuracy;
	}
}
