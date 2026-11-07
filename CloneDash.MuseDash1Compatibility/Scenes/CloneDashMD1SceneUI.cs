using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using CloneDash.Game.Statistics;
using CommunityToolkit.HighPerformance;
using Nucleus;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
using Raylib_cs;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Velopack.Sources;

namespace CloneDash.Scenes;


public class StatisticsPanel : Panel
{
	ICharacterVictoryInstance victory = null!;
	MuseDash1VictoryScreen? screen;
	ISongChart? chart;
	double start = 0;
	double Time() => globals.CurTime - start;
	RhythmGameLevel? game;
	StatisticsData stats;

	public StatisticsPanel(Element? parent, RhythmGameLevel game, StatisticsData stats, MuseDash1VictoryAssets? assets) : base(parent){
		this.game = game;
		this.stats = stats;

		chart = game.GetSongChart();
		if (chart == null) return;
		start = globals.CurTime;

		stats.Compute();
		Open(assets, game.GetParameters().Get<bool>(RhythmGameLoadParameters.AUTOPLAY), Restart, LevelTransitions.LoadMainMenu);
	}

	public StatisticsPanel(Element? parent, StatisticsData stats, MuseDash1VictoryAssets? assets, Action onClose) : base(parent) {
		this.stats = stats;
		start = globals.CurTime;

		Open(assets, false, onClose, onClose);
	}

	void Open(MuseDash1VictoryAssets? assets, bool autoplay, Action onRestart, Action onContinue) {
		ICharacterDescriptor? character = CharacterMod.Instance.GetActive();
		if (character == null) return;

		victory = character.CreateVictory();
		victory.Initialize(game);
		victory.PlayAudio();

		if (assets != null) {
			var canvasPanel = new UnityCanvasPanel(this, assets.Canvas);
			canvasPanel.Dock = Dock.Fill;
			if (assets.Like != null)
				canvasPanel.Overlays.Add(assets.Like);
			assets.Canvas.Play("", "PnlVictoryIn");
			screen = new MuseDash1VictoryScreen(assets, stats, chart, null, false, false, null, autoplay, victory, onRestart, onContinue);
			canvasPanel.Clicked = screen.Click;
		}

		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
	}
	void Restart() {
		// TODO: Probably should just hard restart it...
		// Maybe seeking is stable enough now to justify this though?
		game?.Restart();
		this.Remove();
	}

	protected override void OnThink() {
		base.OnThink();
		if (victory != null)
			victory.Think();
		if (screen == null)
			return;

		screen.Update(Math.Min(Level.RealtimeDeltaF, 0.1f));
		if (Level.FrameState.Keyboard.WasKeyPressed(ButtonCode.KeyR))
			screen.Restart();
		else if (Level.FrameState.Keyboard.WasKeyPressed(ButtonCode.KeyEnter))
			screen.Continue();
		if (Level.FrameState.Keyboard.WasKeyPressed(ButtonCode.KeySpace))
			screen.PressLike();
		if (Level.FrameState.Keyboard.WasKeyReleased(ButtonCode.KeySpace))
			screen.ReleaseLike();
	}
}

public static class UITextAnimationFns
{
	public static void UpwardFadeout(TextImageRenderItem self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, 0, len * 0.8, 0, 1, clampInput: true);
		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);

		float y = (float)(NMath.Ease.OutCirc(moveT) * 1.5f);

		double squishT = NMath.Remap(t, 0, 0.5, 0, 1, clampInput: true);
		double ease = NMath.Ease.OutElastic(squishT);
		double ease2 = NMath.Ease.OutElastic(squishT - 0.1);

		float scaleX = (float)NMath.Lerp(0.1, 1.0, ease2);
		float scaleY = (float)NMath.Lerp(2.0, 1.0, ease);

		self.Position = new(0, 1 + y);
		self.Color.A = (byte)(255 * (1 - alphaT));
		self.Scale = new(scaleX, scaleY);
	}
	// TODO: get this looking right
	public static void UpwardFadeoutCurved(TextImageRenderItem self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, 0, len * 0.8, 0, 1, clampInput: true);
		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);

		float y = (float)(NMath.Ease.InOutCirc(t * 0.5) * 14.5f);

		double squishT = NMath.Remap(t, 0, 0.5, 0, 1, clampInput: true);
		double ease = NMath.Ease.OutElastic(squishT);
		double ease2 = NMath.Ease.OutElastic(squishT - 0.1);

		float scaleX = (float)NMath.Lerp(0.1, 1.0, ease2);
		float scaleY = (float)NMath.Lerp(2.0, 1.0, ease);

		self.Position = new(0, 1 + y);
		self.Color.A = (byte)(255 * (1 - alphaT));
		self.Scale = new(scaleX, scaleY);
	}
	public static void FadeoutInv(TextImageRenderItem self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, 0, len * 0.8, 0, 1, clampInput: true);
		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);

		float y = 1.5f;

		double squishT = NMath.Remap(t, 0, 0.5, 0, 1, clampInput: true);
		double ease = NMath.Ease.OutElastic(squishT);
		double ease2 = NMath.Ease.OutElastic(squishT - 0.1);

		float scaleX = (float)NMath.Lerp(0.1, 1.0, ease2);
		float scaleY = (float)NMath.Lerp(2.0, 1.0, ease);

		self.Position = new(-1.2f, y);
		self.Color.A = (byte)(255 * (1 - alphaT));
		self.Scale = new(scaleY, scaleX);
	}
	public static void UpwardFadeoutInv(TextImageRenderItem self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, 0, len * 0.8, 0, 1, clampInput: true);
		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);

		float y = (float)(NMath.Ease.OutCirc(moveT) * 1.5f);

		double squishT = NMath.Remap(t, 0, 0.5, 0, 1, clampInput: true);
		double ease = NMath.Ease.OutElastic(squishT);
		double ease2 = NMath.Ease.OutElastic(squishT - 0.1);

		float scaleX = (float)NMath.Lerp(0.1, 1.0, ease2);
		float scaleY = (float)NMath.Lerp(2.0, 1.0, ease);

		self.Position = new(-1.2f, y);
		self.Color.A = (byte)(255 * (1 - alphaT));
		self.Scale = new(scaleY, scaleX);
	}
	public static void UpwardFadeoutMoveLeft(TextImageRenderItem self, double curtime) {
		UpwardFadeout(self, curtime);

		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, len * 0.6, len, 0, 1, clampInput: true);
		self.Position.X = (float)(NMath.Ease.InQuart(moveT) * -4.5);
	}
	public static void VibrateAndMoveLeft(TextImageRenderItem self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double scaleT = NMath.Remap(t, 0, len * 0.5, 0, 1, clampInput: true);
		float scale = (float)NMath.Lerp(NMath.Ease.OutCirc(scaleT), 1.5, 1.0);

		double vibrateDecay = 1.0 - NMath.Remap(t, 0, 0.5, 0, 1, clampInput: true);
		float shakeX = (float)(Math.Sin(t * 60) * 0.08 * vibrateDecay);
		float shakeY = (float)(Math.Sin(t * 45 + 1.5) * 0.06 * vibrateDecay);

		double moveT = NMath.Remap(t, len * 0.5, len, 0, 1, clampInput: true);
		float moveX = (float)(NMath.Ease.InQuart(moveT) * -4.5);

		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);

		self.Position = new(moveX + shakeX, 1 + shakeY);
		self.Scale = new(scale, scale);
		self.Color.A = (byte)(255 * (1 - alphaT));
	}
}

