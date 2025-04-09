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

	public T? Evaluate<T>(FCurve<T> fcurve) {
		double frame = Interpolated ? Math.Round(Frame) : Frame;

		return fcurve.DetermineValueAtTime(frame, Stepped ? KeyframeInterpolation.Constant : null) ?? default;
	}

	public void AddDeltaTime(double dt, int maxTime) {
		Frame += dt;
		while (Frame > maxTime)
			Frame -= maxTime;
	}

	public delegate void FrameChangedD(TimelineManager timeline, int frame);
	public event FrameChangedD? FrameChanged;

	public void SetFrame(int frame) {
		frame = Math.Max(0, frame);
		Frame = frame;
		FrameChanged?.Invoke(this, frame);
	}
}
