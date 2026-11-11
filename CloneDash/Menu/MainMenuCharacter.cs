using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.UI;
using CloneDash.Elfins;
using CloneDash.Game;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Text;

namespace CloneDash.Menu;

/// <summary>
/// Renders a character.
/// </summary>
public class MainMenuCharacter : Panel
{
	private ICharacterMainMenuInstance? CharacterInstance;
	private IElfinMainMenuInstance? ElfinInstance;
	private int Click = 0;
	private string? ExpressionText;
	public Vector2F CharacterOffset { get; set; }
	public Vector2F ElfinOffset { get; set; } = new(-750, -1400);
	public float ExpressionRightLimit { get; set; } = float.MaxValue;

	private string? wrapSource;
	private float wrapWidth;
	private float wrapTextSize;
	private string wrapResult = "";
	private Vector2F wrapResultSize;

	public bool SetCharacter(ICharacterDescriptor? character, bool force = false) {
		if (character != null && CharacterInstance != null && character.UUIDEquals(CharacterInstance.GetCharacter()) && !force)
			return character != null;

		if (character != null)
			CharacterMod_CharacterUpdated(null, character);

		return character != null;
	}

	public void SetElfin(IElfinDescriptor? elfin) {
		if (elfin.UUIDEquals(ElfinInstance?.GetElfin()))
			return;

		ElfinInstance?.Dispose();
		ElfinInstance = elfin?.CreateMainMenu();
	}

	protected override void OnThink() {
		base.OnThink();
		CharacterInstance?.Update();
		ElfinInstance?.Update();
	}

	Label ExpressionLabel = null!;

	public MainMenuCharacter(Element? parent) : base(parent) {
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);

		ExpressionLabel = new Label(this);
		ExpressionLabel.SetVisible(false);
		ExpressionLabel.		Origin = Anchor.Center;
		ExpressionLabel.SetPaintBackgroundEnabled(true);
		ExpressionLabel.BorderSize = 3;
		ExpressionLabel.SetPaintBorderEnabled(true);
		ExpressionLabel.Roundness = 4;
		ExpressionLabel.Clipping = false;
		ExpressionLabel.TextOverflowMode = TextOverflowMode.WordWrap;
		ExpressionLabel.SetTextPadding(new Vector2F(64, 24));

		if (SetCharacter(CharacterMod.Instance.GetActive()))
			CharacterMod.Instance.PostChange += CharacterMod_CharacterUpdated;
		else
			CharacterMod.Instance.PostChange -= CharacterMod_CharacterUpdated;

