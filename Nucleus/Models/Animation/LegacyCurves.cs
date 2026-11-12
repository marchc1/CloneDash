using System.Runtime.InteropServices;

namespace Nucleus.Models;

public static class LegacyCurves
{
	private static float Wrap(float degrees) {
		degrees %= 360f;
		if (degrees >= 180f)
			degrees -= 360f;
		else if (degrees < -180f)
			degrees += 360f;
		return degrees;
	}

	public static void ConvertNormalizedHandles(FCurve<float> curve, bool unwrapAngles = false) {
		curve.Recompute();
		var keys = CollectionsMarshal.AsSpan(curve.Keyframes);
		if (keys.Length == 0) return;

		if (unwrapAngles) {
			float lastRaw = keys[0].Value;
			float last = lastRaw;
			for (int i = 1; i < keys.Length; i++) {
				float raw = keys[i].Value;
				last += Wrap(raw - lastRaw);
				lastRaw = raw;
				keys[i].Value = last;
			}
		}

		keys[0].LeftHandle = null;
		for (int i = 0; i < keys.Length - 1; i++) {
			ref var start = ref keys[i];
			ref var end = ref keys[i + 1];
			if (start.Interpolation != KeyframeInterpolation.Bezier) {
				start.RightHandle = null;
				end.LeftHandle = null;
				continue;
			}

			double dt = end.Time - start.Time;
			float dv = end.Value - start.Value;
			var c12 = start.RightHandle;
			var c34 = end.LeftHandle;

			start.RightHandle = new() {
				HandleType = KeyframeHandleType.Free,
				Time = start.Time + (c12?.Time ?? 0) * dt,
				Value = start.Value + (c12?.Value ?? 0) * dv
			};
			end.LeftHandle = new() {
				HandleType = KeyframeHandleType.Free,
				Time = start.Time + (c34?.Time ?? 0) * dt,
				Value = start.Value + (c34?.Value ?? 0) * dv
			};
		}
		keys[keys.Length - 1].RightHandle = null;
	}

	public static void SplitOverruns(FCurve<float>[] curves) {
		if (curves.Length == 0) return;
		foreach (var curve in curves)
			curve.Recompute();
		var reference = curves[0].Keyframes;
		int count = reference.Count;
		foreach (var curve in curves) {
			if (curve.Count != count) return;
			for (int i = 0; i < count; i++) {
				if (Math.Abs(curve.Keyframes[i].Time - reference[i].Time) > FCurve<float>.KEYFRAME_TIME_EPSILON) return;
			}
		}

		bool changed = false;
		// New keys are appended and only sorted at the end.
		for (int k = 0; k < count - 1; k++) {
			int bezier = -1;
			for (int c = 0; c < curves.Length; c++) {
				if (curves[c].Keyframes[k].Interpolation == KeyframeInterpolation.Bezier) {
					bezier = c;
					break;
				}
			}
			if (bezier < 0) continue;

			double t0 = reference[k].Time, t1 = reference[k + 1].Time;
			double dt = t1 - t0;
			if (dt <= 0) continue;
			var handles = HandleTimes(curves[bezier].Keyframes[k], curves[bezier].Keyframes[k + 1]);
			double h1 = handles.H1, h2 = handles.H2;
			if (h1 >= t0 && h1 <= t1 && h2 >= t0 && h2 <= t1) continue;

			// Every bezier curve here has to share the time handles.
			bool shared = true;
			foreach (var curve in curves) {
				if (curve.Keyframes[k].Interpolation != KeyframeInterpolation.Bezier) continue;
				var other = HandleTimes(curve.Keyframes[k], curve.Keyframes[k + 1]);
				if (Math.Abs(other.H1 - h1) > 1e-6 || Math.Abs(other.H2 - h2) > 1e-6) {
					shared = false;
					break;
				}
			}
			if (!shared) continue;

			double da = h1 - t0, db = h2 - h1, dc = t1 - h2;
			bool folded = da < 0 || dc < 0 || (db < 0 && db * db > da * dc);
			int pieces = folded ? 0 : PiecesInside(t0, h1, h2, t1);
			if (pieces > 0)
				Split(curves, k, pieces);
			else
				Bake(curves, k, (float)((h1 - t0) / dt), (float)((h2 - t0) / dt));
			changed = true;
		}

		if (changed) {
			foreach (var curve in curves) {
				curve.Keyframes.Sort(static (x, y) => x.Time.CompareTo(y.Time));
				curve.Recompute();
			}
		}
	}

	private static (double H1, double H2) HandleTimes(Keyframe<float> start, Keyframe<float> end) {
		double dt = end.Time - start.Time;
		return (start.RightHandle?.Time ?? start.Time + dt / 3, end.LeftHandle?.Time ?? end.Time - dt / 3);
	}

