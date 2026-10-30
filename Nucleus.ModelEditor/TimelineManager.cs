using Nucleus.Models;

namespace Nucleus.ModelEditor;

public class TimelineManager
{
	/// <summary>
	/// Current FPS.
	/// </summary>
	public int FPS { get; set; } = 30;
	/// <summary>
	/// Current frame.
	/// </summary>
	public double Frame { get; private set; } = 0;
	/// <summary>
	/// Speed multiplier.
	/// </summary>
	public double Speed { get; set; } = 1;

	/// <summary>
	/// If true, will interpolate at the editors frame-rate. If false, will interpolate at the timeline framerate.
	/// </summary>
	public bool Interpolated { get; set; } = true;
	/// <summary>
	/// If true, will override all channels <see cref="KeyframeInterpolation"/> to use <see cref="KeyframeInterpolation.Constant"/>
	/// during playback.
	/// </summary>
	public bool Stepped { get; set; } = false;

	public int PlayDirection { get; private set; } = 0;

	/// <summary>
	/// Playback starts over at the end
	/// </summary>
	public bool Repeat { get; set; } = true;
	/// <summary>
	/// Loop range in frames, used when both are set and <see cref="Repeat"/> is on
	/// </summary>
	public double? LoopStart { get; set; }
	public double? LoopEnd { get; set; }

	/// <summary>
	/// Differs from Frame, in that it accounts for Interpolated.
	/// </summary>
	public double GetPlayhead() {
		if (Interpolated) return Frame;

		return Math.Round(Frame * GetReferenceFPS()) / GetReferenceFPS();
	}

	/// <summary>
	/// Returns how the playhead should look to the user. The frame is rounded only when playing; otherwise, it is only rounded to 2 digits
	/// </summary>
	/// <returns></returns>
	public double GetVisualPlayhead(bool alwaysInterpolate) => (alwaysInterpolate || PlayDirection == 0) ? Math.Round(Frame * GetReferenceFPS(), 2) : Math.Round(Frame * GetReferenceFPS());
	public int GetReferenceFPS() => 30;

	public bool PlayingBackwards {
		get => PlayDirection == -1;
		set => PlayDirection = value ? -1 : 0;
	}
	public bool PlayingForwards {
		get => PlayDirection == 1;
		set => PlayDirection = value ? 1 : 0;
	}

	public double SavedFrame;
	public void SaveFrame() => SavedFrame = Frame * GetReferenceFPS();
	public void LoadFrame() => SetFrame(SavedFrame);
	public void RoundFrame() => SetFrame(Math.Round(Frame * GetReferenceFPS()));

	public void AddDeltaTime(double dt, double maxTime) {
		if (PlayDirection == 0) return;
		if (!ModelEditor.Active.AnimationMode) return;

		dt *= Speed * (FPS / (double)(GetReferenceFPS()));

		if (PlayingBackwards) dt *= -1;

		Frame += dt;
		double start = 0, end = maxTime;
		if (LoopStart is double loopStart && LoopEnd is double loopEnd && loopEnd > loopStart) {
			start = loopStart / GetReferenceFPS();
			end = loopEnd / GetReferenceFPS();
		}
		if (Repeat && end > start) {
			double length = end - start;
			if (Frame > end)
				Frame = start + (Frame - end) % length;
			if (Frame < start)
				Frame = end - (start - Frame) % length;
		}
		else if (Frame < 0) {
			Frame = 0;
			PlayDirection = 0;
		}
		FrameElapsed?.Invoke(this, Frame);
	}

	public delegate void FrameChangedD(TimelineManager timeline, int frame);
	public delegate void FrameChangedD2(TimelineManager timeline, double frame);
	public event FrameChangedD? FrameChanged;
	public event FrameChangedD2? FrameElapsed;

	public void SetFrame(int frame) => SetFrame((double)frame);
	public void SetFrame(double frame) {
		frame = Math.Max(0, frame / GetReferenceFPS());
		Frame = frame;
		FrameChanged?.Invoke(this, (int)(float)frame);
	}

	internal void TogglePlayBackwards() {
		if (PlayingBackwards) {
			PlayingBackwards = false;
			RoundFrame();
		}
		else if (PlayingForwards) {
			PlayingForwards = false;
			LoadFrame();
		}
		else {
			PlayingBackwards = true;
			SaveFrame();
		}
	}

	internal void TogglePlayForwards() {
		if (PlayingForwards) {
			PlayingForwards = false;
			RoundFrame();
		}
		else if (PlayingBackwards) {
			PlayingBackwards = false;
			LoadFrame();
		}
		else {
			PlayingForwards = true;
			SaveFrame();
		}
	}
}
