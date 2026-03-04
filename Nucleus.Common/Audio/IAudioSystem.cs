using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucleus.Common.Audio;

public struct AudioPlaybackHandle : IValidatable
{
	public IAudioSystem Audio;
	public ulong Channel;
	public ulong Generation;

	public readonly bool IsValid() {
		if (Audio == null) return false;
		if (Channel == 0 && Generation == 0) return false;
		return Audio.IsPlaybackHandleValid(in this);
	}
}

public enum AudioClipSource : byte
{
	File,
	Stream,
	Dynamic
}

public interface IAudioClip : IValidatable
{
	ReadOnlySpan<char> GetName();
	AudioClipSource GetSource();
}

public struct AudioLoadSettings
{
	public bool Stream;
}


public struct AudioPlaybackSettings
{
	/// <summary> The pitch of the audio. Note that this may be synced to <see cref="TimeStretch"/>, depending on what changed this value. </summary>
	public float Pitch;
	/// <summary> The time stretch of the audio. Note that this may be synced to <see cref="Pitch"/>, depending on what changed this value. </summary>
	public float TimeStretch;
	/// <summary> The volume of the audio, from 0 -> 1. </summary>
	public float Volume;
	/// <summary> The panning of the audio, from 0 -> 1, where 0.5 is centered. </summary>
	public float Panning;

	/// <summary> Is the audio looping. If this is true, the sound will not be destroyed until stopsound is called on it. </summary>
	public bool Looping;
	/// <summary> The 3D location of the audio playback, unused </summary>
	public Vector3 Location;
	/// <summary> If true, the audio system will not destroy the playback object when manually/automatically stopped. </summary>
	public bool DoNotAutoDestroy;

	/// <summary>
	/// Returns if the struct has been uninitialized (all zero)
	/// </summary>
	public readonly bool IsUninitialized() => Pitch == default && TimeStretch == default && Volume == default && Panning == default && Looping == default && Location == default && DoNotAutoDestroy == default;
}

[Flags]
// Some descriptions from https://en.wikipedia.org/wiki/Audio_time_stretching_and_pitch_scaling so it follows a standard naming convention
public enum AudioFeatures : ushort
{
	/// <summary>
	/// Supports preloaded audio clips (slammed into a single memory buffer Sound object, mostly for sound effects).
	/// The playbacks will user 
	/// </summary>
	PreloadedAudioClips = 1 << 0,
	/// <summary>
	/// Supports streamed audio clips (streaming from a memory buffer pieces, mostly for music)
	/// </summary>
	StreamedAudioClips = 1 << 1,
	/// <summary>
	/// Supports loading audio files via IFileSystem
	/// </summary>
	LoadingAudioFromFile = 1 << 2,
	/// <summary>
	/// Supports loading audio files from C# streams
	/// </summary>
	LoadingAudioFromStream = 1 << 3,
	/// <summary>
	/// Time stretching is the process of changing the speed or duration of an audio signal without affecting its pitch.
	/// </summary>
	TimeStretching = 1 << 4,
	/// <summary>
	/// Pitch scaling is the process of changing the pitch without affecting the speed.
	/// </summary>
	PitchScaling = 1 << 5,
	/// <summary>
	/// Pitch control is a simpler process which affects pitch and speed simultaneously by slowing down or speeding up a recording.
	/// </summary>
	PitchControl = 1 << 6,
	/// <summary>
	/// Audio processors are <see cref="AudioCallbackFn"/> delegates to read/modify an underlying audio stream
	/// </summary>
	AudioProcessors = 1 << 7
}

public delegate void AudioCallbackFn(in AudioPlaybackHandle handle, Span<float> buffer, object? userdata = null);

public interface IAudioSystem
{
	void Initialize();
	void Update();

	/// <summary>
	/// The current audio playback generation. Each time a sound is destroyed, this variable is incremented, starting at 1.
	/// A handle contains its channel and its generation. To validate a playback handle is valid, the system will check if the
	/// channel is active, and if the channels generation matches the generation of the handle. If this is not the case, the
	/// playback handle is not valid. tl;dr a counter of how many audio playbacks have been destroyed for the lifetime of the
	/// application/game.
	/// </summary>
	[Pure] ulong GetPlaybackGeneration();

