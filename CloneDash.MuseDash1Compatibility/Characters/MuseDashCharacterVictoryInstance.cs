using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

public class MuseDashCharacterVictoryInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterVictoryInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;

	ModelInstance? model;
	readonly AnimationHandler anims = new();

	public void Initialize(RhythmGameLevel game){
		model = Descriptor.GetVictoryModel(EngineCore.Level).Instantiate();
		float scale = Descriptor.GetVictoryModelScale() * 100;
		model.Scale = new(scale, scale);
		anims.SetModel(model);
		anims.SetAnimation(0, "in");
		anims.AddAnimation(0, GetStandby(model), true);
	}

	// Bloodheir uses "Standby"
	private string GetStandby(ModelInstance model) {
		var standby = Descriptor.GetVictoryStandby();
		if (model.Data.FindAnimation(standby) != null)
			return standby;

		foreach (var anim in model.Data.Animations)
			if (string.Equals(anim.Name, standby, StringComparison.OrdinalIgnoreCase))
				return anim.Name;

		return standby;
	}

	public void PlayAudio() {
		int role = MuseDash1Compatibility.Characters.IndexOf(Descriptor.ConfigData);
		if (MuseDash1VoiceSetting.GetRandomVoice(MuseDash1VoiceSetting.VICTORY, role) is not string name)
			return;

		var voice = MuseDash1Compatibility.LoadSoundFromName(EngineCore.Level, name);
		if (voice == null)
			return;
		voice.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		audiosystem.PlaySound(voice, AudioPlaybackSettings.Unaltered);
	}

	public void Think() {
		if (model != null) {
			anims?.AddDeltaTime(globals.CurTimeDelta);
			anims?.Apply(model);
		}
	}

	public void Render() {
		model?.Render();
	}
}
