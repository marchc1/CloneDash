using System;
using System.Collections.Generic;
using System.Linq;

namespace Poly2Tri
{
	public class Shape
	{
		/// <summary>
		/// A list of shape outline points.
		/// </summary>
		public readonly List<TriPoint> Points = [];

		public void AddPoint(float x, float y) => Points.Add(new(x, y));
		public void AddPoint(Vector2 v2) => Points.Add(new(v2.X, v2.Y));
		public void AddPoint(System.Numerics.Vector2 v2) => Points.Add(new(v2.X, v2.Y));

		/// <summary>
		/// Extra points inserted within the shape's area to control or increase triangulation.
		/// </summary>
		public readonly List<TriPoint> SteinerPoints = [];

		/// <summary>
		/// Returns, in order, each <see cref="TriPoint"/> from <see cref="Points"/>, then each <see cref="TriPoint"/> from <see cref="SteinerPoints"/>.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TriPoint> GetAllPoints() {
			foreach (var point in Points)
				yield return point;

			foreach (var point in SteinerPoints)
				yield return point;
		}

		/// <summary>
		/// Specific vertex-to-vertex connections for manual adjustments to the triangulation result.
		/// <br/><br/><b>NOTE: </b> This differs from default Poly2Tri so it may result in additional instability, might need SafeTriangulate of some kind in the future to compensate
		/// </summary>
		public readonly List<Edge> ConstrainedEdges = [];

		/// <summary>
		/// A list of subtraction shapes fully contained inside this shape.<para/>
		/// Shapes added to this list will be used to create holes during triangulation. Any that are outside or intersect the shape outline are invalid.
		/// </summary>
		public readonly List<Shape> Holes = [];

		public Rectangle Bounds { get; private set; }

		public Shape() { }

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points</param>
		public Shape(IList<TriPoint> points) {
			Points.AddRange(points);
		}

#nullable enable
		public Shape(Span<float> x, Span<float> y, object[]? z = null) {
			for (int i = 0; i < x.Length; i++) {
				Points.Add(new(x[i], y[i], z?[i]));
			}
		}
#nullable disable
		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points</param>
		public Shape(IList<TriPoint> points, Vector2 offset, float scale) {
			for (int i = 0; i < points.Count; i++)
				Points.Add(new TriPoint(offset + ((Vector2)points[i] * scale)));
		}

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points.</param>
		public Shape(IEnumerable<TriPoint> points) : this((points as IList<TriPoint>) ?? points.ToArray()) { }

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points.</param>
		public Shape(params TriPoint[] points) : this((IList<TriPoint>)points) { }

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points.</param>
		public Shape(params Vector2[] points) : this((IList<Vector2>)points) { }

		/// <summary>
		/// Creates a polygon from a list of at least 3 Vector3 points, with no duplicates.
		/// </summary>
		/// <param name="points">The input points.</param>
		/// <param name="offset">An offset to apply to all of the provided points.</param>
		/// <param name="scale">The scale of the provided points. 0.5f is half size. 2.0f is 2x the normal size.</param>
		public Shape(IList<Vector2> points, Vector2 offset, float scale) {
			for (int i = 0; i < points.Count; i++)
				Points.Add(new TriPoint(offset + (points[i] * scale)));
		}

		/// <summary>
		/// Creates a polygon from a list of at least 3 Vector3 points, with no duplicates.
		/// </summary>
		/// <param name="points">The input points.</param>
		public Shape(IList<Vector2> points) : this(points, Vector2.Zero, 1.0f) { }

		/// <summary>
		/// Produces a <see cref="RectangleF"/> which contains all of the shape's points.
		/// </summary>
		public Rectangle CalculateBounds() {
			Rectangle b = new() {
				MinX = float.MaxValue,
				MinY = float.MaxValue,
				MaxX = float.MinValue,
				MaxY = float.MinValue,
			};

			foreach (TriPoint p in Points) {
				if (p.X < b.MinX)
					b.MinX = p.X;
				else if (p.X > b.MaxX)
					b.MaxX = p.Y;

				if (p.Y < b.MinY)
					b.MinY = p.Y;
				else if (p.Y > b.MaxY)
					b.MaxY = p.Y;
			}

			return b;
		}

