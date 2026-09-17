using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus.AudioSystem.Raylib;

[SuppressUnmanagedCodeSecurity]
public static unsafe partial class Raylib
{
	/// <summary>
	/// Used by LibraryImport to load the native library
	/// </summary>
	public const string NativeLibName = "raylib";

	/// <summary>Internal memory allocator</summary>
	[LibraryImport(NativeLibName)]
	public static partial void* MemAlloc(int size);

	/// <summary>Internal memory reallocator</summary>
	[LibraryImport(NativeLibName)]
	public static partial void* MemRealloc(void* ptr, int size);

	/// <summary>Internal memory free</summary>
	[LibraryImport(NativeLibName)]
	public static partial void MemFree(void* ptr);

	/// <summary>Initialize audio device and context</summary>
	[LibraryImport(NativeLibName)]
	public static partial void InitAudioDevice();

	/// <summary>Close the audio device and context</summary>
	[LibraryImport(NativeLibName)]
	public static partial void CloseAudioDevice();

	/// <summary>Check if audio device has been initialized successfully</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsAudioDeviceValid();

	/// <summary>Set master volume (listener)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetMasterVolume(float volume);

	/// <summary>Set master volume (listener)</summary>
	[LibraryImport(NativeLibName)]
	public static partial float GetMasterVolume();


	// Wave/Sound loading/unloading functions

	/// <summary>Load wave data from file</summary>
	[LibraryImport(NativeLibName)]
	public static partial Wave LoadWave(sbyte* fileName);

	/// <summary>Load wave from memory buffer, fileType refers to extension: i.e. "wav"</summary>
	[LibraryImport(NativeLibName)]
	public static partial Wave LoadWaveFromMemory(sbyte* fileType, byte* fileData, int dataSize);

	/// <summary>Checks if wave data is ready</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsWaveValid(Wave wave);

	/// <summary>Load sound from file</summary>
	[LibraryImport(NativeLibName)]
	public static partial Sound LoadSound(sbyte* fileName);

	/// <summary>Load sound from wave data</summary>
	[LibraryImport(NativeLibName)]
	public static partial Sound LoadSoundFromWave(Wave wave);

	/// <summary>Create a new sound that shares the same sample data as the source sound, does not own the sound data</summary>
	[LibraryImport(NativeLibName)]
	public static partial Sound LoadSoundAlias(Sound source);

	/// <summary>Checks if a sound is ready</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsSoundValid(Sound sound);

	/// <summary>Update sound buffer with new data</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UpdateSound(Sound sound, void* data, int sampleCount);

	/// <summary>Unload wave data</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadWave(Wave wave);

	/// <summary>Unload sound</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadSound(Sound sound);

	/// <summary>Unload a sound alias (does not deallocate sample data)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadSoundAlias(Sound alias);

	/// <summary>Export wave data to file</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool ExportWave(Wave wave, sbyte* fileName);

	/// <summary>Export wave sample data to code (.h)</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool ExportWaveAsCode(Wave wave, sbyte* fileName);


	// Wave/Sound management functions

	/// <summary>Play a sound</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PlaySound(Sound sound);

	/// <summary>Stop playing a sound</summary>
	[LibraryImport(NativeLibName)]
	public static partial void StopSound(Sound sound);

	/// <summary>Pause a sound</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PauseSound(Sound sound);

	/// <summary>Resume a paused sound</summary>
	[LibraryImport(NativeLibName)]
	public static partial void ResumeSound(Sound sound);

	/// <summary>Get number of sounds playing in the multichannel</summary>
	[LibraryImport(NativeLibName)]
	public static partial int GetSoundsPlaying();

	/// <summary>Check if a sound is currently playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsSoundPlaying(Sound sound);

	/// <summary>Set volume for a sound (1.0 is max level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetSoundVolume(Sound sound, float volume);

	/// <summary>Set pitch for a sound (1.0 is base level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetSoundPitch(Sound sound, float pitch);

	/// <summary>Set pan for a sound (0.5 is center)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetSoundPan(Sound sound, float pan);

	/// <summary>Copy a wave to a new wave</summary>
	[LibraryImport(NativeLibName)]
	public static partial Wave WaveCopy(Wave wave);

	/// <summary>Crop a wave to defined samples range</summary>
	[LibraryImport(NativeLibName)]
	public static partial void WaveCrop(Wave* wave, int initSample, int finalSample);

	/// <summary>Convert wave data to desired format</summary>
	[LibraryImport(NativeLibName)]
	public static partial void WaveFormat(Wave* wave, int sampleRate, int sampleSize, int channels);

	/// <summary>Get samples data from wave as a floats array</summary>
	[LibraryImport(NativeLibName)]
	public static partial float* LoadWaveSamples(Wave wave);

	/// <summary>Unload samples data loaded with LoadWaveSamples()</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadWaveSamples(float* samples);

	// Music management functions

	/// <summary>Load music stream from file</summary>
	[LibraryImport(NativeLibName)]
	public static partial Music LoadMusicStream(sbyte* fileName);

	/// <summary>Load music stream from memory buffer, fileType refers to extension: i.e. ".wav"</summary>
	[LibraryImport(NativeLibName)]
	public static partial Music LoadMusicStreamFromMemory(sbyte* fileType, byte* data, int dataSize);

