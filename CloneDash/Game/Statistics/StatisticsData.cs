using CloneDash.Data;

namespace CloneDash.Game.Statistics;

public class StatisticsData
{
	public StatisticsImpressiveness Title;
	public StatisticsGrade Grade;
	public ChartSheet? Sheet;
	public List<CD_BaseEnemy> OrderedEnemies = [];
	public Dictionary<CD_BaseEnemy, EnemyStatistics> EnemyInfo = [];

	public int Score { get; private set; } = 0;
	public double Accuracy { get; private set; } = 0;
	public int Combo { get; private set; }
	public int MaxCombo { get; private set; }
	public int Perfects { get; private set; } = 0;
	public int Greats { get; private set; } = 0;
	public int Passes { get; private set; } = 0;
	public int Misses { get; private set; } = 0;
	public int Earlys { get; private set; } = 0;
	public int Exacts { get; private set; } = 0;
	public int Lates { get; private set; } = 0;


	public void RegisterEnemy(CD_BaseEnemy enemy) {
		if (EnemyInfo.ContainsKey(enemy)) return;

		OrderedEnemies.Add(enemy);
		EnemyInfo[enemy] = new(enemy);
	}

	public void UploadScore(int score) => Score = score;

	private void DowngradeTitle(ref StatisticsImpressiveness title, StatisticsImpressiveness to) {
		switch (title) {
			case StatisticsImpressiveness.AllPerfect:
				title = to;
				break;
			case StatisticsImpressiveness.FullCombo:
				if (to != StatisticsImpressiveness.AllPerfect)
					title = to;
				break;
			case StatisticsImpressiveness.Cleared:
				if (to != StatisticsImpressiveness.AllPerfect && to != StatisticsImpressiveness.FullCombo)
					title = to;
				break;
		}
	}

	private void SetMaxComboIfApplicable() {
		if (Combo > MaxCombo)
			MaxCombo = Combo;
	}
	public void ResetCombo() {
		SetMaxComboIfApplicable();
		Combo = 0;
	}
	public void UpCombo() {
		Combo++;
		SetMaxComboIfApplicable();
	}
	public void Compute() {
		// Assume all perfect until we know otherwise
		Title = StatisticsImpressiveness.AllPerfect;

		Perfects = 0;
		Combo = 0;
		Greats = 0;
		Passes = 0;
		Misses = 0;
		Earlys = 0;
		Exacts = 0;
		Lates = 0;

		foreach (var ent in OrderedEnemies) {
			var info = EnemyInfo[ent];

			switch (info.Accuracy) {
				case EnemyStatisticsAccuracy.Early: Earlys++; break;
				case EnemyStatisticsAccuracy.Late: Lates++; break;
				case EnemyStatisticsAccuracy.Precise: Exacts++; break;
			}

			switch (info.State) {
				case EnemyStatisticsState.Missed:
					Misses++;
					ResetCombo();
					DowngradeTitle(ref Title, StatisticsImpressiveness.Cleared);
					break;
				case EnemyStatisticsState.Passed:
					Passes++;
					break;
				case EnemyStatisticsState.Great:
					Greats++;
					UpCombo();
					DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
					break;
				case EnemyStatisticsState.Perfect:
					Perfects++;
					UpCombo();
					break;

				case EnemyStatisticsState.InPlay:
					break;
			}
		}


		if (Title == StatisticsImpressiveness.AllPerfect) {
			Accuracy = 100d;
			Grade = StatisticsGrade.SSS;
		}
		else {
			double gradePercentage = (Perfects + Greats * .5d) / (Perfects + Greats + Misses) * 100d;
			if (gradePercentage >= 95d) Grade = StatisticsGrade.SS;
			else if (gradePercentage >= 90d) Grade = StatisticsGrade.S;
			else if (gradePercentage >= 80d) Grade = StatisticsGrade.A;
			else if (gradePercentage >= 70d) Grade = StatisticsGrade.B;
			else if (gradePercentage >= 60d) Grade = StatisticsGrade.C;
			else Grade = StatisticsGrade.D;

			Accuracy = gradePercentage;
		}
	}

	public void Reset() {
		Title = StatisticsImpressiveness.AllPerfect;
		Accuracy = 0;
		Grade = StatisticsGrade.F;
		Score = 0;
		MaxCombo = 0;
		foreach (var kvp in EnemyInfo) {
			EnemyInfo[kvp.Key].Reset();
		}
	}

	public EnemyStatistics GetStatisticsForEnemy(CD_BaseEnemy enemy)
		=> EnemyInfo.TryGetValue(enemy, out var stats) ? stats : throw new Exception("Unregistered CD_BaseEnemy.");

	public EnemyStatisticsAccuracy Hit(CD_BaseEnemy enemy, double hitTime) {
		var stats = GetStatisticsForEnemy(enemy);
		var accuracy = stats.Hit(hitTime);

		if (stats.State != EnemyStatisticsState.Perfect) {
			DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
		}

		return accuracy;
	}

	public void Miss(CD_BaseEnemy enemy) {
		DowngradeTitle(ref Title, StatisticsImpressiveness.Cleared);
		GetStatisticsForEnemy(enemy).Miss();
	}

	public void Pass(CD_BaseEnemy enemy) => GetStatisticsForEnemy(enemy).Pass();


	public void Miss(CD_BaseMEntity ent) {
		if (ent is not CD_BaseEnemy enemy) throw new Exception(); // ugh
		Miss(enemy);
	}
	public void Pass(CD_BaseMEntity ent) {
		if (ent is not CD_BaseEnemy enemy) throw new Exception(); // ugh
		Pass(enemy);
	}
	public void Hit(CD_BaseMEntity ent, double hitTime) {
		if (ent is not CD_BaseEnemy enemy) throw new Exception(); // ugh
		Hit(enemy, hitTime);
	}

	public StatisticsData(ChartSheet? sheet) {
		Sheet = sheet;
		Reset();
	}

	public bool CalculateFullCombo() {
		Compute();
		return Misses <= 0;
	}
}