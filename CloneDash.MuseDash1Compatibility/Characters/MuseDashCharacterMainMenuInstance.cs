using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Common.Audio;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;
using System.Linq.Expressions;

namespace CloneDash.Characters;

public class MuseDashCharacterMainMenuInstance : ICharacterMainMenuInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor;
	private ICharacterMainMenuInstance? CharacterInstance;
	private ModelInstance? Model;
	private readonly AnimationHandler Anims = new();
	private AudioPlaybackHandle Music;
	private ICharacterMainMenuExpression? ApplyExpression;
	private double StartExpressionTime;
	private double NextExpressionTime;
	private AudioPlaybackHandle? ExpressionVoiceHandle;

	private int Form;
	private bool ExpressingStandby;

	public MuseDashCharacterMainMenuInstance(MuseDash1CharacterDescriptor descriptor) {
		Descriptor = descriptor;
		Model = descriptor.GetMainShowModel(EngineCore.Level).Instantiate();
		Anims.SetModel(Model);
		ApplyExpression = null;
		Form = descriptor.GetForm();
	}

	public void Dispose() {
		StopAudio();
	}

	public ICharacterDescriptor GetCharacter() => Descriptor;

	public void PlayAudio() {
		if (audiosystem.IsPlaybackHandleValid(Music))
			audiosystem.DestroyPlayback(Music);

		var clip = Descriptor.GetMainShowMusic(EngineCore.Level);
		if (IValidatable.IsValid(clip)) {
			clip.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			Music = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
				Looping = true,
				ManuallyUpdate = true,
				Stream = true,
				DoNotAutoDestroy = true
			});
			audiosystem.PlaySound(Music);
		}
	}
	public void Update(){
		int form = Descriptor.GetForm();
		if (form != Form) {
			Form = form;
			Anims.ClearAnimation(1);
			ExpressingStandby = false;
			NextExpressionTime = 0;
			if (ExpressionVoiceHandle.HasValue) {
				audiosystem.DestroyPlayback(ExpressionVoiceHandle.Value);
				ExpressionVoiceHandle = default;
			}
			Standby();
			if (audiosystem.IsPlaybackHandleValid(Music))
				PlayAudio();
		}

		if (ExpressingStandby && Anims.Channels[1].CurrentEntry == null && Anims.Channels[1].QueuedEntries.Count == 0) {
			ExpressingStandby = false;
			SwapStandby(GetStandby(Model, false));
		}

		audiosystem.UpdatePlayback(Music);
	}
	public void Render(Vector2F offset = default) {
		if (Model != null) {
			Model.Position = offset;

			Anims?.AddDeltaTime(EngineCore.Level.RendertimeDelta);
			Anims?.Apply(Model);

			Model.Render();
		}
	}

	ICharacterMainMenuExpression? curExp;
	void SetExpressionParams(ICharacterMainMenuExpression? expression) {
		if (Model == null) return;

		string? text = null;
		double duration = 0;
		expression?.Run(EngineCore.Level, Model, Anims, out text, out duration, out ExpressionVoiceHandle);

		if (Anims.Channels[1].QueuedEntries.Count > 0) {
			var expressing = GetStandby(Model, true);
			if (expressing != GetStandby(Model, false)) {
				SwapStandby(expressing);
				ExpressingStandby = true;
			}
		}

		StartExpressionTime = globals.CurTime;
		NextExpressionTime = globals.CurTime + duration + 0.1;
		curExp = expression;
	}

	public ICharacterMainMenuExpression? StartApplyExpression() {
		var exp = Descriptor.GetMainShowApplyExpression();
		SetExpressionParams(exp);
		return exp;
	}

	public ICharacterMainMenuExpression? StartExpression() {
		if (globals.CurTime < NextExpressionTime) return null;
		var exp = Descriptor.GetMainShowExpression();
		SetExpressionParams(exp);
		return exp;
	}

	public void StopAudio() {
		audiosystem.DestroyPlayback(Music);
		if (ExpressionVoiceHandle.HasValue)
			audiosystem.DestroyPlayback(ExpressionVoiceHandle.Value);
		Music = default;
		ExpressionVoiceHandle = default;
	}

	private string? GetStandby(ModelInstance? model, bool expressing = false) {
		if (model == null) return null;
		var standby = Descriptor.GetMainShowStandby(expressing);
		if (expressing && model.Data.FindAnimation(standby) == null) standby = Descriptor.GetMainShowStandby();
		if (model.Data.FindAnimation(standby) == null) standby = "standby";
		if (model.Data.FindAnimation(standby) == null) standby = "Bgmstandby"; // EXCLUSIVELY for miku for whatever reason
		return standby;
	}

	public void Standby() {
		var standby = GetStandby(Model);
		Model?.SetToSetupPose();
		Model?.SetConstraintsToSetupPose();
		Anims.SetAnimation(0, standby, true);
	}

	// Keeps the normalized time like Animator.Play(name, 0, normalizedTime) does
	private void SwapStandby(string? standby) {
		var channel = Anims.Channels[0];
		double phase = 0;
		if (channel.CurrentEntry != null && channel.CurrentEntry.Animation.Duration > 0)
			phase = (channel.Time / channel.CurrentEntry.Animation.Duration) % 1;

		var entry = Anims.SetAnimation(0, standby, true);
		if (entry == null) return;

		// SkeletonMecanim autoReset, whatever the old state keyed goes back to setup
		Model?.SetToSetupPose();
		Model?.SetConstraintsToSetupPose();
		channel.QueuedEntries.Clear();
		channel.CurrentEntry = entry;
		channel.ResetTime();
		channel.Time = phase * entry.Animation.Duration;
	}

	public void GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime) {
		if(globals.CurTime > NextExpressionTime) {
			exp = null;
			startTime = 0;
			endTime = 0;
			return;
		}

		exp = curExp;
		startTime = StartExpressionTime;
		endTime = NextExpressionTime;
	}
}
