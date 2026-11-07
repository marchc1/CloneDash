using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace CloneDash.Scenes;

/// <summary>
/// Muse Dash's fail screen, rebuilt from the GameMain scene's PnlFailText (the "FAIL" letters over a magenta fade)
/// and PnlFail (the panel itself). Layout values are the prefab's RectTransforms on its 1920-wide canvas
/// (CanvasScaler: scale with screen size, match width), timings are its PnlFailText / PnlFailIn animation clips.
/// </summary>
public class FailurePanel : Panel
{
	// English Muse Dash swaps UI text to Teko (matches the official screenshot's text bounds to the pixel); song names keep
	// LongSongNameController's normalFont, Source Han Sans Heavy, for CJK titles
	const string TEKO = "Teko", SONG_FONT = "Source Han Sans Heavy";
	public const double PANEL_START = 3.2; // PnlFailText runs 3.18s, then PnlFail comes in
	const double BUTTONS_ACTIVE = 2.7; // CheckUpdateToDisableBtnAndSetGray.m_FrameKeyTime

	static readonly Color Magenta = new(172, 30, 122); // PnlFailText/ImgBg
	static readonly Color Bg = new(89, 15, 124); // PnlFail/ImgBg
	static readonly Color BgUp = new(110, 20, 143); // ImgBgUp
	static readonly Color BgDown = new(0, 186, 255); // ImgBgDown
	static readonly Color TitleColor = new(134, 37, 175); // TxtFailed, TxtSongName
	static readonly Color BarrageColor = new(134, 112, 175, 128); // TxtBarrage*
	static readonly Color ButtonNormal = new(78, 123, 214); // Button colour tint on white text
	static readonly Color ButtonHover = new(179, 251, 255);

	// Keyframes: (time, value, easing into the next key). The easings approximate the clips' Hermite curves.
	readonly record struct Key(double T, float V, Func<double, double>? Ease = null);
	static float Eval(double t, Key[] keys) {
		if (t <= keys[0].T) return keys[0].V;
		for (int i = 0; i < keys.Length - 1; i++) {
			Key a = keys[i], b = keys[i + 1];
			if (t < b.T) {
				double p = (t - a.T) / (b.T - a.T);
				return (float)(a.V + (b.V - a.V) * (a.Ease ?? NMath.Ease.Linear)(p));
			}
		}
		return keys[^1].V;
	}
	static Key[] Drop(double t0, double t1, float from, float to, float bounce) => [
		new(t0, from, NMath.Ease.InQuad), new(t1, to, NMath.Ease.OutQuad), new(t1 + 0.06, to + bounce, NMath.Ease.InQuad), new(t1 + 0.12, to)
	];

	// PnlFailText
	static readonly Key[] MagentaAlpha = [new(0.25, 0), new(1.75, 1)];
	static readonly (string Texture, float X)[] Letters = [("Fail1", -145.6f), ("Fail2", -46f), ("Fail3", 32.7f), ("Fail4", 116.4f)];
	static Key[] LetterY(int i) {
		double o = i * 0.05;
		return [new(2.5 + o, 795, NMath.Ease.InQuad), new(2.68 + o, -60, NMath.Ease.OutQuad), new(2.8 + o, -15.8f, NMath.Ease.InQuad),
			new(2.9 + o, -60, NMath.Ease.OutQuad), new(2.97 + o, -42.6f, NMath.Ease.InQuad), new(3.03 + o, -60)];
	}
	static readonly Key[][] LetterYs = [LetterY(0), LetterY(1), LetterY(2), LetterY(3)];