public delegate void RenderTextAnimationFn(TextImageRenderItem self, double curtime);

public class TextImageRenderItem
{
	public double StartTime;
	public double Length;

	public bool TopLeftAligned;
	public bool Worldspace;

	public Vector2F StartPosition;
	public float StartRotation;
	public Vector2F StartScale = new(1, 1);
	public Color StartColor = new(255, 255, 255, 255);

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale = new(1, 1);
	public Color Color = new(255, 255, 255, 255);
	public Color? borderColor;

	public string? Text;
	public string? Font;
	public ITexture? Texture;
	public RenderTextAnimationFn? Fn;

	ulong lastTextHash;
	ulong lastFontHash;

	public bool IsImmortal() => StartTime == 0 && Length == 0;
	public double LifeLived(double curtime) => IsImmortal() ? 0 : curtime - StartTime;
	public double LifeRemaining(double curtime) => IsImmortal() ? 200000000 : (Length) - LifeLived(curtime);

	public TextImageRenderItem(double start, double length, Vector2F pos, float rotation, Vector2F scale, string text, string font, Color color, RenderTextAnimationFn? fn = null) {
		StartTime = start;
		Length = length;
		StartPosition = pos;
		StartRotation = rotation;
		StartScale = scale;
		Text = text;
		Font = font;
		StartColor = color;
		Fn = fn;
	}

	public TextImageRenderItem(double start, double length, Vector2F pos, float rotation, Vector2F scale, ITexture? texture, Color color, RenderTextAnimationFn? fn = null) {
		StartTime = start;
		Length = length;
		StartPosition = pos;
		StartRotation = rotation;
		StartScale = scale;
		Texture = texture;
		StartColor = color;
		Fn = fn;
	}

	public bool IsOver(double curtime) => curtime >= (StartTime + Length);
	public void Dispose() {
		if (textRT != null)
			Graphics2D.DestroyRenderTarget(textRT);
	}
	float FontResolution = 90;
	public float BorderSize = 2.6f;
	public float DarkenAmount = 0.3f;
	public float DesaturateAmount = 0.35f;
	public float SplitY = 0.55f;
	int RTPadding => (int)MathF.Ceiling(BorderSize) + 2;
	public void Render(double curtime, IShader? styledTextShader) {
		if (Fn != null)
			Fn(this, curtime);

		Vector2F position = Position;
		Vector2F scale = Scale;
		float rotation = Rotation;

		position *= StartScale;
		position = RotatePoint(position, StartRotation);
		position += StartPosition;

		scale *= StartScale;
		rotation += StartRotation;

		Color color = StartColor * Color;

		if (Text != null && Font != null)
			RenderStyledText(position, scale, rotation, color, borderColor, styledTextShader);
		else if (Texture != null) {
			Rlgl.PushMatrix();
			Rlgl.Translatef(position.x, -position.y, 0);
			Rlgl.Rotatef(rotation, 0, 0, 1);
			Rlgl.Scalef(scale.x, scale.y, 1);
			Graphics2D.SetDrawColor(color);
			Graphics2D.SetTexture(Texture);
			if (TopLeftAligned) {
				if (Worldspace)
					Graphics2D.DrawTexturedRectangle(new Vector2F(0, 0), new Vector2F(Texture.GetWidth() / FontResolution, Texture.GetHeight() / FontResolution));
				else
					Graphics2D.DrawTexturedRectangle(new Vector2F(0, 0), new Vector2F(Texture.GetWidth(), Texture.GetHeight()));
			}
			else {
				if (Worldspace)
					Graphics2D.DrawTexturedRectangle(new Vector2F(Texture.GetWidth() / -FontResolution / 2, Texture.GetHeight() / -FontResolution / 2), new Vector2F(Texture.GetWidth() / FontResolution, Texture.GetHeight() / FontResolution));
				else
					Graphics2D.DrawTexturedRectangle(new Vector2F(Texture.GetWidth() / -2, Texture.GetHeight() / -2), new Vector2F(Texture.GetWidth(), Texture.GetHeight()));
			}
			Rlgl.DrawRenderBatchActive();
			Rlgl.PopMatrix();
		}
	}

