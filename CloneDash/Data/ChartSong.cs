using Nucleus;
using Nucleus.Audio;
using Raylib_cs;

namespace CloneDash.Data
{
	public abstract class ChartSong {
        private bool __gotDemoTrack = false;
        private bool __gotCover = false;
        // Data cache, produced objects get stored here

        public ChartInfo? Info { get; set; }

        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public decimal BPM => GetInfo().BPM;

        public string Difficulty1 => GetInfo().Difficulty1;
        public string Difficulty2 => GetInfo().Difficulty2;
        public string Difficulty3 => GetInfo().Difficulty3;
        public string Difficulty4 => GetInfo().Difficulty4;

        protected MusicTrack? AudioTrack { get; set; }
        protected MusicTrack? DemoTrack { get; set; }
        protected ChartCover? CoverTexture { get; set; }
        protected Dictionary<int, ChartSheet> Sheets { get; set; } = [];

        // These methods will be called when their respective data is not set. They are protected for that reason.

        protected abstract MusicTrack ProduceAudioTrack();
        protected abstract MusicTrack? ProduceDemoTrack();
        protected abstract ChartCover? ProduceCover();
        protected abstract ChartInfo? ProduceInfo();
        protected abstract ChartSheet ProduceSheet(int id);

        // Public facing methods for getting data
        public MusicTrack GetAudioTrack() {
            if (AudioTrack != null && IValidatable.IsValid(AudioTrack))
                return AudioTrack;

            AudioTrack = ProduceAudioTrack();
            return AudioTrack;
        }

        public ChartInfo GetInfo() {
            if (Info != null)
                return Info;

            Info = ProduceInfo();
            return Info;
        }

        public MusicTrack? GetDemoTrack() {
            if (__gotDemoTrack == false && DemoTrack != null && IValidatable.IsValid(AudioTrack))
                return DemoTrack;

            DemoTrack = ProduceDemoTrack();
            __gotDemoTrack = true;
            return DemoTrack;
        }

        public ChartCover? GetCover() {
            if (__gotCover == true)
                return CoverTexture;

            CoverTexture = ProduceCover();
            __gotCover = true;
            return CoverTexture;
        }

        public ChartSheet GetSheet(int difficulty) {
            if (Sheets.TryGetValue(difficulty, out var sheet))
                return sheet;

            Sheets[difficulty] = ProduceSheet(difficulty);
            return Sheets[difficulty];
        }

        ~ChartSong() {
            MainThread.RunASAP(() => {
                if (__gotCover && CoverTexture != null) 
                    Raylib.UnloadTexture(CoverTexture.Texture);
                
                if(AudioTrack != null) AudioTrack.Dispose();
                if(DemoTrack != null) DemoTrack.Dispose();
                
            });
        }
    }
}
