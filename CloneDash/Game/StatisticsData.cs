using CloneDash.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Game
{
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

	public enum CD_StatisticsTitle
	{
		InPlay = -1,
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

			return Accuracy;
		}
	}

	// WIP
	public class StatisticsData
	{
		public CD_StatisticsTitle Title;
		public CD_StatisticsGrade Grade;
		public ChartSheet? Sheet;
		public Dictionary<CD_BaseEnemy, CD_EnemyStatistics> EnemyInfo = [];

		public int Score;
		public int MaxCombo;

		public void RegisterEnemy(CD_BaseEnemy enemy) {
			EnemyInfo[enemy] = new(enemy);
		}

		public void Compute() {

		}

		public void Reset() {
			Title = CD_StatisticsTitle.InPlay;
			Grade = CD_StatisticsGrade.F;
			Score = 0;
			MaxCombo = 0;
			foreach (var kvp in EnemyInfo) {
				EnemyInfo[kvp.Key].Reset();
			}
		}

		public CD_EnemyStatistics GetStatisticsForEnemy(CD_BaseEnemy enemy)
			=> EnemyInfo.TryGetValue(enemy, out var stats) ? stats : throw new Exception("Unregistered CD_BaseEnemy.");

		public void Hit(CD_BaseEnemy enemy, double hitTime) => GetStatisticsForEnemy(enemy).Hit(hitTime);

		public void Miss(CD_BaseEnemy enemy) => GetStatisticsForEnemy(enemy).Miss();

		public void Pass(CD_BaseEnemy enemy) => GetStatisticsForEnemy(enemy).Pass();

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
}
