using CloneDash.Characters;
using CloneDash.Game;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace CloneDash.Menu;

/// <summary>
/// Renders a character.
/// </summary>
public class CharacterPanel : Panel
{
	private ICharacterDescriptor? Character;
	private ModelInstance? Model;
	private readonly AnimationHandler Anims = new();
	private AudioPlaybackHandle Music;
	private int Click = 0;
	private ICharacterExpression? ApplyExpression;
	private double StartExpressionTime;
	private double NextExpressionTime;
	private string? ExpressionText;


	private ModelInstance? PlayModel;
	private ModelInstance? VictoryModel;
	private ModelInstance? FailModel;
	private readonly AnimationHandler PlayAnims = new();
	private readonly AnimationHandler VictoryAnims = new();
	private readonly AnimationHandler FailAnims = new();

	public Vector2F CharacterOffset { get; set; }

	public bool PlaysMusic {
		get => field;
		set {
			if (field == value) return;
			field = value;
			if (!value)
				audiosystem.StopSound(Music);
			else {
				audiosystem.RestartSound(Music);
				audiosystem.PlaySound(Music);
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
		audiosystem.UpdatePlayback(Music);

		if (extendedModels && Character != null) {
			if (!PlayAnims.IsPlayingAnimation()) {
				// todo: fix Character.PlayCharacterAnimation((CharacterAnimationType)Random.Shared.Next(0, (int)(CharacterAnimationType.JumpHitGreat) + 1), PlayAnims);
			}
		}
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
	}

	public override void OnRemoval() {
		base.OnRemoval();
		LinkToConVar = false; // Force removal from list
		audiosystem.DestroyPlayback(Music);
	}

	public override void MouseClick(FrameState state, ButtonCode button) {
		PlayRandomExpression();
	}
	public void PlayApplyExpression() {
		if (Character == null) return;

		ApplyExpression ??= Character.GetMainShowApplyExpression();
		PlayExpression(ApplyExpression);
	}

	public void PlayRandomExpression() {
		if (Character == null) return;
		PlayExpression(Character.GetMainShowExpression());
	}

	public void PlayExpression(ICharacterExpression? expression) {
		if (Character == null) return;
		if (Model == null) return;
		if (Level.Curtime < NextExpressionTime) return;

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
		expression?.Run(Level, Model, Anims, out text, out duration);
		StartExpressionTime = Level.Curtime;
		NextExpressionTime = Level.Curtime + duration + 0.1;
		ExpressionLabel.TextPadding = new(16);
		ExpressionText = text;
	}

	public override void Paint(float width, float height) {
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = ((GetGlobalPosition()) + new Vector2F(
				NMath.Remap(sos_extendedMoveover.Update(extendedModels ? 1 : 0), 0, 1, width / 2, width / 3.5f), 
				(height / 1) - 64)
			).ToNumerics()
		});

		if (Model != null) {
			Model.Position = CharacterOffset;

			Anims?.AddDeltaTime(Level.RendertimeDelta);
			Anims?.Apply(Model);

			Model.Render();
		}

		EngineCore.Window.EndMode2D();

		if (extendedModels) {
			EngineCore.Window.BeginMode2D(new() {
				Zoom = height / 900 / 1.4f,
				Offset = ((GetGlobalPosition()) + new Vector2F(
					NMath.Remap(sos_extendedMoveover.Update(extendedModels ? 1 : 0), 0, 1, width / 1.5f, (width / 2.2f) + (width / 24)),
					(height / 1) - 64)
				).ToNumerics()
			});

			if (PlayModel != null) {
				PlayModel.Position = CharacterOffset;
				PlayAnims?.AddDeltaTime(Level.RendertimeDelta); PlayAnims?.Apply(PlayModel);
				PlayModel.Render();
			}
			EngineCore.Window.EndMode2D();


			EngineCore.Window.BeginMode2D(new() {
				Zoom = height / 900 / 5.4f,
				Offset = ((GetGlobalPosition()) + new Vector2F(
					NMath.Remap(sos_extendedMoveover.Update(extendedModels ? 1 : 0), 0, 1, width / 1.5f, (width / 1.45f) + (width / 24)),
					(height / 1) - 64)
				).ToNumerics()
			});
			if (VictoryModel != null) {
				VictoryModel.Position = CharacterOffset;
				VictoryAnims?.AddDeltaTime(Level.RendertimeDelta); VictoryAnims?.Apply(VictoryModel);
				VictoryModel.Render();
			}
			EngineCore.Window.EndMode2D();
		}

		if (NMath.InRange(Level.Curtime, StartExpressionTime, NextExpressionTime) && !string.IsNullOrEmpty(ExpressionText)) {
			float alphaMult1 = (float)NMath.Remap(Level.Curtime, StartExpressionTime, StartExpressionTime + 0.1, 0, 1, true);
			float alphaMult1_2 = (float)NMath.Remap(Level.Curtime, StartExpressionTime, StartExpressionTime + 0.4, 0, 1, true);
			float alphaMult2 = (float)NMath.Remap(Level.Curtime, NextExpressionTime - 0.2, NextExpressionTime, 0, 1, true);
			float alphaMult = NMath.Ease.InCirc(alphaMult1) - NMath.Ease.OutQuad(alphaMult2);
			float fontSize = Math.Clamp(24 * (height / 900f), 12, 120);
			Vector2F textSize = Graphics2D.GetTextSize(ExpressionText, Graphics2D.UI_FONT_NAME, fontSize);
			Vector2F textPos = new Vector2F(width / 2, height * 0.9f) + new Vector2F(0, (float)NMath.Ease.OutBack(alphaMult1_2) * (height * -.05f));
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

	public void Reset() {
		audiosystem.RestartSound(Music);
		Model?.SetToSetupPose();
		Anims?.ClearAllAnimation();
		ApplyExpression = null;

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
		Anims.SetModel(Model);
		ApplyExpression = null;

		var standby = charDescriptor.GetMainShowStandby();
		if (Model.Data.FindAnimation(standby) == null) standby = "standby";
		if (Model.Data.FindAnimation(standby) == null) standby = "Bgmstandby"; // EXCLUSIVELY for miku for whatever reason
		Anims.SetAnimation(0, standby, true);

		var clip = charDescriptor.GetMainShowMusic(Level);
		if (IValidatable.IsValid(clip)) {
			clip.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			Music = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
				Looping = true,
				ManuallyUpdate = true,
				Stream = true
			});
			audiosystem.PlaySound(Music);
		}

		if (extendedModels)
			LoadExtendedModels();
	}

	void LoadExtendedModels() {
		if (Character == null) return;

		PlayModel = Character.GetPlayModel(Level).Instantiate();
		PlayAnims.SetModel(PlayModel);

		VictoryModel = Character.GetVictoryModel(Level).Instantiate();
		VictoryAnims.SetModel(VictoryModel);
		VictoryAnims.SetAnimation(0, Character.GetVictoryStandby(), true);
	}

	bool extendedModels = false;
	readonly SecondOrderSystem sos_extendedMoveover = new(1.8f, 0.8f, 1f, 0);
	internal void SetExtendedModels(bool @checked) {
		extendedModels = @checked;
		if (@checked)
			LoadExtendedModels();
	}
}
