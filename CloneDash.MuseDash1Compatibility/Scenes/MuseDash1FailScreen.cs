using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Settings;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.UI;

namespace CloneDash.Scenes;

public record MuseDash1FailAssets(UnityCanvas Intro, UnityCanvas Panel, MuseDash1CanvasBehaviours Behaviours, IAudioClip? LikeSound, IAudioClip? UnlikeSound);

/// <summary>
/// Muse Dash's fail screen: the GameMain scene's PnlFailText (magenta fade + F-A-I-L letters) plays from the moment of death,
/// then PnlFail (PnlFailIn) takes over. Both are the original prefabs/clips running through <see cref="UnityCanvas"/>.
/// </summary>
public class MuseDash1FailPanel : Panel
{
	/// <summary>PnlFailText runs 3.18s, then PnlFail comes in.</summary>
	public const float PANEL_START = 3.2f;
	const float BUTTONS_ACTIVE = 2.7f; // PnlFail's CheckUpdateToDisableBtnAndSetGray.m_FrameKeyTime

	const string CHARACTER = "CharPoint/CharPoint2";
	const string CHARACTER_POINT = "CharPoint"; // PnlFail.m_ObjFailNotIn; PnlFailIn animates this one
	const string RESTART = "ImgBgDown/BtnRestart";
	const string RETURN = "ImgBgDown/BtnReturn";

	readonly MuseDash1FailAssets assets;
	readonly ICharacterFailureInstance? character;
	readonly RhythmGameLevel game;
	readonly IMuseDash1SceneInstance scene;
	bool finished;
	bool liked;
	bool panelShown;
	float time;

	float PanelTime => time - PANEL_START;

	public MuseDash1FailPanel(Element parent, RhythmGameLevel game, IMuseDash1SceneInstance scene, MuseDash1FailAssets assets) : base(parent) {
		this.assets = assets;
		this.game = game;
		this.scene = scene;
		Dock = Dock.Fill;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);

		var canvasPanel = new UnityCanvasPanel(this, assets.Intro);
		canvasPanel.Dock = Dock.Fill;
		canvasPanel.Overlays.Add(assets.Panel);
		canvasPanel.Clicked = Click;

		assets.Intro.Play("", "PnlFailText");
		assets.Panel.Root.Active = false;

		var descriptor = CharacterMod.Instance.GetActive();
		character = descriptor?.CreateFailure();
		character?.Initialize(game);
		if (character != null && assets.Panel.Find(CHARACTER) is CanvasNode point) {
			point.CustomRender = character.Render;
			if (assets.Panel.Find(CHARACTER_POINT) is CanvasNode characterPoint)
				characterPoint.Active = true;
		}

		string title = game.GetSongChart().GetSong().FetchMetadata().Name.ToString();
		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.SetText(title);

		var lines = LoadBarrages((descriptor as MuseDash1CharacterDescriptor)?.ConfigIndex ?? -1);
		for (int i = 0; i < assets.Behaviours.FailBarrageTexts.Count; i++)
			if (assets.Behaviours.FailBarrageTexts[i].Text is CanvasText text)
				text.Text = i < lines.Length ? lines[i] : "";
		foreach (var line in assets.Behaviours.FailBarrageLines)
			line.Update(0);

		UpdateLikeIcon();
	}

	class BarrageEntry
	{
		[JsonProperty("index")] public int Index = 0;
		[JsonProperty("barrages")] public string[] Barrages = [];
	}

	/// <summary>FailBarrage fills its lines from the fail_barrage config: the character's own set, else the default (-1).</summary>
	static string[] LoadBarrages(int characterIndex) {
		var asset = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.TextAsset>($"fail_barrage_{MuseDash1Localization.ENGLISH}");
		if (asset == null)
			return [];

		var entries = JsonConvert.DeserializeObject<BarrageEntry[]>(System.Text.Encoding.UTF8.GetString(asset.m_Script).TrimStart('﻿')) ?? [];
		return (entries.FirstOrDefault(e => e.Index == characterIndex) ?? entries.FirstOrDefault(e => e.Index == -1))?.Barrages ?? [];
	}

	bool Click(float x, float y) {
		if (!panelShown || PanelTime < BUTTONS_ACTIVE)
			return true;

		var canvas = assets.Panel;
		if (canvas.Find(RETURN) is CanvasNode returnButton && canvas.HitTest(returnButton, x, y))
			Return();
		else if (canvas.Find(RESTART) is CanvasNode restartButton && canvas.HitTest(restartButton, x, y))
			Restart();
		else if (assets.Behaviours.LikeButton is MuseDash1LikeButton like && canvas.HitTest(like.Button, x, y))
			ToggleLike();
		return true;
	}

	void Restart() {
		if (finished) return;
		finished = true;
		game.Restart(); // SeekTo(0) -> Reset() revives the player and closes this panel via the scene UI
	}

	void Return() {
		if (finished) return;
		finished = true;
		LevelTransitions.LoadMainMenu();
	}

	void ToggleLike() {
		liked = !liked;
		Play(liked ? assets.LikeSound : assets.UnlikeSound);
		UpdateLikeIcon();
	}

	void UpdateLikeIcon() {
		if (assets.Behaviours.LikeButton?.Like is CanvasNode like)
			like.Active = liked;
	}

	static void Play(IAudioClip? clip) {
		if (clip == null)
			return;

		clip.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		audiosystem.PlaySound(clip, AudioPlaybackSettings.Unaltered);
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		scene.StopSound(SceneSound.Fail);
	}

	protected override void OnThink() {
		base.OnThink();
		time += Math.Min(Level.RealtimeDeltaF, 0.1f);
		character?.Think();

		if (!panelShown && time >= PANEL_START) {
			panelShown = true;
			assets.Panel.Root.Active = true;
			assets.Panel.Play("", "PnlFailIn");
			scene.PlaySound(SceneSound.Fail, 0); // PnlFail.m_FailMusic
		}
		if (!panelShown)
			return;

		foreach (var line in assets.Behaviours.FailBarrageLines)
			line.Update(PanelTime);
		foreach (var marquee in assets.Behaviours.Marquees)
			marquee.Update(Math.Min(Level.RealtimeDeltaF, 0.1f));

		if (PanelTime < BUTTONS_ACTIVE)
			return;

		// InputKeyBinding: BtnReturn is bound to Cancel and Submit, BtnRestart to Restart, BtnLike to Space
		var keyboard = Level.FrameState.Keyboard;
		if (keyboard.WasKeyPressed(ButtonCode.KeyEscape) || keyboard.WasKeyPressed(ButtonCode.KeyEnter))
			Return();
		else if (keyboard.WasKeyPressed(ButtonCode.KeyR))
			Restart();
		else if (keyboard.WasKeyPressed(ButtonCode.KeySpace))
			ToggleLike();
	}
}
