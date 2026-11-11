using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Game.Entities;

namespace CloneDash.Game.Statistics;

public class StatisticsData : IRhythmGameComponent
{
	public StatisticsImpressiveness Title;
	public StatisticsGrade Grade;
	public ISongChart? Chart;
	public Func<float, float>? AccuracyFunction;
	public List<DashEnemy> OrderedEnemies = [];
	public Dictionary<DashEnemy, EnemyStatistics> EnemyInfo = [];

	public int Score { get; private set; } = 0;
	public double Accuracy { get; private set; } = 0;
	public int MaxCombo { get; private set; }
	public int Perfects { get; private set; } = 0;
	public int Greats { get; private set; } = 0;
	public int Passes { get; private set; } = 0;
	public int Misses { get; private set; } = 0;
	public int Earlys { get; private set; } = 0;
	public int Exacts { get; private set; } = 0;
	public int Lates { get; private set; } = 0;
	public int Hearts { get; private set; } = 0;
	public int Music { get; private set; } = 0;

	int comboBreaks;
	int totalNotes;


	public void RegisterEnemy(DashEnemy enemy) {
		if (EnemyInfo.ContainsKey(enemy)) return;

		OrderedEnemies.Add(enemy);
		EnemyInfo[enemy] = new(enemy);
		if (enemy.Type == MuseDash1EntityType.SustainBeam)
			EnemyInfo[enemy].End = new(enemy);
	}

	public void UploadMaxCombo(int maxCombo) => MaxCombo = maxCombo;

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

	public void Compute() {
		// Assume all perfect until we know otherwise
		Title = StatisticsImpressiveness.AllPerfect;

		Perfects = 0;
		Greats = 0;
		Passes = 0;
		Misses = comboBreaks;
		Earlys = 0;
		Exacts = 0;
		Lates = 0;
		Hearts = 0;
		Music = 0;
		totalNotes = 0;

		foreach (var ent in OrderedEnemies) {
			var info = EnemyInfo[ent];
			totalNotes += info.End == null ? 1 : 2;
			Count(info);
			if (info.End != null)
				Count(info.End);
		}

		float accuracy = GetRoundedAccuracy(GetTrueAccuracy());
		Accuracy = accuracy * 100d;
		Grade = GetGrade(accuracy, comboBreaks == 0);
	}

	public void BreakCombo() => comboBreaks++;

	static bool BreaksCombo(DashEnemy enemy) => enemy.Type is not (MuseDash1EntityType.Ghost or MuseDash1EntityType.Heart or MuseDash1EntityType.Score);

	static StatisticsGrade GetGrade(float accuracy, bool fullCombo) {
		if (accuracy <= 0.5999f)
			return fullCombo ? StatisticsGrade.A : StatisticsGrade.D;
		if (accuracy <= 0.6999f)
			return fullCombo ? StatisticsGrade.A : StatisticsGrade.C;
		if (accuracy <= 0.7999f)
			return fullCombo ? StatisticsGrade.A : StatisticsGrade.B;
		if (accuracy <= 0.8999f)
			return StatisticsGrade.A;
		if (accuracy <= 0.9499f)
			return StatisticsGrade.S;
		return accuracy < 1f ? StatisticsGrade.SS : StatisticsGrade.SSS;
	}

	static float GetRoundedAccuracy(float accuracy) {
		float rounded = (int)Math.Round((double)(accuracy / 0.0001f)) * 0.0001f;
		if (accuracy < rounded && (rounded == 0.6f || rounded == 0.7f || rounded == 0.8f || rounded == 0.9f || rounded == 1f))
			rounded -= 0.0001f;
		return rounded;
	}

	EnemyStatisticsState GetCountedState(EnemyStatistics info) {
		if (info.Enemy is not DoubleHitEnemy { Partner: DashEnemy partner } || !IsHit(info.State))
			return info.State;

		var partnerState = EnemyInfo[partner].State;
		if (!IsHit(partnerState))
			return EnemyStatisticsState.InPlay;
		return info.State == EnemyStatisticsState.Great || partnerState == EnemyStatisticsState.Great ? EnemyStatisticsState.Great : EnemyStatisticsState.Perfect;
	}

	static bool IsHit(EnemyStatisticsState state) => state is EnemyStatisticsState.Perfect or EnemyStatisticsState.Great;

	float GetTrueAccuracy() {
		if (totalNotes == 0)
			return 0;
		float accuracy = (Greats * 0.5f + Perfects + Passes + (float)(Hearts + Music)) / totalNotes;
		return AccuracyFunction?.Invoke(accuracy) ?? accuracy;
	}

