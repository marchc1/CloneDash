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

	public bool PlayingBackwards {
		get => PlayDirection == -1;
		set => PlayDirection = value ? -1 : 0;
	}
	public bool PlayingForwards {
		get => PlayDirection == 1;
		set => PlayDirection = value ? 1 : 0;
	}

	public T? Evaluate<T>(FCurve<T> fcurve) {
		double frame = Interpolated ? Math.Round(Frame) : Frame;

		return fcurve.DetermineValueAtTime(frame, Stepped ? KeyframeInterpolation.Constant : null) ?? default;
	}

	public int SavedFrame;
	public void SaveFrame() => SavedFrame = (int)(float)Frame;
	public void LoadFrame() => SetFrame(SavedFrame);
	public void RoundFrame() => SetFrame(Math.Round(Frame));

	public void AddDeltaTime(double dt, double maxTime) {
		if (PlayDirection == 0) return;
		if (!ModelEditor.Active.AnimationMode) return;

		dt *= FPS;

		if (PlayingBackwards) dt *= -1;

		Frame += dt;
		if (Frame > maxTime) {
			while (Frame > maxTime)
				Frame -= maxTime;
		}

		if(Frame < 0) {
			while (Frame < 0)
				Frame += maxTime;
		}
		FrameElapsed?.Invoke(this, Frame);
	}

	public delegate void FrameChangedD(TimelineManager timeline, int frame);
	public delegate void FrameChangedD2(TimelineManager timeline, double frame);
	public event FrameChangedD? FrameChanged;
	public event FrameChangedD2? FrameElapsed;

	public void SetFrame(int frame) {
		frame = Math.Max(0, frame);
		Frame = frame;
		FrameChanged?.Invoke(this, frame);
	}
	public void SetFrame(double frame) {
		frame = Math.Max(0, frame);
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