		/// <summary>
		/// Triangulates the shape and adds all of the points (in triangle list layout) to the provided output.
		/// </summary>
		/// <param name="output">The output list.</param>
		public void Triangulate(IList<Vector2> output, Vector2 offset, float scale = 1.0f) {
			Points.Reverse();
			SweepContext tcx = new();
			tcx.AddPoints(Points);

			// Hole edges
			foreach (Shape h in Holes)
				tcx.AddHole(h.Points);

			foreach (TriPoint t in SteinerPoints)
				tcx.AddPoint(t);

			tcx.InitTriangulation();
			Sweep sweep = new();
			sweep.Triangulate(tcx);

			List<Triangle> triangles = tcx.GetTriangles();
			foreach (Triangle tri in triangles) {
				output.Add(((Vector2)tri.Points[0] * scale) + offset);
				output.Add(((Vector2)tri.Points[2] * scale) + offset);
				output.Add(((Vector2)tri.Points[1] * scale) + offset);
			}
		}

		/// <summary>
		/// Triangulates the shape and adds all of the triangles to the provided output.
		/// </summary>
		/// <param name="output">The output list.</param>
		public void Triangulate(IList<Triangle> output) {
			SweepContext tcx = new();
			tcx.AddPoints(Points);

			foreach (Shape p in Holes)
				tcx.AddHole(p.Points);

			foreach (TriPoint t in SteinerPoints)
				tcx.AddPoint(t);

			foreach (Edge e in ConstrainedEdges)
				tcx.AddConstrainedEdge(e.Q, e.P);

			tcx.InitTriangulation();
			Sweep sweep = new();
			sweep.Triangulate(tcx);

			List<Triangle> triangles = tcx.GetTriangles();
			foreach (Triangle tri in triangles)
				output.Add(tri);
		}

		public void Scale(float scale) {
			for (int i = 0; i < Points.Count; i++)
				Points[i] *= scale;

			foreach (Shape h in Holes)
				h.Scale(scale);
		}

		public void Scale(Vector2 scale) {
			for (int i = 0; i < Points.Count; i++)
				Points[i] *= scale;

			foreach (Shape h in Holes)
				h.Scale(scale);
		}

		public void Offset(Vector2 offset) {
			for (int i = 0; i < Points.Count; i++)
				Points[i] += offset;

			foreach (Shape h in Holes)
				h.Offset(offset);
		}

		public void ScaleAndOffset(Vector2 offset, float scale) {
			for (int i = 0; i < Points.Count; i++) {
				Points[i] *= scale;
				Points[i] += offset;
			}

			foreach (Shape h in Holes)
				h.ScaleAndOffset(offset, scale);
		}

		public void ScaleAndOffset(Vector2 offset, Vector2 scale) {
			for (int i = 0; i < Points.Count; i++) {
				Points[i] *= scale;
				Points[i] += offset;
			}

			foreach (Shape h in Holes)
				h.ScaleAndOffset(offset, scale);
		}

		public bool Contains(Shape shape) {
			for (int i = 0; i < shape.Points.Count; i++) {
				// We only need 1 point to be outside to invalidate a containment.
				if (!Contains((Vector2)shape.Points[i]))
					return false;
			}

			return true;
		}

		public bool Contains(Vector2 point) {
			for (int i = 0; i < Holes.Count; i++) {
				if (Holes[i].Contains(point))
					return false;
			}

			// Thanks to: https://codereview.stackexchange.com/a/108903
			int polygonLength = Points.Count;
			int j = 0;
			bool inside = false;
			double pointX = point.X, pointY = point.Y; // x, y for tested point.

			// start / end point for the current polygon segment.
			double startX, startY, endX, endY;
			Vector2 endPoint = (Vector2)Points[polygonLength - 1];
			endX = endPoint.X;
			endY = endPoint.Y;

			while (j < polygonLength) {
				startX = endX; startY = endY;
				endPoint = (Vector2)Points[j++];
				endX = endPoint.X; endY = endPoint.Y;
				//
				inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
						  && /* if so, test if it is under the segment */
						  ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
			}

			return inside;
		}
	}
}
