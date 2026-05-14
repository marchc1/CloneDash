using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using CloneDash.Game.Statistics;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
using Raylib_cs;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Velopack.Sources;

namespace CloneDash.Scenes;


class StatisticsPanel(IGame game, StatisticsData stats) : Panel()
{
	ICharacterVictoryInstance victory = null!;
	ISongChart? chart;
	double start = 0;
	double Time() => globals.CurTime - start;

	protected override void Initialize() {
		chart = game.GetSongChart();
		if (chart == null) return;
		start = globals.CurTime;

		ICharacterDescriptor? character = CharacterMod.GetCharacterData();
		if (character == null) return;

		victory = character.CreateVictory();
		victory.Initialize(game);
		victory.PlayAudio();
		stats.Compute();

		var bottom = Add<Panel>();
		bottom.DrawPanelBackground = false;

		bottom.DynamicallySized = true;
		bottom.Size = new(0.07f);
		bottom.Dock = Dock.Bottom;

		var restart = bottom.Add<Nucleus.UI.Button>();
		restart.DynamicallySized = true;
		restart.Size = new(.2f);
		restart.Text = "Restart";
		restart.Dock = Dock.Left;
		restart.MouseReleaseEvent += (_, _, _) => {
			// TODO: Probably should just hard restart it...
			// Maybe seeking is stable enough now to justify this though?
			game.Restart();
			this.Remove();
		};

		var back = bottom.Add<Nucleus.UI.Button>();
		back.DynamicallySized = true;
		back.Size = new(.2f);
		back.Text = "Main Menu";
		back.Dock = Dock.Right;
		back.MouseReleaseEvent += (_, _, _) => LevelTransitions.LoadMainMenu();

		BorderSize = 0;
	}
	void RenderOneLine(ReadOnlySpan<char> line, int fs, ref int y) {
		Graphics2D.DrawText(16, 16 + y, line, Graphics2D.UI_FONT_NAME, fs);
		y += fs + 4;
	}
	public override void Paint(float width, float height) {
		BackgroundColor = new(0, 0, 0, (int)(220 * (float)NMath.Ease.OutQuad(NMath.Remap(Time(), 0, 0.5, 0, 1, true))));
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
	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		if (victory != null) {
			victory.Think();
		}
	}
}

