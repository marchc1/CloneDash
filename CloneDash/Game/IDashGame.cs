using CloneDash.Data;
using CloneDash.Game.Input;
using CloneDash.Game.Statistics;
using CloneDash.Scenes;
using System.Runtime.CompilerServices;

namespace CloneDash.Game;

public struct DashGameParams
{
	public ChartSheet? Sheet;
	public bool Autoplay;
	public int Measure;

	public DashGameParams(ChartSheet sheet) {
		Sheet = sheet;
	}

	public DashGameParams WithAutoplay(bool autoplay) {
		Autoplay = autoplay;
		return this;
	}

	public DashGameParams WithMeasure(int measure) {
		Measure = measure;
		return this;
	}
}

public interface IDashGameRuntimeComponent
{
	/// <summary>
	/// Ran to initialize a game component
	/// </summary>
	void Initialize(IDashGame game);
	IDashGame GetGame();
}

public interface IDashEnemy
{
	/// <summary>
	/// Ran to initialize a game enemy
	/// </summary>
	void Initialize(IDashGame game, in DashEnemyInfo info);
	IDashGame GetGame();

	void OnSignalReceived<T>(IDashEnemy from, EntitySignalType signalType, T? data);
	void SendSignal<T>(IDashEnemy to, EntitySignalType signalType, T? data);
	void BroadcastSignal<T>(EntitySignalType signalType, T? data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]	public void OnSignalReceived(IDashEnemy from, EntitySignalType signalType) => OnSignalReceived<object>(from, signalType, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]	public void SendSignal(IDashEnemy to, EntitySignalType signalType) => SendSignal<object>(to, signalType, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]	public void BroadcastSignal(EntitySignalType signalType) => BroadcastSignal<object>(signalType, null);

	void Kill();
	double CalcVisualShowTime();
	double CalcVisualHitTime();
	double CalcJudgementShowTime();
	double CalcJudgementHitTime();
	double CalcVisualTimeUntilHit();
	double CalcVisualTimeUntilEnd();
	double CalcJudgementTimeUntilHit();
	double CalcJudgementTimeUntilEnd();

	ref readonly DashEnemyInfo GetInfo();
	PathwaySide GetPathway();
}

// Just in case we need a sep interface here...
public interface IDashBoss : IDashEnemy;

public interface IDashGame
{
	T CreateEnemy<T>(in DashEnemyInfo info) where T : IDashEnemy, new();

	IReadOnlyList<IDashEnemy> GetVisibleEnemies();
	IReadOnlyList<IDashEnemy> GetAllEnemies();

	IDashBoss GetBossEnemy();

	// Hypotheticals:
	// IReadOnlyList<IDashEnemy> GetAllRemainingEnemies();
	// IReadOnlyList<IDashEnemy> GetAllDefeatedEnemies();
	// (we probably won't need these...)

	/// <summary>
	/// Gets the current active scene runtime. This may change due to scene changes!
	/// </summary>
	/// <returns></returns>
	ISceneRuntime GetCurrentScene();
	/// <summary>
	/// Gets all scenes that were loaded at initialization.
	/// </summary>
	/// <returns></returns>
	IReadOnlyList<ISceneRuntime> GetAllScenes();

	StatisticsData GetStatisticsData();
	Conductor GetConductor();

	/// <summary>
	/// Broadcasts an entity signal message.
	/// </summary>
	void BroadcastEntitySignal<T>(IDashEnemy entityFrom, EntitySignalType signalType, T? data = default);
	/// <summary>
	/// Sends a targeted entity signal message.
	/// </summary>
	void SendEntitySignal<T>(IDashEnemy entityFrom, IDashEnemy entityTo, EntitySignalType signalType, T? data = default);

	/// <summary>
	/// Is the player mashing
	/// </summary>
	/// <returns></returns>
	bool IsMashing();

	/// <summary>
	/// Give the player damage. This may kill the player.
	/// <param name="damage">Damage to do. May be modified.</param>
	/// </summary>
	PlayerDamageResult GivePlayerDamage(IDashEnemy? responsible, double damage);
	/// <summary>
	/// Adds to the players score.
	/// </summary>
	/// <param name="score">Score to grant. May be modified.</param>
	PlayerScorePointResult GivePlayerScorePoints(IDashEnemy? responsible, double score);
	/// <summary>
	/// Adds to the players fever.
	/// </summary>
	/// <param name="score">Fever to grant. May be modified.</param>
	PlayerFeverPointResult GivePlayerFeverPoints(IDashEnemy? responsible, double fever);

	/// <summary>
	/// Grant the player health.
	/// </summary>
	/// <param name="responsible"></param>
	/// <param name="healthGiven"></param>
	PlayerHealResult GivePlayerHealth(IDashEnemy? responsible, double healthGiven);

	/// <summary>
	/// Adds +1 to the players combo.
	/// </summary>
	/// <param name="responsible"></param>
	void GivePlayerCombo(IDashEnemy? responsible);

	/// <summary>
	/// Gets the current combo
	/// </summary>
	int GetCurrentCombo();

	/// <summary>
	/// Resets the combo back to 0
	/// </summary>
	void ResetCombo();

	/// <summary>
	/// Returns true if the player is in fever
	/// </summary>
	/// <returns></returns>
	bool IsInFever();
	int NewEnemySortIndexCounter();
}

public struct PlayerDamageResult
{
	public bool Success;
	public double DamageDone;
	public bool KilledPlayer;
}

public struct PlayerHealResult
{
	public bool Success;
	public double HealthGranted;
}

public struct PlayerScorePointResult
{
	public bool Success;
	public double ScoreGranted;
}

public struct PlayerFeverPointResult
{
	public bool Success;
	public double FeverGranted;
	public bool EnteredFever;
}