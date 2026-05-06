using CloneDash.Common.Data;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Game
{
	public class Conductor : LogicalEntity
	{
		public Conductor() {
			currentInaccurateTime = (float)-PreStartTime;
		}
		public List<TempoChange> TempoChanges { get; private set; } = [];
		public List<TimeSignatureChange> TimeSignatureChanges { get; private set; } = [];

		public void AddTempoChange(double time, int beat, double bpm) => TempoChanges.Add(new(time, beat, bpm));
		public void AddTimeSignatureChange(int beat, float percentage) => TimeSignatureChanges.Add(new(beat, percentage));

		// Used for seeking the playhead.
		double? forcedTime;
		public void ForceTimeTo(double time) => forcedTime = time;
		public void RemoveForcedTime() => forcedTime = null;

		/// <summary>
		/// The current music playhead, adjusted for inaccuracies.
		/// </summary>
		public double Time => forcedTime ?? currentInaccurateTime;

		/// <summary>
		/// Offsets the conductor time
		/// </summary>
		public double PreStartTime { get; set; } = CommandLine().CheckParm("-mdbmsc", out _) ? 0 : 5;

		public double BPM => GetTempoAtTime(Time);

		// What was the last music time that Raylib reported?
		private float lastTimeFromFunctionCall = 0;
		// What is the current time + the frametime values
		private double currentInaccurateTime = 0;

		/// <summary>
		/// Invalidates the current time. The time is determined by inaccurate music playhead + frametime delta updates until a new time
		/// update comes in from the music track. This method forces the current inaccurate time to music playhead immediately.
		/// </summary>
		public void InvalidateTime() {
			var game = Level.As<DashGameLevel>();
			if (!game.Music.IsValid()) return;
			audiosystem.GetSoundPlayhead(game.Music, out currentInaccurateTime);
		}

		private class CD_Conductor_UIBar : Element
		{
			public float Playhead { get; set; }
			public float Duration { get; set; }
			public float Completion => Playhead / Duration;

			public double XToSeconds(float x) => (x / RenderBounds.W) * Duration;

			public delegate void Mouse();

			public event Mouse? DragStart;
			public event Mouse? DragUpdate;
			public event Mouse? DragEnd;

			public override void MouseClick(FrameState state, ButtonCode button) {
				if (button != ButtonCode.MouseLeft) return;
				base.MouseClick(state, button);
				DragStart?.Invoke();
			}

			public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
				if (!state.Mouse.Mouse1Held) return;
				base.MouseDrag(self, state, delta);
				DragUpdate?.Invoke();
			}

			public override void MouseReleasedOrLost(Element self, FrameState state, ButtonCode button) {
				if (button != ButtonCode.MouseLeft) return;
				base.MouseRelease(self, state, button);
				DragEnd?.Invoke();
			}

			public override void Paint(float width, float height) {
				Graphics2D.SetDrawColor(230, 235, 255, Depressed ? 100 : 255);
				Graphics2D.DrawRectangle(0, 0, width * Completion, height);

				if (Depressed) {
					Graphics2D.SetDrawColor(230, 235, 255);
					Graphics2D.DrawRectangle(0, 2, GetMousePos().X, height - 4);
				}
			}
		}

		private CD_Conductor_UIBar UIBar;

		public override void Initialize() {
			UIBar = Level.UI.Add<CD_Conductor_UIBar>();
			UIBar.Dock = Dock.Bottom;
			UIBar.Size = new(0, 8);

			UIBar.DragStart += UIBar_DragStart;
			UIBar.DragUpdate += UIBar_DragUpdate;
			UIBar.DragEnd += UIBar_DragEnd;
		}

		private bool wasPaused = false;
		private double? dragSeconds;

		private double uiSeconds => Math.Clamp(UIBar.XToSeconds(UIBar.GetMousePos().X), 0, audiosystem.GetPlaybackDuration(Level.As<DashGameLevel>().Music));

		private void UIBar_DragUpdate() {
			var game = Level.As<DashGameLevel>();
			// if (!game.AutoPlayer.Enabled) return;

			dragSeconds = uiSeconds;
		}

		private void UIBar_DragEnd() {
			var game = Level.As<DashGameLevel>();
			// if (!game.AutoPlayer.Enabled) return;

			if (dragSeconds != null)
				game.SeekTo(dragSeconds.Value);

			if (!wasPaused)
				game.ForceUnpause();

			dragSeconds = null;
		}

		private void UIBar_DragStart() {
			var game = Level.As<DashGameLevel>();
			// if (!game.AutoPlayer.Enabled) return;

			wasPaused = game.Paused;
			if (!wasPaused)
				game.ForcePause();

			dragSeconds = uiSeconds;
		}

		public float GetTimeSignatureAtMeasure(int measure) {
			for (int i = TimeSignatureChanges.Count - 1; i >= 0; i--) {
				if (TimeSignatureChanges[i].Beat <= measure)
					return TimeSignatureChanges[i].Percentage;
			}

			return 1.0f; // default 4/4
		}

		public float GetTimeSignatureAtTime(double time) {
			int measure = (int)SecondsToMeasure(time);
			return GetTimeSignatureAtMeasure(measure);
		}

		/// <summary>
		/// Converts a time in seconds to a fractional beat position.
		/// </summary>
		public double SecondsToBeat(double time) {
			if (TempoChanges.Count == 0) return 0;
			if (time <= 0) return 0;

			for (int i = 0; i < TempoChanges.Count; i++) {
				var change = TempoChanges[i];
				bool isLast = i == TempoChanges.Count - 1;
				var nextChange = isLast ? change : TempoChanges[i + 1];

				if (isLast || nextChange.Time > time) {
					double elapsed = time - change.Time;
					double secondsPerBeat = 240.0 / change.BPM;
					return change.Beat + (elapsed / secondsPerBeat);
				}
			}

			return 0;
		}

		/// <summary>
		/// Converts a time in seconds to a fractional measure position.
		/// </summary>
		private double SecondsToMeasure(double time) {
			double beat = SecondsToBeat(time);
			return BeatToMeasure(beat);
		}

		/// <summary>
		/// Converts a beat position to a fractional measure position,
		/// accounting for time signature changes.
		/// </summary>
		private double BeatToMeasure(double beat) {
			if (TimeSignatureChanges.Count == 0)
				return beat;

			double measure = 0;
			double currentBeat = 0;

			for (int i = 0; i < TimeSignatureChanges.Count; i++) {
				double beatsPerMeasure = TimeSignatureChanges[i].Percentage;
				double nextChangeBeat = (i + 1 < TimeSignatureChanges.Count)
					? TimeSignatureChanges[i + 1].Beat
					: double.MaxValue;

				if (beat < nextChangeBeat) {
					measure += (beat - currentBeat) / beatsPerMeasure;
					return measure;
				}

				measure += (nextChangeBeat - currentBeat) / beatsPerMeasure;
				currentBeat = nextChangeBeat;
			}

			return measure;
		}

		public TempoChange GetTempoChangeAtTime(double time) {
			if (TempoChanges.Count == 0)
				throw new Exception("No tempo changes found in DashGame (likely a DashSheet import error)");

			if (time <= 0)
				return TempoChanges[0];

			for (int i = 0; i < TempoChanges.Count; i++) {
				var tempoChange = TempoChanges[i];
				if (tempoChange.Time > time)
					return TempoChanges[i - 1];
			}

			return TempoChanges.Last();
		}

		public double GetTempoAtTime(double time) => GetTempoChangeAtTime(time).BPM;

		public bool firstTick = true;
		private double lastTime;
		public override void Think(FrameState frameState) {
			lastTime = Time;
			var game = Level.As<DashGameLevel>();
			Level.AddDebugString("Conductor Time", Time);

			var speed = game.GetSpeed();

			if (firstTick) {
				currentInaccurateTime = (float)-PreStartTime;
			}
			else if (currentInaccurateTime < 0) {
				var ft = EngineCore.FrameTime;
				if (ft > 0.5)
					return;

				currentInaccurateTime += ft * speed;
			}
			else if (game.Music.IsValid()) {
				audiosystem.UpdatePlayback(game.Music);
				audiosystem.GetSoundPlayhead(game.Music, out double now);
				bool paused = audiosystem.IsPlaybackPaused(game.Music);

				if (lastTimeFromFunctionCall != now && !paused) {
					lastTimeFromFunctionCall = (float)now;
					currentInaccurateTime = now;
				}
				else {
					if (!paused)
						currentInaccurateTime += EngineCore.Level.CurtimeDelta * speed;
				}
			}
			else {
				currentInaccurateTime += EngineCore.Level.CurtimeDelta * speed;
			}

			if (game.Music.IsValid()) {
				audiosystem.GetSoundPlayhead(game.Music, out double now);
				UIBar.Playhead = (float)now;
				UIBar.Duration = (float)audiosystem.GetPlaybackDuration(game.Music);
			}
			firstTick = false;
			TimeDelta = Time - lastTime;
		}

		public double TimeDelta { get; private set; }

		/// <summary>
		/// Returns how long 1/<paramref name="division"/> of a note takes, in seconds. By default, <paramref name="division"/> is set to 4, which is a quarter note.
		/// </summary>
		/// <param name="division"></param>
		/// <returns></returns>
		public float NoteDivisorToSeconds(float division = 4) => (60 / (float)BPM) * (4 / division);

		/// <summary>
		/// Returns a value between zero to one, where zero is the current beat starting, and one is the current beat ending.<br></br>
		/// <paramref name="division"/> is by default set to 4, which means you'll get a 0-1 value for each quarter note.
		/// </summary>
		/// <param name="division"></param>
		/// <returns></returns>
		public float NoteDivisorRealtime(float division = 4) {
			var div2sec = NoteDivisorToSeconds(division);
			if (float.IsInfinity(div2sec))
				div2sec = 0.5f;

			return ((float)NMath.Modulo(Time, div2sec)) / div2sec;
		}

		/// <summary>
		/// Converts a beat position to a time in seconds,
		/// walking through tempo changes to accumulate elapsed time.
		/// </summary>
		public double BeatToSeconds(double beat) {
			if (TempoChanges.Count == 0) return 0;
			if (beat <= 0) return 0;

			for (int i = 0; i < TempoChanges.Count; i++) {
				var change = TempoChanges[i];
				bool isLast = i == TempoChanges.Count - 1;
				var nextChange = isLast ? change : TempoChanges[i + 1];

				if (isLast || nextChange.Beat > beat) {
					double beatsElapsed = beat - change.Beat;
					double secondsPerBeat = 240.0 / change.BPM;
					return change.Time + (beatsElapsed * secondsPerBeat);
				}
			}

			return 0;
		}

		/// <summary>
		/// Converts a measure position to a time in seconds,
		/// by first resolving the measure to a beat, then converting to seconds.
		/// </summary>
		public double MeasureToSeconds(double measure) {
			double beat = MeasureToBeat(measure);
			return BeatToSeconds(beat);
		}

		/// <summary>
		/// Converts a fractional measure position to a beat position,
		/// accounting for time signature changes.
		/// </summary>
		private double MeasureToBeat(double measure) {
			if (TimeSignatureChanges.Count == 0)
				return measure;

			double currentMeasure = 0;
			double currentBeat = 0;

			for (int i = 0; i < TimeSignatureChanges.Count; i++) {
				double beatsPerMeasure = TimeSignatureChanges[i].Percentage;
				double nextChangeBeat = (i + 1 < TimeSignatureChanges.Count)
					? TimeSignatureChanges[i + 1].Beat
					: double.MaxValue;

				double measuresInThisRegion = (nextChangeBeat - currentBeat) / beatsPerMeasure;

				if (measure < currentMeasure + measuresInThisRegion) {
					double measuresElapsed = measure - currentMeasure;
					return currentBeat + (measuresElapsed * beatsPerMeasure);
				}

				currentMeasure += measuresInThisRegion;
				currentBeat = nextChangeBeat;
			}

			// Fallback: use last time signature
			double lastPercentage = TimeSignatureChanges.Last().Percentage;
			return currentBeat + ((measure - currentMeasure) * lastPercentage);
		}
	}
}