public static class UITextAnimationFns
{
	public static void UpwardFadeout(WorldspaceRenderItem self, double curtime) {
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
	public static void FadeoutInv(WorldspaceRenderItem self, double curtime) {
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
	public static void UpwardFadeoutInv(WorldspaceRenderItem self, double curtime) {
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
	public static void UpwardFadeoutMoveLeft(WorldspaceRenderItem self, double curtime) {
		UpwardFadeout(self, curtime);

		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, len * 0.6, len, 0, 1, clampInput: true);
		self.Position.X = (float)(NMath.Ease.InQuart(moveT) * -4.5);
	}
	public static void VibrateAndMoveLeft(WorldspaceRenderItem self, double curtime) {
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

public delegate void RenderTextAnimationFn(WorldspaceRenderItem self, double curtime);

public class WorldspaceRenderItem
{
	public double StartTime;
	public double Length;

	public Vector2F StartPosition;
	public float StartRotation;
	public Vector2F StartScale = new(1, 1);
	public Color StartColor = new(255, 255, 255, 255);

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale = new(1, 1);
	public Color Color = new(255, 255, 255, 255);

	public string? Text;
	public string? Font;
	public ITexture? Texture;
	public RenderTextAnimationFn? Fn;

	ulong lastTextHash;
	ulong lastFontHash;

	public WorldspaceRenderItem(double start, double length, Vector2F pos, float rotation, Vector2F scale, string text, string font, Color color, RenderTextAnimationFn? fn = null) {
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

	public WorldspaceRenderItem(double start, double length, Vector2F pos, float rotation, Vector2F scale, ITexture? texture, Color color, RenderTextAnimationFn? fn = null) {
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
	static float FontResolution => 90;
	static float BorderSize => 2.6f;
	static float DarkenAmount => 0.3f;
	static float DesaturateAmount => 0.35f;
	static float SplitY => 0.55f;
	static int RTPadding => (int)MathF.Ceiling(BorderSize) + 2;
	public void PreRender() {
		DetermineRenderTexture();
	}
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
			RenderStyledText(position, scale, rotation, color, styledTextShader);
		else if (Texture != null) {
			Rlgl.PushMatrix();
			Rlgl.Translatef(position.x, -position.y, 0);
			Rlgl.Rotatef(rotation, 0, 0, 1);
			Rlgl.Scalef(scale.x, scale.y, 1);
			Graphics2D.SetDrawColor(color);
			Graphics2D.SetTexture(Texture);
			Graphics2D.DrawTexture(new(Texture.Width / -FontResolution / 2, Texture.Height / -FontResolution / 2), new(Texture.Width / FontResolution, Texture.Height / FontResolution));
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
		if (
			(textRT.HasValue && textRT.Value.Texture.Width != rtSize.W && textRT.Value.Texture.Height != rtSize.H)
			|| (textHash != lastTextHash || fontHash != lastFontHash)
		) {
			if (textRT.HasValue)
				Graphics2D.DestroyRenderTarget(textRT.Value);
			textRT = null;
		}

		if (!textRT.HasValue) {
			textRT = Graphics2D.CreateRenderTarget(rtSize.W, rtSize.H);
			RenderTexture2D rt = textRT.Value;

			int pad = RTPadding;
			Graphics2D.BeginRenderTarget(rt);
			Rlgl.ClearColor(55, 0, 0, 255);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(pad, pad, Text!, Font!, FontResolution);
			Rlgl.DrawRenderBatchActive();
			Graphics2D.EndRenderTarget();

			lastTextHash = textHash;
			lastFontHash = fontHash;
		}

		return textRT.Value;
	}
	void RenderStyledText(Vector2F position, Vector2F scale, float rotation, Color color, IShader? shader) {
		float alpha = color.A / 255f;
		var rtN = DetermineRenderTexture();
		if (!rtN.HasValue) return;

		var rt = rtN.Value;
		var rtSize = GetTextSize();
		var rtW = rtSize.W;
		var rtH = rtSize.H;

		Rlgl.PushMatrix();
		Rlgl.Translatef(position.x, -position.y, 0);
		Rlgl.Rotatef(rotation, 0, 0, 1);
		Rlgl.Scalef(scale.x / FontResolution, scale.y / FontResolution, 1);

		if (shader != null) {
			shader.SetUniform("uTexelSize", new System.Numerics.Vector2(1.0f / rtW, 1.0f / rtH));
			shader.SetUniform("uTextColor", new System.Numerics.Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
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

		float drawX = -rtW / 2f;
		float drawY = -rtH / 2f;
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

public class CloneDashMD1SceneUI(IMuseDash1SceneInstance scene) : IMuseDash1SceneUI
{
	StatisticsPanel? CurrentStatisticsPanel;

	readonly List<WorldspaceRenderItem> BackgroundItems = [];
	readonly List<WorldspaceRenderItem> ForegroundItems = [];

	IShader? StyledTextShader;
	IShader? UIAlphatestShader;
	ITexture? GoldGreat;
	ITexture? GoldPerfect;
	ITexture? ScoreGreat;
	ITexture? ScorePerfect;
	ITexture? ScorePass;

	ITexture? MultiHitTip;
	ITexture? MultiHitTipDialog;
	ITexture? HitsBase;
	ITexture? BelowBase;
	ITexture? hp_icon;
	ITexture? hp_icon_mistake;
	ITexture? hp_slider;
	ITexture? hp_slider_base;
	ITexture? HpFeverBase;
	ITexture? slider_light;
	ITexture? power_slider;
	ITexture? power_slider_white;
	ITexture? Fever;
	ITexture? bubble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void CleanupList(List<WorldspaceRenderItem> list, double curtime) {
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

	double Time => scene.GetGame().GetConductor().GetTime();
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

	public static float MinorTextScale => 0.65f;
	public static float TextScale => 0.85f;

	public void SetSeeking(bool seeking) => Seeking = seeking;
	public virtual void Initialize() {
		StyledTextShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "styled_text.fs");
		UIAlphatestShader = EngineCore.Level.Shaders.LoadFragmentShaderFromFile("shaders", "ui_alphatest.fs");

		GoldGreat = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("GoldGreat")!);
		GoldPerfect = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("GoldPerfect")!);
		ScoreGreat = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("ScoreGreat")!);
		ScorePerfect = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("ScorePerfect")!);
		ScorePass = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("ScorePass")!);

		MultiHitTip = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("MultiHitTip")!);
		MultiHitTipDialog = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("MultiHitTipDialog")!);
		HitsBase = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("HitsBase")!);
		BelowBase = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("BelowBase")!);
		hp_icon = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("hp_icon")!);
		hp_icon_mistake = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("hp_icon_mistake")!);
		hp_slider = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("hp_slider")!);
		hp_slider_base = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("hp_slider_base")!);
		HpFeverBase = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("HpFeverBase")!);
		slider_light = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("slider_light")!);
		power_slider = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("power_slider")!);
		power_slider_white = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("power_slider_white")!);
		Fever = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("Fever")!);
		bubble = MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>("bubble")!);
	}
	public void CreateGreatHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		Color color = inFever ? new(255, 108, 0) : new(146, 55, 255);
		var text = new WorldspaceRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(TextScale), inFever ? GoldGreat : ScoreGreat, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : UITextAnimationFns.UpwardFadeout);
		BackgroundItems.Add(text);
	}

	public void CreateHealthText(float healthGiven, PathwaySide pathway) {
		var text = new WorldspaceRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(MinorTextScale), $"{Math.Round(healthGiven)}", "Snaps Taste", new(107, 226, 0), pathway == PathwaySide.Top ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		ForegroundItems.Add(text);
	}

	public void CreatePassText(double precision, PathwaySide pathway) {
		var pos = scene.GetPathwayPosition(pathway);
		pos.X -= 0.8f;
		var text = new WorldspaceRenderItem(Time, 0.5, pos, 0, new(TextScale), ScorePass, Color.White, UITextAnimationFns.UpwardFadeoutMoveLeft);
		BackgroundItems.Add(text);
	}

	public void CreatePerfectHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		var text = new WorldspaceRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(TextScale), inFever ? GoldPerfect : ScorePerfect, Color.White, inFever ? UITextAnimationFns.VibrateAndMoveLeft : UITextAnimationFns.UpwardFadeout);
		BackgroundItems.Add(text);
	}

	public void CreateScoreText(int scoreGiven, PathwaySide pathway) {
		var text = new WorldspaceRenderItem(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(MinorTextScale), $"{scoreGiven}", "Snaps Taste", new(0, 191, 255), pathway == PathwaySide.Top ? UITextAnimationFns.FadeoutInv : UITextAnimationFns.UpwardFadeoutInv);
		ForegroundItems.Add(text);
	}

	public void EndMultiHitText() {
		InMultiHit = false;
	}

	public void EndWarning() {
		Warning = false;
	}

	public void OpenVictory(StatisticsData stats) {

	}

	public void CloseVictory() {
		CurrentStatisticsPanel?.Remove();
		CurrentStatisticsPanel = null;
	}

	public void UpdateHit(){
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
		DrawHealthBar(width, height);
	}

	Vector2F GetTextureSize(ITexture? tex) => tex == null ? default : new(tex.Width, tex.Height);
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

		if (stencilMask != null) { Stencils.End(); }
	}
	private void DrawHealthBar(float w, float h) {
		if (BelowBase == null) return;
		if (HpFeverBase == null) return;
		if (hp_slider == null) return;
		if (power_slider == null) return;
		if (power_slider_white == null) return;

		UIAlphatestShader?.Activate();

		Rlgl.PushMatrix();
		float resize = (h / 1080f);

		Rlgl.Translatef(w / 2, h, 0);
		Rlgl.Scalef(resize, resize, 1);

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

		float feverActivated = Math.Clamp((float)(Time - EnterFeverTime) * 1, 0, 1);
		if (feverActivated < 1) {
			Graphics2D.SetDrawColor(255, 255, 255, (int)((float)NMath.Ease.InCirc(1 - feverActivated) * 255));
			Graphics2D.SetTexture(power_slider_white);
			Graphics2D.DrawImage(new(-(power_slider_whiteSize.W / 2) + 0, -power_slider_whiteSize.H + 16), power_slider_whiteSize);
		}

		Graphics2D.SetDrawColor(255, 255, 255, 255);
		float fontSize = 32;
		Graphics2D.DrawText(new(0, -(fontSize * 0.85f)), $"{HP}/{MaxHP}", "Noto Sans Bold", fontSize, Anchor.Center);
		if (Fever != null) {
			Graphics2D.SetTexture(Fever);
			Graphics2D.DrawImage(new(300, -38f), new(Fever.Width, Fever.Height));
		}
		if (hp_icon != null) {
			Graphics2D.SetTexture(hp_icon);
			float lastHitTime = Math.Clamp((float)(Time - LastHitTime) * 1, 0, 1);
			float size = (float)NMath.Remap(lastHitTime, 0, 0.3, 1, 1.15, clampInput: true);

			Graphics2D.DrawImage(new(-360, -64f), new(hp_icon.Width * size, hp_icon.Height * size));
		}
		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
		UIAlphatestShader?.Deactivate();
	}

	public void Think(double dt) {
		CleanupTextLists(Time);
		foreach (var text in BackgroundItems) text.DetermineRenderTexture();
		foreach (var text in ForegroundItems) text.DetermineRenderTexture();
	}

	public bool ShowingVictoryScreen() => IValidatable.IsValid(CurrentStatisticsPanel);


	public void StartMultiHitText() {
		InMultiHit = true;
		MultiHits = 0;
	}

	public void StartWarning() {
		Warning = true;
	}

	public void UpdateAllPerfect(bool allPerfect) {
		AllPerfect = allPerfect;
	}

	public void UpdateCombo(int currentCombo) {
		Combo = currentCombo;
	}

	public void UpdateFeverProgress(double fever, double maxFever) {
		InFever = false;
		CurrentFever = fever;
		MaxFever = maxFever;
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
	}

	public void Reset() {
		BackgroundItems.Clear();
		ForegroundItems.Clear();
		EnterFeverTime = -2000000;
		LastHitTime = -2000000;
	}
}
