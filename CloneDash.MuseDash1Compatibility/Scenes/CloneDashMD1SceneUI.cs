using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using CloneDash.Game.Statistics;
using CommunityToolkit.HighPerformance;
using Nucleus;
using Nucleus.Common.Graphics;
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
	ISongChart? chart;
	double start = 0;
	double Time() => globals.CurTime - start;
	IGame game;
	StatisticsData stats;

	public StatisticsPanel(Element? parent, IGame game, StatisticsData stats) : base(parent){
		this.game = game;
		this.stats = stats;

		chart = game.GetSongChart();
		if (chart == null) return;
		start = globals.CurTime;

		ICharacterDescriptor? character = CharacterMod.GetCharacterData();
		if (character == null) return;

		victory = character.CreateVictory();
		victory.Initialize(game);
		victory.PlayAudio();
		stats.Compute();

		var bottom = new Panel(this);
		bottom.SetPaintBackgroundEnabled(false);

		bottom.DynamicallySized = true;
		bottom.SetSize(new(0.07f));
		bottom.SetDock(Dock.Bottom);

		var restart = new Button(bottom);
		restart.DynamicallySized = true;
		restart.SetSize(new(.2f));
		restart.SetText("Restart");
		restart.SetDock(Dock.Left);
		restart.OnButtonClick += (_,  _) => {
			// TODO: Probably should just hard restart it...
			// Maybe seeking is stable enough now to justify this though?
			game.Restart();
			this.Remove();
		};

		var back = new Button(bottom);
		back.DynamicallySized = true;
		back.SetSize(new(.2f));
		back.SetText("Main Menu");
		back.SetDock(Dock.Right);
		back.OnButtonClick += (_, _) => LevelTransitions.LoadMainMenu();

		BorderSize = 0;
	}
	void RenderOneLine(ReadOnlySpan<char> line, int fs, ref int y) {
		Graphics2D.DrawText(16, 16 + y, line, Graphics2D.UI_FONT_NAME, fs);
		y += fs + 4;
	}
	public override void Paint(float width, float height) {
		SetBgColor(new Color(0, 0, 0, (int)(220 * (float)NMath.Ease.OutQuad(NMath.Remap(Time(), 0, 0.5, 0, 1, true)))));
		base.Paint(width, height);

		Vector2F position = new(width / 2, (1 - (float)NMath.Ease.OutElastic(Math.Clamp(Time() * 0.2, 0, 1))) * (height));
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = (new Vector2F(width / 2, height / 1)).ToNumerics()
		});
		victory.Render();
		EngineCore.Window.EndMode2D();

		var chart = (MD1_SongChart?)this.chart;
		if (chart == null) return;
		if (stats == null) return;

		Graphics2D.SetDrawColor(255, 255, 255);
		stats.Compute();
		var fs = 24;
		var y = 0;

		Match boldRegexMatch = Util.BoldRegex.Match(chart.Song.Name);
		Graphics2D.DrawText(16, 16 + y,
							boldRegexMatch.Success ? boldRegexMatch.Groups[1].Value : chart.Song.Name,
							boldRegexMatch.Success ? Graphics2D.UI_MONO_BOLD_FONT_NAME : Graphics2D.UI_CN_JP_FONT_NAME,
							fs);
		y += fs + 4;

		RenderOneLine($"      Rating: {chart.Rating}", fs, ref y);
		RenderOneLine($"      Grade: {stats.Grade}", fs, ref y);
		RenderOneLine($"      Accuracy: {stats.Accuracy}", fs, ref y);
		RenderOneLine($"      Score: {stats.Score}", fs, ref y);
		RenderOneLine($"      Max Combo: {stats.MaxCombo}", fs, ref y);
		RenderOneLine("", fs, ref y);
		RenderOneLine($"      Perfects: {stats.Perfects}", fs, ref y);
		RenderOneLine($"      Greats: {stats.Greats}", fs, ref y);
		RenderOneLine($"      Passes: {stats.Passes}", fs, ref y);
		RenderOneLine($"      Misses: {stats.Misses}", fs, ref y);
		RenderOneLine("", fs, ref y);
		RenderOneLine($"      Earlys: {stats.Earlys}", fs, ref y);
		RenderOneLine($"      Exacts: {stats.Exacts}", fs, ref y);
		RenderOneLine($"      Lates: {stats.Lates}", fs, ref y);
		RenderOneLine("", fs, ref y);
		RenderOneLine($"      Registered: {stats.OrderedEnemies.Count}", fs, ref y);
	}
	protected override void OnThink() {
		base.OnThink();
		if (victory != null) 
			victory.Think();
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
		if (textRT.HasValue)
			Graphics2D.DestroyRenderTarget(textRT.Value);
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
					Graphics2D.DrawTexture(new(0, 0), new(Texture.Width / FontResolution, Texture.Height / FontResolution));
				else
					Graphics2D.DrawTexture(new(0, 0), new(Texture.Width, Texture.Height));
			}
			else {
				if (Worldspace)
					Graphics2D.DrawTexture(new(Texture.Width / -FontResolution / 2, Texture.Height / -FontResolution / 2), new(Texture.Width / FontResolution, Texture.Height / FontResolution));
				else
					Graphics2D.DrawTexture(new(Texture.Width / -2, Texture.Height / -2), new(Texture.Width, Texture.Height));
			}
			Rlgl.DrawRenderBatchActive();
			Rlgl.PopMatrix();
		}
	}

	RenderTexture2D? textRT;
	public Vector2F GetTextSize() {
		Vector2F textSize = Graphics2D.GetTextSize(Text!, Font!, FontResolution);
		int pad = RTPadding;
		int rtW = (int)textSize.X + pad * 2;
		int rtH = (int)textSize.Y + pad * 2;
		return new(rtW, rtH);
	}
	public RenderTexture2D? DetermineRenderTexture() {
		if (Text == null || Font == null)
			return null;

		var rtSize = GetTextSize();
		ulong textHash = Text.Hash(invariant: false);
		ulong fontHash = Font.Hash(invariant: false);
		bool requiresRedraw = !textRT.HasValue || (textHash != lastTextHash || fontHash != lastFontHash);
		bool requiresResize = !textRT.HasValue || (textRT.Value.Texture.Width < rtSize.W || textRT.Value.Texture.Height < rtSize.H);
		if (requiresResize) {
			if (textRT.HasValue) {
				Graphics2D.DestroyRenderTarget(textRT.Value);
				textRT = null;
			}
		}

		if (!textRT.HasValue) {
			textRT = Graphics2D.CreateRenderTarget(rtSize.W, rtSize.H);
			Raylib.SetTextureFilter(textRT!.Value.Texture, TextureFilter.Bilinear);
		}

		if (requiresRedraw) {
			RenderTexture2D rt = textRT.Value;

			int pad = RTPadding;
			Graphics2D.BeginRenderTarget(rt);
			Rlgl.ClearColor(255, 255, 255, 0);
			Rlgl.ClearScreenBuffers();
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(pad, pad, Text!, Font!, FontResolution);
			Rlgl.DrawRenderBatchActive();
			Graphics2D.EndRenderTarget();

			lastTextHash = textHash;
			lastFontHash = fontHash;
		}

		return textRT.Value;
	}
	void RenderStyledText(Vector2F position, Vector2F scale, float rotation, Color color, Color? borderColor, IShader? shader) {
		float alpha = color.A / 255f;
		var rtN = textRT;
		if (!rtN.HasValue) return;

		var rt = rtN.Value;
		var rtSize = GetTextSize();
		var rtW = rt.Texture.Width;
		var rtH = rt.Texture.Height;

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

		Rlgl.SetBlendMode(BlendMode.BLEND_CUSTOM);
		Rlgl.SetBlendFactors(GLEnum.ONE, GLEnum.ONE_MINUS_SRC_ALPHA, GLEnum.FUNC_ADD);
		Graphics2D.DrawRendertarget(drawX, drawY, rtW, rtH);
		Rlgl.SetBlendMode(BlendMode.BLEND_ALPHA);

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

public class MD1SceneUI(IMuseDash1SceneInstance scene, IGame game) : IMuseDash1SceneUI
{
	protected StatisticsPanel? CurrentStatisticsPanel;

	protected readonly List<TextImageRenderItem> BackgroundItems = [];
	protected readonly List<TextImageRenderItem> ForegroundItems = [];

	protected virtual Color ScoreColor => new(165, 254, 254);

	protected TextImageRenderItem ScoreNumber = null!;
	protected TextImageRenderItem ScoreLabel = null!;
	protected TextImageRenderItem ComboNumber = null!;

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

	protected ITexture? MultiHitTip;
	protected ITexture? MultiHitTipDialog;
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

	public virtual void Dispose() {
		foreach (var item in BackgroundItems) item.Dispose();
		foreach (var item in ForegroundItems) item.Dispose();
		ScoreNumber.Dispose();
		ScoreLabel.Dispose();
		ComboNumber.Dispose();
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

	protected double Time => scene.GetGame().GetConductor().GetTime();
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
	bool Warning;
	bool Seeking;
	ComboGrade ComboGrade;
	double ComboGradeUpdateTime = -200000;

	public static float MinorTextScale => 0.65f;
	public static float TextScale => 0.85f;

	protected ITexture? LoadTextureByName(ReadOnlySpan<char> name) => MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>(name)!);
	public void SetSeeking(bool seeking) => Seeking = seeking;
	public virtual void Initialize() {
		SetupScoreNumber();
		SetupComboNumber();

		StyledTextShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "styled_text.fs");
		UIAlphatestShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "ui_alphatest.fs");

		score_English = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("score_English")!);
		ScoreLabel = new(0, 0, new(0, 0), 0, new(1, 1), score_English, new(165, 254, 254), null);

		LoadScoreAssets();
		LoadMultiHitAssets();
		LoadHealthBarAssets();
		LoadComboModels();

		ForceDeactivateCombo1();
		ForceDeactivateCombo2();
	}

	public virtual void LoadScoreAssets() {
		GoldGreat = LoadTextureByName("GoldGreat");
		GoldPerfect = LoadTextureByName("GoldPerfect");
		ScoreGreat = LoadTextureByName("ScoreGreat");
		ScorePerfect = LoadTextureByName("ScorePerfect");
		ScorePass = LoadTextureByName("ScorePass");
	}

	public virtual void LoadMultiHitAssets() {
		MultiHitTip = LoadTextureByName("MultiHitTip");
		MultiHitTipDialog = LoadTextureByName("MultiHitTipDialog");
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

	public virtual void CreateGreatHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		Color color = inFever ? new(255, 108, 0) : new(146, 55, 255);
		var text = new TextImageRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(TextScale), inFever ? GoldGreat : ScoreGreat, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : pathway == PathwaySide.Top ? UITextAnimationFns.UpwardFadeout : UITextAnimationFns.UpwardFadeoutCurved);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreateHealthText(float healthGiven, PathwaySide pathway) {
		var text = new TextImageRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(MinorTextScale), $"{Math.Round(healthGiven)}", "Snaps Taste", new(107, 226, 0), pathway == PathwaySide.Top ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		text.Worldspace = true;
		ForegroundItems.Add(text);
	}

	public virtual void CreatePassText(double precision, PathwaySide pathway) {
		var pos = scene.GetPathwayPosition(pathway);
		pos.X -= 0.8f;
		var text = new TextImageRenderItem(Time, 0.5, pos, 0, new(TextScale), ScorePass, Color.White, UITextAnimationFns.UpwardFadeoutMoveLeft);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreatePerfectHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		var text = new TextImageRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(TextScale), inFever ? GoldPerfect : ScorePerfect, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : pathway == PathwaySide.Top ? UITextAnimationFns.UpwardFadeout : UITextAnimationFns.UpwardFadeoutCurved);
		text.Worldspace = true;
		BackgroundItems.Add(text);
	}

	public virtual void CreateScoreText(int scoreGiven, PathwaySide pathway) {
		var text = new TextImageRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(MinorTextScale), $"{scoreGiven}", "Snaps Taste", new(0, 191, 255), pathway == PathwaySide.Top ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		text.Worldspace = true;
		ForegroundItems.Add(text);
	}

	public void EndMultiHitText() {
		InMultiHit = false;
	}

	public void EndWarning() {
		Warning = false;
	}

	public void OpenVictory(StatisticsData stats) {
		if (IValidatable.IsValid(CurrentStatisticsPanel))
			return;

		CurrentStatisticsPanel = LoadPanel(stats);
		if (CurrentStatisticsPanel == null)
			return;

		CurrentStatisticsPanel.SetSize(new(1, 1));
		CurrentStatisticsPanel.DynamicallySized = true;
	}

	public void OpenFailure() {

	}

	protected virtual StatisticsPanel? LoadPanel(StatisticsData stats) {
		var panel = new StatisticsPanel(EngineCore.Level.RootPanel, game, stats);
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
	}

	public void RenderUI() {
		float width = EngineCore.Level.FrameState.WindowWidth;
		float height = EngineCore.Level.FrameState.WindowHeight;
		DrawUI(width, height);
	}

	Vector2F GetTextureSize(ITexture? tex) => tex == null ? default : new(tex.Width, tex.Height);
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
			Graphics2D.DrawImageHorizontalProgress(drawRectPos, stencilSize, horizontalProgress: progress);

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
		Graphics2D.DrawImage((drawRectPos + new Vector2F((stencilSize.W * progress), 0)).Round(), new(32, stencilSize.H));
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
			Graphics2D.DrawImageHorizontalProgress(new(-(stencilSize.W / 2) + offset.X, -stencilSize.H + offset.Y), stencilSize, horizontalProgress: progress);

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
		ComboNumber.borderColor = borderColor;
		ComboNumber.StartColor = color;
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
		Graphics2D.DrawImage(new(0, -belowBaseSize.H), belowBaseSize);
		Graphics2D.DrawImage(new(-belowBaseSize.W, -belowBaseSize.H), belowBaseSize, flipX: true);

		Graphics2D.SetTexture(HpFeverBase);
		Graphics2D.DrawImage(new(-(hpFeverBaseSize.W / 2), -hpFeverBaseSize.H), hpFeverBaseSize);

		float hpRatio = (float)(HP / MaxHP);
		float feverRatio;
		if (!InFever)
			feverRatio = (float)(CurrentFever / MaxFever);
		else
			feverRatio = (float)(FeverRemainingTime / FeverTotalTime);

		Graphics2D.SetTexture(hp_slider);
		Graphics2D.SetDrawColor(255, 53, 133);
		Graphics2D.DrawImageHorizontalProgress(new(-(hp_sliderSize.W / 2) - 56, -hp_sliderSize.H - 4), hp_sliderSize, horizontalProgress: hpRatio);
		Graphics2D.SetDrawColor(255, 255, 255);
		DrawSomeBubbles(hp_slider, hp_sliderSize, (float)hpRatio, 0, new(-56, -4), new(185, 43, 105));

		Graphics2D.SetTexture(power_slider);
		Graphics2D.DrawImageHorizontalProgress(new(-(power_sliderSize.W / 2) + 61, -power_sliderSize.H - 4), power_sliderSize, horizontalProgress: feverRatio);
		DrawSomeBubbles(power_slider, power_sliderSize, (float)feverRatio, 0.3f, new(61, -4), new(87, 181, 245));
		DrawSliderLight(power_slider, power_sliderSize, (float)feverRatio, (float)NMath.Remap(Time - LastFeverUpdateTime, 0, 0.3, 1, 0, clampInput: true), new(61, -4), new(180, 229, 255));

		float feverActivated = Math.Clamp((float)(Time - EnterFeverTime) * 1, 0, 1);
		if (feverActivated < 1) {
			Graphics2D.SetDrawColor(255, 255, 255, (int)((float)NMath.Ease.InQuart(1 - feverActivated) * 255));
			Graphics2D.SetTexture(power_slider_white);
			Graphics2D.DrawImage(new Vector2F(-(power_slider_whiteSize.W / 2) + 0, -power_slider_whiteSize.H + 16), power_slider_whiteSize);
		}

		Graphics2D.SetDrawColor(255, 255, 255, 255);
		float fontSize = 32;
		Graphics2D.DrawText(new(0, -(fontSize * 0.85f)), $"{Math.Round(HP)}/{MaxHP}", "Noto Sans Bold", fontSize, Anchor.Center);
		if (Fever != null) {
			Graphics2D.SetTexture(Fever);
			Graphics2D.DrawImage(new(300, -38f), new(Fever.Width, Fever.Height));
		}
		if (hp_icon != null) {
			Graphics2D.SetTexture(hp_icon);
			float lastHitTime = Math.Clamp((float)(Time - LastHitTime) * 1, 0, 1);
			float size = (float)NMath.Remap(lastHitTime, 0, 0.3, 1, 1.15, clampInput: true);

			Vector2F sizeOfHeart = new(hp_icon.Width * size, hp_icon.Height * size);
			Graphics2D.DrawImage(new(-328, -38f), sizeOfHeart, sizeOfHeart / 2);
		}
	}
	public virtual void RenderScore() {
		ScoreNumber.Render(Time, StyledTextShader);
		ScoreLabel.Render(Time, StyledTextShader);
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
		ComboNumber.SplitY = 1;
		ComboNumber.StartPosition = new(0, -180);
		ComboNumber.Scale = new(GetSizeForComboHit());
		ComboNumber.Render(Time, StyledTextShader);
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

		ScoreLabel.TopLeftAligned = ScoreNumber.TopLeftAligned = true;
		ScoreLabel.DesaturateAmount = ScoreNumber.DesaturateAmount = 0;
		ScoreLabel.DarkenAmount = ScoreNumber.DarkenAmount = 0.3f;
		ScoreLabel.SplitY = ScoreNumber.SplitY = 0;
		ScoreNumber.BorderSize = 4;
		ComboNumber.BorderSize = 4;

		ScoreNumber.StartPosition = new(100, -16);
		ScoreLabel.StartPosition = new(100, -96);
		ScoreNumber.borderColor = new Color(173, 173, 255) * new Color(200, 255);

		ScoreNumber.StartScale = new(1f);
		ScoreNumber.StartScale = new(0.9f);
		ScoreNumber.DetermineRenderTexture();
		ComboNumber.DetermineRenderTexture();
	}

	public bool ShowingVictoryScreen() => IValidatable.IsValid(CurrentStatisticsPanel);
	public bool ShowingFailureScreen() => false; // todo


	public void StartMultiHitText() {
		InMultiHit = true;
		MultiHits = 0;
	}

	double LastComboUpdateTime = -2000000;

	public void StartWarning() {
		Warning = true;
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
		ComboNumber.Text = $"{Combo}";
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
		ScoreNumber.Text = $"{Math.Round(score)}";
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

		ForceDeactivateCombo1();
		ForceDeactivateCombo2();
	}
}

