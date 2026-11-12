using CloneDash.Characters;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.Unity;
using CloneDash.Game.Statistics;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Commands;

namespace CloneDash.Scenes;

public record MuseDash1VictoryAssets(UnityCanvas Canvas, MuseDash1CanvasBehaviours Behaviours, UnityCanvas? Like, MuseDash1LikeButton? LikeButton, IAudioClip? Music, IAudioClip? Click, IAudioClip? LikeSound, IAudioClip? UnlikeSound);

[MarkForStaticConstruction]
public class MuseDash1VictoryScreen
{
	public static ConVar md_advancedjudgment = new(nameof(md_advancedjudgment), "1", FCvar.Saved, "Shows the EARLY/LATE counts on the victory screen, like Muse Dash's Advanced Judgment toggle.", 0, 1);
	public static ConCommand md_dummyvictory = new(nameof(md_dummyvictory), OpenDummyVictory, DummyVictoryAutocomplete, "Opens a dummy Muse Dash 1 victory panel on the current level. Optional grade: SSS, SS, S, A, B, C or D.");

	const string ACCURACY = "PnlVictory_3D/SongTittle/TxtAccurcacy/TxtValue";
	const string ADVANCED_JUDGMENT = "PnlVictory_3D/DetailInfo/Other/AdvJudgment";
	const string BACKGROUND = "PnlVictory_2D/Bg";
	const string CHARACTER = "PnlVictory_2D/GirlShow/Point";
	const string CONTINUE = "PnlVictory_2D/Buttons/BtnContinue";
	const string DIFFICULTY = "PnlVictory_3D/SongTittle/ImgDifficulty/TxtDifficulty";
	const string EARLY = "PnlVictory_3D/DetailInfo/Other/AdvJudgment/TxtEarly/TxtValue";
	const string GREAT = "PnlVictory_3D/DetailInfo/Other/TxtGreat/TxtValue";
	const string HIGH_SCORE = "PnlVictory_3D/TxtHighScore";
	const string HIGH_SCORE_VALUE = "PnlVictory_3D/TxtHighScore/TxtValue";
	const string INFO = "PnlVictory_2D/Info";
	const string LATE = "PnlVictory_3D/DetailInfo/Other/AdvJudgment/TxtLate/TxtValue";
	public const string LIKE_SOUND = "sfx_count_finish";
	const string MAX_COMBO = "PnlVictory_3D/SongTittle/TxtMaxCombe/TxtValue";
	const string MISS = "PnlVictory_3D/DetailInfo/Other/TxtMiss/TxtValue";
	const string NEW_BEST = "PnlVictory_3D/TxtScore/TxtValue/TxtNewBest";
	const string PASS = "PnlVictory_3D/DetailInfo/Other/TxtPass/TxtValue";
	const string PERFECT = "PnlVictory_3D/DetailInfo/Other/TxtPerfect/TxtValue";
	const string RESTART = "PnlVictory_2D/Buttons/BtnRestart";
	const float RESTART_DELAY = 2f;
	const string SCORE = "PnlVictory_3D/TxtScore/TxtValue";
	const float SCORE_ROLL_DELAY = 1f;
	const float SCORE_ROLL_TIME = 0.75f;
	public const string UNLIKE_SOUND = "sfx_common_back";

	static readonly Dictionary<StatisticsGrade, string> GradeObjects = new() {
		[StatisticsGrade.SSS] = "ImgGradeGoldS",
		[StatisticsGrade.SS] = "ImgGradeSilverS",
		[StatisticsGrade.S] = "ImgGradeS",
		[StatisticsGrade.A] = "ImgGradeA",
		[StatisticsGrade.B] = "ImgGradeB",
		[StatisticsGrade.C] = "ImgGradeC",
		[StatisticsGrade.D] = "ImgGradeD"
	};

	readonly MuseDash1VictoryAssets assets;
	readonly bool hidden;
	readonly AudioPlaybackHandle music;
	readonly Action onContinue;
	readonly Action<bool>? onLikedChanged;
	readonly Action onRestart;
	readonly int score;
	bool finished;
	bool liked;
	bool likePressed;
	float likeTimer;
	float time;

	public MuseDash1VictoryScreen(MuseDash1VictoryAssets assets, StatisticsData stats, ISongChart? chart, int? highScore, bool liked, bool hidden, Action<bool>? onLikedChanged, bool autoplay, ICharacterVictoryInstance? character, Action onRestart, Action onContinue) {
		this.assets = assets;
		this.hidden = hidden;
		this.liked = liked;
		this.onContinue = onContinue;
		this.onLikedChanged = onLikedChanged;
		this.onRestart = onRestart;
		score = stats.Score;
		UpdateLikeIcons();

		string title = chart == null ? "" : chart.GetSong().FetchMetadata().Name.ToString();
		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.SetText(title);
		SetText(DIFFICULTY, chart?.FetchMetadata().Difficulty ?? "");
		SetText(ACCURACY, $"{stats.Accuracy:0.00} %");
		SetText(MAX_COMBO, stats.MaxCombo.ToString());
		SetText(PERFECT, stats.Perfects.ToString());
		SetText(GREAT, stats.Greats.ToString());
		SetText(PASS, stats.Passes.ToString());
		SetText(MISS, stats.Misses.ToString());
		SetText(EARLY, stats.Earlys.ToString());
		SetText(LATE, stats.Lates.ToString());
		SetText(SCORE, "0");

		if (highScore is int best)
			SetText(HIGH_SCORE_VALUE, best.ToString());
		else {
			HideText(HIGH_SCORE);
			HideText(HIGH_SCORE_VALUE);
		}
		if (Canvas.Find(NEW_BEST) is CanvasNode newBest)
			newBest.Active = !autoplay && highScore is int previous && score > previous;

		bool advancedJudgment = md_advancedjudgment.GetBool();
		if (Canvas.Find(ADVANCED_JUDGMENT) is CanvasNode judgment) {
			judgment.Active = advancedJudgment;
			Canvas.Play(ADVANCED_JUDGMENT, advancedJudgment ? "AdvJudgmentIn" : "AdvJudgmentOut", 1);
		}

		GradeObjects.TryGetValue(stats.Grade, out var gradeName);
		foreach (var grade in assets.Behaviours.GradeObjects)
			grade.Active = grade.Name == gradeName;

		if (character != null && Canvas.Find(CHARACTER) is CanvasNode point)
			point.CustomRender = character.Render;

		music = Play(assets.Music);
	}