	// PnlFailIn
	static readonly Key[] BgY = [new(0.17, 1495, NMath.Ease.OutQuad), new(0.31, -20, NMath.Ease.InOutSine), new(0.33, 0)];
	static readonly Key[] FailedY = Drop(0.55, 0.83, 1617, 341, 60);
	static readonly Key[] SongY = Drop(0.55, 0.83, 1838, 531, 60);
	static readonly Key[] SquashX = [new(0.76, 1), new(0.82, 0.9f), new(0.83, 1.05f), new(0.9, 1)];
	static readonly Key[] SquashY = [new(0.76, 1), new(0.82, 1.1f), new(0.83, 0.95f), new(0.9, 1)];
	static readonly Key[] CharY = Drop(1.08, 1.33, 1065, -382, 77);
	static readonly Key[] BottomY = [new(2.2, -206.3f, NMath.Ease.OutQuad), new(2.4, -3, NMath.Ease.InOutSine), new(2.57, -22.3f)];
	static Key[] ButtonY(double d) => [new(2.37 + d, 156.9f, NMath.Ease.OutQuad), new(2.49 + d, -25, NMath.Ease.InOutSine), new(2.62 + d, 6.9f)];
	static Key[] ButtonAlpha(double d) => [new(2.37 + d, 0), new(2.62 + d, 1)];
	static readonly Key[] GlyphAlpha = [new(2.45, 0), new(2.68, 1)];

	// Barrage: FailTxtBarrageAnimation slides each line from x=1400 to x=-3000 over 5s after its own delay
	static readonly (float Y, double Delay)[] BarrageLines = [(350, -0.5), (235, 3), (70, 1), (287, 1.5), (137, 4.5), (410, 4)];

	class Button(string text, string glyph, double delay, float rightOffset)
	{
		public readonly string Text = text;
		public readonly ITexture? Glyph = LoadTexture(glyph);
		public readonly Key[] Y = ButtonY(delay), Alpha = ButtonAlpha(delay);
		public readonly float RightOffset = rightOffset;
		public RectangleF Bounds;
	}

	readonly IGame game;
	readonly IMuseDash1SceneInstance? scene;
	readonly ICharacterFailureInstance? character;
	readonly string songName;
	readonly string[] barrages;
	readonly ITexture?[] letterTextures;
	readonly Button returnButton = new("RETURN", "ButtonEsc", 0, 528);
	readonly Button restartButton = new("RESTART!!", "PcButtonR", 0.05, 182);
	readonly double start;
	double Time => Level.Curtime - start;
	double PanelTime => Time - PANEL_START;

	public FailurePanel(Element parent, IGame game, IMuseDash1SceneInstance? scene) : base(parent) {
		this.game = game;
		this.scene = scene;
		start = Level.Curtime;
		Dock = Dock.Fill;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);

		var descriptor = CharacterMod.GetCharacterData();
		character = descriptor?.CreateFailure();
		character?.Initialize(game);

