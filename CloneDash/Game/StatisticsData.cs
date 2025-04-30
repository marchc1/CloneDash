using CloneDash.Data;
using Nucleus;
using System.Security.Principal;

namespace CloneDash.Game;

public enum CD_EnemyStatisticsState
{
	InPlay,
	Missed,
	Passed,
	Great,
	Perfect
}

public enum CD_EnemyStatisticsAccuracy
{
	NotApplicable,

	Early,
	Precise,
	Late
}

public enum CD_StatisticsImpressiveness
{
	Failed = 0,
	Cleared = 1 << 0,
	FullCombo = 1 << 1 | Cleared,
	AllPerfect = 1 << 2 | FullCombo
}

public enum CD_StatisticsGrade
{
	SSS = 100,
	SS = 95,
	S = 90,
	A = 80,
	B = 70,
	C = 60,
	D = 0,
	F = -1
}

public static class CD_StatsExtensions
{
	public static bool IsOK(this CD_EnemyStatisticsState state) => state >= CD_EnemyStatisticsState.Passed;
}

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

		Accuracy = hitTime > (postPerfectRange / 2d) 
					? CD_EnemyStatisticsAccuracy.Late 
					: hitTime < (-prePerfectRange / 2d) 
						? CD_EnemyStatisticsAccuracy.Early 
						: CD_EnemyStatisticsAccuracy.Precise;

		double preperfect = -Enemy.PrePerfectRange, postperfect = Enemy.PostPerfectRange;
		State = NMath.InRange(hitTime, preperfect, postperfect) ? CD_EnemyStatisticsState.Perfect : CD_EnemyStatisticsState.Great;

		return Accuracy;
	}
}

public class StatisticsData
{
	public CD_StatisticsImpressiveness Title;
	public CD_StatisticsGrade Grade;
	public ChartSheet? Sheet;
	public List<CD_BaseEnemy> OrderedEnemies = [];
	public Dictionary<CD_BaseEnemy, CD_EnemyStatistics> EnemyInfo = [];

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

	private void DowngradeTitle(ref CD_StatisticsImpressiveness title, CD_StatisticsImpressiveness to) {
		switch (title) {
			case CD_StatisticsImpressiveness.AllPerfect:
				title = to;
				break;
			case CD_StatisticsImpressiveness.FullCombo:
				if(to != CD_StatisticsImpressiveness.AllPerfect)
					title = to;
				break;
			case CD_StatisticsImpressiveness.Cleared:
				if (to != CD_StatisticsImpressiveness.AllPerfect && to != CD_StatisticsImpressiveness.FullCombo)
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
		Title = CD_StatisticsImpressiveness.AllPerfect;

		Perfects = 0;
		Combo = 0;
		Greats = 0;
		Passes = 0;
		Misses = 0;
		Earlys = 0;
		Exacts = 0;
		Lates = 0;

		foreach(var ent in OrderedEnemies) {
			var info = EnemyInfo[ent];

			switch (info.Accuracy) {
				case CD_EnemyStatisticsAccuracy.Early: Earlys++; break;
				case CD_EnemyStatisticsAccuracy.Late: Lates++; break;
				case CD_EnemyStatisticsAccuracy.Precise: Exacts++; break;
			}

			switch (info.State) {
				case CD_EnemyStatisticsState.Missed:
					Misses++;
					ResetCombo();
					DowngradeTitle(ref Title, CD_StatisticsImpressiveness.Cleared);
					break;
				case CD_EnemyStatisticsState.Passed:
					Passes++;
					break;
				case CD_EnemyStatisticsState.Great:
					Greats++;
					UpCombo();
					DowngradeTitle(ref Title, CD_StatisticsImpressiveness.FullCombo);
					break;
				case CD_EnemyStatisticsState.Perfect:
					Perfects++;
					UpCombo();
					break;

				case CD_EnemyStatisticsState.InPlay:
					break;
			}
		}


		if (Title == CD_StatisticsImpressiveness.AllPerfect) {
			Accuracy = 100d;
			Grade = CD_StatisticsGrade.SSS;
		}
		else {
			double gradePercentage = (Perfects + (Greats * .5d)) / (Perfects + Greats + Misses);
			if (gradePercentage >= 95d) Grade = CD_StatisticsGrade.SS;
			else if (gradePercentage >= 90d) Grade = CD_StatisticsGrade.S;
			else if (gradePercentage >= 80d) Grade = CD_StatisticsGrade.A;
			else if (gradePercentage >= 70d) Grade = CD_StatisticsGrade.B;
			else if (gradePercentage >= 60d) Grade = CD_StatisticsGrade.C;
			else Grade = CD_StatisticsGrade.D;

			Accuracy = gradePercentage * 100;
		}
	}

	public void Reset() {
		Title = CD_StatisticsImpressiveness.AllPerfect;
		Accuracy = 0;
		Grade = CD_StatisticsGrade.F;
		Score = 0;
		MaxCombo = 0;
		foreach (var kvp in EnemyInfo) {
			EnemyInfo[kvp.Key].Reset();
		}
	}

	public CD_EnemyStatistics GetStatisticsForEnemy(CD_BaseEnemy enemy)
		=> EnemyInfo.TryGetValue(enemy, out var stats) ? stats : throw new Exception("Unregistered CD_BaseEnemy.");

	public CD_EnemyStatisticsAccuracy Hit(CD_BaseEnemy enemy, double hitTime) {
		var stats = GetStatisticsForEnemy(enemy);
		var accuracy = stats.Hit(hitTime);

		if(stats.State != CD_EnemyStatisticsState.Perfect) {
			DowngradeTitle(ref Title, CD_StatisticsImpressiveness.FullCombo);
		}

		return accuracy;
	}

	public void Miss(CD_BaseEnemy enemy) {
		DowngradeTitle(ref Title, CD_StatisticsImpressiveness.Cleared);
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
		foreach (var kvp in EnemyInfo) {
			if (!kvp.Value.State.IsOK())
				return false;
		}
		return true;
	}
}