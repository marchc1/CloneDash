using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Core;
using Nucleus.Engine;
using Raylib_cs;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus.ManagedMemory
{
    public interface ISound : IManagedMemory
    {

    }

    public class MusicTrack : ISound
    {
        SoundManagement? parent;
        Music underlying;
        bool selfDisposing = true;

        public unsafe MusicTrack(SoundManagement? parent, Music underlying, bool selfDisposing = true, byte* memoryBound = null) {
            this.parent = parent;
            this.underlying = underlying;
            this.selfDisposing = selfDisposing;
            __isMemoryBound = memoryBound;
            Raylib.PlayMusicStream(underlying);
        }
        private bool disposedValue;
        public bool IsValid() => !disposedValue;
        public ulong UsedBits =>
            // size * rate * channels = bits per second
            (ulong)((underlying.Stream.SampleSize * underlying.Stream.SampleRate * underlying.Stream.Channels)
            * Raylib.GetMusicTimeLength(underlying));

        private unsafe byte* __isMemoryBound = null;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue && selfDisposing) {
                MainThread.RunASAP(() => {
                    Raylib.UnloadMusicStream(underlying);
                    unsafe {
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
            set { _volume = value; Raylib.SetMusicVolume(underlying, value); }
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

        public void Update() => Raylib.UpdateMusicStream(underlying);
    }
    public class Sound(SoundManagement? parent, Raylib_cs.Sound underlying, bool selfDisposing = true) : ISound
    {
        private bool disposedValue;
        public bool IsValid() => !disposedValue;
        public ulong UsedBits =>
            // size * rate * channels = bits per second
            (underlying.Stream.SampleSize * underlying.Stream.SampleRate * underlying.Stream.Channels)
            / underlying.FrameCount; // this is wrong...

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue && selfDisposing) {
                MainThread.RunASAP(() => {
                    Raylib.UnloadSound(underlying);
                    parent?.EnsureISoundRemoved(this);
                }, ThreadExecutionTime.BeforeFrame);
                disposedValue = true;
            }
        }
        ~Sound() { if (selfDisposing) Dispose(false); }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private Raylib_cs.Sound Underlying => underlying;
        public static implicit operator Raylib_cs.Sound(Sound self) => self.Underlying;
    }
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

    }
}
