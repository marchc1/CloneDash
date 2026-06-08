using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Util;
using Nucleus.Util;
using Raylib_cs;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucleus.Audio;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void AudioCallback(void* buffer, uint frames);
public static unsafe class RaylibAudioHelpers
{
	public const int MUSIC_HEADER_RIFF = ('R' << 24) + ('I' << 16) + ('F' << 8) + 'F';
	public const int MUSIC_HEADER_OGGS = ('O' << 24) + ('g' << 16) + ('g' << 8) + 'S';
	public const int MUSIC_HEADER_ID3 = ('I' << 24) + ('D' << 16) + ('3' << 8) + 0x03;

	public const string MUSIC_HEADER_RIFF_EXTENSION = ".wav";
	public const string MUSIC_HEADER_OGGS_EXTENSION = ".ogg";
	public const string MUSIC_HEADER_ID3_EXTENSION = ".mp3";

	static sbyte* AllocUnmanagedStr(ReadOnlySpan<char> text) {
		var c = Encoding.ASCII.GetByteCount(text) + 1;
		sbyte* data = Raylib.New<sbyte>(c);
		Encoding.ASCII.GetBytes(text, new(data, c));
		data[c - 1] = 0;
		return data;
	}

	public static readonly sbyte* UNM_MUSIC_HEADER_RIFF_EXTENSION = AllocUnmanagedStr(".wav");
	public static readonly sbyte* UNM_MUSIC_HEADER_OGGS_EXTENSION = AllocUnmanagedStr(".ogg");
	public static readonly sbyte* UNM_MUSIC_HEADER_ID3_EXTENSION = AllocUnmanagedStr(".mp3");

	public static byte* AllocCopyStream(Stream stream) {
		byte* data = Raylib.New<byte>((int)stream.Length);
		using UnmanagedMemoryStream str = new(data, 0, stream.Length, FileAccess.ReadWrite);
		stream.CopyTo(str);
		return data;
	}

	public static sbyte* DetermineFileType(ReadOnlySpan<byte> data) {
		if (data.Length < 4) return null;

		Span<byte> byteHeader = stackalloc byte[] { data[3], data[2], data[1], data[0] };
		Span<int> headerCast = MemoryMarshal.Cast<byte, int>(byteHeader);
		sbyte* fileExtension = headerCast[0] switch {
			MUSIC_HEADER_RIFF => UNM_MUSIC_HEADER_RIFF_EXTENSION,
			MUSIC_HEADER_OGGS => UNM_MUSIC_HEADER_OGGS_EXTENSION,
			MUSIC_HEADER_ID3 => UNM_MUSIC_HEADER_ID3_EXTENSION,
			_ => UNM_MUSIC_HEADER_ID3_EXTENSION,
		};

		return fileExtension;
	}

	public static Music AllocMusicStream(byte* data, nuint len) {
		return Raylib.LoadMusicStreamFromMemory(DetermineFileType(new(data, (int)len)), data, (int)len);
	}

	public static Sound AllocSound(byte* data, nuint len) {
		var filetype = DetermineFileType(new(data, (int)len));
		if (filetype == null) return default;
		var wave = Raylib.LoadWaveFromMemory(filetype, data, (int)len);
		var sound = Raylib.LoadSoundFromWave(wave);
		Raylib.UnloadWave(wave);
		return sound;
	}
}

public abstract unsafe class BaseAudioClip : IAudioClip
{
	private float volumeMultiplier = 1f;
	private bool volumeDirty = false;
	readonly HashSet<ConVar> boundConVars = [];

	private byte* data;
	private nuint length;
	private string clipIdentifier;
	private AudioClipSource clipSource;
	private Sound sound;
	private bool destroyed;

	public byte* Data => data;
	public nuint Length => length;
	public Sound Sound {
		get {
			if (!Raylib.IsSoundValid(sound))
				sound = RaylibAudioHelpers.AllocSound(data, length);

			return sound;
		}
	}

	public BaseAudioClip(ReadOnlySpan<char> identifier, AudioClipSource source, Stream stream) {
		length = (nuint)stream.Length;
		data = RaylibAudioHelpers.AllocCopyStream(stream);
		clipSource = source;
		clipIdentifier = new(identifier.SliceNullTerminatedString());
	}

