using CloneDash.Settings;

using Nucleus;
using Nucleus.Audio;

using Raylib_cs;

namespace CloneDash.Data
{
	public abstract class ChartSong
	{
		private bool __gotDemoTrack = false;
		private bool __gotCover = false;
		// Data cache, produced objects get stored here

		public ChartInfo? Info { get; set; }

		public string Name { get; set; } = "";
		public string Author { get; set; } = "";

		public string Difficulty1 => GetInfo()?.Difficulty1 ?? "";
		public string Difficulty2 => GetInfo()?.Difficulty2 ?? "";
		public string Difficulty3 => GetInfo()?.Difficulty3 ?? "";
		public string Difficulty4 => GetInfo()?.Difficulty4 ?? "";
		public string Difficulty5 => GetInfo()?.Difficulty5 ?? "";

		public string Difficulty(int i) => i switch {
			1 => Difficulty1,
			2 => Difficulty2,
			3 => Difficulty3,
			4 => Difficulty4,
			5 => Difficulty5,
			_ => ""
		};


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
			AudioTrack.BindVolumeToConVar(AudioSettings.clonedash_music_volume);
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
			DemoTrack?.BindVolumeToConVar(AudioSettings.clonedash_music_volume);
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

		public virtual bool ShouldReproduceSheet(int difficulty) => false;

		public ChartSheet GetSheet(int difficulty) {
			if (Sheets.TryGetValue(difficulty, out var sheet) && !ShouldReproduceSheet(difficulty))
				return sheet;

			Sheets[difficulty] = ProduceSheet(difficulty);
			return Sheets[difficulty];
		}

		~ChartSong() {
			MainThread.RunASAP(() => {
				if (__gotCover && CoverTexture != null)
					Raylib.UnloadTexture(CoverTexture.Texture);

				if (AudioTrack != null) AudioTrack.Dispose();
				if (DemoTrack != null) DemoTrack.Dispose();
			});
		}
	}
}