using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Statistics;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game;

public class MuseDash1HudComponent : IRhythmGameComponent, IRhythmGameEntityPostRenderer, IRhythmGamePostRenderer, IRhythmGameThinker, IDisposable
{
	readonly ComboComponent Combo;
	readonly ScreenEffectsComponent Effects;
	readonly FeverComponent Fever;
	readonly HealthComponent Health;
	readonly RhythmGameLevel level;
	readonly MashComponent Mash;
	readonly MusicComponent Music;
	readonly MuseDash1RulesComponent rules;
	readonly MuseDash1ScenesComponent Scenes;
	readonly ScoreComponent Score;
	readonly StatisticsData Stats;

	public MuseDash1HudComponent(ComboComponent combo, ScreenEffectsComponent effects, GameEventManager events, FeverComponent fever, HealthComponent health, RhythmGameLevel level, MashComponent mash, MusicComponent music, ResultComponent results, MuseDash1RulesComponent rules, MuseDash1ScenesComponent scenes, ScoreComponent score, StatisticsData stats) {
		Combo = combo;
		Effects = effects;
		Fever = fever;
		Health = health;
		this.level = level;
		Mash = mash;
		Music = music;
		this.rules = rules;
		Scenes = scenes;
		Score = score;
		Stats = stats;

		results.CanClear = () => UI != null && !UI.ShowingVictoryScreen();

		events.Listen<FeverChangedGameEvent>(UpdateFeverProgress);
		events.Listen<FeverStartGameEvent>(EnterFever);
		events.Listen<MashEndGameEvent>(EndMultiHitText);
		events.Listen<MashStartGameEvent>(StartMultiHitText);
		events.Listen<NoteHitGameEvent>(CreateHitText);
		events.Listen<NoteMissedGameEvent>(BreakFullCombo);
		events.Listen<PlayerDamagedGameEvent>(BreakFullCombo);
		events.Listen<ScoreChangedGameEvent>(UpdateScore);
		events.Listen<SeekFinishedGameEvent>(EndSeeking);
		events.Listen<SongClearedGameEvent>(ShowVictory);
		events.Listen<SongFailedGameEvent>(ShowFailure);
		events.Listen<UnpauseStartGameEvent>(PlayUnpauseSound);
		events.Listen<WarningStartGameEvent>(StartWarning);
	}

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public IMuseDash1SceneUI? UI { get; private set; }

	public void Dispose() {
		UI?.Dispose();
		UI = null;
	}

	public double GetBgScrollSpeedMultiplier() => 1 - Effects.Get<BgFreezeScreenEffect>().Value;

	public void Load() {
		// The Scene UI never changes, it always inherits the original starting scene it seems
		UI = Scenes.GetFirstScene()?.CreateUI();

		UI?.Initialize();

		UI?.UpdateAllPerfect(true);
		UI?.UpdateFullCombo(true);

		if (!CommandLine().CheckParm("-mdbmsc", out var p) && Scenes.HasActiveScene(out var sceneInstance))
			sceneInstance.PlaySound(SceneSound.Begin, 0);
	}

	public void PostRender(FrameState frameState) {
		UI?.RenderUI();
	}

	public void PostRenderEntities(FrameState frameState) {
		UI?.PostRenderWorldspace();
		Rlgl.DrawRenderBatchActive();
	}

	public void Reset() {
		UI?.UpdateAllPerfect(true);
		UI?.UpdateFullCombo(true);
		UI?.Reset();
		UI?.SetSeeking(true);
		UI?.UpdateScore(0);
		UI?.UpdateCombo(0);
		UI?.UpdateHP(Health.Max, Health.Max);
		UI?.UpdateFeverProgress(0, 0);
	}

	public void Think(FrameState frameState) {
		if (!rules.IsDead() && Mash.InMashState)
			UpdateMashTextEffect();

		var sceneUI = UI;
		if (sceneUI != null) {
			sceneUI.UpdateCombo(Combo.Value);
			if (Fever.InFever)
				sceneUI.UpdateInFever(Fever.TimeLeft, Fever.Duration);

			if (Mash.MashingEntity != null)
				sceneUI.UpdateMultiHitText(Mash.MashingEntity.Hits);

			sceneUI.UpdateHP(Health.Value, Health.Max);
		}

		if (Scenes.HasActiveScene(out var scene)) {
			scene.Think(GetBgScrollSpeedMultiplier());
		}
		UI?.Think(globals.CurTimeDelta);
	}

	public void UpdateMashTextEffect() {
		if (!IValidatable.IsValid(Mash.MashingEntity)) return;
		UI?.UpdateMultiHitText(Mash.MashingEntity.Hits);
	}

	private void BreakFullCombo(ref NoteMissedGameEvent ev) => UI?.UpdateFullCombo(false);

	private void BreakFullCombo(ref PlayerDamagedGameEvent ev) => UI?.UpdateFullCombo(false);

	private void CreateHitText(ref NoteHitGameEvent ev) {
		if (ev.Perfect)
			UI?.CreatePerfectHitText(ev.Precision, ev.Pathway.AsSet, ev.InFever, ev.EarlyLate);
		else {
			UI?.CreateGreatHitText(ev.Precision, ev.Pathway.AsSet, ev.InFever, ev.EarlyLate);
			UI?.UpdateAllPerfect(false);
		}
		UI?.UpdateHit();
	}

	private void EndMultiHitText(ref MashEndGameEvent ev) {
		if (ev.Note != null)
			UI?.UpdateMultiHitText(ev.Note.Hits);
		UI?.EndMultiHitText();
	}

	private void EndSeeking(ref SeekFinishedGameEvent ev) => UI?.SetSeeking(false);

	private void EnterFever(ref FeverStartGameEvent ev) {
		UI?.UpdateInFever(Fever.TimeLeft, ev.Duration);
		UI?.PlayFeverTips(Fever.Manual ? MuseDash1FeverTipsClip.End : MuseDash1FeverTipsClip.Light);
	}

	private void PlayUnpauseSound(ref UnpauseStartGameEvent ev) {
		if (Scenes.HasActiveScene(out var scene))
			scene.PlaySound(SceneSound.Unpause, 0);
	}

	void ShowFailure(ref SongFailedGameEvent ev) {
		if (Fever.Manual)
			UI?.PlayFeverTips(MuseDash1FeverTipsClip.Failed);
		if (UI != null && !UI.ShowingFailureScreen())
			UI.OpenFailure();
	}

	void ShowVictory(ref SongClearedGameEvent ev) {
		if (!Health.Dead)
			Stats.UploadScore(Score.Value);
		UI?.OpenVictory(Stats);
		Music.Pause();
	}

	private void StartMultiHitText(ref MashStartGameEvent ev) {
		if (!level.IsSeeking) {
			UI?.StartMultiHitText();
			UpdateMashTextEffect();
		}
	}

	private void StartWarning(ref WarningStartGameEvent ev) => UI?.StartWarning();

	private void UpdateFeverProgress(ref FeverChangedGameEvent ev) {
		UI?.UpdateFeverProgress(ev.Value, ev.Max);
		if (Fever.Manual && ev.Max > 0 && ev.Value >= ev.Max)
			UI?.PlayFeverTips(MuseDash1FeverTipsClip.Start);
	}

	private void UpdateScore(ref ScoreChangedGameEvent ev) => UI?.UpdateScore(ev.Score);
}