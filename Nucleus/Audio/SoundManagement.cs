using System.Text;
using Nucleus.Core;
using Nucleus.ManagedMemory;
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

		public Sound LoadSoundFromFile(string filepath, bool localToAudio = true) {
			filepath = localToAudio ? Filesystem.Resolve(filepath, "audio") : filepath;

			if (LoadedSoundsFromFile.TryGetValue(filepath, out Sound? sndFromFile)) return sndFromFile;

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

			Raylib.StopSound(sound);
			Raylib.SetSoundVolume(sound, volume);
			Raylib.SetSoundPitch(sound, pitch);
			Raylib.SetSoundPan(sound, pan);

			Raylib.PlaySound(sound);

			//var ready = Raylib.IsSoundReady(sound);
			//var playing = Raylib.IsSoundPlaying(sound);

			Sounds.Add(sound);
			return sound;
		}


		public MusicTrack LoadMusicFromFile(string file, bool autoplay = false) {
			if (!File.Exists(file))
				throw new FileNotFoundException($"File '{file}' does not exist.");
			unsafe {
				var music = new MusicTrack(this, Raylib.LoadMusicStream(file), true, null);

				if (!autoplay)
					music.Playing = false;

				Sounds.Add(music);
				return music;
			}
		}

		public MusicTrack LoadMusicFromMemory(byte[] bytearray, bool autoplay = false) {
			// Automatically determines the file extension from the bytearray
			string fileExtension = "";
			byte b1 = bytearray[0], b2 = bytearray[1], b3 = bytearray[2], b4 = bytearray[3];
			string header = Encoding.ASCII.GetString([b1, b2, b3, b4]);
			switch (header) {
				case "RIFF": //.wav
					fileExtension = ".wav";
					break;

				case "OggS": //.ogg
					fileExtension = ".ogg";
					break;

				default: // There should be a better way to find MP3 files... does Raylib support other file types too?
					fileExtension = ".mp3";
					break;
			}
			Music m;
			unsafe {
				var alloc = Raylib.New<byte>(bytearray.Length);
				for (int i = 0; i < bytearray.Length; i++) {
					alloc[i] = bytearray[i];
				}
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
