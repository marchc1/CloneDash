using CloneDash.Game;
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

        public string Difficulty1 => GetInfo()?.Difficulty1 ?? "";
        public string Difficulty2 => GetInfo()?.Difficulty2 ?? "";
        public string Difficulty3 => GetInfo()?.Difficulty3 ?? "";
        public string Difficulty4 => GetInfo()?.Difficulty4 ?? "";
        public string Difficulty5 => GetInfo()?.Difficulty5 ?? "";

        protected MusicTrack? AudioTrack { get; set; }
        protected MusicTrack? DemoTrack { get; set; }
        protected ChartCover? CoverTexture { get; set; }
        protected Dictionary<int, ChartSheet> Sheets { get; set; } = [];

		protected void Clear() {
			AudioTrack?.Dispose(); AudioTrack = null;
			// Not clearing the demo since i dont want a sudden jump
			// DemoTrack?.Dispose(); DemoTrack = null;
			Info = null;
			__gotCover = false;
			CoverTexture = null;
			Sheets.Clear();

			DeferringCoverToAsyncHandler = false;
		}

		// These methods will be called when their respective data is not set. They are protected for that reason.

		protected object AsyncLock { get; } = new object();
		protected bool DeferringDemoToAsyncHandler { get; set; }
		protected bool DeferringCoverToAsyncHandler { get; set; }

		public bool IsLoadingDemoAsync {
			get {
				lock (AsyncLock) {
					return DeferringDemoToAsyncHandler && DemoTrack == null;
				}
			}
		}
		public bool IsLoadingCoverAsync {
			get {
				lock (AsyncLock) {
					return DeferringCoverToAsyncHandler && CoverTexture == null;
				}
			}
		}

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
			if (DeferringDemoToAsyncHandler) {
				lock (AsyncLock) {
					return DemoTrack;
				}
			}
			if (__gotDemoTrack == false && DemoTrack != null && IValidatable.IsValid(AudioTrack))
                return DemoTrack;

            DemoTrack = ProduceDemoTrack();
            __gotDemoTrack = true;
            return DemoTrack;
        }

        public ChartCover? GetCover() {
			if (DeferringCoverToAsyncHandler) {
				lock (AsyncLock) {
					return CoverTexture;
				}
			}

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

		internal static CD_GameLevel LoadLevel(ChartSong song, int mapID, bool autoplay) {
			Interlude.Begin($"Loading '{song.Name}'...");

			var sheet = song.GetSheet(mapID);
			var workingLevel = new CD_GameLevel(sheet);
			if (workingLevel == null) return workingLevel;
			EngineCore.LoadLevel(workingLevel, autoplay);
			MainThread.RunASAP(Interlude.End, ThreadExecutionTime.AfterFrame);
			return workingLevel;
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