		SetElfin(ElfinMod.Instance.GetActive());
		ElfinMod.Instance.PostChange += ElfinMod_ElfinUpdated;
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		CharacterMod.Instance.PostChange -= CharacterMod_CharacterUpdated;
		ElfinMod.Instance.PostChange -= ElfinMod_ElfinUpdated;
		CharacterInstance?.Dispose();
		ElfinInstance?.Dispose();
	}

	private void ElfinMod_ElfinUpdated(IElfinDescriptor? previous, IElfinDescriptor? elfin) => SetElfin(elfin);

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		PlayRandomExpression();
		return true;
	}

	public void PlayApplyExpression() {
		if (CharacterInstance == null) return;
		var exp = CharacterInstance.StartApplyExpression();
		PlayExpression(exp);
	}

	public void PlayRandomExpression() {
		if (CharacterInstance == null) return;
		PlayExpression(CharacterInstance.StartExpression());
	}

	public void PlayExpression(ICharacterMainMenuExpression? expression) {
		if (CharacterInstance == null) return;
		if (expression == null) return;

		Click++;

		string? text = null;
		expression?.GetSpeech(EngineCore.Level, out text, out _);
		ExpressionLabel.SetTextPadding(new(16));
		ExpressionText = text;
	}

	public override void Paint(float width, float height) {
		EngineCore.Window.BeginMode2D(new Camera2D {
			Zoom = height / 900 / 2.3f,
			Offset = (GetGlobalPosition() + new Vector2F(
				width / 2,
				height - 32)
			).ToNumerics()
		});

		CharacterInstance?.Render(CharacterOffset);
		ElfinInstance?.Render(CharacterOffset + ElfinOffset);

		EngineCore.Window.EndMode2D();

		if (CharacterInstance != null) {
			CharacterInstance.GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime);
			if (NMath.InRange(Level.Curtime, startTime, endTime) && !string.IsNullOrEmpty(ExpressionText)) {
				float alphaTweenIn = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.1, 0, 1, true);
				float alphaTweenOut = (float)NMath.Remap(Level.Curtime, endTime - 0.2, endTime, 0, 1, true);
				float alphaTween = NMath.Ease.InCirc(alphaTweenIn) - NMath.Ease.OutQuad(alphaTweenOut);

				ExpressionLabel.Opacity = alphaTween;
				ExpressionLabel.SetVisible(true);

				Color primary = MainMenuLevel.PrimaryColor;
				ExpressionLabel.SetBgColor(MainMenuLevel.BackgroundColor);
				ExpressionLabel.SetFgColor(primary);
				ExpressionLabel.SetTextColor(primary);

				float positionTween = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.4, 0, 1, true);
				float fontSize = Math.Clamp(CloneDashUI.GetFontSize(20) * (height / 900f), 12, 120);
				ExpressionLabel.TextSize = fontSize;

				float globalX = GetGlobalPosition().X;
				float margin = 24 * (height / 900f);
				float left = margin - globalX;
				float right = MathF.Min(EngineCore.GetWindowWidth(), ExpressionRightLimit) - margin - globalX;
				Vector2F chrome = ExpressionLabel.GetTextPadding() * 2 + new Vector2F(ExpressionLabel.GetContentInset() * 2);

				WrapExpression(ExpressionText, ExpressionLabel.Font.ToString(),ExpressionLabel.GetRenderTextSize(), MathF.Max(right - left - chrome.X, 1));
				ExpressionLabel.Text = wrapResult;

				Vector2F labelSize = wrapResultSize + chrome;
				labelSize = new Vector2F(MathF.Ceiling(labelSize.X) + 2, MathF.Ceiling(labelSize.Y));
				ExpressionLabel.Size = labelSize;

				float x = MathF.Max(left + labelSize.X / 2, MathF.Min(width / 2, right - labelSize.X / 2));
				Vector2F textPos = new Vector2F(x, height * 0.75f) + new Vector2F(0, (float)NMath.Ease.OutBack(positionTween) * (height * -.05f));
				ExpressionLabel.Position = textPos;
			}
			else
				ExpressionLabel.SetVisible(false);
		}
	}

	private void WrapExpression(string text, string font, float textSize, float maxWidth) {
		if (text == wrapSource && maxWidth == wrapWidth && textSize == wrapTextSize)
			return;

		wrapSource = text;
		wrapWidth = maxWidth;
		wrapTextSize = textSize;

		StringBuilder result = new();
		StringBuilder line = new();
		float widest = 0, totalHeight = 0;

		void pushLine() {
			Vector2F lineSize = Graphics2D.GetTextSize(line.ToString(), font, textSize);
			widest = MathF.Max(widest, lineSize.W);
			totalHeight += lineSize.H;
			if (result.Length > 0)
				result.Append('\n');
			result.Append(line);
			line.Clear();
		}

		foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
			if (line.Length > 0 && Graphics2D.GetTextSize($"{line} {word}", font, textSize).W > maxWidth)
				pushLine();
			if (line.Length > 0)
				line.Append(' ');
			line.Append(word);
		}
		if (line.Length > 0)
			pushLine();

		wrapResult = result.ToString();
		wrapResultSize = new Vector2F(widest, totalHeight);
	}

	public void PlayAudio() => CharacterInstance?.PlayAudio();
	public void StopAudio() => CharacterInstance?.StopAudio();

	public void Reset() => SetCharacter(CharacterInstance?.GetCharacter(), true);

	private void ResetExpression() {
		ExpressionText = null;

		if (CharacterInstance == null)
			return;

		CharacterInstance.Standby();
	}

	private void CharacterMod_CharacterUpdated(ICharacterDescriptor? previous, ICharacterDescriptor? charDescriptor) {
		if (charDescriptor == null) return;
		if (CharacterInstance != null && charDescriptor != null && CharacterInstance.GetCharacter().UUIDEquals(charDescriptor)) return;

		StopAudio();
		CharacterInstance = charDescriptor?.CreateMainMenu();

		if (CharacterInstance == null)
			return;

		ResetExpression();
		CharacterInstance.PlayAudio();
	}
}
