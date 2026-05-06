namespace CloneDash.Common.Data;

public struct TempoChange
{
	public double Time;
	public int Beat;
	public double BPM;

	public TempoChange(double time, int beat, double bpm) {
		this.Time = time;
		this.Beat = beat;
		this.BPM = bpm;
	}

	public override string ToString() {
		return $"Tempo Change [time: {Time}, beat {Beat}, bpm: {BPM}]";
	}
}
