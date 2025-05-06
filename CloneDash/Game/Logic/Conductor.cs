using CloneDash.Systems.CustomCharts;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using static CloneDash.Game.CD_GameLevel;

namespace CloneDash.Game
{
	/// <summary>
	/// Currently unused, and may be a bad idea to try implementing?
	/// </summary>
	public struct TempoChange
	{
		public double Time;
		public double Measure;
		public double BPM;

		public TempoChange(double time, double measure, double bpm) {
			this.Time = time;
			this.Measure = measure;
			this.BPM = bpm;
		}

		public override string ToString() {
			return $"Tempo Change [time: {Time}, measure {Measure}, bpm: {BPM}]";
		}
	}

	public class Conductor : LogicalEntity
	{
		public Conductor() {
			currentInaccurateTime = (float)-PreStartTime;
		}
		public List<TempoChange> TempoChanges { get; private set; } = [];

		public void AddTempoChange(double time, double measure, double bpm) => TempoChanges.Add(new(time, measure, bpm));

		/// <summary>
		/// The current music playhead, adjusted for inaccuracies.
		/// </summary>
		public double Time => currentInaccurateTime;

		/// <summary>
		/// Offsets the conductor time
		/// </summary>
		public double PreStartTime { get; set; } = CommandLineArguments.GetParam("pretime", 5d);

		public double BPM => GetTempoAtTime(Time);

		// What was the last music time that Raylib reported?
		private float lastTimeFromFunctionCall = 0;
		// What is the current time + the frametime values
		private double currentInaccurateTime = 0;

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

			public override void MouseClick(FrameState state, Nucleus.Input.MouseButton button) {
				base.MouseClick(state, button);
				DragStart?.Invoke();
			}

			public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
				base.MouseDrag(self, state, delta);
				DragUpdate?.Invoke();
			}

			public override void MouseRelease(Element self, FrameState state, Nucleus.Input.MouseButton button) {
				base.MouseRelease(self, state, button);
				DragEnd?.Invoke();
			}

			public override void Paint(float width, float height) {
				Graphics2D.SetDrawColor(230, 235, 255, Depressed ? 100 : 255);
				Graphics2D.DrawRectangle(0, 0, width * Completion, height);

				if (Depressed) {
					Graphics2D.SetDrawColor(230, 235, 255);
					Graphics2D.DrawRectangle(0, 2, (float)XToSeconds(GetMousePos().X) * Completion, height - 4);
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

		private double uiSeconds => Math.Clamp(UIBar.XToSeconds(UIBar.GetMousePos().X), 0, Level.As<CD_GameLevel>()?.Music?.Length ?? throw new Exception());

		private void UIBar_DragUpdate() {
			var game = Level.As<CD_GameLevel>();
			if (!game.AutoPlayer.Enabled) return;

			dragSeconds = uiSeconds;
		}

		private void UIBar_DragEnd() {
			var game = Level.As<CD_GameLevel>();
			if (!game.AutoPlayer.Enabled) return;

			if (dragSeconds != null) 
				game.SeekTo(dragSeconds.Value);

			if (!wasPaused)
				game.ForceUnpause();

			dragSeconds = null;
		}

		private void UIBar_DragStart() {
			var game = Level.As<CD_GameLevel>();
			if (!game.AutoPlayer.Enabled) return;

			wasPaused = game.Paused;
			if (!wasPaused)
				game.ForcePause();

			dragSeconds = uiSeconds;
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
		/// <summary>
		/// Gets the current BPM from the song position
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public double GetTempoAtTime(double time) => GetTempoChangeAtTime(time).BPM;

		public bool firstTick = true;
		private double lastTime;
		public override void Think(FrameState frameState) {
			lastTime = Time;
			var game = Level.As<CD_GameLevel>();
			Level.FrameDebuggingStrings.Add($"Conductor Time: {Time}");

			if (firstTick) {
				currentInaccurateTime = (float)-PreStartTime;
			}
			else if (currentInaccurateTime < 0) {
				var ft = EngineCore.FrameTime;
				if (ft > 0.5)
					return;

				currentInaccurateTime += ft;
			}
			else if(game.Music != null) {
				game.Music.Update();

				var now = game.Music.Playhead;
				var paused = game.Music.Paused;

				if (lastTimeFromFunctionCall != now && !paused) {
					lastTimeFromFunctionCall = now;
					currentInaccurateTime = now;
				}
				else {
					if (!paused)
						currentInaccurateTime += EngineCore.FrameTime;
				}
			}
			else {
				currentInaccurateTime += EngineCore.FrameTime;
			}

			if (game.Music != null) {
				UIBar.Playhead = game.Music.Playhead;
				UIBar.Duration = game.Music.Length;
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

		public double MeasureToSeconds(double measure) {
			double ret = 0;

			for (int i = 0; i < TempoChanges.Count; i++) {
				var lastChange = TempoChanges[i - (i == 0 ? 0 : 1)];
				var change = TempoChanges[i];

				if(i == TempoChanges.Count - 1 || change.Measure > measure) {
					return lastChange.Time + ((measure - lastChange.Measure) * (60 / change.BPM));
				}
			}

			return 0;
		}
	}
}