using CloneDash.Common;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Statistics;

namespace CloneDash.Scenes;

public interface IMuseDash1SceneUI : IDisposable
{
	void Initialize();
	void Think(double dt);
	void OpenVictory(StatisticsData stats);
	void OpenFailure();
	// Draws the fail screen's fade-in (if open) in the world pass, so the character can be drawn over it
	void RenderFailureUnderlay();
	bool ShowingVictoryScreen();
	bool ShowingFailureScreen();
	void PreRenderWorldspace();
	void PostRenderWorldspace();
	void RenderUI();
	void UpdateHP(double hp, double maxHP);
	void UpdateFeverProgress(double fever, double maxFever);
	void UpdateInFever(double feverRemainingTime, double feverTotalTime);
	void UpdateScore(double score);
	void UpdateAllPerfect(bool allPerfect);
	void UpdateFullCombo(bool fullCombo);
	void UpdateCombo(int currentCombo);

	void CreatePerfectHitText(double precision, PathwaySet pathways, bool inFever, EarlyLate earlylate);
	void CreateGreatHitText(double precision, PathwaySet pathways, bool inFever, EarlyLate earlylate);
	void CreatePassText(double precision, PathwaySet pathways);
	void StartMultiHitText();
	void UpdateMultiHitText(int hits);
	void EndMultiHitText();
	void CreateScoreText(int scoreGiven, PathwaySet pathways);
	void CreateHealthText(float healthGiven, PathwaySet pathways);
	void StartWarning();
	void EndWarning();
	void SetSeeking(bool seeking);
	void Reset();
	void UpdateHit();
}