	/// <summary>
	/// Gets all supported features of this audio system implementation.
	/// </summary>
	[Pure] AudioFeatures GetSupportedFeatures();
	/// <summary>
	/// Gets if a feature is supported by this audio system implementation.
	/// </summary>
	public bool SupportsFeature(AudioFeatures feature) => (GetSupportedFeatures() & feature) == feature;

	/// <summary>
	/// Attempts to load an audio clip from a file on the mounted filesystem. 
	/// <br/><br/><b>NOTE:</b> If the audio clip at this path is already loaded, then this function will return an existing instance.
	/// <br/><b>NOTE:</b> If the identifier hash-conflicts with another audio clip that is not of the same <see cref="AudioClipSource"/>, this method will return null.
	/// </summary>
	[Pure] IAudioClip? CreateFileAudioClip(in AudioLoadSettings settings, ReadOnlySpan<char> name, ReadOnlySpan<char> pathId = default);

	/// <summary>
	/// Attempts to load an audio clip from a stream. The data on the stream is copied into an internal clip buffer, so the stream can be released after use.
	/// <br/><br/> <b>NOTE:</b> The identifier can be default, in which case no duplicate checks are performed.
	/// <br/> <b>NOTE:</b> This may return the same loaded object in memory, if the identifier hash-conflicts with another audio clip. 
	/// <br/> <b>NOTE:</b> If the identifier hash-conflicts with another audio clip that is not of the same <see cref="AudioClipSource"/>, this method will return null.
	/// </summary>
	[Pure] IAudioClip? CreateStreamAudioClip(in AudioLoadSettings settings, Stream stream, ReadOnlySpan<char> identifier = default);

	/// <summary>
	/// Creates a dynamic audio clip. 
	/// <br/><br/> <b>NOTE:</b> The identifier can be default, in which case no duplicate checks are performed.
	/// <br/> <b>NOTE:</b> If the identifier hash-conflicts with another audio clip that is not of the same <see cref="AudioClipSource"/>, this method will return null.
	/// </summary>
	[Pure] IAudioClip? CreateDynamicAudioClip(in AudioLoadSettings settings, AudioCallbackFn fn, ReadOnlySpan<char> identifier = default);

	/// <summary>
	/// Destroys an audio clip. When this function returns, <see cref="IAudioClip.IsValid"/> will begin to return false.
	/// If the audio clip was null or already destroyed, this function returns false.
	/// Otherwise, it returns true.
	/// </summary>
	bool DestroyAudioClip(IAudioClip? clip);

	/// <summary>
	/// Creates a playback handle on an available audio channel. If the clip is null, or otherwise invalid, this returns an invalid handle.
	/// <see cref="PlaySound(in AudioPlaybackHandle)"/> must be called to actually play audio. The playback object will destroy itself when
	/// it is complete or stopped, unless <see cref="AudioPlaybackSettings.DoNotAutoDestroy"/> is set to true.
	/// </summary>
	[Pure] AudioPlaybackHandle CreatePlayback(IAudioClip? clip, in AudioPlaybackSettings init = default);

	/// <summary>
	/// Validtes if a playback handle is valid (channel and generation are both non-zero)
	/// </summary>
	[Pure] bool IsPlaybackHandleValid(in AudioPlaybackHandle handle);

	/// <summary>
	/// Plays a playback handle
	/// </summary>
	bool PlaySound(in AudioPlaybackHandle handle);

	/// <summary>
	/// Pauses a playback handle
	/// </summary>
	bool PauseSound(in AudioPlaybackHandle handle);

	/// <summary>
	/// Resumes a playback handle
	/// </summary>
	bool ResumeSound(in AudioPlaybackHandle handle);

	/// <summary>
	/// Stops a playback handle
	/// </summary>
	bool StopSound(in AudioPlaybackHandle handle);

