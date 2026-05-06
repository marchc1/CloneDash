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
	public readonly MuseDashCharacterDescriptor Descriptor;
	private ICharacterMainMenuInstance? CharacterInstance;
	private ModelInstance? Model;
	private readonly AnimationHandler Anims = new();
	private AudioPlaybackHandle Music;
	private ICharacterMainMenuExpression? ApplyExpression;
	private double StartExpressionTime;
	private double NextExpressionTime;
	private AudioPlaybackHandle? ExpressionVoiceHandle;

	public MuseDashCharacterMainMenuInstance(MuseDashCharacterDescriptor descriptor) {
		Descriptor = descriptor;
		Model = descriptor.GetMainShowModel(EngineCore.Level).Instantiate();
		Anims.SetModel(Model);
		ApplyExpression = null;
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

	private string? GetStandby(ModelInstance? model) {
		if (model == null) return null;
		var standby = Descriptor.GetMainShowStandby();
		if (model.Data.FindAnimation(standby) == null) standby = "standby";
		if (model.Data.FindAnimation(standby) == null) standby = "Bgmstandby"; // EXCLUSIVELY for miku for whatever reason
		return standby;
	}

	public void Standby() {
		var standby = GetStandby(Model);
		Anims.SetAnimation(0, standby, true);
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