	/// <summary>Checks if a music stream is ready</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsMusicValid(Music music);

	/// <summary>Unload music stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadMusicStream(Music music);

	/// <summary>Start music playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PlayMusicStream(Music music);

	/// <summary>Check if music is playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsMusicStreamPlaying(Music music);

	/// <summary>Updates buffers for music streaming</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UpdateMusicStream(Music music);

	/// <summary>Stop music playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial void StopMusicStream(Music music);

	/// <summary>Pause music playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PauseMusicStream(Music music);

	/// <summary>Resume playing paused music</summary>
	[LibraryImport(NativeLibName)]
	public static partial void ResumeMusicStream(Music music);

	/// <summary>Seek music to a position (in seconds)</summary>
	[LibraryImport(NativeLibName, EntryPoint = "SeekMusicStream")]
	private static partial void _SeekMusicStream(Music music, float position);
	public static void SeekMusicStream(Music music, float position) {
		_SeekMusicStream(music, position);
		// Is this going to cause crashes? The original source locks a mutex in MiniAudio...
		// https://github.com/raysan5/raylib/commit/11429b48eb244c8e153838cf7d876d75da992815
		// hoping we can get away with it... would be way more annoying to recompile RL for this fix.
		unsafe {
			AudioBuffer* buffer = music.Stream.Buffer;
			buffer->IsSubBufferProcessed_0 = true;
			buffer->IsSubBufferProcessed_1 = true;
		}
	}

	/// <summary>Set volume for music (1.0 is max level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetMusicVolume(Music music, float volume);

	/// <summary>Set pitch for a music (1.0 is base level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetMusicPitch(Music music, float pitch);

	/// <summary>Set pan for a music (0.5 is center)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetMusicPan(Music music, float pan);

	/// <summary>Get music time length (in seconds)</summary>
	[LibraryImport(NativeLibName)]
	public static partial float GetMusicTimeLength(Music music);

	/// <summary>Get current music time played (in seconds)</summary>
	[LibraryImport(NativeLibName)]
	public static partial float GetMusicTimePlayed(Music music);


	// AudioStream management functions

	/// <summary>Init audio stream (to stream raw audio pcm data)</summary>
	[LibraryImport(NativeLibName)]
	public static partial AudioStream LoadAudioStream(uint sampleRate, uint sampleSize, uint channels);

	/// <summary>Checks if an audio stream is ready</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsAudioStreamValid(AudioStream stream);

	/// <summary>Unload audio stream and free memory</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UnloadAudioStream(AudioStream stream);

	/// <summary>Update audio stream buffers with data</summary>
	[LibraryImport(NativeLibName)]
	public static partial void UpdateAudioStream(AudioStream stream, void* data, int frameCount);

	/// <summary>Check if any audio stream buffers requires refill</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsAudioStreamProcessed(AudioStream stream);

	/// <summary>Play audio stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PlayAudioStream(AudioStream stream);

	/// <summary>Pause audio stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void PauseAudioStream(AudioStream stream);

	/// <summary>Resume audio stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void ResumeAudioStream(AudioStream stream);

	/// <summary>Check if audio stream is playing</summary>
	[LibraryImport(NativeLibName)]
	public static partial CBool IsAudioStreamPlaying(AudioStream stream);

	/// <summary>Stop audio stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void StopAudioStream(AudioStream stream);

	/// <summary>Set volume for audio stream (1.0 is max level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetAudioStreamVolume(AudioStream stream, float volume);

	/// <summary>Set pitch for audio stream (1.0 is base level)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetAudioStreamPitch(AudioStream stream, float pitch);

	/// <summary>Set pan for audio stream (0.5 is centered)</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetAudioStreamPan(AudioStream stream, float pan);

	/// <summary>Default size for new audio streams</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetAudioStreamBufferSizeDefault(int size);

	/// <summary>Audio thread callback to request new data</summary>
	[LibraryImport(NativeLibName)]
	public static partial void SetAudioStreamCallback(
		AudioStream stream,
		delegate* unmanaged[Cdecl]<void*, uint, void> callback
	);

	/// <summary>Attach audio stream processor to stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void AttachAudioStreamProcessor(
		AudioStream stream,
		delegate* unmanaged[Cdecl]<void*, uint, void> processor
	);

	/// <summary>Detach audio stream processor from stream</summary>
	[LibraryImport(NativeLibName)]
	public static partial void DetachAudioStreamProcessor(
		AudioStream stream,
		delegate* unmanaged[Cdecl]<void*, uint, void> processor
	);

	/// <summary>Attach audio stream processor to the entire audio pipeline</summary>
	[LibraryImport(NativeLibName)]
	public static partial void AttachAudioMixedProcessor(
		delegate* unmanaged[Cdecl]<void*, uint, void> processor
	);

	/// <summary>Detach audio stream processor from the entire audio pipeline</summary>
	[LibraryImport(NativeLibName)]
	public static partial void DetachAudioMixedProcessor(
		delegate* unmanaged[Cdecl]<void*, uint, void> processor
	);
}