	public UnityCanvas Canvas => assets.Canvas;

	public bool Click(float x, float y) {
		if (assets.Like != null && assets.LikeButton != null && assets.Like.HitTest(assets.LikeButton.Button, x, y)) {
			ToggleLike();
			return true;
		}
		if (Canvas.Find(CONTINUE) is CanvasNode continueButton && Canvas.HitTest(continueButton, x, y)) {
			Continue();
			return true;
		}
		if (Canvas.Find(RESTART) is CanvasNode restartButton && Canvas.HitTest(restartButton, x, y)) {
			Restart();
			return true;
		}
		return false;
	}

	public void Continue() {
		if (finished)
			return;

		finished = true;
		StopMusic();
		Play(assets.Click);
		onContinue();
	}

	public void PressLike() {
		likePressed = true;
		likeTimer = 0;
	}

	public void ReleaseLike() {
		if (likePressed && likeTimer <= (assets.LikeButton?.HoldTime ?? 0))
			ToggleLike();
		likePressed = false;
	}

	public void Restart() {
		if (finished || time < RESTART_DELAY)
			return;

		finished = true;
		StopMusic();
		Play(assets.Click);
		onRestart();
	}

	public void ToggleLike() {
		if (finished || hidden || assets.LikeButton == null)
			return;

		liked = !liked;
		Play(liked ? assets.LikeSound : assets.UnlikeSound);
		UpdateLikeIcons();
		onLikedChanged?.Invoke(liked);
	}

	public void Update(float deltaTime) {
		time += deltaTime;
		if (likePressed)
			likeTimer += deltaTime;
		float progress = Math.Clamp((time - SCORE_ROLL_DELAY) / SCORE_ROLL_TIME, 0, 1);
		SetText(SCORE, ((int)(score * progress)).ToString());
		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.Update(deltaTime);
		FillWideScreen();
	}

	void FillWideScreen() {
		if (Canvas.Find(BACKGROUND) is not CanvasNode background || background.Parent == null)
			return;

		float overflow = MathF.Max(0, (Canvas.Size.X - background.Parent.RectSize.X) / 2);
		background.SizeDelta = background.SizeDelta with { X = overflow * 2 };
		if (Canvas.Find(INFO) is CanvasNode info)
			info.MaskOverflow = new(overflow, 0);
	}

	static void DummyVictoryAutocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		if (curArgPos == 1)
			returns = [.. GradeObjects.Keys.Select(grade => grade.ToString())];
	}

	static void OpenDummyVictory(ConCommand cmd, in TokenizedCommand args) {
		var gradeName = args.Arg(1);
		var grade = StatisticsGrade.SSS;
		if (!gradeName.IsEmpty && (!Enum.TryParse(gradeName, true, out grade) || !GradeObjects.ContainsKey(grade))) {
			Logs.Warn($"Unknown grade '{gradeName}'. Use SSS, SS, S, A, B, C or D.");
			return;
		}

		if (EngineCore.LoadingLevel || EngineCore.Level?.RootPanel == null) {
			Logs.Warn("No level is ready yet; open the dummy victory panel once a level (like the main menu) has loaded.");
			return;
		}

		var assets = MD1SceneUI.ImportVictoryCanvas();
		if (assets == null) {
			Logs.Warn("Couldn't load the Muse Dash victory panel.");
			return;
		}

		StatisticsPanel? panel = null;
		panel = new StatisticsPanel(EngineCore.Level.RootPanel, StatisticsData.CreateDummy(grade), assets, () => panel?.Remove()) {
			Size = new(1, 1),
			DynamicallySized = true
		};
	}

	static AudioPlaybackHandle Play(IAudioClip? clip) {
		if (clip == null)
			return AudioPlaybackHandle.Null;

		clip.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		return audiosystem.PlaySound(clip, AudioPlaybackSettings.Unaltered);
	}

	void HideText(string path) {
		if (Canvas.Find(path) is CanvasNode node)
			node.Text = null;
	}

	void SetText(string path, string text) {
		if (Canvas.Find(path)?.Text is CanvasText canvasText)
			canvasText.Text = text;
		else
			Logs.Warn($"[MuseDash1VictoryScreen] No text at '{path}'.");
	}

	void StopMusic() {
		if (music.IsValid())
			audiosystem.StopSound(music);
	}

	void UpdateLikeIcons() {
		if (assets.LikeButton?.Hide is CanvasNode hide)
			hide.Active = hidden;
		if (assets.LikeButton?.Like is CanvasNode like)
			like.Active = liked;
	}
}
