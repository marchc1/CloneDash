using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor;

public partial class EditorPathAttachment : IPathSource
{
	private const int SAMPLES_PER_CURVE = 24;

	/// <summary>
	/// At least one curve, whole triplets
	/// </summary>
	public bool HasValidShape => ShapeEdges.Count >= 6 && ShapeEdges.Count % 3 == 0;
	int curveCount => ShapeEdges.Count / 3 - (Closed ? 0 : 1);

	// world space, rebuilt every draw
	Vector2F[] worldVertices = [];
	readonly List<Vector2F> curvePoints = [];

	private void BuildCurve() {
		curvePoints.Clear();
		if (!HasValidShape) return;
		SetupWorldTransform();
		var transform = WorldTransform;
		int count = ShapeEdges.Count;
		if (worldVertices.Length != count)
			worldVertices = new Vector2F[count];
		for (int i = 0; i < count; i++)
			worldVertices[i] = CalculateVertexWorldPosition(transform, ShapeEdges[i]);

		for (int curve = 0, curves = curveCount; curve < curves; curve++) {
			int start = curve * 3 + 1;
			Vector2F p0 = worldVertices[start], c1 = worldVertices[start + 1];
			Vector2F c2 = worldVertices[(start + 2) % count], p1 = worldVertices[(start + 3) % count];
			if (curve == 0)
				curvePoints.Add(p0);
			for (int sample = 1; sample <= SAMPLES_PER_CURVE; sample++)
				curvePoints.Add(Bezier(p0, c1, c2, p1, sample / (float)SAMPLES_PER_CURVE));
		}
	}

	private static Vector2F Bezier(Vector2F p0, Vector2F c1, Vector2F c2, Vector2F p1, float t) {
		float u = 1 - t;
		return p0 * (u * u * u) + c1 * (3 * u * u * t) + c2 * (3 * u * t * t) + p1 * (t * t * t);
	}

	public override void Render() {
		if (Hidden) return;
		if (!ModelEditor.Active.Editor.ViewportOptions.Visible[(int)ViewportItemKind.Others]) return;
		BuildCurve();
		if (Selected || Hovered) return;

		var color = Color with { A = 170 };
		for (int i = 1; i < curvePoints.Count; i++)
			ViewportDraw.Line(curvePoints[i - 1], curvePoints[i], 1.5f, color);
		for (int i = 1; i < worldVertices.Length; i += 3)
			ViewportDraw.Square(worldVertices[i], 2, color);
	}

	public override void RenderOverlay() {
		if (Hidden) return;
		BuildCurve();
		if (curvePoints.Count == 0) return;

		var color = Hovered && !Selected ? Color.Adjust(0, -0.2f, 0.25f) : Color;
		for (int i = 1; i < curvePoints.Count; i++)
			ViewportDraw.Line(curvePoints[i - 1], curvePoints[i], 2.5f, color);

		if (Selected) {
			int count = worldVertices.Length;
			// open paths point on past their ends
			if (!Closed) {
				var start = worldVertices[1];
				var end = worldVertices[count - 2];
				ViewportDraw.Dashed(start,
					start + Direction(worldVertices[2], start) * (60 / ModelEditor.Active.Editor.CameraZoom),
					1.5f, color with { A = 160 });
				ViewportDraw.Dashed(end,
					end + Direction(worldVertices[count - 3], end) * (60 / ModelEditor.Active.Editor.CameraZoom),
					1.5f, color with { A = 160 });
			}

			var handleColor = new Color(200, 205, 215, 220);
			for (int i = 1; i < count; i += 3) {
				var knot = worldVertices[i];
				var before = worldVertices[i - 1];
				var after = worldVertices[(i + 1) % count];
				bool showBefore = Closed || i > 1, showAfter = Closed || i < count - 2;
				if (showBefore) {
					ViewportDraw.Line(knot, before, 1, handleColor);
					ViewportDraw.Dot(before, 3.5f, Color.Black);
					ViewportDraw.Dot(before, 2.5f, handleColor);
				}
				if (showAfter) {
					ViewportDraw.Line(knot, after, 1, handleColor);
					ViewportDraw.Dot(after, 3.5f, Color.Black);
					ViewportDraw.Dot(after, 2.5f, handleColor);
				}
				ViewportDraw.Square(knot, 4, Color.Black);
				ViewportDraw.Square(knot, 3, color);
			}
			// todo: path edit tool (create/delete knots, new vertices mode, angle lock, cusps)
		}

		foreach (var constraint in GetModel().PathConstraints) {
			if (constraint.Target == Slot)
				constraint.RenderPositionHandle();
		}
	}

	private static Vector2F Direction(Vector2F from, Vector2F to) {
		var delta = to - from;
		float length = delta.Length;
		return length < 0.0001f ? Vector2F.Zero : delta / length;
	}

