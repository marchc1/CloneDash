using Nucleus;

namespace CloneDash.Game.Statistics;

public class CD_EnemyStatistics
{
	public CD_BaseEnemy Enemy;
	public CD_EnemyStatisticsState State;
	public CD_EnemyStatisticsAccuracy Accuracy;

	public void Reset() {
		State = CD_EnemyStatisticsState.InPlay;
		Accuracy = CD_EnemyStatisticsAccuracy.NotApplicable;
	}

	public CD_EnemyStatistics(CD_BaseEnemy enemy) {
		Enemy = enemy;
		Reset();
	}

	public void Miss() {
		Accuracy = CD_EnemyStatisticsAccuracy.NotApplicable;
		State = CD_EnemyStatisticsState.Missed;
	}

	public void Pass() {
		Accuracy = CD_EnemyStatisticsAccuracy.NotApplicable;
		State = CD_EnemyStatisticsState.Passed;
	}

	public CD_EnemyStatisticsAccuracy Hit(double hitTime) {
		var prePerfectRange = Enemy.PrePerfectRange;
		var postPerfectRange = Enemy.PostPerfectRange;

		Accuracy = hitTime > postPerfectRange / 2d
					? CD_EnemyStatisticsAccuracy.Late
					: hitTime < -prePerfectRange / 2d
						? CD_EnemyStatisticsAccuracy.Early
						: CD_EnemyStatisticsAccuracy.Precise;

		double preperfect = -Enemy.PrePerfectRange, postperfect = Enemy.PostPerfectRange;
		State = NMath.InRange(hitTime, preperfect, postperfect) ? CD_EnemyStatisticsState.Perfect : CD_EnemyStatisticsState.Great;

		return Accuracy;
	}
}
