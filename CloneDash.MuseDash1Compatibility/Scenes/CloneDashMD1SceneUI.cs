using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game.Statistics;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Text.RegularExpressions;

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
	public static void UpwardFadeout(WorldspaceRenderText self, double curtime) {
		double t = curtime - self.StartTime;
		double len = self.Length;

		double moveT = NMath.Remap(t, 0, len * 0.8, 0, 1, clampInput: true);
		double alphaT = NMath.Remap(t, len * 0.75, len, 0, 1, clampInput: true);
		double scaleT = NMath.Remap(t, 0, len * 0.9, 0, 1, clampInput: true);

		float y = (float)(NMath.Ease.OutCirc(moveT) * 4);
		float scaleX = (float)NMath.Ease.OutElastic(scaleT);
		float scaleY = (float)NMath.Ease.OutElastic(scaleT * 1.4);

		self.Position = new(0, y);
		self.Color.A = (byte)(255 * (1 - alphaT));
		self.Scale = new(scaleX, scaleY);
	}
}

public delegate void RenderTextAnimationFn(WorldspaceRenderText self, double curtime);

public class WorldspaceRenderText
{
	public double StartTime;
	public double Length;

	public WorldspaceRenderText(double start, double length, Vector2F pos, float rotation, Vector2F scale, string text, string font, Color color, RenderTextAnimationFn? fn = null) {
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

	public bool IsOver(double curtime) => curtime >= (StartTime + Length);

	public Vector2F StartPosition;
	public float StartRotation;
	public Vector2F StartScale = new(1, 1);
	public Color StartColor = new(255, 255, 255, 255);

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale = new(1, 1);
	public Color Color = new(255, 255, 255, 255);

	public float Border = 2;
	public Color BorderColor = new(0, 0, 0, 255);

	public string? Text;
	public string? Font;
	public RenderTextAnimationFn? Fn;

	public static float FontResolution => 90;

	public void Render(double curtime) {
		if (Font == null)
			return;

		if (Text == null)
			return;

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

		Rlgl.PushMatrix();
		Rlgl.Translatef(position.x, -position.y, 0);
		Rlgl.Rotatef(rotation, 0, 0, 1);
		Rlgl.Scalef(scale.x / FontResolution, scale.y / FontResolution, 1);
		{
			if (Border != 0) {
				Graphics2D.SetDrawColor(BorderColor * Color);
				for (float ox = -Border; ox <= Border; ox += Border)
					for (float oy = -Border; oy <= Border; oy += Border)
						if (ox != 0 || oy != 0)
							Graphics2D.DrawText(new Vector2F(ox, oy) / FontResolution, Text, Font, FontResolution, Anchor.Center);
			}
			Graphics2D.SetDrawColor(color);
			Graphics2D.DrawText(new(0, 0), Text, Font, FontResolution, Anchor.Center);
		}
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

	readonly List<WorldspaceRenderText> BackgroundText = [];
	readonly List<WorldspaceRenderText> ForegroundText = [];

	void CleanupTextLists(double curtime) {
		for (int i = BackgroundText.Count - 1; i >= 0; i--)
			if (BackgroundText[i].IsOver(curtime))
				BackgroundText.RemoveAt(i);
		for (int i = ForegroundText.Count - 1; i >= 0; i--)
			if (ForegroundText[i].IsOver(curtime))
				ForegroundText.RemoveAt(i);
	}

	double Time;
	bool AllPerfect, FullCombo;
	int Combo;
	double CurrentFever, MaxFever;
	double HP, MaxHP;
	bool InFever;
	double FeverRemainingTime, FeverTotalTime;
	int MultiHits = 0;
	bool InMultiHit;
	double Score = 0;
	bool Warning;
	bool Seeking;

	public static float TextScale => 0.4f;

	public void SetSeeking(bool seeking) => Seeking = seeking;
	public virtual void Initialize() {

	}
	public void CreateGreatHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {

	}

	public void CreateHealthText(float healthGiven) {

	}

	public void CreatePassText(double precision, PathwaySide pathway) {

	}

	public void CreatePerfectHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		var text = new WorldspaceRenderText(Time, 0.5, scene.GetPathwayPosition(pathway), 0, new(TextScale), $"PERFECT {Math.Round(precision, 1)}ms", "Luckiest Guy", new(255, 255, 255), UITextAnimationFns.UpwardFadeout);
		BackgroundText.Add(text);
	}

	public void CreateScoreText(int scoreGiven) {

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

	public void PreRenderWorldspace() {
		foreach (var text in BackgroundText)
			text.Render(Time);
	}

	public void PostRenderWorldspace() {
		foreach (var text in ForegroundText)
			text.Render(Time);
	}

	public void RenderUI() {

	}

	public void Think(double dt) {
		Time = scene.GetGame().GetConductor().GetTime();
		CleanupTextLists(Time);
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
		BackgroundText.Clear();
		ForegroundText.Clear();
	}
}