	void Count(EnemyStatistics info) {
		switch (info.Accuracy) {
			case EnemyStatisticsAccuracy.Early: Earlys++; break;
			case EnemyStatisticsAccuracy.Late: Lates++; break;
			case EnemyStatisticsAccuracy.Precise: Exacts++; break;
		}

		switch (GetCountedState(info)) {
			case EnemyStatisticsState.Missed:
				DowngradeTitle(ref Title, StatisticsImpressiveness.Cleared);
				break;
			case EnemyStatisticsState.Passed:
				switch (info.Enemy.Type) {
					case MuseDash1EntityType.Gear: Passes++; break;
					case MuseDash1EntityType.Heart: Hearts++; break;
					case MuseDash1EntityType.Score: Music++; break;
				}
				break;
			case EnemyStatisticsState.Great:
				Greats++;
				DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
				break;
			case EnemyStatisticsState.Perfect:
				Perfects++;
				break;

			case EnemyStatisticsState.InPlay:
				break;
		}
	}

	public void Reset() {
		Title = StatisticsImpressiveness.AllPerfect;
		Accuracy = 0;
		Grade = StatisticsGrade.F;
		Score = 0;
		MaxCombo = 0;
		comboBreaks = 0;
		foreach (var kvp in EnemyInfo) {
			EnemyInfo[kvp.Key].Reset();
		}
	}

	public EnemyStatistics GetStatisticsForEnemy(DashEnemy enemy)
		=> EnemyInfo.TryGetValue(enemy, out var stats) ? stats : throw new Exception("Unregistered CD_BaseEnemy.");

	public EnemyStatisticsAccuracy Hit(DashEnemy enemy, double hitTime) {
		var stats = GetStatisticsForEnemy(enemy);
		var accuracy = stats.Hit(hitTime);

		if (stats.State != EnemyStatisticsState.Perfect) {
			DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
		}

		return accuracy;
	}

	public EnemyStatisticsAccuracy HitEnd(DashEnemy enemy, double hitTime) {
		var end = GetStatisticsForEnemy(enemy).End ?? throw new Exception("Enemy has no end judgement.");
		var accuracy = end.Hit(hitTime);
		if (end.State != EnemyStatisticsState.Perfect)
			DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
		return accuracy;
	}

	public void MissEnd(DashEnemy enemy) {
		DowngradeTitle(ref Title, StatisticsImpressiveness.Cleared);
		var end = GetStatisticsForEnemy(enemy).End ?? throw new Exception("Enemy has no end judgement.");
		if (end.State != EnemyStatisticsState.Missed)
			comboBreaks++;
		end.Miss();
	}

	public void Great(DashEnemy enemy) {
		GetStatisticsForEnemy(enemy).Great();
		DowngradeTitle(ref Title, StatisticsImpressiveness.FullCombo);
	}

	public void Miss(DashEnemy enemy) {
		DowngradeTitle(ref Title, StatisticsImpressiveness.Cleared);
		var stats = GetStatisticsForEnemy(enemy);
		if (stats.State != EnemyStatisticsState.Missed && BreaksCombo(enemy))
			comboBreaks++;
		stats.Miss();
	}

	public void Pass(DashEnemy enemy) => GetStatisticsForEnemy(enemy).Pass();

	public StatisticsData(ISongChart? chart) {
		Chart = chart;
		Reset();
	}

	public static StatisticsData CreateDummy(StatisticsGrade grade) {
		var (perfects, greats, misses) = grade switch {
			StatisticsGrade.SSS => (500, 0, 0),
			StatisticsGrade.SS => (480, 20, 0),
			StatisticsGrade.S => (440, 40, 20),
			StatisticsGrade.A => (380, 60, 60),
			StatisticsGrade.B => (330, 80, 90),
			StatisticsGrade.C => (280, 90, 130),
			_ => (200, 100, 200)
		};

		return new(null) {
			Accuracy = (perfects + greats * .5d) / (perfects + greats + misses) * 100d,
			Earlys = greats / 2,
			Grade = grade,
			Greats = greats,
			Lates = greats - greats / 2,
			MaxCombo = misses == 0 ? perfects + greats : (perfects + greats) / 3,
			Misses = misses,
			Passes = 40,
			Perfects = perfects,
			Score = perfects * 600 + greats * 300,
			Title = misses > 0 ? StatisticsImpressiveness.Cleared : greats > 0 ? StatisticsImpressiveness.FullCombo : StatisticsImpressiveness.AllPerfect
		};
	}

	public bool CalculateFullCombo() {
		Compute();
		return Misses <= 0;
	}
}