	protected BaseAudioClip(string identifier, AudioClipSource source) {
		clipIdentifier = identifier;
		clipSource = source;
	}

	public void SetData(byte* ptr, nuint len) {
		data = ptr;
		length = len;
	}

	public bool IsVolumeDirty => volumeDirty;

	public float GetVolumeMultiplier() {
		if (volumeDirty) RecalculateVolumeMultiplier();
		return volumeMultiplier;
	}

	public void RecalculateVolumeMultiplier() {
		volumeDirty = false;
		volumeMultiplier = 1f;

		if (boundConVars.Count == 0)
			return;

		foreach (var cv in boundConVars)
			volumeMultiplier *= (float)cv.GetDouble();
	}

	public void BindVolumeToConVar(ConVar cv) {
		if (boundConVars.Add(cv)) {
			cv.OnChange += Cv_OnChange;
			RecalculateVolumeMultiplier();
		}
	}

	private void Cv_OnChange(ConVar self, ReadOnlySpan<char> old, double oldD) => volumeDirty = true;

	public ReadOnlySpan<char> GetName() => clipIdentifier;
	public AudioClipSource GetSource() => clipSource;

	public bool IsValid() => !destroyed && data != null && length != 0;

	public void Destroy() {
		if (destroyed) return;
		destroyed = true;
		if (Raylib.IsSoundValid(sound))
			Raylib.UnloadSound(sound);
		if (data != null)
			Raylib.MemFree(data);
		data = null;
	}
}

public unsafe class FileAudioClip : BaseAudioClip
{
	public FileAudioClip(ReadOnlySpan<char> identifier, Stream stream) : base(identifier, AudioClipSource.File, stream) { }
}

public unsafe class StreamAudioClip : BaseAudioClip
{
	public StreamAudioClip(ReadOnlySpan<char> identifier, Stream stream) : base(identifier, AudioClipSource.Stream, stream) { }
}

public unsafe class DynamicAudioClip : BaseAudioClip
{
	public AudioCallbackFn Callback { get; }

	public DynamicAudioClip(ReadOnlySpan<char> identifier, AudioCallbackFn fn) : base(new string(identifier), AudioClipSource.Dynamic) {
		Callback = fn;
	}

	public new bool IsValid() => Callback != null;
}

public struct PlaybackProcessor(AudioCallbackFn fn, object? userdata)
{
	public readonly AudioCallbackFn Fn = fn;
	public readonly object? UserData = userdata;
}

internal unsafe class PlaybackChannel
{
	public ulong Generation;
	public BaseAudioClip? Clip;
	public AudioPlaybackSettings Settings;
	public bool Active;
	public bool Paused;
	public bool Complete;

	public bool IsStream;
	public Music MusicStream;
	public Sound SoundAlias;

	public readonly List<PlaybackProcessor> Processors = [];

	public void Reset() {
		Clip = null;
		Settings = default;
		Active = false;
		Paused = false;
		Complete = false;
		IsStream = false;
		MusicStream = default;
		SoundAlias = default;
		Processors.Clear();
	}

	// I want to rename this, but it captures my anger at the time so perfectly
	private unsafe AudioCallback? FUCKFUCKFUCKFUCK;
	[MemberNotNull(nameof(FUCKFUCKFUCKFUCK))]
	private unsafe void InitializeAudioProcessor() {
		FUCKFUCKFUCKFUCK = new((buffer, frames) => {
			foreach (var proc in Processors)
				proc.Fn(new(buffer, (int)frames), proc.UserData);
		});
	}

	public void SetupSoundProcessors() {
		InitializeAudioProcessor();
		Raylib.AttachAudioStreamProcessor(SoundAlias.Stream, (delegate* unmanaged[Cdecl]<void*, uint, void>)Marshal.GetFunctionPointerForDelegate(FUCKFUCKFUCKFUCK));
	}

	public void SetupMusicProcessors() {
		InitializeAudioProcessor();
		Raylib.AttachAudioStreamProcessor(MusicStream.Stream, (delegate* unmanaged[Cdecl]<void*, uint, void>)Marshal.GetFunctionPointerForDelegate(FUCKFUCKFUCKFUCK));
	}
}

