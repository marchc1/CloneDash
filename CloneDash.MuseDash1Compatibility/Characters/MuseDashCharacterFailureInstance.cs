using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

public class MuseDashCharacterFailureInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterFailureInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;

	ModelInstance? model;
	readonly AnimationHandler anims = new();

	/// <summary>
	/// Creates the character's failShow model, scaled like the victory model, and loops its standby animation.
	/// </summary>
	public void Initialize(RhythmGameLevel game) {
		model = Descriptor.GetFailModel(EngineCore.Level).Instantiate();
		float scale = Descriptor.GetFailModelScale() * 100;
		model.Scale = new(scale, scale);
		anims.SetModel(model);
		anims.SetAnimation(0, GetStandby(model), true);
	}

	// The fail prefabs' SkeletonGraphic starts on a looping "standby" (there's no "in"); match case like the victory model does
	/// <summary>
	/// Finds the model's standby animation name, matching case-insensitively since some characters use "Standby".
	/// </summary>
	private string GetStandby(ModelInstance model) {
		var standby = Descriptor.GetFailStandby();
		if (model.Data.FindAnimation(standby) != null) {
			return standby;
		}

		foreach (var anim in model.Data.Animations)
			if (string.Equals(anim.Name, standby, StringComparison.OrdinalIgnoreCase)) {
				return anim.Name;
			}

		return standby;
	}

	public void PlayAudio() {
		int role = MuseDash1Compatibility.Characters.IndexOf(Descriptor.ConfigData);
		if (MuseDash1VoiceSetting.GetRandomVoice(MuseDash1VoiceSetting.FAIL, role) is not string name) {
			return;
		}

		var voice = MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, name);
		if (voice == null) {
			return;
		}
		voice.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		audiosystem.PlaySound(voice, AudioPlaybackSettings.Unaltered);
	}

	/// <summary>
	/// Advances the fail model's animation.
	/// </summary>
	public void Think() {
		if (model != null) {
			anims.AddDeltaTime(globals.CurTimeDelta);
			anims.Apply(model);
		}
	}

	public void Render() {
		model?.Render();
	}
}
