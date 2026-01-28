using CloneDash.Characters;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Menu;

/// <summary>
/// Renders a character.
/// </summary>
public class CharacterPanel : Panel
{
	private ICharacterDescriptor? Character;
	private ModelInstance? Model;
	private AnimationHandler? Anims;
	private MusicTrack? Music;
	private ICharacterExpression? TouchResponse;
	private int Click = 0;
	private double StartExpressionTime;
	private double NextExpressionTime;
	private string? ExpressionText;

	public Vector2F CharacterOffset { get; set; }

	public bool PlaysMusic {
		get => field;
		set {
			if (field == value) return;
			field = value;
			if (value)
				Music?.Playing = false;
			else {
				Music?.Playing = true;
				Music?.Restart();
			}
		}
	} = true;

	public bool ExpressiveOnClicks {
		get => field;
		set {
			if (field == value) return;
			field = value;
		}
	} = true;

	public bool LinkToConVar {
		get => field;
		set {
			if (field == value) return;
			field = value;

			if (LinkToConVar) {
				if (SetCharacter(CharacterMod.GetCharacterData()))
					CharacterMod.CharacterUpdated += CharacterMod_CharacterUpdated;
				else
					CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
			}
			else
				CharacterMod.CharacterUpdated -= CharacterMod_CharacterUpdated;
		}
	} = false;

	public bool SetCharacter(ICharacterDescriptor? character) {
		if (character == Character)
			return character != null;

		if (character != null)
			CharacterMod_CharacterUpdated(character);

		return character != null;
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		Music?.Update();
	}

	protected override void Initialize() {
		base.Initialize();
		BorderSize = 0;
		DrawPanelBackground = false;
	}

	public override void OnRemoval() {
		base.OnRemoval();
		LinkToConVar = false; // Force removal from list
	}

	public override void MouseClick(FrameState state, MouseButton button) {
		if (Character == null) return;
		if (Model == null) return;
		if (Anims == null) return;
		if (Level.Curtime < NextExpressionTime) return;

		TouchResponse = Character.GetMainShowExpression();
		Click++;

		var mainResponse = Character.GetMainShowInitialExpression();
		if (mainResponse != null) {
			Anims.SetAnimation(0, mainResponse);
			var standby = Character.GetMainShowStandby();
			if (Model.Data.FindAnimation(standby) == null) standby = "standby";
			Anims.AddAnimation(0, standby, true);
		}

		string? text = null;
		double duration = 0;
		TouchResponse?.Run(Level, Model, Anims, out text, out duration);
		StartExpressionTime = Level.Curtime;
		NextExpressionTime = Level.Curtime + duration + 0.1;
		ExpressionText = text;
	}

	public override void Paint(float width, float height) {
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = ((GetGlobalPosition()) + new Vector2F(width / 2, (height / 1) - 64)).ToNumerics()
		});

		if (Model != null) {
			Model.Position = CharacterOffset;

			Anims?.AddDeltaTime(Level.RendertimeDelta);
			Anims?.Apply(Model);

			Model.Render();
		}

		EngineCore.Window.EndMode2D();

		if (NMath.InRange(Level.Curtime, StartExpressionTime, NextExpressionTime) && ExpressionText != null) {
			float alphaMult1 = (float)NMath.Remap(Level.Curtime, StartExpressionTime, StartExpressionTime + 0.1, 0, 1, true);
			float alphaMult1_2 = (float)NMath.Remap(Level.Curtime, StartExpressionTime, StartExpressionTime + 0.4, 0, 1, true);
			float alphaMult2 = (float)NMath.Remap(Level.Curtime, NextExpressionTime - 0.2, NextExpressionTime, 0, 1, true);
			float alphaMult = NMath.Ease.InCirc(alphaMult1) - NMath.Ease.OutQuad(alphaMult2);
			float fontSize = Math.Clamp(24 * (height / 900f), 12, 120);
			Vector2F textSize = Graphics2D.GetTextSize(ExpressionText, Graphics2D.UI_FONT_NAME, fontSize);
			Vector2F textPos = new Vector2F(width / 2 - width * .2f, height * 0.9f) + new Vector2F(0, (float)NMath.Ease.OutBack(alphaMult1_2) * (height * -.05f));
			Graphics2D.SetDrawColor(10, 20, 25, (int)(alphaMult * 200));
			textSize += new Vector2F(16);
			Graphics2D.DrawRectangle(textPos - textSize / 2, textSize);
			Graphics2D.SetDrawColor(255, 255, 255, (int)(alphaMult * 255));
			Graphics2D.DrawText(textPos, ExpressionText, Graphics2D.UI_FONT_NAME, fontSize, Anchor.Center);
		}
	}

	public void Reset() {
		Music?.Restart();
		Model?.SetToSetupPose();
		Anims?.ClearAllAnimation();

		if (Model == null) return;
		if (Anims == null) return;
		if (Character == null) return;

		var standby = Character.GetMainShowStandby();
		if (Model.Data.FindAnimation(standby) == null) standby = "standby";
		Anims.AddAnimation(0, standby, true);
	}

	private void CharacterMod_CharacterUpdated(ICharacterDescriptor? charDescriptor) {
		if (charDescriptor == null) return;
		Character = charDescriptor;

		Model = charDescriptor.GetMainShowModel(Level).Instantiate();
		Anims = new(Model.Data);

		var standby = charDescriptor.GetMainShowStandby();
		if (Model.Data.FindAnimation(standby) == null) standby = "standby";
		if (Model.Data.FindAnimation(standby) == null) standby = "Bgmstandby"; // EXCLUSIVELY for miku for whatever reason
		Anims.SetAnimation(0, standby, true);

		Music = charDescriptor.GetMainShowMusic(Level);
		if (Music != null) {
			Music.Playing = true;
			Music.Loops = true;
			Music.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		}
	}
}