		songName = game.GetSongChart()?.GetSong().FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name ?? "";
		barrages = LoadBarrages((descriptor as MuseDash1CharacterDescriptor)?.ConfigIndex ?? -1);
		letterTextures = Letters.Select(l => LoadTexture(l.Texture)).ToArray();

	}

	/// <summary>
	/// Draws Teko text the way Unity's Text does: <paramref name="unitySize"/> is em pixels, vertically centered so cap height
	/// sits 0.015em above <paramref name="centerY"/> (measured from the official screen). raylib sizes fonts by line height,
	/// which is 1.433em for Teko (ascent 0.958, descent 0.475).
	/// </summary>
	static float DrawTeko(float x, float centerY, string text, float unitySize, TextAlignment horizontal) {
		// ponytail: FontManager caps baked sizes at 256, so FAILED (300 -> 430) is upscaled and a little soft
		Graphics2D.DrawText(x, centerY - 0.652f * unitySize, text, TEKO, 1.433f * unitySize, horizontal, TextAlignment.Top);
		return Graphics2D.GetTextSize(text, TEKO, 1.433f * unitySize).W;
	}

	static ITexture? LoadTexture(string name) {
		var tex = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>(name);
		return tex == null ? null : MuseDash1Compatibility.ConvertTexture(EngineCore.Level, tex);
	}

	class BarrageEntry
	{
		[JsonProperty("index")] public int Index;
		[JsonProperty("barrages")] public string[] Barrages = [];
	}

	static string[] LoadBarrages(int characterIndex) {
		// ponytail: English only, the config also ships ChineseS/ChineseT/Japanese/Korean variants
		var asset = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.TextAsset>("fail_barrage_English");
		if (asset == null) return [];
		var entries = JsonConvert.DeserializeObject<BarrageEntry[]>(System.Text.Encoding.UTF8.GetString(asset.m_Script).TrimStart('﻿')) ?? [];
		return (entries.FirstOrDefault(e => e.Index == characterIndex) ?? entries.FirstOrDefault(e => e.Index == -1))?.Barrages ?? [];
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		scene?.StopSound(SceneSound.Fail);
	}

	void Return() => LevelTransitions.LoadMainMenu();

	void Restart() => game.Restart(); // SeekTo(0) -> Reset() revives the player and closes this panel via SceneUI.Reset

	bool playedFailMusic;
	protected override void OnThink() {
		base.OnThink();
		character?.Think();

		if (!playedFailMusic && PanelTime >= 0) {
			playedFailMusic = true;
			scene?.PlaySound(SceneSound.Fail, 0); // PnlFail.m_FailMusic
		}

		if (PanelTime < BUTTONS_ACTIVE) return;

		ref var keyboard = ref Level.FrameState.Keyboard;
		// InputKeyBinding: BtnReturn is bound to Cancel and Submit, BtnRestart to Restart
		if (keyboard.WasKeyPressed(ButtonCode.KeyEscape) || keyboard.WasKeyPressed(ButtonCode.KeyEnter)) { Return(); return; }
		if (keyboard.WasKeyPressed(ButtonCode.KeyR)) { Restart(); return; }

		ref var mouse = ref Level.FrameState.Mouse;
		if (mouse.Clicked(ButtonCode.MouseLeft)) {
			var local = mouse.MousePos - GetGlobalPosition();
			if (returnButton.Bounds.ContainsPoint(local)) Return();
			else if (restartButton.Bounds.ContainsPoint(local)) Restart();
		}
	}

	public override void Paint(float width, float height) {
		float s = width / 1920f; // canvas unit -> pixels
		double t = Time, pt = PanelTime;
		// Canvas space is centered with y up; these convert to panel pixels
		float X(float cx) => width / 2 + cx * s;
		float Y(float cy) => height / 2 - cy * s;

		// --- PnlFailText: magenta fade, then F-A-I-L drop in ---
		if (pt < 0.5) {
			Graphics2D.SetDrawColor(Magenta, (int)(255 * Eval(t, MagentaAlpha)));
			Graphics2D.DrawRectangle(0, 0, width, height);
			Graphics2D.SetDrawColor(255, 255, 255);
			for (int i = 0; i < Letters.Length; i++) {
				var tex = letterTextures[i];
				if (tex == null) continue;
				float letterBottom = Y(Eval(t, LetterYs[i])); // pivot is bottom-center
				Graphics2D.SetTexture(tex);
				Graphics2D.DrawTexturedRectangle(X(Letters[i].X) - 51 * s, letterBottom - 113 * s, 102 * s, 113 * s);
			}
			Graphics2D.SetTexture(null);
		}
		if (pt < 0) return;

		// --- PnlFail ---
		float bgOffset = Eval(pt, BgY) * s;
		Graphics2D.SetDrawColor(Bg);
		Graphics2D.DrawRectangle(0, -bgOffset, width, height);
		Graphics2D.SetDrawColor(BgUp);
		Graphics2D.DrawRectangle(0, -bgOffset, width, 80 * s);

		// Barrage is anchored to the top of the canvas, 540 units down
		Graphics2D.SetDrawColor(BarrageColor);
		for (int i = 0; i < BarrageLines.Length && i < barrages.Length; i++) {
			var (y, delay) = BarrageLines[i];
			float x = (float)NMath.Remap(pt, delay, delay + 5, 1400, -3000, clampInput: true);
			DrawTeko(X(x), (540 - y) * s, barrages[i], 60 * s, TextAlignment.Left);
		}

		// ImgBgDown: anchored to the bottom, 160 tall, buttons centered on it
		float bottomY = Eval(pt, BottomY);
		Graphics2D.SetDrawColor(BgDown);
		Graphics2D.DrawRectangle(0, height - (bottomY + 160) * s, width, 160 * s);
		PaintButton(returnButton, width, height, s, pt, bottomY);
		PaintButton(restartButton, width, height, s, pt, bottomY);

		// TxtFailed: anchors x 0.55-0.95 (+30 wide, -75 offset), left aligned, squashes around its center on landing
		float failedHalfW = width * 0.2f + 15 * s;
		// ponytail: 310 not the prefab's 300 -- the official render measures ~3% bigger (784x201 vs Teko's 766x193 at 300)
		PaintSquashed(width * 0.75f - 75 * s, height - Eval(pt, FailedY) * s, pt, () =>
			DrawTeko(-failedHalfW, 0, "FAILED", 310 * s, TextAlignment.Left));
		PaintSongName(width, height, s, pt);

		// CharPoint (-450, y) + CharPoint2 (0, 57); SkeletonGraphic scale 0.005 * 100 px/unit = 0.5 canvas units
		if (character != null) {
			EngineCore.Window.BeginMode2D(new() {
				Zoom = 0.5f * s,
				Offset = (GetGlobalPosition() + new Vector2F(X(-450), Y(Eval(pt, CharY) + 57))).ToNumerics()
			});
			character.Render();
			EngineCore.Window.EndMode2D();
		}
	}

	/// <summary>Draws around a pivot (x, y) with the landing squash applied; draw() works relative to the pivot.</summary>
	void PaintSquashed(float x, float y, double pt, Action draw) {
		Rlgl.DrawRenderBatchActive();
		Rlgl.PushMatrix();
		Rlgl.Translatef(x, y, 0);
		Rlgl.Scalef(Eval(pt, SquashX), Eval(pt, SquashY), 1);
		Graphics2D.SetDrawColor(TitleColor);
		draw();
		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
	}

	void PaintSongName(float width, float height, float s, double pt) {
		// ImgSongNameMask: 0.4 * width + 20 wide, 120 tall, centered at 0.75 * width - 70; long names scroll (LongSongNameController, 70 units/s)
		float maskW = width * 0.4f + 20 * s, maskH = 120 * s;
		float maskX = width * 0.75f - 70 * s - maskW / 2, centerY = height - Eval(pt, SongY) * s;
		float fontSize = 80 * s;
		float textW = Graphics2D.GetTextSize(songName, SONG_FONT, fontSize).W;
		float textX = maskW / 2 + 25 * s - 420.3f * s; // TxtSongName: 840.6 wide, offset 25, left aligned
		float scroll = 0;
		float loop = Math.Max(textW, 840 * s) + 100 * s;
		if (textW > maskW - textX) scroll = (float)(pt * 70 * s % loop);

		Graphics2D.ScissorRect(RectangleF.XYWH(GetGlobalPosition().X + maskX, GetGlobalPosition().Y + centerY - maskH / 2 - 40 * s, maskW, maskH + 80 * s));
		float left = textX - maskW / 2 - scroll;
		PaintSquashed(maskX + maskW / 2, centerY, pt, () => {
			Graphics2D.DrawText(left, 0, songName, SONG_FONT, fontSize, TextAlignment.Left, TextAlignment.Center);
			if (scroll > 0) Graphics2D.DrawText(left + loop, 0, songName, SONG_FONT, fontSize, TextAlignment.Left, TextAlignment.Center);
		});
		Graphics2D.ScissorRect();
	}

	void PaintButton(Button button, float width, float height, float s, double pt, float bottomY) {
		float centerX = width - button.RightOffset * s;
		float centerY = height - (bottomY + 80 + Eval(pt, button.Y)) * s;
		button.Bounds = RectangleF.XYWH(centerX - 345 * s / 2, centerY - 70 * s, 345 * s, 140 * s);

		bool hovered = pt >= BUTTONS_ACTIVE && button.Bounds.ContainsPoint(Level.FrameState.Mouse.MousePos - GetGlobalPosition());
		Graphics2D.SetDrawColor(hovered ? ButtonHover : ButtonNormal, (int)(255 * Eval(pt, button.Alpha)));
		float textW = DrawTeko(centerX, centerY, button.Text, 60 * s, TextAlignment.Center);

		if (button.Glyph != null) {
			// ImgBtn: 60x60, centered 40 units left of the text
			float glyphX = centerX - textW / 2 - 40 * s;
			Graphics2D.SetDrawColor(ButtonNormal, (int)(255 * Eval(pt, GlyphAlpha)));
			Graphics2D.SetTexture(button.Glyph);
			Graphics2D.DrawTexturedRectangle(glyphX - 30 * s, centerY - 30 * s, 60 * s, 60 * s);
			Graphics2D.SetTexture(null);
		}
	}
}
