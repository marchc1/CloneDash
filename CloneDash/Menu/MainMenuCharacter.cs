using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.UI;
using CloneDash.Game;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace CloneDash.Menu;

/// <summary>
/// Renders a character.
/// </summary>
public class MainMenuCharacter : Panel
{
	private ICharacterMainMenuInstance? CharacterInstance;
	private int Click = 0;
	private string? ExpressionText;
	public Vector2F CharacterOffset { get; set; }

	public bool SetCharacter(ICharacterDescriptor? character, bool force = false) {
		if (character != null && CharacterInstance != null && character.UUIDEquals(CharacterInstance.GetCharacter()) && !force)
			return character != null;

		if (character != null)
			CharacterMod_CharacterUpdated(character);

		return character != null;
	}

	protected override void OnThink() {
		base.OnThink();
		CharacterInstance?.Update();
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
		ExpressionLabel.SetTextPadding(new Vector2F(64, 24));

		if (SetCharacter(CharacterMod.GetCharacterData()))
			CharacterMod.CharacterUpdated += CharacterMod_CharacterUpdated;
		else
			CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
		CharacterInstance?.Dispose();
	}

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

		EngineCore.Window.EndMode2D();

		if (CharacterInstance != null) {
			CharacterInstance.GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime);
			if (NMath.InRange(Level.Curtime, startTime, endTime) && !string.IsNullOrEmpty(ExpressionText)) {
				float alphaTweenIn = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.1, 0, 1, true);
				float alphaTweenOut = (float)NMath.Remap(Level.Curtime, endTime - 0.2, endTime, 0, 1, true);
				float alphaTween = NMath.Ease.InCirc(alphaTweenIn) - NMath.Ease.OutQuad(alphaTweenOut);

				ExpressionLabel.
				Opacity = alphaTween;
				ExpressionLabel.Text = ExpressionText;
				ExpressionLabel.SetVisible(true);

				Color primary = MainMenuLevel.PrimaryColor;
				ExpressionLabel.SetBgColor(MainMenuLevel.BackgroundColor);
				ExpressionLabel.SetFgColor(primary);
				ExpressionLabel.SetTextColor(primary);

				float positionTween = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.4, 0, 1, true);
				Vector2F textPos = new Vector2F(width / 2, height * 0.75f) + new Vector2F(0, (float)NMath.Ease.OutBack(positionTween) * (height * -.05f));
				ExpressionLabel.Position = textPos;

				float fontSize = Math.Clamp(CloneDashUI.GetFontSize(20) * (height / 900f), 12, 120);
				Vector2F textSize = Graphics2D.GetTextSize(ExpressionText, ExpressionLabel.Font, fontSize);
				ExpressionLabel.TextSize = fontSize;
				ExpressionLabel.Size = textSize + ExpressionLabel.GetTextPadding();
			}
			else
				ExpressionLabel.SetVisible(false);
		}
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

	private void CharacterMod_CharacterUpdated(ICharacterDescriptor? charDescriptor) {
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
