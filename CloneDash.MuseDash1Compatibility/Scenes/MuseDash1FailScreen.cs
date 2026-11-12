using CloneDash.Characters;
using CloneDash.Common.Game;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Commands;
using Nucleus.Common.Input;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Scenes;

public record MuseDash1FailAssets(UnityCanvas Canvas, MuseDash1CanvasBehaviours Behaviours, UnityCanvas? Text, IAudioClip? Music, IAudioClip? Click, IAudioClip? LikeSound, IAudioClip? UnlikeSound, float FailInShowDelay);

[MarkForStaticConstruction]
public class MuseDash1FailScreen : Panel
{
	public static ConCommand md_dummyfail = new(nameof(md_dummyfail), OpenDummyFail, "Opens a dummy Muse Dash 1 fail panel on the current level.");

	const string CHARACTER = "CharPoint/CharPoint2";
	const string CHARACTER_FAIL_IN = "CharPointFailIn/CharPoint2";
	public const string CLICK_SOUND = "sfx_common_button";
	const float FADE_TIME = 4f;
	const float KEYS_DELAY = 2.6667f;
	const int NO_FAIL_TEXT_ROLE = 17;
	const string RESTART = "ImgBgDown/BtnRestart";
	const float RESTART_DELAY = 2.5f;
	const string RETURN = "ImgBgDown/BtnReturn";
	const float RETURN_DELAY = 2.6167f;

	readonly MuseDash1FailAssets assets;
	readonly ICharacterFailureInstance? character;
	readonly bool failIn;
	readonly bool hidden;
	readonly MusicComponent? music;
	readonly float musicVolume = 1;
	readonly Action onRestart;
	readonly Action onReturn;
	readonly int role;
	readonly UnityCanvas? text;
	readonly string title;
	AudioPlaybackHandle failMusic;
	UnityCanvasPanel? failPanel;
	bool failInShown;
	bool finished;
	bool liked;
	bool likePressed;
	float likeTimer;
	float panelTime;
	float time;

	public MuseDash1FailScreen(Element? parent, MuseDash1FailAssets assets, ISongChart? chart, MusicComponent? music, bool showText, bool liked, bool hidden, Action onRestart, Action onReturn) : base(parent) {
		this.assets = assets;
		this.hidden = hidden;
		this.liked = liked;
		this.music = music;
		this.onRestart = onRestart;
		this.onReturn = onReturn;
		title = chart == null ? "" : chart.GetSong().FetchMetadata().Name.ToString();

		BorderSize = 0;
		DockPadding = RectangleF.Zero;
		SetPaintBackgroundEnabled(false);
		Size = new(1, 1);
		DynamicallySized = true;

		if (music != null && music.IsValid())
			musicVolume = audiosystem.GetSoundVolume(in music.Handle);

		if (CharacterMod.Instance.GetActive() is ICharacterDescriptor descriptor) {
			character = descriptor.CreateFailure();
			character.Initialize(EngineCore.Level as RhythmGameLevel);
			if (descriptor is MuseDash1CharacterDescriptor museDash) {
				failIn = museDash.GetFailIn();
				role = MuseDash1Compatibility.Characters.IndexOf(museDash.ConfigData);
			}
		}

		if (showText && role != NO_FAIL_TEXT_ROLE && assets.Text != null) {
			text = assets.Text;
			PlayDefaultStates(text);
		}

		if (music == null)
			Open();
	}

	public bool Opened => failPanel != null;

	public UnityCanvas Canvas => assets.Canvas;

	public bool Click(float x, float y) {
		if (assets.Behaviours.LikeButton is MuseDash1LikeButton likeButton && Canvas.HitTest(likeButton.Button, x, y)) {
			ToggleLike();
			return true;
		}
		if (Canvas.Find(RETURN) is CanvasNode returnButton && Canvas.HitTest(returnButton, x, y)) {
			Return();
			return true;
		}
		if (Canvas.Find(RESTART) is CanvasNode restartButton && Canvas.HitTest(restartButton, x, y)) {
			Restart();
			return true;
		}
		return false;
	}

	public void Close() {
		finished = true;
		StopMusic();
		RestoreMusicVolume();
		Remove();
	}

	public void PressLike() {
		likePressed = true;
		likeTimer = 0;
	}

	public void RenderText() {
		if (!finished && text != null)
			text.Render(EngineCore.GetWindowWidth(), EngineCore.GetWindowHeight());
	}

	public void ReleaseLike() {
		if (likePressed && likeTimer <= (assets.Behaviours.LikeButton?.HoldTime ?? 0))
			ToggleLike();
		likePressed = false;
	}

	public void Restart() {
		if (finished || !Opened || panelTime < RESTART_DELAY)
			return;

		Play(assets.Click, AudioSettings.snd_hitvolume);
		Close();
		onRestart();
	}

	public void Return() {
		if (finished || !Opened || panelTime < RETURN_DELAY)
			return;

		finished = true;
		StopMusic();
		RestoreMusicVolume();
		Play(assets.Click, AudioSettings.snd_hitvolume);
		onReturn();
	}

