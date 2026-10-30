using Nucleus.Common.Images;
using Nucleus.Types;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Nucleus.ModelEditor;

/// <summary>
/// Builds a mesh hull around an image's opaque pixels, in uvs.
/// </summary>
public static class MeshTracer
{
	// Traced on a grid no bigger than this
	private const int MAX_GRID = 200;

	/// <param name="detail">0-100, how many hull vertices</param>
	/// <param name="concavity">0-100, 0 gives a convex hull</param>
	/// <param name="refinement">0-100, how many tries to keep the best of</param>
	/// <param name="alphaThreshold">0-255, pixels at or below this are empty</param>
	/// <param name="padding">pixels between the image and the hull</param>
	/// <returns>Null when the image has nothing above the threshold</returns>
	public static unsafe List<Vector2F>? Trace(Image image, float detail, float concavity, float refinement, float alphaThreshold, float padding, Random random) {
		int width = image.Width, height = image.Height;
		if (width <= 0 || height <= 0) return null;

		int scale = Math.Max(1, (int)MathF.Ceiling(Math.Max(width, height) / (float)MAX_GRID));
		int gridWidth = (width + scale - 1) / scale, gridHeight = (height + scale - 1) / scale;

		// a cell is solid when any pixel in it is
		bool[] solid = new bool[gridWidth * gridHeight];
		bool any = false;
		Vector4* pixels = Image.LoadImageDataNormalized(image);
		try {
			float threshold = alphaThreshold / 255;
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					if (pixels[y * width + x].W > threshold) {
						solid[(y / scale) * gridWidth + x / scale] = true;
						any = true;
					}
				}
			}
		}
		finally {
			Marshal.FreeHGlobal((IntPtr)pixels);
		}
		if (!any) return null;

		bool[] filled = Pad(solid, gridWidth, gridHeight, padding / scale);
		var points = BoundaryCorners(filled, gridWidth, gridHeight);
		var hull = ConvexHull(points);
		if (hull.Count < 3) return null;

		int target = 4 + (int)MathF.Round(Math.Clamp(detail, 0, 100) / 100 * 44);
		float concavityAmount = Math.Clamp(concavity, 0, 100) / 100;
		int attempts = 1 + (int)MathF.Round(Math.Clamp(refinement, 0, 100) / 100 * 11);

		Reduce(hull, target, 1, gridWidth, gridHeight, () => 1, reflexToo: false);
		SplitCollinear(hull, points);

		List<Vector2>? best = null;
		float bestArea = float.MaxValue;
		for (int i = 0; i < attempts; i++) {
			var poly = Attempt(hull, points, target, concavityAmount, gridWidth, gridHeight, random);
			float area = MathF.Abs(SignedArea(poly));
			if (poly.Count >= 3 && area < bestArea) {
				bestArea = area;
				best = poly;
			}
		}
		if (best == null) return null;

		List<Vector2F> uvs = new(best.Count);
		foreach (var point in best) {
			float pixelX = Math.Min(point.X * scale, width), pixelY = Math.Min(point.Y * scale, height);
			uvs.Add(new(pixelX / width, 1 - pixelY / height));
		}
		return uvs;
	}

	private static bool[] Pad(bool[] solid, int gridWidth, int gridHeight, float radius) {
		if (radius <= 0) return solid;

		// Two pass chamfer distance to the nearest solid cell
		const float D = 1;
		const float DD = 1.41421356f;
		float[] dist = new float[gridWidth * gridHeight];
		for (int i = 0; i < dist.Length; i++)
			dist[i] = solid[i] ? 0 : float.MaxValue;
		for (int y = 0; y < gridHeight; y++) {
			for (int x = 0; x < gridWidth; x++) {
				float distance = dist[y * gridWidth + x];
				distance = MathF.Min(distance, At(x - 1, y, gridWidth, gridHeight, dist) + D);
				distance = MathF.Min(distance, At(x, y - 1, gridWidth, gridHeight, dist) + D);
				distance = MathF.Min(distance, At(x - 1, y - 1, gridWidth, gridHeight, dist) + DD);
				distance = MathF.Min(distance, At(x + 1, y - 1, gridWidth, gridHeight, dist) + DD);
				dist[y * gridWidth + x] = distance;
			}
		}
		for (int y = gridHeight - 1; y >= 0; y--) {
			for (int x = gridWidth - 1; x >= 0; x--) {
				float distance = dist[y * gridWidth + x];
				distance = MathF.Min(distance, At(x + 1, y, gridWidth, gridHeight, dist) + D);
				distance = MathF.Min(distance, At(x, y + 1, gridWidth, gridHeight, dist) + D);
				distance = MathF.Min(distance, At(x + 1, y + 1, gridWidth, gridHeight, dist) + DD);
				distance = MathF.Min(distance, At(x - 1, y + 1, gridWidth, gridHeight, dist) + DD);
				dist[y * gridWidth + x] = distance;
			}
		}

		bool[] filled = new bool[gridWidth * gridHeight];
		for (int i = 0; i < filled.Length; i++)
			filled[i] = dist[i] <= radius;
		return filled;
	}

	private static float At(int x, int y, int gridWidth, int gridHeight, float[] dist)
		=> x < 0 || y < 0 || x >= gridWidth || y >= gridHeight ? float.MaxValue : dist[y * gridWidth + x];

	// Corners of filled cells that touch an empty cell or the edge
	private static List<Vector2> BoundaryCorners(bool[] filled, int gridWidth, int gridHeight) {
		bool[] seen = new bool[(gridWidth + 1) * (gridHeight + 1)];
		List<Vector2> points = [];
		for (int y = 0; y < gridHeight; y++) {
			for (int x = 0; x < gridWidth; x++) {
				if (!Get(x, y, filled, gridWidth, gridHeight)) continue;
				if (Get(x - 1, y, filled, gridWidth, gridHeight) && Get(x + 1, y, filled, gridWidth, gridHeight) && Get(x, y - 1, filled, gridWidth, gridHeight)
					&& Get(x, y + 1, filled, gridWidth, gridHeight)) continue;
				Add(x, y, gridWidth, seen, points);
				Add(x + 1, y, gridWidth, seen, points);
				Add(x, y + 1, gridWidth, seen, points);
				Add(x + 1, y + 1, gridWidth, seen, points);
			}
		}
		return points;
	}

	private static void Add(int x, int y, int gridWidth, bool[] seen, List<Vector2> points) {
		int i = y * (gridWidth + 1) + x;
		if (seen[i]) return;
		seen[i] = true;
		points.Add(new(x, y));
	}

	private static bool Get(int x, int y, bool[] filled, int gridWidth, int gridHeight)
		=> x >= 0 && y >= 0 && x < gridWidth && y < gridHeight && filled[y * gridWidth + x];

	private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

	private static float SignedArea(List<Vector2> poly) {
		float area = 0;
		for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
			area += Cross(poly[j], poly[i]);
		return area / 2;
	}

	// monotone chain, counter clockwise by signed area
	private static List<Vector2> ConvexHull(List<Vector2> points) {
		var order = new int[points.Count];
		for (int i = 0; i < order.Length; i++)
			order[i] = i;
		Array.Sort(order, (i, j) => {
			int compare = points[i].X.CompareTo(points[j].X);
			if (compare != 0) return compare;
			compare = points[i].Y.CompareTo(points[j].Y);
			if (compare != 0) return compare;
			return i.CompareTo(j);
		});
		List<Vector2> sorted = new(points.Count);
		foreach (int index in order)
			sorted.Add(points[index]);
		if (sorted.Count < 3) return sorted;
		List<Vector2> hull = [];
		for (int pass = 0; pass < 2; pass++) {
			int start = hull.Count;
			foreach (var p in sorted) {
				while (hull.Count >= start + 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 2]) <= 0)
					hull.RemoveAt(hull.Count - 1);
				hull.Add(p);
			}
			hull.RemoveAt(hull.Count - 1);
			sorted.Reverse();
		}
		return hull;
	}

	private static bool SegmentsTouch(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
		int o1 = Side(a, b, c);
		int o2 = Side(a, b, d);
		int o3 = Side(c, d, a);
		int o4 = Side(c, d, b);
		if (o1 != o2 && o3 != o4) return true;
		return (o1 == 0 && On(a, c, b)) || (o2 == 0 && On(a, d, b)) || (o3 == 0 && On(c, a, d)) || (o4 == 0 && On(c, b, d));
	}

	private static bool On(Vector2 p, Vector2 q, Vector2 r) => MathF.Min(p.X, r.X) <= q.X && q.X <= MathF.Max(p.X, r.X)
			&& MathF.Min(p.Y, r.Y) <= q.Y && q.Y <= MathF.Max(p.Y, r.Y);

	private static int Side(Vector2 p, Vector2 q, Vector2 r) {
		float cross = Cross(q - p, r - p);
		if (cross > 1e-6f) return 1;
		if (cross < -1e-6f) return -1;
		return 0;
	}

	// Does a new segment hit any edge, skipping the given ones
	private static bool Hits(List<Vector2> poly, Vector2 a, Vector2 b, int skipFrom, int skipCount) {
		int n = poly.Count;
		for (int j = 0; j < n; j++) {
			int k = ((j - skipFrom) % n + n) % n;
			if (k < skipCount) continue;
			if (SegmentsTouch(a, b, poly[j], poly[(j + 1) % n])) return true;
		}
		return false;
	}

	private static int Wrap(int i, int n) => ((i % n) + n) % n;

	private static List<Vector2> Attempt(List<Vector2> hull, List<Vector2> points, int target, float concavity, int gridWidth, int gridHeight, Random random) {
		var poly = new List<Vector2>(hull);
		Func<float> jitter = () => 0.6f + (float)random.NextDouble() * 0.8f;

		// dig into concave areas, biggest first
		int digsLeft = (int)MathF.Round(target * 2 * concavity);
		if (digsLeft > 0) {
			// Best dig per edge, edge i runs from poly[i]
			List<(bool Ok, Vector2 Point, float Gain)> digs = [];
			for (int i = 0; i < poly.Count; i++)
				digs.Add(Dig(i, points, poly, jitter));

			while (digsLeft > 0) {
				int bestEdge = -1;
				for (int i = 0; i < digs.Count; i++) {
					if (digs[i].Ok && digs[i].Gain > 1 && (bestEdge < 0 || digs[i].Gain > digs[bestEdge].Gain))
						bestEdge = i;
				}
				if (bestEdge < 0) break;

				// The polygon may have grown into the way since.
				int n = poly.Count;
				var point = digs[bestEdge].Point;
				if (Hits(poly, poly[bestEdge], point, Wrap(bestEdge - 1, n), 3) || Hits(poly, point, poly[(bestEdge + 1) % n], Wrap(bestEdge - 1, n), 3)) {
					digs[bestEdge] = (false, default, 0);
					continue;
				}

				poly.Insert(bestEdge + 1, point);
				digsLeft--;
				digs[bestEdge] = Dig(bestEdge, points, poly, jitter);
				digs.Insert(bestEdge + 1, Dig(bestEdge + 1, points, poly, jitter));
			}
		}

		Reduce(poly, target, 0.25f + concavity * 3.75f, gridWidth, gridHeight, jitter, reflexToo: true);

		// straight vertices do nothing
		for (int i = poly.Count - 1; i >= 0 && poly.Count > 3; i--) {
			int n = poly.Count;
			if (MathF.Abs(Cross(poly[i] - poly[Wrap(i - 1, n)], poly[(i + 1) % n] - poly[i])) <= 1e-6f)
				poly.RemoveAt(i);
		}
		return poly;
	}

	private static (bool, Vector2, float) Dig(int i, List<Vector2> points, List<Vector2> poly, Func<float> jitter)
		=> DigPoint(poly, points, i, out var point, out float gain) ? (true, point, gain * jitter()) : (false, default, 0);

	// points on an edge become vertices
	private static void SplitCollinear(List<Vector2> poly, List<Vector2> points) {
		List<(float T, Vector2 P)> onEdge = [];
		for (int i = poly.Count - 1; i >= 0; i--) {
			Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
			Vector2 ab = b - a;
			float len = ab.Length();
			if (len < 1e-6f) continue;
			onEdge.Clear();
			foreach (var p in points) {
				if (MathF.Abs(Cross(ab, p - a) / len) > 1e-4f) continue;
				float t = Vector2.Dot(p - a, ab) / (len * len);
				if (t > 1e-6f && t < 1 - 1e-6f)
					onEdge.Add((t, p));
			}
			onEdge.Sort((x, y) => x.T.CompareTo(y.T));
			for (int k = onEdge.Count - 1; k >= 0; k--)
				poly.Insert(i + 1, onEdge[k].P);
		}
	}

	// Nearest point inside the edge's slab, the cut triangle can't hold any other point
	private static bool DigPoint(List<Vector2> poly, List<Vector2> points, int i, out Vector2 best, out float gain) {
		int n = poly.Count;
		Vector2 a = poly[i], b = poly[(i + 1) % n];
		Vector2 ab = b - a;
		float len = ab.Length();
		best = default;
		gain = 0;
		if (len < 1) return false;

		float bestDist = float.MaxValue;
		foreach (var p in points) {
			float distance = Cross(ab, p - a) / len;
			if (distance < -1e-4f || distance >= bestDist) continue;
			float t = Vector2.Dot(p - a, ab) / (len * len);
			if (t <= 0 || t >= 1) continue;
			// Points on the edge would end up outside
			if (distance <= 1e-4f) return false;
			bestDist = distance;
			best = p;
		}
		// a point hugging the edge means pixels right against it
		if (bestDist <= 0.5f || bestDist == float.MaxValue) return false;
		if (Hits(poly, a, best, Wrap(i - 1, n), 3) || Hits(poly, best, b, Wrap(i - 1, n), 3)) return false;
		gain = len * bestDist / 2;
		return true;
	}

	/// <summary>
	/// Removes vertices until there are <paramref name="target"/>, only ever growing the polygon.
	/// </summary>
	private static void Reduce(List<Vector2> poly, int target, float reflexWeight, int gridWidth, int gridHeight, Func<float> jitter, bool reflexToo) {
		while (poly.Count > target && poly.Count > 3) {
			int n = poly.Count;
			int bestKind = 0;
			int bestAt = -1;
			Vector2 bestCorner = default;
			float bestCost = float.MaxValue;

			for (int i = 0; i < n; i++) {
				Vector2 p0 = poly[Wrap(i - 1, n)];
				Vector2 v = poly[i];
				Vector2 v1 = poly[(i + 1) % n];
				Vector2 p3 = poly[(i + 2) % n];

				// Reflex or straight vertex: drop it, the gap fills in
				if (reflexToo && Cross(v - p0, v1 - v) <= 1e-6f) {
					float cost = MathF.Abs(Cross(v - p0, v1 - p0)) / 2 * reflexWeight * jitter();
					if (cost < bestCost && !Hits(poly, p0, v1, Wrap(i - 2, n), 4)) {
						bestCost = cost;
						bestKind = 1;
						bestAt = i;
					}
				}

				// Edge v-v1: extend both neighbors until they meet
				if (n < 4) continue;
				Vector2 d1 = v - p0, d2 = v1 - p3;
				float denom = Cross(d1, d2);
				if (MathF.Abs(denom) < 1e-6f) continue;
				float t = Cross(p3 - p0, d2) / denom;
				float s = Cross(p3 - p0, d1) / denom;
				if (t <= 1 || s <= 1) continue;
				Vector2 corner = p0 + d1 * t;
				if (corner.X < -0.01f || corner.Y < -0.01f || corner.X > gridWidth + 0.01f || corner.Y > gridHeight + 0.01f) continue;
				if (Cross(v1 - v, corner - v) >= 0) continue;
				float area = MathF.Abs(Cross(corner - v, v1 - v)) / 2 * jitter();
				// convex polygon remains convex and simple
				if (area < bestCost && (!reflexToo || (!Hits(poly, p0, corner, Wrap(i - 2, n), 5) && !Hits(poly, corner, p3, Wrap(i - 2, n), 5)))) {
					bestCost = area;
					bestKind = 2;
					bestAt = i;
					bestCorner = corner;
				}
			}

			if (bestAt < 0) return;
			if (bestKind == 1)
				poly.RemoveAt(bestAt);
			else {
				int next = (bestAt + 1) % n;
				poly[bestAt] = bestCorner;
				poly.RemoveAt(next);
			}
		}
	}
}