	IRenderTexture? textRT;
	public Vector2F GetTextSize() {
		Vector2F textSize = Graphics2D.GetTextSize(Text!, Font!, FontResolution);
		int pad = RTPadding;
		int rtW = (int)textSize.X + pad * 2;
		int rtH = (int)textSize.Y + pad * 2;
		return new(rtW, rtH);
	}
	public IRenderTexture? DetermineRenderTexture() {
		if (Text == null || Font == null)
			return null;

		var rtSize = GetTextSize();
		ulong textHash = Text.Hash(invariant: false);
		ulong fontHash = Font.Hash(invariant: false);
		bool requiresRedraw = textRT == null || (textHash != lastTextHash || fontHash != lastFontHash);
		bool requiresResize = textRT == null || (textRT.GetWidth() < rtSize.W || textRT.GetHeight() < rtSize.H);
		if (requiresResize) {
			if (textRT != null) {
				Graphics2D.DestroyRenderTarget(textRT);
				textRT = null;
			}
		}

		if (textRT == null) {
			textRT = Graphics2D.CreateRenderTarget(rtSize.W, rtSize.H);
			textRT.SetFilter(TextureFilter.Bilinear);
		}

		if (requiresRedraw) {
			IRenderTexture rt = textRT;

			int pad = RTPadding;
			Graphics2D.BeginRenderTarget(rt);
			Rlgl.ClearColor(255, 255, 255, 0);
			Rlgl.ClearScreenBuffers();
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(pad, pad, Text!, Font!, FontResolution);
			Rlgl.DrawRenderBatchActive();
			Graphics2D.EndRenderTarget(rt);

			lastTextHash = textHash;
			lastFontHash = fontHash;
		}

		return textRT;
	}
	void RenderStyledText(Vector2F position, Vector2F scale, float rotation, Color color, Color? borderColor, IShader? shader) {
		float alpha = color.A / 255f;
		var rtN = textRT;
		if (rtN == null) return;

		var rt = rtN;
		var rtSize = GetTextSize();
		var rtW = rt.GetWidth();
		var rtH = rt.GetHeight();

		Rlgl.PushMatrix();
		Rlgl.Translatef(position.x, -position.y, 0);
		Rlgl.Rotatef(rotation, 0, 0, 1);
		if (Worldspace)
			Rlgl.Scalef(scale.x / FontResolution, scale.y / FontResolution, 1);
		else
			Rlgl.Scalef(scale.x, scale.y, 1);

		Color borderC = borderColor ?? color.Multiply(0.5f);
		if (shader != null) {
			shader.SetUniform("uTexelSize", new System.Numerics.Vector2(1.0f / rtW, 1.0f / rtH));
			shader.SetUniform("uTextColor", new System.Numerics.Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
			shader.SetUniform("uBorderColor", new System.Numerics.Vector3(borderC.R / 255f, borderC.G / 255f, borderC.B / 255f));
			shader.SetUniform("uBorderSize", BorderSize);
			shader.SetUniform("uDarkenAmount", DarkenAmount);
			shader.SetUniform("uDesaturateAmount", DesaturateAmount);
			shader.SetUniform("uSplitY", SplitY);
			shader.SetUniform("uAlpha", alpha);
			shader.Activate();
		}
		else {
			Graphics2D.SetDrawColor(color);
		}
		float drawX, drawY;
		if (TopLeftAligned) {
			drawX = 0;
			drawY = 0;
		}
		else {
			drawX = -rtSize.x / 2f;
			drawY = -rtSize.y / 2f;
		}
		Graphics2D.SetTexture(rt);

		Rlgl.SetBlendMode(BlendMode.Custom);
		Rlgl.SetBlendFactors(GLEnum.ONE, GLEnum.ONE_MINUS_SRC_ALPHA, GLEnum.FUNC_ADD);
		Graphics2D.DrawTexturedRectangle(drawX, drawY, rtW, rtH);
		Rlgl.SetBlendMode(BlendMode.Alpha);

		if (shader != null)
			shader.Deactivate();

		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
	}

	static Vector2F RotatePoint(Vector2F p, float angle) {
		float cos = MathF.Cos(angle);
		float sin = MathF.Sin(angle);
		return new Vector2F(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
	}
}

public enum ComboGrade
{
	NotApplicable = 0,
	Low = 5,
	High = 30
}

public class MD1SceneUI(IMuseDash1SceneInstance scene, RhythmGameLevel game) : IMuseDash1SceneUI
{
	protected StatisticsPanel? CurrentStatisticsPanel;

	protected readonly List<TextImageRenderItem> BackgroundItems = [];
	protected readonly List<TextImageRenderItem> ForegroundItems = [];

	protected virtual Color ScoreColor => new(165, 254, 254);

	protected TextImageRenderItem? ScoreNumber = null;
	protected TextImageRenderItem? ScoreLabel = null;
	protected TextImageRenderItem? ComboNumber = null;

	public virtual void SetupScoreNumber() {
		ScoreNumber = new(0, 0, new(0, 0), 0, new(1, 1), "0", "Snaps Taste", ScoreColor, null);
	}

	public virtual void SetupComboNumber() {
		ComboNumber = new(0, 0, new(0, 0), 0, new(1, 1), "0", "Snaps Taste", new(165, 254, 254), null);
	}

	protected virtual Color ComboLowColor => new(154, 233, 254);
	protected virtual Color ComboLowBorderColor => new(61, 139, 224);
	protected virtual Color ComboHighColor => new(255, 225, 41);
	protected virtual Color ComboHighBorderColor => new(180, 49, 79);

	protected IShader? StyledTextShader;
	protected IShader? UIAlphatestShader;
	protected ITexture? GoldGreat;
	protected ITexture? GoldPerfect;
	protected ITexture? ScoreGreat;
	protected ITexture? ScorePerfect;
	protected ITexture? ScorePass;

	protected MuseDash1MultiHitTip? MultiHitTip;
	protected ITexture? HitsBase;
	protected ITexture? BelowBase;
	protected ITexture? hp_icon;
	protected ITexture? hp_icon_mistake;
	protected ITexture? hp_slider;
	protected ITexture? hp_slider_base;
	protected ITexture? HpFeverBase;
	protected ITexture? slider_light;
	protected ITexture? power_slider;
	protected ITexture? power_slider_white;
	protected ITexture? Fever;
	protected ITexture? bubble;
	protected ITexture? score_English;
	protected ModelData? fx_combo_1;
	protected ModelData? fx_combo_2;
	MuseDash1VictoryAssets? victoryCanvas;
	MuseDash1FailAssets? failCanvas;
	protected MuseDash1FailPanel? CurrentFailPanel;

	public virtual void Dispose() {
		foreach (var item in BackgroundItems) item.Dispose();
		foreach (var item in ForegroundItems) item.Dispose();
		ScoreNumber?.Dispose();
		ScoreLabel?.Dispose();
		ComboNumber?.Dispose();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void CleanupList(List<TextImageRenderItem> list, double curtime) {
		for (int i = list.Count - 1; i >= 0; i--) {
			var item = list[i];
			if (item.IsOver(curtime)) {
				list.RemoveAt(i);
				item.Dispose();
			}
		}
	}
	void CleanupTextLists(double curtime) {
		CleanupList(BackgroundItems, curtime);
		CleanupList(ForegroundItems, curtime);
	}

	protected double Time => scene.GetGame().Component<IConductor>().GetTime();
	bool AllPerfect, FullCombo;
	int Combo;
	double CurrentFever, MaxFever;
	double HP, MaxHP;
	bool InFever;
	double EnterFeverTime = -20000;
	double FeverRemainingTime, FeverTotalTime;
	double LastHitTime = -2000000;
	int MultiHits = 0;
	bool InMultiHit;
	double Score = 0;
	bool Seeking;
	ComboGrade ComboGrade;
	double ComboGradeUpdateTime = -200000;

	public static float MinorTextScale => 0.65f;
	public static float TextScale => 0.85f;

	protected ITexture? LoadTextureByName(ReadOnlySpan<char> name) => UnityAssetUtils.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>(name)!);
	public void SetSeeking(bool seeking) => Seeking = seeking;
	public virtual void Initialize() {
		SetupScoreNumber();
		SetupComboNumber();

		StyledTextShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "styled_text.fs");
		UIAlphatestShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "ui_alphatest.fs");

		score_English = UnityAssetUtils.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("score_English")!);
		ScoreLabel = new(0, 0, new(0, 0), 0, new(1, 1), score_English, new(165, 254, 254), null);

		LoadScoreAssets();
		LoadMultiHitAssets();
		LoadHealthBarAssets();
		LoadComboModels();
		victoryCanvas = ImportVictoryCanvas();
		failCanvas = ImportFailCanvas();