	/// <summary>
	/// Stop all sounds
	/// </summary>
	/// <returns>How many sound playbacks were stopped and destroyed</returns>
	long StopAllSounds();

	/// <summary>
	/// Destroy all unused audio clips - ie. all audio clips not on a playback channel
	/// </summary>
	/// <returns>How many sound clips were destroyed</returns>
	long DestroyAllUnusedAudioClips();

	/// <summary>
	/// Destroy all audio clips.
	/// </summary>
	/// <returns>How many sound clips were destroyed</returns>
	long DestroyAllAudioClips();

	/// <summary>
	/// Gets the current playback settings for this playback handle.
	/// </summary>
	[Pure] ref readonly AudioPlaybackSettings GetPlaybackSettings(in AudioPlaybackHandle handle);

	/// <summary>
	/// The volume of the playback handle, from 0 -> 1.
	/// </summary>
	[Pure] float GetSoundVolume(in AudioPlaybackHandle handle);

	/// <summary>
	/// The panning of the playback handle, from 0 -> 1, where 0.5 is centered.
	/// </summary>
	[Pure] float GetSoundPanning(in AudioPlaybackHandle handle);

	/// <summary>
	/// The time stretching of the playback handle. If pitch control was used, this will be the same value as the pitch scale. Starts at 1.
	/// </summary>
	[Pure] float GetSoundTimeStretch(in AudioPlaybackHandle handle);

	/// <summary>
	/// The pitch scaling of the playback handle. If pitch control was used, this will be the same value as the time stretch. Starts at 1.
	/// </summary>
	[Pure] float GetSoundPitchScaling(in AudioPlaybackHandle handle);

	/// <summary>
	/// Sets the volume of the playback handle, from 0 -> 1.
	/// </summary>
	bool SetSoundVolume(in AudioPlaybackHandle handle, float volume);

	/// <summary>
	/// Sets the panning of the playback handle, from 0 -> 1, where 0.5 is centered.
	/// </summary>
	bool SetSoundPanning(in AudioPlaybackHandle handle, float panning);

	/// <summary> Stretches the playback speed without stretching the pitch of the audio. May be unsupported. Starts at 1. </summary>
	bool SetSoundTimeStretch(in AudioPlaybackHandle handle, float stretchRatio);

	/// <summary> Stretches the playback pitch without stretching the speed of the audio. May be unsupported. Starts at 1. </summary>
	bool SetSoundPitchScaling(in AudioPlaybackHandle handle, float pitchRatio);

	/// <summary> Stretches the playback speed and pitch of the audio. May be unsupported. Starts at 1. </summary>
	bool SetSoundPitchControl(in AudioPlaybackHandle handle, float pitch);

	/// <summary>
	/// Gets the playhead location of the audio playback. If the audio clip at this channel does not support this behavior,
	/// then this function returns false.
	/// </summary>
	bool GetSoundPlayhead(in AudioPlaybackHandle handle, out double playhead);

	/// <summary>
	/// Sets the playhead location of the audio playback. If the audio clip at this channel does not support this behavior,
	/// then this function returns false.
	/// </summary>
	bool SetSoundPlayhead(in AudioPlaybackHandle handle, double playhead);

	/// <summary>
	/// Restarts a sound.
	/// </summary>
	void RestartSound(in AudioPlaybackHandle handle);

	/// <summary>
	/// Gets the audio clip this playback handle's channel is designated to play. Returns null if the handle is invalid.
	/// </summary>
	[Pure] IAudioClip? GetAudioClip(in AudioPlaybackHandle handle);

	/// <summary>
	/// Attach an audio processor (with optional userdata) to a playback handle. Returns false if the handle is invalid, or if this is not supported by this audio system/playback handle.
	/// </summary>
	bool AttachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn, object? userdata = null);

	/// Detaches an audio processor (with optional userdata) from a playback handle. Returns false if the handle is invalid, if this is not supported by this audio system/playback handle, or
	/// if the processor function was not attached in the first place.
	bool DetachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn);
}