public unsafe class RaylibAudioSystem : IAudioSystem
{
	const int MAX_CHANNELS = 256;

	readonly GenerationalAllocator allocator = new();
	readonly PlaybackChannel[] channels = new PlaybackChannel[MAX_CHANNELS];
	readonly Dictionary<UtlSymId_t, BaseAudioClip> clipsByName = [];
	readonly HashSet<BaseAudioClip> allClips = [];
	float masterVolume = 1f;
	ulong totalMemory = 0;

	PlaybackChannel? GetChannel(in AudioPlaybackHandle handle) {
		if (handle.Audio != this) return null;
		if (handle.Channel == 0 || handle.Channel > MAX_CHANNELS) return null;
		var ch = channels[handle.Channel - 1];
		if (ch == null || !ch.Active) return null;
		if (ch.Generation != handle.Generation) return null;
		return ch;
	}

	int FindFreeChannel() {
		for (int i = 0; i < MAX_CHANNELS; i++) {
			if (channels[i] == null) {
				channels[i] = new PlaybackChannel();
				return i;
			}
			if (!channels[i].Active)
				return i;
		}
		return -1;
	}

	void InternalDestroyPlayback(int index) {
		var ch = channels[index];
		if (ch == null || !ch.Active) return;

		if (ch.IsStream) {
			if (Raylib.IsMusicValid(ch.MusicStream)) {
				Raylib.StopMusicStream(ch.MusicStream);
				Raylib.UnloadMusicStream(ch.MusicStream);
			}
		}
		else {
			if (Raylib.IsSoundValid(ch.SoundAlias)) {
				Raylib.StopSound(ch.SoundAlias);
				Raylib.UnloadSoundAlias(ch.SoundAlias);
			}
		}

		ch.Reset();
	}

	void ApplySettings(PlaybackChannel ch) {
		float clipVol = 1f;
		if (ch.Clip != null) {
			clipVol = ch.Clip.GetVolumeMultiplier();
		}

		float finalVolume = ch.Settings.Volume * clipVol;

		if (ch.IsStream) {
			Raylib.SetMusicVolume(ch.MusicStream, finalVolume);
			Raylib.SetMusicPan(ch.MusicStream, ch.Settings.Panning);
			Raylib.SetMusicPitch(ch.MusicStream, ch.Settings.Pitch);
		}
		else {
			Raylib.SetSoundVolume(ch.SoundAlias, finalVolume);
			Raylib.SetSoundPan(ch.SoundAlias, ch.Settings.Panning);
			Raylib.SetSoundPitch(ch.SoundAlias, ch.Settings.Pitch);
		}
	}