	private static (double V1, double V2) HandleValues(Keyframe<float> start, Keyframe<float> end) {
		float dv = end.Value - start.Value;
		return (start.RightHandle?.Value ?? start.Value + dv / 3, end.LeftHandle?.Value ?? end.Value - dv / 3);
	}

	// The part of a cubic between sa and sb
	private static (double P0, double P1, double P2, double P3) Sub(double p0, double p1, double p2, double p3, double sa, double sb) {
		double q1 = NMath.Lerp(sb, p0, p1), m = NMath.Lerp(sb, p1, p2), r = NMath.Lerp(sb, p2, p3);
		double q2 = NMath.Lerp(sb, q1, m), m2 = NMath.Lerp(sb, m, r);
		double q3 = NMath.Lerp(sb, q2, m2);
		if (sb <= 0) return (p0, p0, p0, p0);
		double u = sa / sb;
		double a = NMath.Lerp(u, p0, q1), b = NMath.Lerp(u, q1, q2), c = NMath.Lerp(u, q2, q3);
		double d = NMath.Lerp(u, a, b), e = NMath.Lerp(u, b, c);
		return (NMath.Lerp(u, d, e), e, c, q3);
	}

	// Fewest even splits that keep every piece's handles inside it, 0 if none up to 64
	private static int PiecesInside(double t0, double h1, double h2, double t1) {
		for (int pieces = 2; pieces <= 64; pieces *= 2) {
			bool ok = true;
			for (int j = 0; j < pieces && ok; j++) {
				var piece = Sub(t0, h1, h2, t1, (double)j / pieces, (double)(j + 1) / pieces);
				ok = piece.P1 >= piece.P0 && piece.P1 <= piece.P3 && piece.P2 >= piece.P0 && piece.P2 <= piece.P3;
			}
			if (ok) return pieces;
		}
		return 0;
	}

	private static void Split(FCurve<float>[] curves, int k, int pieces) {
		foreach (var curve in curves) {
			var keys = curve.Keyframes;
			var start = keys[k];
			var end = keys[k + 1];
			double t0 = start.Time, t1 = end.Time;
			var handleTimes = HandleTimes(start, end);
			var handleValues = HandleValues(start, end);
			double h1 = handleTimes.H1, h2 = handleTimes.H2;

			KeyframeHandle<float>? left = null;
			for (int j = 0; j < pieces; j++) {
				double sa = (double)j / pieces, sb = (double)(j + 1) / pieces;
				var times = Sub(t0, h1, h2, t1, sa, sb);
				var key = keys[k];
				if (j > 0) {
					key = new Keyframe<float>() {
						Time = times.P0,
						Interpolation = start.Interpolation,
						LeftHandle = left
					};
				}
				switch (start.Interpolation) {
					case KeyframeInterpolation.Bezier: {
							var values = Sub(start.Value, handleValues.V1, handleValues.V2, end.Value, sa, sb);
							key.Value = (float)values.P0;
							key.RightHandle = new() {
								HandleType = KeyframeHandleType.Free,
								Time = times.P1,
								Value = (float)values.P1
							};
							left = new() {
								HandleType = KeyframeHandleType.Free,
								Time = times.P2,
								Value = (float)values.P2
							};
							break;
						}
					case KeyframeInterpolation.Linear:
						key.Value = (float)NMath.Lerp((times.P0 - t0) / (t1 - t0), start.Value, end.Value);
						break;
					default:
						key.Value = start.Value;
						break;
				}
				if (j == 0)
					keys[k] = key;
				else
					keys.Add(key);
			}
			if (start.Interpolation == KeyframeInterpolation.Bezier) {
				end = keys[k + 1];
				end.LeftHandle = left;
				keys[k + 1] = end;
			}
		}
	}

	private static void SampleCurve(float cx1, float cy1, float cx2, float cy2, float[] xs, float[] ys) {
		float tmpx = (-cx1 * 2 + cx2) * 0.03f, tmpy = (-cy1 * 2 + cy2) * 0.03f;
		float dddfx = ((cx1 - cx2) * 3 + 1) * 0.006f, dddfy = ((cy1 - cy2) * 3 + 1) * 0.006f;
		float ddfx = tmpx * 2 + dddfx, ddfy = tmpy * 2 + dddfy;
		float dfx = cx1 * 0.3f + tmpx + dddfx * 0.16666667f, dfy = cy1 * 0.3f + tmpy + dddfy * 0.16666667f;
		float x = dfx, y = dfy;
		for (int i = 0; i < xs.Length; i++) {
			xs[i] = x;
			ys[i] = y;
			dfx += ddfx;
			dfy += ddfy;
			ddfx += dddfx;
			ddfy += dddfy;
			x += dfx;
			y += dfy;
		}
	}

