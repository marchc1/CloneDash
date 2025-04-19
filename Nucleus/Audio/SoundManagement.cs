using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Util;
using Raylib_cs;

namespace Nucleus.Audio
{
	[MarkForStaticConstruction]
	public class SoundManagement : IManagedMemory
	{
		private List<ISound> Sounds = [];
		private bool disposedValue;
		public ulong UsedBits {
			get {
				ulong ret = 0;

				foreach (var snd in Sounds) {
					ret += snd.UsedBits;
				}

				return ret;
			}
		}
		public bool IsValid() => !disposedValue;

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				lock (Sounds) {
					foreach (ISound t in Sounds) {
						t.Dispose();
					}
					disposedValue = true;
				}
			}
		}

		~SoundManagement() {
			Dispose(disposing: true);
		}

		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private Dictionary<string, MusicTrack> LoadedMusicTracksFromFile = [];
		private Dictionary<MusicTrack, string> LoadedFilesFromMusicTrack = [];

		private Dictionary<string, Sound> LoadedSoundsFromFile = [];
		private Dictionary<Sound, string> LoadedFilesFromSound = [];

		public Sound LoadSoundFromFile(string pathID, string filepath) {
			if (LoadedSoundsFromFile.TryGetValue(filepath, out Sound? soundFromFile)) return soundFromFile;

			Sound snd = new(this, Filesystem.ReadSound(pathID, filepath), true);

			LoadedSoundsFromFile.Add(filepath, snd);
			LoadedFilesFromSound.Add(snd, filepath);
			Sounds.Add(snd);

			return snd;
		}
		public Sound LoadSoundFromFile(string filepath, bool localToAudio = true) {
			if(localToAudio)
				return LoadSoundFromFile("audio", filepath);

			Sound snd = new(this, Raylib.LoadSound(filepath), true);
			LoadedSoundsFromFile.Add(filepath, snd);
			LoadedFilesFromSound.Add(snd, filepath);
			Sounds.Add(snd);

			return snd;
		}

		public void EnsureISoundRemoved(ISound isnd) {
			switch (isnd) {
				case Sound snd:
					if (LoadedFilesFromSound.TryGetValue(snd, out var sndFilepath)) {
						LoadedSoundsFromFile.Remove(sndFilepath);
						LoadedFilesFromSound.Remove(snd);
						Sounds.Remove(snd);

						snd.Dispose();
					}
					break;
				case MusicTrack mus:
					if (LoadedFilesFromMusicTrack.TryGetValue(mus, out var musFilepath)) {
						LoadedMusicTracksFromFile.Remove(musFilepath);
						LoadedFilesFromMusicTrack.Remove(mus);
						Sounds.Remove(mus);

						mus.Dispose();
					}
					break;
			}
		}

		public Sound PlaySound(string soundpath, bool localToAudio = true, float volume = 1f, float pitch = 1f, float pan = 0.5f) {
			var sound = LoadSoundFromFile(soundpath, localToAudio);
			return PlaySound(sound, volume, pitch, pan);
		}

		public Sound PlaySound(Sound sound, float volume = 1f, float pitch = 1f, float pan = 0.5f) {
			Raylib.StopSound(sound);
			Raylib.SetSoundVolume(sound, volume);
			Raylib.SetSoundPitch(sound, pitch);
			Raylib.SetSoundPan(sound, pan);

			Raylib.PlaySound(sound);

			//var ready = Raylib.IsSoundReady(sound);
			//var playing = Raylib.IsSoundPlaying(sound);

			return sound;
		}

		public MusicTrack LoadMusicFromFile(string pathID, string file, bool autoplay = false) {
			unsafe {
				var data = Filesystem.ReadAllBytes(pathID, file);
				if (data == null) {
					throw new Exception();
				}
				return LoadMusicFromMemory(data, autoplay);
			}
		}

		public MusicTrack LoadMusicFromFile(string file, bool autoplay = false) => LoadMusicFromFile("audio", file, autoplay);

		public const int MUSIC_HEADER_RIFF = ('R' << 24) + ('I' << 16) + ('F' << 8) + 'F';
		public const int MUSIC_HEADER_OGGS = ('O' << 24) + ('g' << 16) + ('g' << 8) + 'S';
		public const int MUSIC_HEADER_ID3 = ('I' << 24) + ('D' << 16) + ('3' << 8) + 0x03;

		public const string MUSIC_HEADER_RIFF_EXTENSION = ".wav";
		public const string MUSIC_HEADER_OGGS_EXTENSION = ".ogg";
		public const string MUSIC_HEADER_ID3_EXTENSION = ".mp3";

		// Automatically determines the file extension from the bytearray
		public MusicTrack LoadMusicFromMemory(byte[] bytearray, bool autoplay = false) {
			if (bytearray.Length < 4) throw new Exception("Can't even determine the file type... file < 4 bytes!");

			Span<byte> byteHeader = [bytearray[3], bytearray[2], bytearray[1], bytearray[0]];
			Span<int> headerCast = MemoryMarshal.Cast<byte, int>(byteHeader);
			string fileExtension = headerCast[0] switch {
				MUSIC_HEADER_RIFF => MUSIC_HEADER_RIFF_EXTENSION,
				MUSIC_HEADER_OGGS => MUSIC_HEADER_OGGS_EXTENSION,
				MUSIC_HEADER_ID3 => MUSIC_HEADER_ID3_EXTENSION,
				_ => MUSIC_HEADER_ID3_EXTENSION,
			};

			Music m;
			unsafe {
				var alloc = bytearray.ToUnmanagedPointer();
				using var fileTypeNative = fileExtension.ToAnsiBuffer();
				m = Raylib.LoadMusicStreamFromMemory(fileTypeNative.AsPointer(), alloc, bytearray.Length);
				MusicTrack music = new(this, m, true, alloc);

				if (!autoplay)
					music.Playing = false;

				Sounds.Add(music);
				return music;
			}
		}

		public static ConVar snd_volume = ConVar.Register("snd_volume", "1.0", ConsoleFlags.Saved, "Overall sound volume.", 0, 10f, (cv, o, n) => {
			Raylib.SetMasterVolume(n.AsFloat ?? 1);
		});
	}
}
