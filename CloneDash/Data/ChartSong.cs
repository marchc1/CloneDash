using CloneDash.Settings;

using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Raylib_cs;

using System.Collections.Concurrent;

namespace CloneDash.Data
{
	public delegate void ChartCoverAvailableToMainThreadFn(ChartCover? cover);
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

		public bool TryDifficultyInteger(int i, out int d) => int.TryParse(Difficulty(i), out d);
		public string Difficulty(int i) => i switch {
			1 => Difficulty1,
			2 => Difficulty2,
			3 => Difficulty3,
			4 => Difficulty4,
			5 => Difficulty5,
			_ => ""
		};


		protected IAudioClip? AudioTrack { get; set; }
		protected IAudioClip? DemoTrack { get; set; }
		protected ChartCover? CoverTexture { get; set; }
		protected Dictionary<int, ChartSheet> Sheets { get; set; } = [];

		protected void Clear() {
			audiosystem.DestroyAudioClip(AudioTrack);
			AudioTrack = null;
			// Not clearing the demo since i dont want a sudden jump
			// DemoTrack?.Dispose(); DemoTrack = null;
			Info = null;
			__gotCover = false;
			CoverTexture = null;
			chartCoverCallbacks.Clear();
			Sheets.Clear();
		}

		// These methods will be called when their respective data is not set. They are protected for that reason.

		protected object AsyncLock { get; } = new object();
		protected bool DeferringDemoToAsyncHandler { get; set; }


		public bool IsLoadingDemoAsync {
			get {
				lock (AsyncLock) {
					return DeferringDemoToAsyncHandler && DemoTrack == null;
				}
			}
		}

		protected abstract IAudioClip ProduceAudioTrack();
		protected abstract IAudioClip? ProduceDemoTrack();
		protected abstract void ProduceCover(ChartCoverAvailableToMainThreadFn callback);
		protected abstract ChartInfo? ProduceInfo();
		protected abstract ChartSheet ProduceSheet(int id);

		// Public facing methods for getting data
		public IAudioClip GetAudioTrack() {
			if (AudioTrack != null && IValidatable.IsValid(AudioTrack))
				return AudioTrack;

			AudioTrack = ProduceAudioTrack();
			AudioTrack.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			return AudioTrack;
		}

		public ChartInfo? GetInfo() {
			if (Info != null)
				return Info;

			Info = ProduceInfo();
			return Info;
		}

		public IAudioClip? GetDemoTrack() {
			if (DeferringDemoToAsyncHandler) {
				lock (AsyncLock) {
					return DemoTrack;
				}
			}
			if (__gotDemoTrack == false && DemoTrack != null && IValidatable.IsValid(AudioTrack))
				return DemoTrack;

			DemoTrack = ProduceDemoTrack();
			DemoTrack?.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			__gotDemoTrack = true;
			return DemoTrack;
		}

		// TODO: This all really sucks!
		readonly ConcurrentDictionary<object, ChartCoverAvailableToMainThreadFn> chartCoverCallbacks = [];
		public ChartCover? GetCoverWhenAvailable(object consumer) {
			ChartCover? cover = null;
			GetCoverWhenAvailable(consumer, (c) => cover = c);
			return cover;
		}
		public void GetCoverWhenAvailable(object consumer, ChartCoverAvailableToMainThreadFn fn) {
			if(CoverTexture != null) {
				fn(CoverTexture);
				return;
			}

			if (chartCoverCallbacks.ContainsKey(consumer))
				return;

			int count = chartCoverCallbacks.Count;
			chartCoverCallbacks[consumer] = fn;
			if (count == 0)
				Task.Run(StartRetrievingCover);
		}

		private void StartRetrievingCover() {
			ProduceCover((cover) => {
				CoverTexture = cover;
					foreach (var callback in chartCoverCallbacks)
						callback.Value(cover);
					chartCoverCallbacks.Clear();
			});
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

				if (AudioTrack != null) audiosystem.DestroyAudioClip(AudioTrack);
				if (DemoTrack != null) audiosystem.DestroyAudioClip(DemoTrack);
			});
		}
	}
}