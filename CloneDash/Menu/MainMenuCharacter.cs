using CloneDash.Characters;
using CloneDash.Common;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;

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

	public MainMenuCharacter(Element? parent) : base(parent){
		SetBorderSize(0);
		SetPaintBackgroundEnabled(false);
		ExpressionLabel = new Label(this);
		ExpressionLabel.SetVisible(false);
		ExpressionLabel.SetOrigin(Anchor.Center);
		ExpressionLabel.TextOverflowMode = TextOverflowMode.WordWrap;

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
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = ((GetGlobalPosition()) + new Vector2F(
				width / 2,
				(height / 1) - 64)
			).ToNumerics()
		});

		CharacterInstance?.Render(CharacterOffset);

		EngineCore.Window.EndMode2D();

		if (CharacterInstance != null) {
			CharacterInstance.GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime);
			if (NMath.InRange(Level.Curtime, startTime, endTime) && !string.IsNullOrEmpty(ExpressionText)) {
				float alphaMult1 = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.1, 0, 1, true);
				float alphaMult1_2 = (float)NMath.Remap(Level.Curtime, startTime, startTime + 0.4, 0, 1, true);
				float alphaMult2 = (float)NMath.Remap(Level.Curtime, endTime - 0.2, endTime, 0, 1, true);
				float alphaMult = NMath.Ease.InCirc(alphaMult1) - NMath.Ease.OutQuad(alphaMult2);
				float fontSize = Math.Clamp(24 * (height / 900f), 12, 120);
				Vector2F textSize = Graphics2D.GetTextSize(ExpressionText, Graphics2D.UI_FONT_NAME, fontSize);
				Vector2F textPos = new Vector2F(width / 2, height * 0.75f) + new Vector2F(0, (float)NMath.Ease.OutBack(alphaMult1_2) * (height * -.05f));
				textSize += new Vector2F(16);

				ExpressionLabel.SetPos(textPos);
				ExpressionLabel.SetSize(textSize);
				ExpressionLabel.SetVisible(true);
				ExpressionLabel.SetPaintBackgroundEnabled(true);
				ExpressionLabel.SetBgColor(new Color(10, 20, 25, (int)(alphaMult * 200)));
				ExpressionLabel.SetTextColor(new(255, 255, 255, (int)(alphaMult * 255)));
				ExpressionLabel.SetAutoSize(true);
				ExpressionLabel.SetSize(new(Math.Clamp(textSize.X + 32, 0, width), textSize.Y));
				ExpressionLabel.SetText(ExpressionText);
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
		CharacterInstance = charDescriptor?.CreateMainMenu();
		if (CharacterInstance == null)
			return;

		ResetExpression();
		CharacterInstance.PlayAudio();
	}
}
