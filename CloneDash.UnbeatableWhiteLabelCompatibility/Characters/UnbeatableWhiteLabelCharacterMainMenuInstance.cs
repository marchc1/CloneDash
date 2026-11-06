using CloneDash.Characters;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Common.Audio;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelCharacterMainMenuInstance(UnbeatableWhiteLabelCharacterDescriptor descriptor) : ICharacterMainMenuInstance
{
	public const float SCALE = 1000;
	public const string STANDBY = "BeatCombatIdle01";

	const string MUSIC_INTRO = "nosignal_intro";
	const string MUSIC_LOOP = "nosignal_loop";

	readonly UnbeatableWhiteLabelBeatModel model = CreateModel();
	bool looping;
	AudioPlaybackHandle music;

	public void Dispose() => StopAudio();

	public ICharacterDescriptor GetCharacter() => descriptor;

	public void GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime) {
		exp = null;
		startTime = 0;
		endTime = 0;
	}

	public void PlayAudio() {
		StopAudio();
		looping = false;
		music = Play(MUSIC_INTRO, false);
	}

	public void Render(Vector2F offset = default) {
		model.Update(EngineCore.Level.RendertimeDelta);
		model.RenderBeat(offset, SCALE);
	}

	public void Standby() => model.PlayBeat(STANDBY);

	public ICharacterMainMenuExpression? StartApplyExpression() => null;

	public ICharacterMainMenuExpression? StartExpression() => null;

	public void StopAudio() {
		if (audiosystem.IsPlaybackHandleValid(music))
			audiosystem.DestroyPlayback(music);
		music = default;
	}

	public void Update() {
		if (!audiosystem.IsPlaybackHandleValid(music))
			return;

		audiosystem.UpdatePlayback(music);
		if (!looping && audiosystem.IsPlaybackComplete(music)) {
			StopAudio();
			looping = true;
			music = Play(MUSIC_LOOP, true);
		}
	}

	static UnbeatableWhiteLabelBeatModel CreateModel() {
		var model = new UnbeatableWhiteLabelBeatModel();
		model.PlayBeat(STANDBY);
		return model;
	}

	static AudioPlaybackHandle Play(string name, bool loop) {
		var clip = UnbeatableWhiteLabelCompatibility.GetMasterClip(name);
		if (!IValidatable.IsValid(clip))
			return default;

		clip.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		var handle = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
			Looping = loop,
			ManuallyUpdate = true,
			Stream = true,
			DoNotAutoDestroy = true
		});
		audiosystem.PlaySound(handle);
		return handle;
	}
}