	public override bool HoverTest(Vector2F gridPos) {
		if (curvePoints.Count < 2)
			BuildCurve();
		float limit = 6 / ModelEditor.Active.Editor.CameraZoom;
		for (int i = 1; i < curvePoints.Count; i++) {
			if (DistanceToSegment(gridPos, curvePoints[i - 1], curvePoints[i]) < limit)
				return true;
		}
		return false;
	}

	private static float DistanceToSegment(Vector2F p, Vector2F a, Vector2F b) {
		var ab = b - a;
		float lengthSqr = ab.X * ab.X + ab.Y * ab.Y;
		float t = lengthSqr <= 0 ? 0 : Math.Clamp(Vector2F.Dot(p - a, ab) / lengthSqr, 0, 1);
		return p.Distance(a + ab * t);
	}

	public override void BuildProperties(Panel props, PreUIDeterminations determinations) {
		base.BuildProperties(props, determinations);
		var file = ModelEditor.Active.File;

		var pathRow = PropertiesPanel.NewRow(props, "Path", "models/path.png");
		var closed = PropertiesPanel.AddLabeledCheckbox(pathRow, "Closed", Closed);
		closed.OnCheckedChanged += (s) => file.SetProperty(this, "closed", s.Checked, () => Closed, v => Closed = v);
		var constantSpeed = PropertiesPanel.AddLabeledCheckbox(pathRow, "Constant speed", ConstantSpeed);
		constantSpeed.OnCheckedChanged += (s)
			=> file.SetProperty(this, "constant speed", s.Checked, () => ConstantSpeed, v => ConstantSpeed = v);
		pathRow.Thinking += (_) => pathRow.SetMouseInputEnabled(!ModelEditor.Active.AnimationMode);

		var lengthRow = PropertiesPanel.NewRow(props, "Length", "models/bonelength.png");
		var lengthPanel = PropertiesPanel.AddInternalPropPanel(lengthRow);
		var lengthLabel = new Label(lengthPanel);
		lengthLabel.Dock = Dock.Fill;
		lengthLabel.SetTextAlignment(Anchor.Center);
		lengthLabel.TextSize = 18;
		lengthLabel.Thinking += (_) => {
			var lengths = CurveLengths();
			lengthLabel.Text = lengths.Length == 0 ? "-" : $"{lengths[lengths.Length - 1]:0.00}";
		};

		var colorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");
		var color = PropertiesPanel.AddColorSelector(colorRow, Color);
		color.SelectedColor = Color;
		color.OnColorChanged += (_, ref newColor)
			=> file.SetProperty(this, "color", newColor, () => Color, v => Color = v);
	}

	public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
		// todo: Edit Path
		PropertiesPanel.ButtonIcon(buttons, "Freeze", "models/path.png",
			(_, _) => ModelEditor.Active.File.FreezeVertexAttachment(this));
		PropertiesPanel.ButtonIcon(buttons, "Reverse", "models/path.png",
			(_, _) => ModelEditor.Active.File.ReversePath(this));
		base.BuildOperators(buttons, determinations);
	}

	// Path constraint source

	float[] computedLengths = [];

	/// <summary>
	/// Curve end lengths, the imported ones if they still fit the shape
	/// </summary>
	private float[] CurveLengths() {
		int curves = HasValidShape ? curveCount : 0;
		if (curves <= 0) return [];
		if (Lengths.Count == curves) {
			if (computedLengths.Length != curves)
				computedLengths = new float[curves];
			for (int i = 0; i < curves; i++)
				computedLengths[i] = Lengths[i];
			return computedLengths;
		}

		if (computedLengths.Length != curves)
			computedLengths = new float[curves];
		var local = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal);
		int count = ShapeEdges.Count;
		float total = 0;
		for (int curve = 0; curve < curves; curve++) {
			int start = curve * 3 + 1;
			Vector2F p0 = Vertex(start, local, count), c1 = Vertex(start + 1, local, count);
			Vector2F c2 = Vertex(start + 2, local, count), p1 = Vertex(start + 3, local, count);
			var last = p0;
			for (int sample = 1; sample <= 32; sample++) {
				var next = Bezier(p0, c1, c2, p1, sample / 32f);
				total += last.Distance(next);
				last = next;
			}
			computedLengths[curve] = total;
		}
		return computedLengths;
	}

	private Vector2F Vertex(int i, Transformation local, int count) => local.LocalToWorld(ShapeEdges[i % count].X, ShapeEdges[i % count].Y);

	float[] IPathSource.Lengths => CurveLengths();
	int IPathSource.WorldVerticesLength => ShapeEdges.Count * 2;

	void IPathSource.ComputeWorldVertices(int start, int count, float[] world, int offset) {
		SetupWorldTransform();
		var transform = WorldTransform;
		int first = start / 2, vertices = count / 2;
		for (int i = 0; i < vertices; i++) {
			var pos = CalculateVertexWorldPosition(transform, ShapeEdges[first + i]);
			world[offset + i * 2] = pos.X;
			world[offset + i * 2 + 1] = pos.Y;
		}
	}
}