		ForceDeactivateCombo1();
		ForceDeactivateCombo2();
	}

	static AssetStudio.Transform? FindChild(AssetStudio.Transform? transform, string name)
		=> transform?.GetChildren().FirstOrDefault(child => child.GetGameObject()?.m_Name == name);

	internal static MuseDash1VictoryAssets? ImportVictoryCanvas() {
		var assets = MuseDash1Compatibility.StreamingAssets;
		var found = assets.FindAssetByName<AssetStudio.GameObject>("PnlVictory");
		if (found == null)
			return null;

		HashSet<AssetStudio.SerializedFile> pinned = [found.assetsFile];
		assets.Pin(found.assetsFile);
		try {
			var root = found.assetsFile.Objects.OfType<AssetStudio.Transform>().FirstOrDefault(transform => !transform.m_Father.TryGet(out _) && transform.GetGameObject()?.m_Name == "PnlVictory");
			var panel = FindChild(FindChild(root, "Default"), "PnlVictory")?.GetGameObject();
			if (panel == null)
				return null;

			var script = root!.GetGameObject()?.GetMonoBehaviorByScriptName("PnlVictory");
			var data = script?.ToType();
			var settings = new UnityCanvasSettings("GameMain.unity UI_3D canvas (CanvasScaler) + Camera_3D", true, 60, 100, new(1920, 1080), 1);
			UnityCanvas ImportCanvas(AssetStudio.GameObject gameObject, MuseDash1CanvasBehaviours behaviours) {
				var canvas = UnityCanvas.Import(gameObject, settings,
					files => assets.LoadDependenciesContaining("PnlVictory", files, pinned),
					(canvas, node, behaviour, className, data) => MuseDash1Localization.Apply(canvas, node, behaviour, className, data) || behaviours.Capture(canvas, node, behaviour, className, data));
				MuseDash1Localization.ApplyGlobalFonts(canvas, assets.Assets.AssetsFileList);
				if (data?["btnDownloadReports"] is System.Collections.IList reports)
					foreach (System.Collections.Specialized.OrderedDictionary pointer in reports)
						if (canvas.TryResolve<AssetStudio.MonoBehaviour>(script!, pointer, out var button) && canvas.FindNode(button) is CanvasNode reportNode)
							reportNode.Active = false;
				canvas.Report();
				return canvas;
			}

			var behaviours = new MuseDash1CanvasBehaviours();
			var canvas = ImportCanvas(panel, behaviours);

			UnityCanvas? likeCanvas = null;
			var likeBehaviours = new MuseDash1CanvasBehaviours();
			if (data?["pnlControls"] is System.Collections.IList controls)
				foreach (System.Collections.Specialized.OrderedDictionary control in controls)
					if (control["mainPnl"] is System.Collections.Specialized.OrderedDictionary mainPnl && Convert.ToInt64(mainPnl["m_PathID"]) == panel.m_PathID
						&& control["tglLike"] is System.Collections.Specialized.OrderedDictionary tglLike && canvas.TryResolve<AssetStudio.GameObject>(script!, tglLike, out var likeObject)) {
						likeCanvas = ImportCanvas(likeObject, likeBehaviours);
						break;
					}

			IAudioClip? LoadClip(string field) {
				if (script == null || data?[field] is not System.Collections.Specialized.OrderedDictionary pointer)
					return null;

				var pptr = new AssetStudio.PPtr<AssetStudio.AudioClip>(Convert.ToInt32(pointer["m_FileID"]), Convert.ToInt64(pointer["m_PathID"]), script.assetsFile);
				if (!pptr.TryGet(out var clip) && pptr.m_FileID > 0 && pptr.m_FileID <= script.assetsFile.m_Externals.Count)
					assets.LoadDependenciesContaining("PnlVictory", new HashSet<string> { Path.GetFileName(script.assetsFile.m_Externals[pptr.m_FileID - 1].fileName) }, pinned);
				return pptr.TryGet(out clip) ? MuseDash1Compatibility.LoadSound(clip) : null;
			}

			return new MuseDash1VictoryAssets(canvas, behaviours, likeCanvas, likeBehaviours.LikeButton, LoadClip("victoryBgm"), LoadClip("sfxClip"),
				MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, MuseDash1VictoryScreen.LIKE_SOUND), MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, MuseDash1VictoryScreen.UNLIKE_SOUND));
		}
		finally {
			foreach (var file in pinned)
				if (file != found.assetsFile)
					assets.Release(file);
			assets.Unpin(found.assetsFile);
		}
	}

	static MuseDash1FailAssets? ImportFailCanvas() {
		var assets = MuseDash1Compatibility.StreamingAssets;
		// PnlFail isn't an addressable prefab (the "PnlFail" key is its animator controller); it lives in the GameMain scene.
		// Load just the scene's own bundle; the canvas import pulls in only the dependencies PnlFail uses.
		HashSet<AssetStudio.SerializedFile> pinned = [];
		assets.LoadOwnBundle("GameMain", pinned);
		// Already-loaded bundles (an earlier song) aren't reloaded into `pinned`, but their objects are in the name cache
		var failText = pinned.SelectMany(f => f.Objects).OfType<AssetStudio.GameObject>().FirstOrDefault(g => g.m_Name == "PnlFailText")
			?? assets.FindAssetByName<AssetStudio.GameObject>("PnlFailText");
		if (failText == null) {
			foreach (var pinnedFile in pinned)
				assets.Release(pinnedFile);
			Logs.Warn("[MD1SceneUI] Couldn't find PnlFailText in the GameMain scene; no fail screen.");
			return null;
		}

		var file = failText.assetsFile;
		// There's another PnlFail under AreaFail; the real one is UI_2D/Standard/PnlFail, next to PnlFailText
		var panel = file.Objects.OfType<AssetStudio.Transform>()
			.FirstOrDefault(transform => transform.GetGameObject()?.m_Name == "PnlFail" && transform.GetFather()?.GetGameObject()?.m_Name == "Standard")
			?.GetGameObject();
		if (panel == null) {
			Logs.Warn("[MD1SceneUI] Couldn't find UI_2D/Standard/PnlFail in the GameMain scene; no fail screen.");
			return null;
		}

		pinned.Add(file);
		assets.Pin(file);
		try {
			// UI_2D: CanvasScaler 1920x1080, match width
			var settings = new UnityCanvasSettings("GameMain.unity UI_2D canvas (CanvasScaler)", false, 60, 100, new(1920, 1080), 0);
			UnityCanvas ImportCanvas(AssetStudio.GameObject gameObject, MuseDash1CanvasBehaviours behaviours) {
				var canvas = UnityCanvas.Import(gameObject, settings,
					files => assets.LoadDependenciesContaining("GameMain", files, pinned),
					(canvas, node, behaviour, className, data) => MuseDash1Localization.Apply(canvas, node, behaviour, className, data) || behaviours.Capture(canvas, node, behaviour, className, data));
				MuseDash1Localization.ApplyGlobalFonts(canvas, assets.Assets.AssetsFileList);
				canvas.Report();
				return canvas;
			}

			var behaviours = new MuseDash1CanvasBehaviours();
			var intro = ImportCanvas(failText, new MuseDash1CanvasBehaviours());
			var canvas = ImportCanvas(panel, behaviours);
			return new MuseDash1FailAssets(intro, canvas, behaviours,
				MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, MuseDash1VictoryScreen.LIKE_SOUND), MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, MuseDash1VictoryScreen.UNLIKE_SOUND));
		}
		finally {
			foreach (var pinnedFile in pinned)
				if (pinnedFile != file)
					assets.Release(pinnedFile);
			assets.Unpin(file);
		}
	}

	public virtual void LoadScoreAssets() {
		GoldGreat = LoadTextureByName("GoldGreat");
		GoldPerfect = LoadTextureByName("GoldPerfect");
		ScoreGreat = LoadTextureByName("ScoreGreat");
		ScorePerfect = LoadTextureByName("ScorePerfect");
		ScorePass = LoadTextureByName("ScorePass");
	}

	public virtual void LoadMultiHitAssets() {
		MultiHitTip = new();
		MultiHitTip.Initialize();
		HitsBase = LoadTextureByName("HitsBase");
	}

	public virtual void LoadHealthBarAssets() {
		BelowBase = LoadTextureByName("BelowBase");
		hp_icon = LoadTextureByName("hp_icon");
		hp_icon_mistake = LoadTextureByName("hp_icon_mistake");
		hp_slider = LoadTextureByName("hp_slider");
		hp_slider_base = LoadTextureByName("hp_slider_base");
		HpFeverBase = LoadTextureByName("HpFeverBase");
		slider_light = LoadTextureByName("slider_light");
		power_slider = LoadTextureByName("power_slider");
		power_slider_white = LoadTextureByName("power_slider_white");
		Fever = LoadTextureByName("Fever");
		bubble = LoadTextureByName("bubble");
	}

	public virtual void LoadComboModels() {
		combo1model = MuseDash1ModelConverter.MD_GetModelData(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.MonoBehaviour>("fx_combo_1_SkeletonData")!)?.Instantiate();
		combo2model = MuseDash1ModelConverter.MD_GetModelData(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.MonoBehaviour>("fx_combo_2_SkeletonData")!)?.Instantiate();
	}

	Vector2F TextPosition(PathwaySet pathways) => pathways.Count == 1 ? scene.GetPathwayPosition(pathways.First) : scene.GetCenterPosition();
	bool IsAirText(PathwaySet pathways) => pathways.Count == 1 && scene.IsAirPathway(pathways.First);

	public virtual void CreateGreatHitText(double precision, PathwaySet pathways, bool inFever, EarlyLate earlylate) {
		Color color = inFever ? new(255, 108, 0) : new(146, 55, 255);
		var text = new TextImageRenderItem(Time, 0.5, TextPosition(pathways), 0, new(TextScale), inFever ? GoldGreat : ScoreGreat, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : IsAirText(pathways) ? UITextAnimationFns.UpwardFadeout : UITextAnimationFns.UpwardFadeoutCurved);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreateHealthText(float healthGiven, PathwaySet pathways) {
		var text = new TextImageRenderItem(Time, 0.5, TextPosition(pathways), 0, new(MinorTextScale), $"{Math.Round(healthGiven)}", "Snaps Taste", new(107, 226, 0), IsAirText(pathways) ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		text.Worldspace = true;
		ForegroundItems.Add(text);
	}

	public virtual void CreatePassText(double precision, PathwaySet pathways) {
		var pos = TextPosition(pathways);
		pos.X -= 0.8f;
		var text = new TextImageRenderItem(Time, 0.5, pos, 0, new(TextScale), ScorePass, Color.White, UITextAnimationFns.UpwardFadeoutMoveLeft);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreatePerfectHitText(double precision, PathwaySet pathways, bool inFever, EarlyLate earlylate) {
		var text = new TextImageRenderItem(Time, 0.5, TextPosition(pathways), 0, new(TextScale), inFever ? GoldPerfect : ScorePerfect, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : IsAirText(pathways) ? UITextAnimationFns.UpwardFadeout : UITextAnimationFns.UpwardFadeoutCurved);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreateScoreText(int scoreGiven, PathwaySet pathways) {
		var text = new TextImageRenderItem(Time, 0.5, TextPosition(pathways), 0, new(MinorTextScale), $"{scoreGiven}", "Snaps Taste", new(0, 191, 255), IsAirText(pathways) ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		text.Worldspace = true;
		ForegroundItems.Add(text);
	}

	public void EndMultiHitText() {
		InMultiHit = false;
	}

	public void EndWarning() {
		MultiHitTip?.Hide();
	}

	public void OpenVictory(StatisticsData stats) {
		if (IValidatable.IsValid(CurrentStatisticsPanel))
			return;

		CurrentStatisticsPanel = LoadPanel(stats);
		if (CurrentStatisticsPanel == null)
			return;

		CurrentStatisticsPanel.
		Size = new(1, 1);
		CurrentStatisticsPanel.DynamicallySized = true;
	}

	public void OpenFailure() {
		if (IValidatable.IsValid(CurrentFailPanel))
			return;

		// Reused across deaths: each open replays the clips from 0, which resets every animated node, and re-sets the rest.
		// Re-importing on restart meant parsing the whole scenes bundle mid-game, which froze the game.
		failCanvas ??= ImportFailCanvas();
		if (failCanvas != null)
			CurrentFailPanel = new MuseDash1FailPanel(EngineCore.Level.RootPanel, game, scene, failCanvas);
	}

	public void RenderFailureUnderlay() {
		if (IValidatable.IsValid(CurrentFailPanel))
			CurrentFailPanel.RenderIntro(EngineCore.GetWindowWidth(), EngineCore.GetWindowHeight());
	}

	void CloseFailure() {
		if (CurrentFailPanel == null)
			return;

		CurrentFailPanel.Remove();
		CurrentFailPanel = null;
	}

	protected virtual StatisticsPanel? LoadPanel(StatisticsData stats) {
		var canvas = victoryCanvas ?? ImportVictoryCanvas();
		victoryCanvas = null;
		var panel = new StatisticsPanel(EngineCore.Level.RootPanel, game, stats, canvas);
		return panel;
	}

	void CloseVictory() {
		CurrentStatisticsPanel?.Remove();
		CurrentStatisticsPanel = null;
	}

	public void UpdateHit() {
		LastHitTime = Time;
	}

	public void PreRenderWorldspace() {
		var t = Time;
		foreach (var text in BackgroundItems)
			text.Render(t, StyledTextShader);
	}

	public void PostRenderWorldspace() {
		var t = Time;
		foreach (var text in ForegroundItems)
			text.Render(t, StyledTextShader);
		MultiHitTip?.Render();
	}

	public void RenderUI() {
		float width = EngineCore.Level.FrameState.WindowWidth;
		float height = EngineCore.Level.FrameState.WindowHeight;
		DrawUI(width, height);
	}

	Vector2F GetTextureSize(ITexture? tex) => tex == null ? default : new(tex.GetWidth(), tex.GetHeight());
	public void DrawSliderLight(ITexture? stencilMask, Vector2F stencilSize, float progress, float alpha, Vector2F offset, Color sliderColor) {
		sliderColor.A = (byte)(int)(255 * Math.Clamp(alpha, 0, 1));
		Vector2F drawRectPos = new(-(stencilSize.W / 2) + offset.X, -stencilSize.H + offset.Y);

		bool returnBackFlag = false; // used in the gotos
	drawStencilMask:
		if (stencilMask != null) {
			float oldProgress = progress;
			if (returnBackFlag)
				progress = 1; // draw the full thing for the second stencil pass

			Stencils.Begin();
			Stencils.Function = StencilFunction.Always;
			Stencils.Reference = 1;
			Stencils.Mask = 0xFF;
			Stencils.OnFail = StencilOperation.Keep;
			Stencils.OnDepthFail = StencilOperation.Keep;
			Stencils.OnDepthPass = StencilOperation.Replace;
			Stencils.BeginMask();

			Graphics2D.SetTexture(stencilMask);
			Graphics2D.DrawTexturedProgress(drawRectPos, stencilSize, progress, Axis.Horizontal);

			Stencils.EndMask();

			if (returnBackFlag) {
				progress = oldProgress;
				goto returnBackHere;
			}
		}
		Graphics2D.SetDrawColor(sliderColor);
		Graphics2D.DrawRectangle(drawRectPos.Round(), new(stencilSize.W * progress, stencilSize.H));
		if (stencilMask != null) Stencils.End();

		returnBackFlag = true;
		goto drawStencilMask;
	returnBackHere:
		Graphics2D.SetTexture(slider_light);
		Graphics2D.DrawTexturedRectangle((drawRectPos + new Vector2F((stencilSize.W * progress), 0)).Round(), new(32, stencilSize.H));
		if (stencilMask != null) Stencils.End();
	}

	public void DrawSomeBubbles(ITexture? stencilMask, Vector2F stencilSize, float progress, float uvAdd, Vector2F offset, Color bubbleColor) {
		if (bubble == null) return;

		if (stencilMask != null) {
			Stencils.Begin();
			Stencils.Function = StencilFunction.Always;
			Stencils.Reference = 1;
			Stencils.Mask = 0xFF;
			Stencils.OnFail = StencilOperation.Keep;
			Stencils.OnDepthFail = StencilOperation.Keep;
			Stencils.OnDepthPass = StencilOperation.Replace;
			Stencils.BeginMask();

			Graphics2D.SetTexture(stencilMask);
			Graphics2D.DrawTexturedProgress(new(-(stencilSize.W / 2) + offset.X, -stencilSize.H + offset.Y), stencilSize, progress, Axis.Horizontal);

			Stencils.EndMask();
		}

		Rlgl.DrawRenderBatchActive();
		Rlgl.SetTexture(bubble.GetTextureHandle());
		Rlgl.Begin(DrawMode.QUADS);

		Rlgl.Color4ub(bubbleColor.R, bubbleColor.G, bubbleColor.B, bubbleColor.A);

		float size = 500;
		float bubbleScaling = 5;
		float uvOffset = (float)((Time / 12) % 1.0);

		Rlgl.TexCoord2f(0f, ((0f + uvOffset) * bubbleScaling) + uvAdd);
		Rlgl.Vertex3f(-size, -size, 0f);

		Rlgl.TexCoord2f(1f * bubbleScaling, ((0f + uvOffset) * bubbleScaling) + uvAdd);
		Rlgl.Vertex3f(size, -size, 0f);

		Rlgl.TexCoord2f(1f * bubbleScaling, ((1f + uvOffset) * bubbleScaling) + uvAdd);
		Rlgl.Vertex3f(size, size, 0f);

		Rlgl.TexCoord2f(0f, ((1f + uvOffset) * bubbleScaling) + uvAdd);
		Rlgl.Vertex3f(-size, size, 0f);

		Rlgl.End();
		Rlgl.DrawRenderBatchActive();
		Rlgl.SetTexture(0);

		if (stencilMask != null) Stencils.End();
	}
	private void DrawUI(float w, float h) {
		UIAlphatestShader?.Activate();

		float resize = (h / 1080f);
		// Draw the health/fever bars
		{
			Rlgl.PushMatrix();

			Rlgl.Translatef(w / 2, h, 0);
			Rlgl.Scalef(resize, resize, 1);

			RenderHealth();
			Rlgl.DrawRenderBatchActive();
			Rlgl.PopMatrix();
		}

		// Draw score
		{
			Rlgl.PushMatrix();

			Rlgl.Translatef(0, 0, 0);
			Rlgl.Scalef(resize, resize, 1);
			RenderScore();
			Rlgl.PopMatrix();
		}

		UIAlphatestShader?.Deactivate();

		// Draw combo

		Rlgl.PushMatrix();

		Rlgl.Translatef(w / 2, 0, 0);
		Rlgl.Scalef(resize * 0.5f, resize * 0.5f, 1);

		RenderComboBackground();

		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();

		if (ComboGrade != ComboGrade.NotApplicable) {
			PrepareComboColors(ComboGrade, out var borderColor, out var color);
			SetComboColors(borderColor, color);

			Rlgl.PushMatrix();
			PreRenderCombo();
			Rlgl.Translatef(w / 2, 0, 0);
			Rlgl.Scalef(resize, resize, 1);

			UIAlphatestShader?.Activate();
			RenderComboForeground();
			UIAlphatestShader?.Deactivate();

			Rlgl.PopMatrix();
		}
	}

	protected virtual void SetComboColors(Color borderColor, Color color) {
		ComboNumber?.borderColor = borderColor;
		ComboNumber?.StartColor = color;
	}

	protected virtual void PrepareComboColors(ComboGrade comboGrade, out Color borderColor, out Color color) {
		switch (comboGrade) {
			case ComboGrade.Low:
				borderColor = ComboLowBorderColor;
				color = ComboLowColor;
				break;
			case ComboGrade.High:
				borderColor = ComboHighBorderColor;
				color = ComboHighColor;
				break;
			default:
				borderColor = default;
				color = default;
				break;
		}
	}

	public virtual void RenderHealth() {
		Vector2F belowBaseSize = GetTextureSize(BelowBase);
		Vector2F hpFeverBaseSize = GetTextureSize(HpFeverBase);
		Vector2F hp_sliderSize = GetTextureSize(hp_slider);
		Vector2F power_sliderSize = GetTextureSize(power_slider);
		Vector2F power_slider_whiteSize = GetTextureSize(power_slider_white);

		Graphics2D.SetDrawColor(255, 255, 255, 255);
		Graphics2D.SetTexture(BelowBase);
		Graphics2D.DrawTexturedRectangle(new(0, -belowBaseSize.H), belowBaseSize);
		Graphics2D.DrawTexturedRectangle(new(-belowBaseSize.W, -belowBaseSize.H), belowBaseSize, flipX: true);

		Graphics2D.SetTexture(HpFeverBase);
		Graphics2D.DrawTexturedRectangle(new(-(hpFeverBaseSize.W / 2), -hpFeverBaseSize.H), hpFeverBaseSize);

		float hpRatio = (float)(HP / MaxHP);
		float feverRatio;
		if (!InFever)
			feverRatio = (float)(CurrentFever / MaxFever);
		else
			feverRatio = (float)(FeverRemainingTime / FeverTotalTime);

		Graphics2D.SetTexture(hp_slider);
		Graphics2D.SetDrawColor(255, 53, 133);
		Graphics2D.DrawTexturedProgress(new(-(hp_sliderSize.W / 2) - 56, -hp_sliderSize.H - 4), hp_sliderSize, hpRatio, Axis.Horizontal);
		Graphics2D.SetDrawColor(255, 255, 255);
		DrawSomeBubbles(hp_slider, hp_sliderSize, (float)hpRatio, 0, new(-56, -4), new(185, 43, 105));

		Graphics2D.SetTexture(power_slider);
		Graphics2D.DrawTexturedProgress(new(-(power_sliderSize.W / 2) + 61, -power_sliderSize.H - 4), power_sliderSize, feverRatio, Axis.Horizontal);
		DrawSomeBubbles(power_slider, power_sliderSize, (float)feverRatio, 0.3f, new(61, -4), new(87, 181, 245));
		DrawSliderLight(power_slider, power_sliderSize, (float)feverRatio, (float)NMath.Remap(Time - LastFeverUpdateTime, 0, 0.3, 1, 0, clampInput: true), new(61, -4), new(180, 229, 255));

		float feverActivated = Math.Clamp((float)(Time - EnterFeverTime) * 1, 0, 1);
		if (feverActivated < 1) {
			Graphics2D.SetDrawColor(255, 255, 255, (int)((float)NMath.Ease.InQuart(1 - feverActivated) * 255));
			Graphics2D.SetTexture(power_slider_white);
			Graphics2D.DrawTexturedRectangle(new Vector2F(-(power_slider_whiteSize.W / 2) + 0, -power_slider_whiteSize.H + 16), power_slider_whiteSize);
		}

		Graphics2D.SetDrawColor(255, 255, 255, 255);
		float fontSize = 32;
		Graphics2D.DrawText(new(0, -(fontSize * 0.85f)), $"{Math.Round(HP)}/{MaxHP}", "Noto Sans Bold", fontSize, Anchor.Center);
		if (Fever != null) {
			Graphics2D.SetTexture(Fever);
			Graphics2D.DrawTexturedRectangle(new Vector2F(300, -38f), new Vector2F(Fever.GetWidth(), Fever.GetHeight()));
		}
		if (hp_icon != null) {
			Graphics2D.SetTexture(hp_icon);
			float lastHitTime = Math.Clamp((float)(Time - LastHitTime) * 1, 0, 1);
			float size = (float)NMath.Remap(lastHitTime, 0, 0.3, 1, 1.15, clampInput: true);

			Vector2F sizeOfHeart = new(hp_icon.GetWidth() * size, hp_icon.GetHeight() * size);
			Graphics2D.DrawTexturedRectangle(new(-328, -38f), sizeOfHeart, origin: sizeOfHeart / 2);
		}
	}
	public virtual void RenderScore() {
		ScoreNumber?.Render(Time, StyledTextShader);
		ScoreLabel?.Render(Time, StyledTextShader);
	}
	// allows setting up custom transforms in inheritors
	public virtual void PreRenderCombo() {

	}
	public virtual void RenderComboBackground() {
		if (ShouldRenderCombo1()) renderOneCombo(combo1model, animations_1);
		if (ShouldRenderCombo2()) renderOneCombo(combo2model, animations_2);
	}
	public float GetSizeForComboHit() {
		float sizeT = (float)NMath.Remap(Time - LastComboUpdateTime, 0, 0.25, 0, 1, clampInput: true);
		float size = (float)NMath.Remap(NMath.Ease.OutQuad(sizeT), 0, 1, 1.2, 1, clampInput: true);
		return size;
	}
	public virtual void RenderComboForeground() {
		ComboNumber?.SplitY = 1;
		ComboNumber?.StartPosition = new(0, -180);
		ComboNumber?.Scale = new(GetSizeForComboHit());
		ComboNumber?.Render(Time, StyledTextShader);
	}
	void renderOneCombo(ModelInstance? model, AnimationHandler anims) {
		if (model == null) return;
		model.Scale = new(1f);
		model.Position = new(0, 1105);
		anims.Apply(model);
		model.Render();
	}
	ModelInstance? combo1model;
	ModelInstance? combo2model;
	AnimationHandler animations_1 = new();
	AnimationHandler animations_2 = new();
	public void Think(double dt) {
		CleanupTextLists(Time);
		foreach (var text in BackgroundItems) text.DetermineRenderTexture();
		foreach (var text in ForegroundItems) text.DetermineRenderTexture();

		animations_1.AddDeltaTime(dt);
		animations_2.AddDeltaTime(dt);
		if (!game.Paused)
			MultiHitTip?.Think(dt);

		if (ScoreLabel != null && ScoreNumber != null) {
			ScoreLabel.TopLeftAligned = ScoreNumber.TopLeftAligned = true;
			ScoreLabel.DesaturateAmount = ScoreNumber.DesaturateAmount = 0;
			ScoreLabel.DarkenAmount = ScoreNumber.DarkenAmount = 0.3f;
			ScoreLabel.SplitY = ScoreNumber.SplitY = 0;
			ScoreNumber.BorderSize = 4;
		}
		ComboNumber?.BorderSize = 4;

		ScoreNumber?.StartPosition = new(100, -16);
		ScoreLabel?.StartPosition = new(100, -96);
		ScoreNumber?.borderColor = new Color(173, 173, 255) * new Color(200, 255);

		ScoreNumber?.StartScale = new(1f);
		ScoreNumber?.StartScale = new(0.9f);
		ScoreNumber?.DetermineRenderTexture();
		ComboNumber?.DetermineRenderTexture();
	}

	public bool ShowingVictoryScreen() => IValidatable.IsValid(CurrentStatisticsPanel);
	public bool ShowingFailureScreen() => IValidatable.IsValid(CurrentFailPanel);


	public void StartMultiHitText() {
		InMultiHit = true;
		MultiHits = 0;
	}

	double LastComboUpdateTime = -2000000;

	public void StartWarning() {
		MultiHitTip?.Play();
	}

	public void UpdateAllPerfect(bool allPerfect) {
		AllPerfect = allPerfect;
	}

	public static ComboGrade GetGrade(int combo) {
		if (combo >= (int)ComboGrade.High)
			return ComboGrade.High;
		if (combo >= (int)ComboGrade.Low)
			return ComboGrade.Low;
		return ComboGrade.NotApplicable;
	}

	public void UpdateCombo(int currentCombo) {
		if (Combo == currentCombo) return;
		Combo = currentCombo;
		ComboNumber?.Text = $"{Combo}";
		LastComboUpdateTime = Time;
		var currentComboGrade = GetGrade(currentCombo);
		if (currentComboGrade != ComboGrade)
			UpdateComboGrade(currentComboGrade);
	}

	private void UpdateComboGrade(ComboGrade nextComboGrade) {
		var lastComboGrade = ComboGrade;
		ComboGrade = nextComboGrade;

		if (animations_1.GetModelData() == null) animations_1.SetModel(combo1model);
		if (animations_2.GetModelData() == null) animations_2.SetModel(combo2model);

		switch (nextComboGrade) {
			case ComboGrade.NotApplicable:
				switch (lastComboGrade) {
					case ComboGrade.Low:
						DeactivateCombo1();
						break;
					case ComboGrade.High:
						DeactivateCombo2();
						break;
				}
				break;
			case ComboGrade.Low:
				switch (lastComboGrade) {
					case ComboGrade.NotApplicable:
						ActivateCombo1();
						break;
					case ComboGrade.High:
						DeactivateCombo2();
						ActivateCombo1();
						break;
				}
				break;
			case ComboGrade.High:
				switch (lastComboGrade) {
					case ComboGrade.NotApplicable:
						ActivateCombo2();
						break;
					case ComboGrade.Low:
						DeactivateCombo1();
						ActivateCombo2();
						break;
				}
				break;
		}
	}

	void ActivateCombo1() {
		animations_1.ClearAllAnimation();
		animations_1.SetAnimation(0, "start");
		animations_1.AddAnimation(0, "stand", true);
		combo1model?.SetToSetupPose();
	}
	void ActivateCombo2() {
		animations_2.ClearAllAnimation();
		animations_2.SetAnimation(0, "start");
		animations_2.AddAnimation(0, "stand", true);
		combo2model?.SetToSetupPose();
	}
	void DeactivateCombo1() {
		animations_1.ClearAllAnimation();
		animations_1.SetAnimation(0, "end");
	}
	void DeactivateCombo2() {
		animations_2.ClearAllAnimation();
		animations_2.SetAnimation(0, "end");
	}
	void ForceDeactivateCombo1() {
		animations_1.ClearAllAnimation();
		animations_1.SetAnimation(0, "end");
		if (animations_1.IsAnimationQueued())
			animations_1.AddDeltaTime(animations_1.Channels[0].QueuedEntries.Peek().Animation.Duration);
	}
	void ForceDeactivateCombo2() {
		animations_2.ClearAllAnimation();
		animations_2.SetAnimation(0, "end");
		if (animations_2.IsAnimationQueued())
			animations_2.AddDeltaTime(animations_2.Channels[0].QueuedEntries.Peek().Animation.Duration);
	}
	public bool ShouldRenderCombo1() => animations_1.IsPlayingAnimation();
	public bool ShouldRenderCombo2() => animations_2.IsPlayingAnimation();

	double LastFeverUpdateTime;
	public void UpdateFeverProgress(double fever, double maxFever) {
		InFever = false;
		CurrentFever = fever;
		MaxFever = maxFever;
		LastFeverUpdateTime = Time;
	}

	public void UpdateFullCombo(bool fullCombo) {
		FullCombo = fullCombo;
	}

	public void UpdateHP(double hp, double maxHP) {
		HP = hp;
		MaxHP = maxHP;
	}

	public void UpdateInFever(double feverRemainingTime, double feverTotalTime) {
		if (!InFever) {
			EnterFeverTime = Time;
		}
		InFever = true;
		FeverRemainingTime = feverRemainingTime;
		FeverTotalTime = feverTotalTime;
	}

	public void UpdateMultiHitText(int hits) {
		MultiHits = hits;
	}

	public void UpdateScore(double score) {
		Score = score;
		ScoreNumber?.Text = $"{Math.Round(score)}";
	}

	public void Reset() {
		BackgroundItems.Clear();
		ForegroundItems.Clear();
		EnterFeverTime = -2000000;
		LastHitTime = -2000000;
		LastComboUpdateTime = -2000000;
		ComboGrade = ComboGrade.NotApplicable;
		Combo = 0;
		FeverRemainingTime = 0;
		FeverTotalTime = 0;
		CloseVictory();
		EndWarning();
		CloseFailure();

		ForceDeactivateCombo1();
		ForceDeactivateCombo2();
	}
}

public class MD1SceneUIGrooveCoaster(IMuseDash1SceneInstance scene, RhythmGameLevel game) : MD1SceneUI(scene, game)
{
	protected TextImageRenderItem ComboLabel = null!;

	public override void SetupComboNumber() {
		base.SetupComboNumber();
		ComboNumber?.Font = "Infinity Font";
		ComboLabel = new(0, 0, new(0, 0), 0, new(1, 1), "COMBO", "Snaps Taste", new(165, 254, 254), null);
	}

	protected override void SetComboColors(Color borderColor, Color color) {
		base.SetComboColors(borderColor, color);
		ComboLabel.borderColor = borderColor;
		ComboLabel.StartColor = color;
	}

	public override void SetupScoreNumber() {
		base.SetupScoreNumber();
		ScoreNumber?.Font = "Infinity Font";
	}

	public override void LoadScoreAssets() {
		GoldGreat = LoadTextureByName("GoldGreatGC");
		GoldPerfect = LoadTextureByName("GoldPerfectGC");
		ScoreGreat = LoadTextureByName("ScoreGreatGC");
		ScorePerfect = LoadTextureByName("ScorePerfectGC");
		ScorePass = LoadTextureByName("ScorePassGC");
	}

	public override void PreRenderCombo() {
		float value = GetSizeForComboHit();
		Rlgl.Scalef(value, value, 1);
	}

	public override void RenderComboBackground() {

	}

	public override void RenderComboForeground() {
		if (ComboNumber == null) return;
		if (ComboLabel == null) return;
		float dist = 180;
		ComboNumber.SplitY = 1;
		ComboNumber.StartPosition = new(0, -dist);
		ComboNumber.Render(Time, StyledTextShader);

		ComboLabel.SplitY = 1;
		ComboLabel.StartPosition = new(0, dist);
		ComboLabel.Render(Time, StyledTextShader);
	}
}

public class CloneDashMD1SceneUIArknights(IMuseDash1SceneInstance scene, RhythmGameLevel game) : MD1SceneUI(scene, game)
{

}