	public void ToggleLike() {
		if (finished || hidden || !Opened || assets.Behaviours.LikeButton == null)
			return;

		liked = !liked;
		Play(liked ? assets.LikeSound : assets.UnlikeSound, AudioSettings.snd_hitvolume);
		UpdateLikeIcons();
	}

	protected override void OnThink() {
		base.OnThink();
		if (finished)
			return;

		float deltaTime = Math.Min(Level.RealtimeDeltaF, 0.1f);
		time += deltaTime;
		text?.Update(deltaTime);
		if (!Opened) {
			float fade = Math.Clamp(1 - time / FADE_TIME, 0, 1);
			if (music != null && music.IsValid())
				audiosystem.SetSoundVolume(in music.Handle, musicVolume * fade * fade);
			if (time >= FADE_TIME)
				Open();
			return;
		}

		panelTime += deltaTime;
		if (likePressed)
			likeTimer += deltaTime;
		if (!failInShown && panelTime >= assets.FailInShowDelay) {
			failInShown = true;
			if (Canvas.Find("CharPointFailIn") is CanvasNode failInPoint)
				failInPoint.Active = failIn;
		}
		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.Update(deltaTime);
		character?.Think();
		if (panelTime < KEYS_DELAY)
			return;

		var keyboard = Level.FrameState.Keyboard;
		if (keyboard.WasKeyPressed(ButtonCode.KeyR))
			Restart();
		else if (keyboard.WasKeyPressed(ButtonCode.KeyEscape) || keyboard.WasKeyPressed(ButtonCode.KeyEnter))
			Return();
		if (keyboard.WasKeyPressed(ButtonCode.KeySpace))
			PressLike();
		if (keyboard.WasKeyReleased(ButtonCode.KeySpace))
			ReleaseLike();
	}

	static void OpenDummyFail(ConCommand cmd, in TokenizedCommand args) {
		if (EngineCore.LoadingLevel || EngineCore.Level?.RootPanel == null) {
			Logs.Warn("No level is ready yet; open the dummy fail panel once a level (like the main menu) has loaded.");
			return;
		}

		var assets = MD1SceneUI.ImportFailCanvas();
		if (assets == null) {
			Logs.Warn("Couldn't load the Muse Dash fail panel.");
			return;
		}

		MuseDash1FailScreen? screen = null;
		screen = new MuseDash1FailScreen(EngineCore.Level.RootPanel, assets, null, null, true, false, false, () => { }, () => screen?.Close());
	}

	static AudioPlaybackHandle Play(IAudioClip? clip, ConVar volume) {
		if (clip == null)
			return AudioPlaybackHandle.Null;

		clip.BindVolumeToConVar(volume);
		return audiosystem.PlaySound(clip, AudioPlaybackSettings.Unaltered);
	}

	static void PlayDefaultStates(UnityCanvas canvas) {
		foreach (var animator in canvas.Animators)
			if (animator.DefaultState != null)
				canvas.Play(animator.Node.Path, animator.DefaultState);
	}

	void Open() {
		RestoreMusicVolume();
		if (music != null && music.IsValid())
			music.Pause();

		var lines = FailBarrageConfigData.GetBarrages(role);
		var barrageLines = assets.Behaviours.BarrageLines;
		for (int i = 0; i < barrageLines.Count; i++)
			if (barrageLines[i].Text is CanvasText text)
				text.Text = i < lines.Length ? lines[i] : "";
		foreach (var text in assets.Behaviours.BarrageTexts)
			text.Restart();

		failPanel = new UnityCanvasPanel(this, Canvas);
		failPanel.Dock = Dock.Fill;
		failPanel.Clicked = Click;
		PlayDefaultStates(Canvas);

		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.SetText(title);
		UpdateLikeIcons();

		if (Canvas.Find("CharPoint") is CanvasNode point)
			point.Active = !failIn;
		if (Canvas.Find("CharPointFailIn") is CanvasNode failInPoint)
			failInPoint.Active = false;
		if (character != null) {
			if (Canvas.Find(CHARACTER) is CanvasNode characterNode)
				characterNode.CustomRender = character.Render;
			if (Canvas.Find(CHARACTER_FAIL_IN) is CanvasNode failInNode)
				failInNode.CustomRender = character.Render;
		}

		failMusic = Play(assets.Music, AudioSettings.snd_musicvolume);
		character?.PlayAudio();
	}

	void RestoreMusicVolume() {
		if (music != null && music.IsValid())
			audiosystem.SetSoundVolume(in music.Handle, musicVolume);
	}

	void StopMusic() {
		if (failMusic.IsValid())
			audiosystem.StopSound(failMusic);
	}

	void UpdateLikeIcons() {
		if (assets.Behaviours.LikeButton?.Hide is CanvasNode hide)
			hide.Active = hidden;
		if (assets.Behaviours.LikeButton?.Like is CanvasNode like)
			like.Active = liked;
	}
}
