namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public enum MD1_CurveEase : byte
{
	Linear,
	Smooth,
	EaseIn,
	EaseOut
}

public readonly record struct MD1_CurveNode(double Time, float Value, MD1_CurveEase Ease);

public sealed class MD1_CurvePath
{
	readonly MD1_CurveNode[] nodes;

	public MD1_CurvePath(IEnumerable<MD1_CurveNode> nodes) {
		this.nodes = nodes.OrderBy(static n => n.Time).ToArray();
	}

	public int NodeCount => nodes.Length;

	int SegmentAt(double t) {
		int lo = 0, hi = nodes.Length - 1, found = -1;
		while (lo <= hi) {
			int mid = (lo + hi) >> 1;
			if (nodes[mid].Time <= t) {
				found = mid;
				lo = mid + 1;
			}
			else
				hi = mid - 1;
		}
		return found;
	}

	static double Ease(MD1_CurveEase ease, double f) => ease switch {
		MD1_CurveEase.Smooth => f * f * (3 - 2 * f),
		MD1_CurveEase.EaseIn => f * f,
		MD1_CurveEase.EaseOut => 1 - (1 - f) * (1 - f),
		_ => f
	};

	public float Evaluate(double t) {
		if (nodes.Length == 0)
			return 0;

		int i = SegmentAt(t);
		
		if (i < 0)
			return nodes[0].Value;

		if (i >= nodes.Length - 1)
			return nodes[^1].Value;

		var a = nodes[i];
		var b = nodes[i + 1];
		double f = Ease(b.Ease, (t - a.Time) / (b.Time - a.Time));
		return a.Value + (b.Value - a.Value) * (float)f;
	}

	public int Sample(double from, double to, double step, Span<double> times, Span<float> values) {
		int max = Math.Min(times.Length, values.Length);
		if (max < 2 || nodes.Length == 0 || to <= from)
			return 0;

		int count = 0;
		double t = from;
		times[count] = t;
		values[count++] = Evaluate(t);

		int i = SegmentAt(from) + 1;
		while (t < to && count < max - 1) {
			double next = i < nodes.Length ? Math.Min(nodes[i].Time, to) : to;

			while (t + step < next && count < max - 1) {
				t += step;
				times[count] = t;
				values[count++] = Evaluate(t);
			}

			t = next;
			if (i >= nodes.Length || nodes[i].Time > to || count >= max - 1)
				break;

			times[count] = t;
			values[count++] = nodes[i++].Value;
			while (i < nodes.Length && nodes[i].Time == t && count < max - 1) {
				times[count] = t;
				values[count++] = nodes[i++].Value;
			}
		}

		if (times[count - 1] < to) {
			times[count] = to;
			values[count++] = Evaluate(to);
		}

		return count;
	}
}
