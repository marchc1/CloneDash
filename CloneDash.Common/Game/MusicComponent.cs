using Nucleus.Common.Audio;

namespace CloneDash.Common.Game;

public class MusicComponent
{
	public AudioPlaybackHandle Handle = AudioPlaybackHandle.Null;

	double Speed = 1;

	public double Duration => audiosystem.GetPlaybackDuration(in Handle);

	public bool IsComplete => audiosystem.IsPlaybackComplete(in Handle);

	public bool IsPaused => audiosystem.IsPlaybackPaused(in Handle);

	public double GetPlayhead() {
		audiosystem.GetSoundPlayhead(in Handle, out double playhead);
		return playhead;
	}

	public double GetSpeed() {
		return Speed;
	}

	public bool IsValid() => Handle.IsValid();

	public void Load(IAudioClip? clip) {
		Handle = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
			Looping = false,
			ManuallyUpdate = true,
			DoNotAutoDestroy = true,
			Stream = true
		});

		audiosystem.PlaySound(Handle);
	}

	public void Pause() => audiosystem.PauseSound(in Handle);

	public void Resume() => audiosystem.ResumeSound(in Handle);

	public void SeekTo(double time) {
		if (time < 0.06f)
			audiosystem.RestartSound(Handle);
		else
			audiosystem.SetSoundPlayhead(Handle, time);
	}

	public void SetSpeed(double speed) {
		Speed = speed;
		audiosystem.SetSoundPitchControl(Handle, (float)speed);
	}

	public void SetVolume(float volume) => audiosystem.SetSoundVolume(in Handle, volume);

	public void Update() => audiosystem.UpdatePlayback(in Handle);
}