	public bool AttachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn, object? userdata = null) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Processors.Add(new(fn, userdata));
		return true;
	}

	public bool DetachProcessor(in AudioPlaybackHandle handle, AudioCallbackFn fn) {
		var ch = GetChannel(in handle);
		if (ch == null || ch.Processors == null) return false;
		for (int i = 0; i < ch.Processors.Count; i++) {
			if (ch.Processors[i].Fn == fn) {
				ch.Processors.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public IAudioClip? CreateDynamicAudioClip(AudioCallbackFn fn, ReadOnlySpan<char> identifier = default) {
		string id = identifier.Length > 0 ? new string(identifier) : $"__dynamic_{Guid.NewGuid():N}";
		UtlSymId_t hash = id.Hash();
		if (identifier.Length > 0 && clipsByName.TryGetValue(hash, out var existing)) {
			if (existing.GetSource() != AudioClipSource.Dynamic) return null;
			return existing;
		}

		var clip = new DynamicAudioClip(identifier, fn);
		clipsByName[hash] = clip;
		allClips.Add(clip);
		return clip;
	}

	public IAudioClip? CreateFileAudioClip(ReadOnlySpan<char> name) => CreateFileAudioClip(name, "audio");

	public IAudioClip? CreateFileAudioClip(ReadOnlySpan<char> name, ReadOnlySpan<char> pathId) {
		name = name.SliceNullTerminatedString();
		UtlSymId_t nameHash = name.Hash();

		Stream stream;
		if (Path.IsPathFullyQualified(name) && pathId.IsEmpty) {
			if (clipsByName.TryGetValue(nameHash, out var existing)) {
				if (existing.GetSource() != AudioClipSource.File) return null;
				return existing;
			}

			string nameStr = new(name);

			if (!File.Exists(nameStr)) {
				Logs.Warn($"'{nameStr}' does not exist");
				return null;
			}

			stream = File.OpenRead(nameStr);
		}
		else{
			stream = filesystem.Open(pathId, name, FileAccess.Read, FileMode.Open)!;
			if (stream == null) {
				Logs.Warn($"'{name}' not found in {pathId}");
				return null;
			}
		}

			var clip = new FileAudioClip(name, stream);
		totalMemory += clip.Length;
		clipsByName[nameHash] = clip;
		allClips.Add(clip);
		stream.Dispose();
		return clip;
	}

	public IAudioClip? CreateStreamAudioClip(Stream stream, ReadOnlySpan<char> identifier = default) {
		string id = identifier.Length > 0 ? new string(identifier) : $"__stream_{Guid.NewGuid():N}";
		UtlSymId_t hash = id.Hash();

		if (identifier.Length > 0 && clipsByName.TryGetValue(hash, out var existing)) {
			if (existing.GetSource() != AudioClipSource.Stream) return null;
			return existing;
		}

		var clip = new StreamAudioClip(identifier, stream);
		totalMemory += clip.Length;
		clipsByName[hash] = clip;
		allClips.Add(clip);
		return clip;
	}

	public AudioPlaybackHandle CreatePlayback(IAudioClip? clip, in AudioPlaybackSettings init = default) {
		if (clip == null || !clip.IsValid()) return AudioPlaybackHandle.Null;
		if (clip is not BaseAudioClip baseClip) return AudioPlaybackHandle.Null;

		int idx = FindFreeChannel();
		if (idx < 0) return AudioPlaybackHandle.Null;

		var ch = channels[idx];
		var gen = allocator.Alloc((ulong)(idx + 1));
		ch.Generation = gen.Generation;
		ch.Clip = baseClip;
		ch.Active = true;
		ch.Paused = false;
		ch.Complete = false;

		var settings = init.IsUninitialized() ? AudioPlaybackSettings.Unaltered : init;
		ch.Settings = settings;

		if (settings.Stream) {
			ch.IsStream = true;
			ch.MusicStream = RaylibAudioHelpers.AllocMusicStream(baseClip.Data, baseClip.Length);
			ch.MusicStream.Looping = settings.Looping;
			ch.SetupMusicProcessors();
		}
		else {
			ch.IsStream = false;
			ch.SoundAlias = Raylib.LoadSoundAlias(baseClip.Sound);
			ch.SetupSoundProcessors();
		}

		ApplySettings(ch);

		return new AudioPlaybackHandle {
			Audio = this,
			Handle = gen
		};
	}

	public bool DestroyAudioClip(IAudioClip? clip) {
		if (clip == null || clip is not BaseAudioClip baseClip) return false;
		if (!allClips.Remove(baseClip)) return false;

		string name = new string(baseClip.GetName());
		clipsByName.Remove(name.Hash());

		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active && ch.Clip == baseClip)
				InternalDestroyPlayback(i);
		}

		if (baseClip.Length > 0)
			totalMemory -= baseClip.Length;

		baseClip.Destroy();
		return true;
	}

	public long DestroyAllAudioClips() {
		long count = allClips.Count;
		StopAllSounds();

		foreach (var clip in allClips)
			clip.Destroy();

		totalMemory = 0;
		allClips.Clear();
		clipsByName.Clear();
		return count;
	}

	public long DestroyAllUnusedAudioClips() {
		HashSet<BaseAudioClip> inUse = [];
		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active && ch.Clip != null)
				inUse.Add(ch.Clip);
		}

		long count = 0;
		List<BaseAudioClip> toRemove = [];
		foreach (var clip in allClips) {
			if (!inUse.Contains(clip))
				toRemove.Add(clip);
		}

		foreach (var clip in toRemove) {
			allClips.Remove(clip);
			string name = new string(clip.GetName());
			clipsByName.Remove(name.Hash());
			if (clip.Length > 0)
				totalMemory -= clip.Length;
			clip.Destroy();
			count++;
		}

		return count;
	}

	public void DestroyPlayback(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return;
		int idx = (int)(handle.Channel - 1);
		InternalDestroyPlayback(idx);
	}

	public long GetActiveChannelCount() {
		long count = 0;
		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active && !ch.Paused && !ch.Complete)
				count++;
		}
		return count;
	}

	public IAudioClip? GetAudioClip(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		return ch?.Clip;
	}

	public long GetAudioClipCount() => allClips.Count;

	public float GetMasterVolume() => masterVolume;

	public ulong GetMemoryAllocated() => totalMemory;

	public long GetPlaybackCount(IAudioClip? clip) {
		if (clip == null) return 0;
		long count = 0;
		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active && ch.Clip == clip)
				count++;
		}
		return count;
	}

	public double GetPlaybackDuration(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return 0;
		if (ch.IsStream)
			return Raylib.GetMusicTimeLength(ch.MusicStream);
		else
			return (double)((double)ch.SoundAlias.FrameCount / ch.SoundAlias.Stream.SampleRate);
	}

	public ulong GetPlaybackGeneration() => allocator.GetGeneration();

	public ref readonly AudioPlaybackSettings GetPlaybackSettings(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null)
			return ref AudioPlaybackSettings.Unaltered;
		return ref ch.Settings;
	}

	public float GetSoundPanning(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return 0.5f;
		return ch.Settings.Panning;
	}

	public float GetSoundPitchScaling(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return 1f;
		return ch.Settings.Pitch;
	}

	public bool GetSoundPlayhead(in AudioPlaybackHandle handle, out double playhead) {
		playhead = 0;
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		if (!ch.IsStream) return false;
		playhead = Raylib.GetMusicTimePlayed(ch.MusicStream);
		return true;
	}

	public float GetSoundTimeStretch(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return 1f;
		return ch.Settings.TimeStretch;
	}

	public float GetSoundVolume(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return 0f;
		return ch.Settings.Volume;
	}

	public AudioFeatures GetSupportedFeatures() {
		return AudioFeatures.PreloadedAudioClips
			| AudioFeatures.StreamedAudioClips
			| AudioFeatures.LoadingAudioFromFile
			| AudioFeatures.LoadingAudioFromStream
			| AudioFeatures.PitchControl;
	}

	public void Initialize() {
		Raylib.InitAudioDevice();
	}

	public bool IsPlaybackActive(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		if (ch.IsStream)
			return Raylib.IsMusicStreamPlaying(ch.MusicStream);
		return Raylib.IsSoundPlaying(ch.SoundAlias);
	}

	public bool IsPlaybackComplete(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return true;
		return ch.Complete;
	}

	public bool IsPlaybackHandleValid(in AudioPlaybackHandle handle) {
		return GetChannel(in handle) != null;
	}

	public bool IsPlaybackPaused(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		return ch.Paused;
	}

	public bool PauseSound(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Paused = true;
		if (ch.IsStream)
			Raylib.PauseMusicStream(ch.MusicStream);
		else
			Raylib.PauseSound(ch.SoundAlias);
		return true;
	}

	public bool PlaySound(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Paused = false;
		ch.Complete = false;
		if (ch.IsStream)
			Raylib.PlayMusicStream(ch.MusicStream);
		else
			Raylib.PlaySound(ch.SoundAlias);
		return true;
	}

	public void RestartSound(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return;
		ch.Paused = false;
		ch.Complete = false;
		if (ch.IsStream) {
			Raylib.SeekMusicStream(ch.MusicStream, 0);
			Raylib.PlayMusicStream(ch.MusicStream);
		}
		else {
			Raylib.StopSound(ch.SoundAlias);
			Raylib.PlaySound(ch.SoundAlias);
		}
	}

	public bool ResumeSound(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Paused = false;
		if (ch.IsStream)
			Raylib.ResumeMusicStream(ch.MusicStream);
		else
			Raylib.ResumeSound(ch.SoundAlias);
		return true;
	}

	public void SetMasterVolume(float volume) {
		masterVolume = volume;
		Raylib.SetMasterVolume(volume);
	}

	public bool SetSoundPanning(in AudioPlaybackHandle handle, float panning) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Settings.Panning = panning;
		if (ch.IsStream)
			Raylib.SetMusicPan(ch.MusicStream, panning);
		else
			Raylib.SetSoundPan(ch.SoundAlias, panning);
		return true;
	}

	public bool SetSoundPitchControl(in AudioPlaybackHandle handle, float pitch) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Settings.Pitch = pitch;
		ch.Settings.TimeStretch = pitch;
		if (ch.IsStream)
			Raylib.SetMusicPitch(ch.MusicStream, pitch);
		else
			Raylib.SetSoundPitch(ch.SoundAlias, pitch);
		return true;
	}

	public bool SetSoundPitchScaling(in AudioPlaybackHandle handle, float pitchRatio) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Settings.Pitch = pitchRatio;
		if (ch.IsStream)
			Raylib.SetMusicPitch(ch.MusicStream, pitchRatio);
		else
			Raylib.SetSoundPitch(ch.SoundAlias, pitchRatio);
		return true;
	}

	public bool SetSoundPlayhead(in AudioPlaybackHandle handle, double playhead) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		if (!ch.IsStream) return false;
		Raylib.SeekMusicStream(ch.MusicStream, (float)playhead);
		return true;
	}

	public bool SetSoundTimeStretch(in AudioPlaybackHandle handle, float stretchRatio) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Settings.TimeStretch = stretchRatio;
		return false;
	}

	public bool SetSoundVolume(in AudioPlaybackHandle handle, float volume) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		ch.Settings.Volume = volume;
		ApplySettings(ch);
		return true;
	}

	public void Shutdown() {
		DestroyAllAudioClips();
		Raylib.CloseAudioDevice();
	}

	public long StopAllSounds() {
		long count = 0;
		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active) {
				InternalDestroyPlayback(i);
				count++;
			}
		}
		return count;
	}

	public bool StopSound(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return false;
		int idx = (int)(handle.Channel - 1);

		if (ch.Settings.DoNotAutoDestroy) {
			ch.Complete = true;
			ch.Paused = false;
			if (ch.IsStream)
				Raylib.StopMusicStream(ch.MusicStream);
			else
				Raylib.StopSound(ch.SoundAlias);
			return true;
		}

		InternalDestroyPlayback(idx);
		return true;
	}

	public long StopSounds(IAudioClip? clip) {
		if (clip == null) return 0;
		long count = 0;
		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch != null && ch.Active && ch.Clip == clip) {
				InternalDestroyPlayback(i);
				count++;
			}
		}
		return count;
	}

	public void Update() {
		HashSet<BaseAudioClip>? dirtyClips = null;
		foreach (var clip in allClips) {
			if (clip.IsVolumeDirty) {
				clip.RecalculateVolumeMultiplier();
				dirtyClips ??= [];
				dirtyClips.Add(clip);
			}
		}

		for (int i = 0; i < MAX_CHANNELS; i++) {
			var ch = channels[i];
			if (ch == null || !ch.Active) continue;

			if (dirtyClips != null && ch.Clip != null && dirtyClips.Contains(ch.Clip))
				ApplySettings(ch);

			if (ch.IsStream) {
				if (!ch.Settings.ManuallyUpdate)
					Raylib.UpdateMusicStream(ch.MusicStream);

				if (!ch.Paused && !Raylib.IsMusicStreamPlaying(ch.MusicStream) && !ch.Complete) {
					ch.Complete = true;
					if (!ch.Settings.DoNotAutoDestroy)
						InternalDestroyPlayback(i);
				}
			}
			else {
				if (!ch.Paused && !Raylib.IsSoundPlaying(ch.SoundAlias) && !ch.Complete) {
					ch.Complete = true;
					if (!ch.Settings.DoNotAutoDestroy)
						InternalDestroyPlayback(i);
				}
			}
		}
	}

	public void UpdatePlayback(in AudioPlaybackHandle handle) {
		var ch = GetChannel(in handle);
		if (ch == null) return;
		if (ch.IsStream)
			Raylib.UpdateMusicStream(ch.MusicStream);
	}
}