	private static float SamplePercent(float percent, float[] xs, float[] ys) {
		percent = Math.Clamp(percent, 0, 1);
		float x = 0;
		for (int i = 0; i < xs.Length; i++) {
			x = xs[i];
			if (x >= percent) {
				if (i == 0) return ys[0] * percent / x;
				return ys[i - 1] + (ys[i] - ys[i - 1]) * (percent - xs[i - 1]) / (x - xs[i - 1]);
			}
		}
		float y = ys[ys.Length - 1];
		return y + (1 - y) * (percent - x) / (1 - x);
	}

	private static void Bake(FCurve<float>[] curves, int k, float cx1, float cx2) {
		var reference = curves[0].Keyframes;
		double t0 = reference[k].Time, t1 = reference[k + 1].Time;
		double dt = t1 - t0;
		float[] xs = new float[9], ys = new float[9];
		SampleCurve(cx1, 0, cx2, 0, xs, ys);

		double gap = 2 * FCurve<float>.KEYFRAME_TIME_EPSILON / dt;
		List<double> at = [gap, 1 - gap];
		foreach (var x in xs) {
			if (x > 0 && x < 1)
				at.AddRange([x, x + gap]);
		}
		List<double> inside = [];
		foreach (var t in at) {
			if (t >= gap && t <= 1 - gap)
				inside.Add(t);
		}
		inside.Sort();
		at = inside;
		for (int i = at.Count - 1; i > 0; i--) {
			if (at[i] - at[i - 1] < gap)
				at.RemoveAt(i);
		}

		var values = new float[curves.Length][];
		for (int curveIndex = 0; curveIndex < curves.Length; curveIndex++) {
			var start = curves[curveIndex].Keyframes[k];
			var end = curves[curveIndex].Keyframes[k + 1];
			values[curveIndex] = new float[at.Count];
			if (start.Interpolation == KeyframeInterpolation.Bezier) {
				float dv = end.Value - start.Value;
				var handleValues = HandleValues(start, end);
				float cy1 = dv == 0 ? 0 : (float)((handleValues.V1 - start.Value) / dv);
				float cy2 = dv == 0 ? 0 : (float)((handleValues.V2 - start.Value) / dv);
				SampleCurve(cx1, cy1, cx2, cy2, xs, ys);
				for (int i = 0; i < at.Count; i++)
					values[curveIndex][i] = start.Value + SamplePercent((float)at[i], xs, ys) * dv;
			}
			else {
				for (int i = 0; i < at.Count; i++)
					values[curveIndex][i] = start.Interpolation == KeyframeInterpolation.Linear ? (float)NMath.Lerp(at[i], start.Value, end.Value)
						: start.Value;
			}
		}

		List<int> keep = [];
		for (int i = 0; i < at.Count; i++) {
			double pPrev = keep.Count == 0 ? 0 : at[keep[keep.Count - 1]];
			double pNext = i + 1 < at.Count ? at[i + 1] : 1;
			bool needed = false;
			for (int curveIndex = 0; curveIndex < curves.Length && !needed; curveIndex++) {
				var start = curves[curveIndex].Keyframes[k];
				var end = curves[curveIndex].Keyframes[k + 1];
				if (start.Interpolation == KeyframeInterpolation.Constant) continue;
				float vPrev = keep.Count == 0 ? start.Value : values[curveIndex][keep[keep.Count - 1]];
				float vNext = i + 1 < at.Count ? values[curveIndex][i + 1] : end.Value;
				float line = (float)NMath.Lerp((at[i] - pPrev) / (pNext - pPrev), vPrev, vNext);
				needed = Math.Abs(line - values[curveIndex][i]) > 1e-4f * Math.Max(1, Math.Abs(values[curveIndex][i]));
			}
			if (needed)
				keep.Add(i);
		}

		for (int curveIndex = 0; curveIndex < curves.Length; curveIndex++) {
			var keys = curves[curveIndex].Keyframes;
			var start = keys[k];
			var end = keys[k + 1];
			var interpolation = start.Interpolation == KeyframeInterpolation.Constant ? KeyframeInterpolation.Constant : KeyframeInterpolation.Linear;
			if (start.Interpolation == KeyframeInterpolation.Bezier) {
				start.Interpolation = KeyframeInterpolation.Linear;
				start.RightHandle = null;
				keys[k] = start;
				end.LeftHandle = null;
				keys[k + 1] = end;
			}
			foreach (var i in keep) {
				curves[curveIndex].Keyframes.Add(new Keyframe<float>() {
					Time = t0 + at[i] * dt,
					Value = values[curveIndex][i],
					Interpolation = interpolation
				});
			}
		}
	}
}
