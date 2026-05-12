using CloneDash.Common;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Statistics;

namespace CloneDash.Scenes;

public interface IMuseDash1SceneUI
{
	void Initialize();
	void Think(double dt);
	void OpenVictory(StatisticsData stats);
	void CloseVictory();
	bool ShowingVictoryScreen();
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

	void CreatePerfectHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate);
	void CreateGreatHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate);
	void CreatePassText(double precision, PathwaySide pathway);
	void StartMultiHitText();
	void UpdateMultiHitText(int hits);
	void EndMultiHitText();
	void CreateScoreText(int scoreGiven);
	void CreateHealthText(float healthGiven);
	void StartWarning();
	void EndWarning();
	void SetSeeking(bool seeking);
	void Reset();
}
