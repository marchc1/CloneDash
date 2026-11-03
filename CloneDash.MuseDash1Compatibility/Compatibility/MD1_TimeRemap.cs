namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public sealed class MD1_TimeRemap
{
	readonly double[] times;
	readonly double[] speeds;
	readonly double[] positions;

	public MD1_TimeRemap(IEnumerable<(double Time, double Speed)> keys) {
		var sorted = keys.OrderBy(static k => k.Time).ToArray();
		times = new double[sorted.Length];
		speeds = new double[sorted.Length];
		positions = new double[sorted.Length];

		for (int i = 0; i < sorted.Length; i++) {
			times[i] = sorted[i].Time;
			speeds[i] = sorted[i].Speed;
			positions[i] = i == 0 ? times[0] : positions[i - 1] + speeds[i - 1] * (times[i] - times[i - 1]);
		}
	}

	public int KeyCount => times.Length;

	int SegmentAt(double t) {
		int idx = Array.BinarySearch(times, t);

		if (idx < 0)
			return ~idx - 1;

		while (idx + 1 < times.Length && times[idx + 1] == t)
			idx++;

		return idx;
	}

	public double Position(double t) {
		int i = SegmentAt(t);
		return i < 0 ? t : positions[i] + speeds[i] * (t - times[i]);
	}

	public double Distance(double target, double now) => Position(target) - Position(now);

	public double FirstTimeAtLeast(double value) {
		int n = times.Length;
		if (n == 0 || value < times[0])
			return value;

		for (int i = 0; i < n; i++) {
			double p = positions[i], s = speeds[i];
			double end = i + 1 < n ? times[i + 1] : double.PositiveInfinity;
			
			if (p >= value)
				return times[i];

			if (s > 0) {
				double crossing = times[i] + (value - p) / s;
				
				if (crossing <= end)
					return crossing;
			}
		}

		return double.PositiveInfinity;
	}

	public double LastTimeAtMost(double value) {
		int n = times.Length;
		if (n == 0)
			return value;

		double lastP = positions[n - 1];
		double lastS = speeds[n - 1];
		
		if (lastS < 0 || (lastS == 0 && lastP <= value))
			return double.PositiveInfinity;
			
		if (lastS > 0 && lastP <= value)
			return times[n - 1] + (value - lastP) / lastS;

		for (int i = n - 2; i >= 0; i--) {
			double p0 = positions[i], p1 = positions[i + 1];

			if (p1 <= value)
				return times[i + 1];

			if (p0 <= value)
				return times[i] + (value - p0) / speeds[i];
		}

		return Math.Min(value, times[0]);
	}

	public void VisibleWindow(double hitTime, double tailTime, double approach, double depart, double fallbackLength, out double startOut, out double endOut) {
		double start = FirstTimeAtLeast(Position(hitTime) - approach);
		double end = LastTimeAtMost(Position(tailTime) + depart);

		if (double.IsPositiveInfinity(start))
			start = hitTime - approach;

		if (double.IsPositiveInfinity(end))
			end = Math.Max(times.Length > 0 ? times[^1] : tailTime, tailTime) + fallbackLength;

		startOut = Math.Min(start, hitTime - approach);
		endOut = Math.Max(end, tailTime + depart);
	}
}
