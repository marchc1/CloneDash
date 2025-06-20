using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using Raylib_cs;

namespace Nucleus.Audio
{
	public class MusicTrack : ISound
	{
		SoundManagement? parent;
		Music underlying;
		bool selfDisposing = true;

		/// <summary>
		/// Note: If anything ever becomes multithreaded and uses this stuff it will all crash and burn
		/// </summary>
		public static MusicTrack? Current { get; private set; }

		public delegate void OnProcessDelegate(MusicTrack self, Span<float> frames);
		public event OnProcessDelegate? Processing;

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static unsafe void FUCKFUCKFUCKFUCK(void* buffer, uint frames) {
			float* floatBuffer = (float*)buffer;
			Span<float> stackAllocatedBuffer = stackalloc float[(int)frames];
			for (int i = 0; i < frames; i++) {
				stackAllocatedBuffer[i] = floatBuffer[i];
			}

			Current?.Processing?.Invoke(Current, stackAllocatedBuffer);
		}


		private float __volumeMultiplier = 1f;
		List<ConVar> boundConVars = [];
		private void recalculateVolumeMultiplier() {
			__volumeMultiplier = 1;
			if (boundConVars.Count == 0)
				return;

			foreach (var cv in boundConVars)
				__volumeMultiplier *= (float)cv.GetDouble();

			Raylib.SetMusicVolume(underlying, _volume * __volumeMultiplier);
		}
		public void BindVolumeToConVar(ConVar cv) {
			boundConVars.Add(cv);
			cv.OnChange += Cv_OnChange;
			recalculateVolumeMultiplier();
		}
		private void Cv_OnChange(ConVar self, CVValue old, CVValue now) => recalculateVolumeMultiplier();


		public unsafe MusicTrack(SoundManagement? parent, Music underlying, bool selfDisposing = true, byte* memoryBound = null) {
			this.parent = parent;
			this.underlying = underlying;
			this.selfDisposing = selfDisposing;
			__isMemoryBound = memoryBound;
			Raylib.PlayMusicStream(underlying);
			Raylib.AttachAudioStreamProcessor(underlying.Stream, &FUCKFUCKFUCKFUCK);
			recalculateVolumeMultiplier();
		}
		private bool disposedValue;
		public bool IsValid() => !disposedValue;
		public ulong UsedBits =>
			// size * rate * channels = bits per second
			(ulong)(underlying.Stream.SampleSize * underlying.Stream.SampleRate * underlying.Stream.Channels
			* Raylib.GetMusicTimeLength(underlying));

		private unsafe byte* __isMemoryBound = null;

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue && selfDisposing) {
				MainThread.RunASAP(() => {
					Raylib.UnloadMusicStream(underlying);
					unsafe {
						foreach (var cv in boundConVars)
							cv.OnChange -= Cv_OnChange;

						if (__isMemoryBound != null)
							Raylib.MemFree(__isMemoryBound);
					}
					parent?.EnsureISoundRemoved(this);
				}, ThreadExecutionTime.BeforeFrame);
				disposedValue = true;
			}
		}
		~MusicTrack() { if (selfDisposing) Dispose(false); }
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private float _volume = 1f, _pitch = 1f, _pan = 0.5f;

		/// <summary>
		/// Get/sets the volume of the music track.<br></br>
		/// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
		/// </summary>
		public float Volume {
			get { return _volume; }
			set { 
				_volume = value; 
				Raylib.SetMusicVolume(underlying, value * __volumeMultiplier); 
			}
		}
		/// <summary>
		/// Get/sets the pitch of the music track. <b>This will cause the track to slow down. Not recommended to use it.</b> <br></br>
		/// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
		/// </summary>
		public float Pitch {
			get { return _pitch; }
			set { _pitch = value; Raylib.SetMusicPitch(underlying, value); }
		}
		/// <summary>
		/// Get/sets the pan of the music track.<br></br>
		/// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
		/// </summary>
		public float Pan {
			get { return _pan; }
			set { _pan = value; Raylib.SetMusicPan(underlying, value); }
		}
		/// <summary>
		/// Get how long this music track is, in seconds.
		/// </summary>
		public float Length => Raylib.GetMusicTimeLength(underlying);
		/// <summary>
		/// Gets the current playhead of the music track, in seconds.<br></br><br></br>
		/// Note that this function is only accurate up to 0.01 seconds! Use the Conductors methods to get a semi-accurate playtime.
		/// </summary>
		public float Playhead {
			get { return Raylib.GetMusicTimePlayed(underlying); }
			set { Raylib.SeekMusicStream(underlying, value); }
		}

		/// <summary>
		/// Get/sets if the music track is paused or not.
		/// </summary>
		public bool Paused {
			get { return !Raylib.IsMusicStreamPlaying(underlying); }
			set {
				if (value)
					Raylib.PauseMusicStream(underlying);
				else
					Raylib.ResumeMusicStream(underlying);
			}
		}

		/// <summary>
		/// Get/sets if the music track is playing or not.
		/// </summary>
		public bool Playing {
			get { return !Paused; }
			set { Paused = !value; }
		}

		/// <summary>
		/// Does the music loop when the song ends?
		/// </summary>
		public bool Loops {
			get { return underlying.Looping; }
			set { underlying.Looping = value; }
		}

		/// <summary>
		/// Is the track done playing
		/// </summary>
		public bool Complete => Playhead == Length;

		public void Update() {
			Current = this;
			Raylib.UpdateMusicStream(underlying);
		}

		public void Restart() {
			Raylib.SeekMusicStream(underlying, 0);
		}
	}
}
