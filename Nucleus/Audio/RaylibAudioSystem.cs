using Nucleus.Common.Audio;
using Raylib_cs;

namespace Nucleus.Audio;

public class RaylibAudioSystem : IAudioSystem
{
	public bool AttachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn, object? userdata = null) {
		throw new NotImplementedException();
	}

	public IAudioClip? CreateDynamicAudioClip(AudioCallbackFn fn, ReadOnlySpan<char> identifier = default) {
		throw new NotImplementedException();
	}

	public IAudioClip? CreateFileAudioClip(ReadOnlySpan<char> name) => CreateFileAudioClip(name, "audio");
	public IAudioClip? CreateFileAudioClip(ReadOnlySpan<char> name, ReadOnlySpan<char> pathId) {
		throw new NotImplementedException();
	}

	public AudioPlaybackHandle CreatePlayback(IAudioClip? clip, in AudioPlaybackSettings init = default) {
		throw new NotImplementedException();
	}

	public IAudioClip? CreateStreamAudioClip(Stream stream, ReadOnlySpan<char> identifier = default) {
		throw new NotImplementedException();
	}

	public long DestroyAllAudioClips() {
		throw new NotImplementedException();
	}

	public long DestroyAllUnusedAudioClips() {
		throw new NotImplementedException();
	}

	public bool DestroyAudioClip(IAudioClip? clip) {
		throw new NotImplementedException();
	}

	public void DestroyPlayback(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public bool DetachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn) {
		throw new NotImplementedException();
	}

	public long GetActiveChannelCount() {
		throw new NotImplementedException();
	}

	public IAudioClip? GetAudioClip(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public long GetAudioClipCount() {
		throw new NotImplementedException();
	}

	public float GetMasterVolume() {
		throw new NotImplementedException();
	}

	public ulong GetMemoryAllocated() {
		throw new NotImplementedException();
	}

	public long GetPlaybackCount(IAudioClip? clip) {
		throw new NotImplementedException();
	}

	public double GetPlaybackDuration(in AudioPlaybackHandle music) {
		throw new NotImplementedException();
	}

	public ulong GetPlaybackGeneration() {
		throw new NotImplementedException();
	}

	public ref readonly AudioPlaybackSettings GetPlaybackSettings(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public float GetSoundPanning(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public float GetSoundPitchScaling(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public bool GetSoundPlayhead(in AudioPlaybackHandle handle, out double playhead) {
		throw new NotImplementedException();
	}

	public float GetSoundTimeStretch(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public float GetSoundVolume(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public AudioFeatures GetSupportedFeatures() {
		throw new NotImplementedException();
	}

	public void Initialize() {
		Raylib.InitAudioDevice();
	}

	public bool IsPlaybackActive(in AudioPlaybackHandle music) {
		throw new NotImplementedException();
	}

	public bool IsPlaybackComplete(in AudioPlaybackHandle music) {
		throw new NotImplementedException();
	}

	public bool IsPlaybackHandleValid(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public bool IsPlaybackPaused(in AudioPlaybackHandle music) {
		throw new NotImplementedException();
	}

	public bool PauseSound(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public bool PlaySound(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public void RestartSound(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public bool ResumeSound(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public void SetMasterVolume(float volume) {
		Raylib.SetMasterVolume(volume);
	}

	public bool SetSoundPanning(in AudioPlaybackHandle handle, float panning) {
		throw new NotImplementedException();
	}

	public bool SetSoundPitchControl(in AudioPlaybackHandle handle, float pitch) {
		throw new NotImplementedException();
	}

	public bool SetSoundPitchScaling(in AudioPlaybackHandle handle, float pitchRatio) {
		throw new NotImplementedException();
	}

	public bool SetSoundPlayhead(in AudioPlaybackHandle handle, double playhead) {
		throw new NotImplementedException();
	}

	public bool SetSoundTimeStretch(in AudioPlaybackHandle handle, float stretchRatio) {
		throw new NotImplementedException();
	}

	public bool SetSoundVolume(in AudioPlaybackHandle handle, float volume) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		Raylib.CloseAudioDevice();
	}

	public long StopAllSounds() {
		throw new NotImplementedException();
	}

	public bool StopSound(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}

	public long StopSounds(IAudioClip? clip) {
		throw new NotImplementedException();
	}

	public void Update() {
		throw new NotImplementedException();
	}

	public void UpdatePlayback(in AudioPlaybackHandle handle) {
		throw new NotImplementedException();
	}
}
