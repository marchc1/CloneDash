using CloneDash.Characters;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Linq.Expressions;

namespace CloneDash.Menu;

/// <summary>
/// Renders a character.
/// </summary>
public class CharacterPanel : Panel
{
	private ICharacterMainMenuInstance? CharacterInstance;
	private int Click = 0;
	private string? ExpressionText;
	public Vector2F CharacterOffset { get; set; }

	public bool SetCharacter(ICharacterDescriptor? character, bool force = false) {
		if (character != null && CharacterInstance != null && character.GetUUID() == CharacterInstance.GetCharacter().GetUUID() && !force)
			return character != null;

		if (character != null)
			CharacterMod_CharacterUpdated(character);

		return character != null;
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		CharacterInstance?.Update();
	}

	Label ExpressionLabel = null!;

	protected override void Initialize() {
		base.Initialize();
		BorderSize = 0;
		DrawPanelBackground = false;
		ExpressionLabel = Add<Label>();
		ExpressionLabel.Visible = false;
		ExpressionLabel.Origin = Anchor.Center;
		ExpressionLabel.TextOverflowMode = TextOverflowMode.WordWrap;

		if (SetCharacter(CharacterMod.GetCharacterData()))
			CharacterMod.CharacterUpdated += CharacterMod_CharacterUpdated;
		else
			CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
	}

	public override void OnRemoval() {
		base.OnRemoval();
		CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
		CharacterInstance?.Dispose();
	}

	public override void MouseClick(FrameState state, ButtonCode button) {
		PlayRandomExpression();
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
		ExpressionLabel.TextPadding = new(16);
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

				ExpressionLabel.Position = textPos;
				ExpressionLabel.Size = textSize;
				ExpressionLabel.Visible = true;
				ExpressionLabel.DrawBackground = true;
				ExpressionLabel.BackgroundColor = new(10, 20, 25, (int)(alphaMult * 200));
				ExpressionLabel.TextColor = new(255, 255, 255, (int)(alphaMult * 255));
				ExpressionLabel.AutoSize = true;
				ExpressionLabel.Size = new(Math.Clamp(textSize.X + 32, 0, width), textSize.Y);
				ExpressionLabel.Text = ExpressionText;
			}
			else
				ExpressionLabel.Visible = false;
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
		if (CharacterInstance != null && charDescriptor != null && CharacterInstance.GetCharacter().GetUUID() == charDescriptor.GetUUID()) return;
		CharacterInstance = charDescriptor?.CreateMainMenu();
		if (CharacterInstance == null)
			return;

		ResetExpression();
		CharacterInstance.PlayAudio();
	}
}