public class MD1SceneUIGrooveCoaster(IMuseDash1SceneInstance scene, IGame game) : MD1SceneUI(scene, game)
{
	protected TextImageRenderItem ComboLabel = null!;

	public override void SetupComboNumber() {
		base.SetupComboNumber();
		ComboNumber.Font = "Infinity Font";
		ComboLabel = new(0, 0, new(0, 0), 0, new(1, 1), "COMBO", "Snaps Taste", new(165, 254, 254), null);
	}

	protected override void SetComboColors(Color borderColor, Color color) {
		base.SetComboColors(borderColor, color);
		ComboLabel.borderColor = borderColor;
		ComboLabel.StartColor = color;
	}

	public override void SetupScoreNumber() {
		base.SetupScoreNumber();
		ScoreNumber.Font = "Infinity Font";
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
		float dist = 180;
		ComboNumber.SplitY = 1;
		ComboNumber.StartPosition = new(0, -dist);
		ComboNumber.Render(Time, StyledTextShader);

		ComboLabel.SplitY = 1;
		ComboLabel.StartPosition = new(0, dist);
		ComboLabel.Render(Time, StyledTextShader);
	}
}

public class CloneDashMD1SceneUIArknights(IMuseDash1SceneInstance scene, IGame game) : MD1SceneUI(scene, game)
{

}