using Raylib_cs;
using System.Text;

namespace Nucleus.Core
{
    /// <summary>
    /// Wrapper around <see cref="Raylib"/>'s sound functionality. <br></br><br></br>
    /// Has two benefits:<br></br>
    /// 1. Object-oriented approach, which also allows for doing some things Raylib doesnt allow for right now (ie. getting the volume)<br></br>
    /// 2. In the case of music loaded from byte arrays in memory, can store a copy of the byte array so it doesnt get GC'd pre-emptively
    /// </summary>
    public class MusicTrack : IDisposable
    {
        private Music Music;
        private byte[]? Array;

        private float _volume = 1f, _pitch = 1f, _pan = 0.5f;

        /// <summary>
        /// Get/sets the volume of the music track.<br></br>
        /// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
        /// </summary>
        public float Volume {
            get { return _volume; }
            set { _volume = value; Raylib.SetMusicVolume(Music, value); }
        }
        /// <summary>
        /// Get/sets the pitch of the music track. <b>This will cause the track to slow down. Not recommended to use it.</b> <br></br>
        /// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
        /// </summary>
        public float Pitch {
            get { return _pitch; }
            set { _pitch = value; Raylib.SetMusicPitch(Music, value); }
        }
        /// <summary>
        /// Get/sets the pan of the music track.<br></br>
        /// <b>Note:</b> This is internally tracked. Should be accurate, as long as nothing else touches the property outside of this field.
        /// </summary>
        public float Pan {
            get { return _pan; }
            set { _pan = value; Raylib.SetMusicPan(Music, value); }
        }
        /// <summary>
        /// Get how long this music track is, in seconds.
        /// </summary>
        public float Length => Raylib.GetMusicTimeLength(Music);
        /// <summary>
        /// Gets the current playhead of the music track, in seconds.<br></br><br></br>
        /// Note that this function is only accurate up to 0.01 seconds! Use the Conductors methods to get a semi-accurate playtime.
        /// </summary>
        public float Playhead {
            get { return Raylib.GetMusicTimePlayed(Music); }
            set { Raylib.SeekMusicStream(Music, value); }
        }

        /// <summary>
        /// Get/sets if the music track is paused or not.
        /// </summary>
        public bool Paused {
            get { return !Raylib.IsMusicStreamPlaying(Music); }
            set {
                if (value)
                    Raylib.PauseMusicStream(Music);
                else
                    Raylib.ResumeMusicStream(Music);
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
            get { return Music.Looping; }
            set { Music.Looping = value; }
        }

        /// <summary>
        /// Is the track done playing
        /// </summary>
        public bool Complete => Playhead == Length;

        public void Dispose() {
            Raylib.UnloadMusicStream(Music);
        }



        public static MusicTrack LoadFromFile(string file, bool autoplay = false) {
            if (!File.Exists(file))
                throw new FileNotFoundException($"File '{file}' does not exist.");

            var music = new MusicTrack();
            music.Music = Raylib.LoadMusicStream(file);
            Raylib.PlayMusicStream(music.Music);

            if (!autoplay)
                Raylib.PauseMusicStream(music.Music);

            return music;
        }

        public static MusicTrack LoadFromMemory(byte[] bytearray, bool autoplay = false) {
            var music = new MusicTrack();
            music.Array = bytearray; // Holds a reference to the raw data, so it doesnt get GC'd

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

            music.Music = Raylib.LoadMusicStreamFromMemory(fileExtension, bytearray);

            Raylib.PlayMusicStream(music.Music);

            if (!autoplay)
                Raylib.PauseMusicStream(music.Music);

            return music;
        }

        public void Update() => Raylib.UpdateMusicStream(Music);
    }

    public static class AudioSystem
    {
        public static Dictionary<string, Sound> LoadedSounds { get; private set; } = new();

        public static void Reset() {
            foreach (var kvp in LoadedSounds)
                Raylib.UnloadSound(kvp.Value);
            LoadedSounds.Clear();
        }

        // weird bug where this stops working sometimes?

        /// <summary>
        /// Caches and plays sounds from files.
        /// </summary>
        /// <param name="soundpath"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="pan"></param>
        /// <returns></returns>
        public static Sound PlaySound(string soundpath, float volume = 1f, float pitch = 1f, float pan = 0.5f) {
            if (!LoadedSounds.ContainsKey(soundpath))
                LoadedSounds[soundpath] = Raylib.LoadSound(soundpath);

            var sound = LoadedSounds[soundpath];

            Raylib.StopSound(sound);
            Raylib.SetSoundVolume(sound, volume);
            Raylib.SetSoundPitch(sound, pitch);
            Raylib.SetSoundPan(sound, pan);

            Raylib.PlaySound(sound);

            //var ready = Raylib.IsSoundReady(sound);
            //var playing = Raylib.IsSoundPlaying(sound);

            return sound;
        }
        public static void UnloadSound(string name) {
            if (!LoadedSounds.ContainsKey(name))
                return;

            LoadedSounds.Remove(name);
            Raylib.UnloadSound(LoadedSounds[name]);
        }
    }
}
