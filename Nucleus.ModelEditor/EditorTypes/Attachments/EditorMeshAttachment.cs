// TODO: This file needs to be split apart!

using Newtonsoft.Json;

using Nucleus.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Models;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using Color = Nucleus.Common.Types.Color;
using RectangleF = Nucleus.Types.RectangleF;

namespace Nucleus.ModelEditor
{
	public enum EditMesh_Mode
	{
		Modify,
		Create,
		Delete,
		New,
		Reset
	}
	public class EditMeshOperator : Operator
	{
		public static ConVar meshedit_triangles = new("meshedit_triangles", "0", FCvar.Saved, "Visualizes the triangulated mesh attachment when using the Edit Mesh operator.");
		public static ConVar meshedit_dim = new("meshedit_dim", "0", FCvar.Saved, "Dims the mesh attachment's texture when using the Edit Mesh operator.");
		public static ConVar meshedit_isolate = new("meshedit_isolate", "0", FCvar.Saved, "Isolates the active mesh attachment when using the Edit Mesh operator.");
		public static ConVar meshedit_deformed = new("meshedit_deformed", "1", FCvar.Saved, "Renders the mesh deformed");

		public EditMesh_Mode CurrentMode { get; private set; } = EditMesh_Mode.Modify;
		public override string Name => $"Edit Mesh: {CurrentMode}";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(EditorVertex)];

		public static ConVar meshedit_trace_detail = new("meshedit_trace_detail", "50", FCvar.Saved,
			"Edit Mesh trace: how many hull vertices.");
		public static ConVar meshedit_trace_concavity = new("meshedit_trace_concavity", "50", FCvar.Saved,
			"Edit Mesh trace: priority for vertices in concave areas.");
		public static ConVar meshedit_trace_refinement = new("meshedit_trace_refinement", "50", FCvar.Saved,
			"Edit Mesh trace: effort spent finding a good hull.");
		public static ConVar meshedit_trace_alpha = new("meshedit_trace_alpha", "0", FCvar.Saved,
			"Edit Mesh trace: pixels with alpha at or below this are ignored.");
		public static ConVar meshedit_trace_padding = new("meshedit_trace_padding", "2", FCvar.Saved,
			"Edit Mesh trace: pixels between the image and the hull.");

		public void SetMode(EditMesh_Mode mode) {
			// Leaving New closes the outline, a too short one is dropped
			if (CurrentMode == EditMesh_Mode.New && mode != EditMesh_Mode.New && WorkingLines.Count >= 3 && HullSegmentAllowed(WorkingLines[WorkingLines.Count - 1], WorkingLines[0], true))
				ApplyNew();
			CurrentMode = mode;
			UpdateButtonState();
			if (mode == EditMesh_Mode.New)
				FitUV();
		}

		private static readonly Vector2F[] imageCorners = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

		private void ReplaceHull(IEnumerable<Vector2F> uvs) {
			var attachment = Attachment;
			ModelEditor.Active.Actions.TrackMesh(attachment);
			FitUV();
			attachment.ShapeEdges.Clear();
			foreach (var uv in uvs) {
				var pos = PosOf(uv);
				attachment.ShapeEdges.Add(new EditorVertex {
					X = pos.X,
					Y = pos.Y,
					U = uv.X,
					V = uv.Y,
					Attachment = attachment
				});
			}
			attachment.SteinerPoints.Clear();
			attachment.Weights.Clear();
			attachment.SelectedVertices.Clear();
			attachment.Invalidate();
		}

		/// <summary>
		/// Adds a vertex in the middle of every big triangle. Clears the weights.
		/// </summary>
		public void Generate() {
			var attachment = Attachment;
			attachment.RefreshDelaunator();
			var tris = attachment.Triangles;

			float biggest = 0;
			for (int i = 0; i < tris.Count; i += 3)
				biggest = MathF.Max(biggest, Area(i, tris));
			if (biggest <= 0) return;

			ModelEditor.Active.Actions.TrackMesh(attachment);
			List<EditorVertex> added = [];
			for (int i = 0; i < tris.Count; i += 3) {
				if (Area(i, tris) < biggest / 2) continue;
				EditorVertex t0 = tris[i], t1 = tris[i + 1], t2 = tris[i + 2];
				added.Add(new EditorVertex {
					X = (t0.X + t1.X + t2.X) / 3, Y = (t0.Y + t1.Y + t2.Y) / 3,
					U = (t0.U + t1.U + t2.U) / 3, V = (t0.V + t1.V + t2.V) / 3,
					Attachment = attachment
				});
			}
			attachment.Weights.Clear();
			attachment.SteinerPoints.AddRange(added);
			attachment.Invalidate();
		}

		private static float Area(int i, IReadOnlyList<EditorVertex> tris)
			=> MathF.Abs((tris[i + 1].X - tris[i].X) * (tris[i + 2].Y - tris[i].Y) - (tris[i + 1].Y - tris[i].Y) * (tris[i + 2].X - tris[i].X)) / 2;

		readonly Random traceRandom = new();
		bool traceDirty;
		Window? traceWindow;
		CenteredObjectsPanel? panel;
		float editHeight;

		/// <summary>
		/// Traces a new hull around the image. Clears the weights and inner vertices.
		/// </summary>
		public void Trace() {
			var attachment = Attachment;
			var model = attachment.Slot.Bone.Model;
			var image = attachment.Path == null ? null : model.ResolveImage(attachment.Path);
			if (image == null || !model.Images.TextureAtlas.UnpackedImages.TryGetValue(image.Name, out var pixels)) {
				Logs.Warn($"Trace: no image for '{attachment.Path}'");
				return;
			}

			var uvs = MeshTracer.Trace(pixels,
				meshedit_trace_detail.GetFloat(),
				meshedit_trace_concavity.GetFloat(),
				meshedit_trace_refinement.GetFloat(),
				meshedit_trace_alpha.GetFloat(),
				meshedit_trace_padding.GetFloat(),
				traceRandom);
			if (uvs == null) {
				Logs.Warn($"Trace: '{attachment.Path}' has no pixels above the alpha threshold");
				return;
			}
			ReplaceHull(uvs);
		}

		private void OpenTrace() {
			Trace();
			if (traceWindow != null || panel == null) return;

			var win = traceWindow = new Window(panel);
			win.Title = "Trace";
			win.HideNonCloseButtons();
			win.Resizable = false;
			win.Removed += _ => {
				traceWindow = null;
				FitPanel();
			};

			var form = new Form(win);
			form.Dock = Dock.Top;
			form.DockPadding = RectangleF.TLRB(GAP);
			form.RowGap = GAP;
			Slider(form, "Detail", meshedit_trace_detail, 0, 100, "%");
			Slider(form, "Concavity", meshedit_trace_concavity, 0, 100, "%");
			Slider(form, "Refinement", meshedit_trace_refinement, 0, 100, "%");
			Slider(form, "Alpha threshold", meshedit_trace_alpha, 0, 255, "");
			Slider(form, "Padding", meshedit_trace_padding, 0, 64, " px");
			var refresh = form.Add(null, r => new Button(r));
			refresh.Text = "Refresh";
			refresh.OnButtonClick += (_, _) => Trace();

			// titlebar + window content margins + rows
			win.Size = new(360, win.Titlebar.Size.H + 8 + GAP * 2 + (ROW_HEIGHT + GAP) * 6);
			FitPanel();
		}

		private void Slider(Form form, string label, ConVar convar, double min, double max, string suffix) {
			var slider = form.Add(label, r => new NumSlider(r));
			slider.MinimumValue = min;
			slider.MaximumValue = max;
			slider.Digits = 0;
			slider.Suffix = suffix;
			slider.Value = convar.GetDouble();
			slider.OnValueChanged += (_, _, value) => {
				convar.SetValue(value);
				traceDirty = true;
			};
		}

		private void FitPanel() {
			if (panel == null) return;
			float height = MathF.Max(editHeight, traceWindow?.Size.H ?? 0);
			panel.Size = new(0, height + GAP);
		}

		/// <summary>
		/// Back to a quad over the image. Clears the weights and inner vertices.
		/// </summary>
		public void Reset() => ReplaceHull(imageCorners);

		private Button ModifyButton, CreateButton, DeleteButton, NewButton;
		List<EditorVertex> WorkingLines = [];
		private void UpdateButtonState() {
			ModifyButton.Pulsing = CurrentMode == EditMesh_Mode.Modify;
			CreateButton.Pulsing = CurrentMode == EditMesh_Mode.Create;
			DeleteButton.Pulsing = CurrentMode == EditMesh_Mode.Delete;
			NewButton.Pulsing = CurrentMode == EditMesh_Mode.New;
			WorkingLines.Clear();
		}

		public void CycleMode() {
			SetMode(CurrentMode switch {
				EditMesh_Mode.Modify => EditMesh_Mode.Create,
				EditMesh_Mode.Create => EditMesh_Mode.Delete,
				_ => EditMesh_Mode.Modify
			});
		}

		private const float ROW_HEIGHT = ToolRows.ROW_HEIGHT;
		private const float GAP = ToolRows.GAP;

		public override void ChangeEditorProperties(CenteredObjectsPanel panel) {
			var win = new Window(panel);
			win.Title = "Edit Mesh";
			win.HideNonCloseButtons();
			win.Resizable = false;

			var content = new Panel(win);
			content.Dock = Dock.Fill;
			content.DockPadding = RectangleF.TLRB(GAP);
			content.SetPaintBackgroundEnabled(false);
			content.SetPaintBorderEnabled(false);

			var tools = ToolRows.AddRow(content);
			ModifyButton = ToolRows.AddButton(tools, "Modify", () => SetMode(EditMesh_Mode.Modify));
			CreateButton = ToolRows.AddButton(tools, "Create", () => SetMode(EditMesh_Mode.Create));
			DeleteButton = ToolRows.AddButton(tools, "Delete", () => SetMode(EditMesh_Mode.Delete));
			ToolRows.SpaceCells(tools);

			var actions = ToolRows.AddRow(content);
			NewButton = ToolRows.AddButton(actions, "New",
				() => SetMode(CurrentMode == EditMesh_Mode.New ? EditMesh_Mode.Modify : EditMesh_Mode.New));
			ToolRows.AddButton(actions, "Reset", Reset);
			ToolRows.AddButton(actions, "Generate", Generate);
			ToolRows.AddButton(actions, "Trace", OpenTrace);
			ToolRows.SpaceCells(actions);

			var display = ToolRows.AddRow(content);
			display.Size = new(0, ROW_HEIGHT);
			display.DockMargin = RectangleF.TLRB(0);
			ToolRows.AddCheckbox(display, "Triangles", meshedit_triangles);
			ToolRows.AddCheckbox(display, "Dim", meshedit_dim);
			ToolRows.AddCheckbox(display, "Isolate", meshedit_isolate);
			ToolRows.AddCheckbox(display, "Deformed", meshedit_deformed);
			ToolRows.SpaceCells(display);

			editHeight = win.Titlebar.Size.H + 8 + GAP * 2 + ROW_HEIGHT * 3 + GAP * 2;
			win.Size = new(440, editHeight);
			this.panel = panel;
			panel.XSeparation = GAP;
			FitPanel();

			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}

		public EditorVertex? HoveredVertex;
		public bool IsHoveredSteinerPoint;
		/// <summary>
		/// Hull or inner edge under the mouse, only when no vertex is
		/// </summary>
		public (EditorVertex A, EditorVertex B, bool Hull)? HoveredEdge;
		public EditorMeshAttachment Attachment => this.UIDeterminations.Last as EditorMeshAttachment ?? throw new Exception("Wtf?");

		// in pixels
		private const float VERTEX_PICK = 10;
		private const float EDGE_PICK = 6;
		private const float DRAG_START = 4;
		private const double DOUBLE_CLICK = 0.3;
		static bool ShiftDown => EngineCore.Level.FrameState.Keyboard.ShiftDown;
		static bool CtrlDown => EngineCore.Level.FrameState.Keyboard.ControlDown;

		bool mouseDown;
		bool dragged;
		bool boxing;
		Vector2F clickScreen;
		EditorVertex? edgeFrom;
		readonly List<(EditorVertex Vertex, Vector2F Pos, Vector2F UV)> moving = [];
		readonly HashSet<EditorVertex> movingSet = [];
		Vector2F moveStartLocal;
		float[] uvMap = new float[6];
		double lastClickTime = -1;
		EditorVertex? lastClickVertex;

		private Vector2F WorldOf(EditorVertex vertex) => Attachment.CalculateVertexWorldPosition(Attachment.WorldTransform, vertex);

		private Vector2F MouseLocal(ModelEditor editor, Vector2F screen) {
			PatchAttachmentTransform();
			var local = Attachment.WorldTransform.WorldToLocal(editor.Editor.ScreenToGrid(screen));
			RestoreAttachmentTransform();
			return local;
		}

		public override void Think(ModelEditor editor, Vector2F mousePos) {
			HoveredVertex = null;
			HoveredEdge = null;
			IsHoveredSteinerPoint = false;
			if (traceDirty && !EngineCore.Level.FrameState.Mouse.MouseHeld) {
				traceDirty = false;
				Trace();
			}
			if (!editor.Editor.IsHovered() && !mouseDown) return;

			PatchAttachmentTransform();
			if (CurrentMode == EditMesh_Mode.New)
				HoverWorkingLines(mousePos);
			else if (!(mouseDown && edgeFrom != null && ShiftDown))
				// shift creates on top of a vertex
				HoverMesh(editor, mousePos, vertices: !(CurrentMode == EditMesh_Mode.Create && ShiftDown));
			RestoreAttachmentTransform();
		}

		private void HoverWorkingLines(Vector2F mousePos) {
			float best = VERTEX_PICK / ModelEditor.Active.Editor.CameraZoom;
			foreach (var vertex in WorkingLines) {
				var d = Attachment.WorldTransform.LocalToWorld(vertex.X, vertex.Y).Distance(mousePos);
				if (d < best) {
					best = d;
					HoveredVertex = vertex;
				}
			}
		}

		private bool Skipped(EditorVertex vertex) => vertex == edgeFrom || movingSet.Contains(vertex);

		private void HoverMesh(ModelEditor editor, Vector2F mousePos, bool vertices) {
			var attachment = Attachment;
			float zoom = editor.Editor.CameraZoom;
			float best = VERTEX_PICK / zoom;
			int hull = attachment.ShapeEdges.Count;
			int i = 0;
			foreach (var vertex in attachment.GetVertices()) {
				bool steiner = i++ >= hull;
				if (!vertices || Skipped(vertex)) continue;
				float distance = WorldOf(vertex).Distance(mousePos);
				if (distance < best) {
					best = distance;
					HoveredVertex = vertex;
					IsHoveredSteinerPoint = steiner;
				}
			}
			if (HoveredVertex != null) return;

			best = EDGE_PICK / zoom;
			foreach (var edge in Edges()) {
				if (Skipped(edge.A) || Skipped(edge.B)) continue;
				float distance = DistanceToSegment(mousePos, WorldOf(edge.A), WorldOf(edge.B));
				if (distance < best) {
					best = distance;
					HoveredEdge = edge;
				}
			}
		}

		private IEnumerable<(EditorVertex A, EditorVertex B, bool Hull)> Edges() {
			var attachment = Attachment;
			var hull = attachment.ShapeEdges;
			for (int i = 0; i < hull.Count; i++)
				yield return (hull[i], hull[(i + 1) % hull.Count], true);
			foreach (var vertex in attachment.GetVertices()) {
				foreach (var other in vertex.ConstrainedVertices) {
					if (attachment.IndexOfVertex(vertex) < attachment.IndexOfVertex(other))
						yield return (vertex, other, false);
				}
			}
		}

		private static float ProjectT(Vector2F p, Vector2F a, Vector2F b) {
			var ab = b - a;
			float len = ab.X * ab.X + ab.Y * ab.Y;
			if (len <= 0) return 0;
			return Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / len, 0, 1);
		}

		private static float DistanceToSegment(Vector2F p, Vector2F a, Vector2F b) => p.Distance(Vector2F.Lerp(ProjectT(p, a, b), a, b));

		private static int Orientation(Vector2F a, Vector2F b, Vector2F c) {
			double d = ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);
			return d switch {
				> 0 => 1,
				< 0 => -1,
				_ => 0
			};
		}

		// touching and overlapping count
		private static bool SegmentsCross(Vector2F a, Vector2F b, Vector2F c, Vector2F d) {
			int o1 = Orientation(a, b, c), o2 = Orientation(a, b, d);
			int o3 = Orientation(c, d, a), o4 = Orientation(c, d, b);
			if (o1 != o2 && o3 != o4) return true;
			return (o1 == 0 && On(a, c, b)) || (o2 == 0 && On(a, d, b)) || (o3 == 0 && On(c, a, d)) || (o4 == 0 && On(c, b, d));
		}

		private static bool On(Vector2F p, Vector2F q, Vector2F r) => MathF.Min(p.X, r.X) <= q.X && q.X <= MathF.Max(p.X, r.X)
				&& MathF.Min(p.Y, r.Y) <= q.Y && q.Y <= MathF.Max(p.Y, r.Y);

		/// <summary>
		/// Poly2Tri throws on edges that cross another edge or run through a vertex
		/// </summary>
		private bool EdgeAllowed(EditorVertex from, EditorVertex? to, Vector2F end, (EditorVertex, EditorVertex)? splitting = null) {
			Vector2F start = from.ToVector();
			foreach (var edge in Edges()) {
				if (edge.A == from || edge.B == from || edge.A == to || edge.B == to) continue;
				if (splitting is { } split && ((edge.A == split.Item1 && edge.B == split.Item2) || (edge.A == split.Item2 && edge.B == split.Item1))) continue;
				if (SegmentsCross(start, end, edge.A.ToVector(), edge.B.ToVector())) return false;
			}
			foreach (var vertex in Attachment.GetVertices()) {
				if (vertex != from && vertex != to && DistanceToSegment(vertex.ToVector(), start, end) < 0.01f) return false;
			}
			return true;
		}

		private bool Connected(EditorVertex from, EditorVertex to) {
			if (from.IsConstrainedTo(to)) return true;
			var hull = Attachment.ShapeEdges;
			int i = hull.IndexOf(from), j = hull.IndexOf(to);
			if (i < 0 || j < 0) return false;
			int gap = Math.Abs(i - j);
			return gap == 1 || gap == hull.Count - 1;
		}

		private void FitUV() => uvMap = FitUVMap(Attachment);

		// uv = (m0 x + m1 y + m2, m3 x + m4 y + m5), least squares over the mesh
		public static float[] FitUVMap(EditorMeshAttachment attachment) {
			var uvMap = new float[6];
			double sxx = 0;
			double sxy = 0;
			double sx = 0;
			double syy = 0;
			double sy = 0;
			double n = 0;
			double sxu = 0;
			double syu = 0;
			double su = 0;
			double sxv = 0;
			double syv = 0;
			double sv = 0;
			foreach (var vertex in attachment.GetVertices()) {
				double x = vertex.X, y = vertex.Y;
				sxx += x * x;
				sxy += x * y;
				sx += x;
				syy += y * y;
				sy += y;
				n++;
				sxu += x * vertex.U;
				syu += y * vertex.U;
				su += vertex.U;
				sxv += x * vertex.V;
				syv += y * vertex.V;
				sv += vertex.V;
			}
			double det = Det3(sxx, sxy, sx, sxy, syy, sy, sx, sy, n);
			bool fits = n >= 3 && Math.Abs(det) > 1e-9 * Math.Abs(sxx * syy * n);
			if (fits) {
				Solve(sxu, syu, su, 0, uvMap, sxx, sxy, sx, syy, sy, n, det);
				Solve(sxv, syv, sv, 3, uvMap, sxx, sxy, sx, syy, sy, n, det);
				// All uvs the same, like a freshly converted region
				fits = Math.Abs(uvMap[0] * uvMap[4] - uvMap[1] * uvMap[3]) > 1e-12;
			}
			if (!fits) {
				// GetVertexUV's mapping
				float w = attachment.LocalWidth, h = attachment.LocalHeight;
				uvMap = [1 / w, 0, 0.5f, 0, 1 / h, 0.5f];
			}
			return uvMap;
		}

		private static void Solve(double r0, double r1, double r2, int index, float[] uvMap, double sxx, double sxy, double sx, double syy, double sy, double n, double det) {
			uvMap[index] = (float)(Det3(r0, sxy, sx, r1, syy, sy, r2, sy, n) / det);
			uvMap[index + 1] = (float)(Det3(sxx, r0, sx, sxy, r1, sy, sx, r2, n) / det);
			uvMap[index + 2] = (float)(Det3(sxx, sxy, r0, sxy, syy, r1, sx, sy, r2) / det);
		}

		private static double Det3(double a, double b, double c, double d, double e, double f, double g, double h, double i)
			=> a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);

		/// <summary>
		/// Inverse of <see cref="FitUVMap"/>
		/// </summary>
		public static Vector2F PosOf(float[] uvMap, Vector2F uv) {
			var offset = uv - new Vector2F(uvMap[2], uvMap[5]);
			float a = uvMap[0], b = uvMap[1], c = uvMap[3], e = uvMap[4];
			float det = a * e - b * c;
			if (det == 0) return Vector2F.Zero;
			return new((e * offset.X - b * offset.Y) / det, (a * offset.Y - c * offset.X) / det);
		}

		private Vector2F UvOf(Vector2F pos) => UvDelta(pos) + new Vector2F(uvMap[2], uvMap[5]);
		private Vector2F PosOf(Vector2F uv) => PosOf(uvMap, uv);
		private Vector2F ClampToImage(Vector2F pos) {
			var uv = UvOf(pos);
			return PosOf(new(Math.Clamp(uv.X, 0, 1), Math.Clamp(uv.Y, 0, 1)));
		}

		private Vector2F UvDelta(Vector2F delta) => new(uvMap[0] * delta.X + uvMap[1] * delta.Y, uvMap[3] * delta.X + uvMap[4] * delta.Y);
		private Vector2F PosDelta(Vector2F delta) {
			float a = uvMap[0], b = uvMap[1], c = uvMap[3], e = uvMap[4];
			float det = a * e - b * c;
			if (det == 0) return Vector2F.Zero;
			return new((e * delta.X - b * delta.Y) / det, (a * delta.Y - c * delta.X) / det);
		}

		private void BeginMove(IEnumerable<EditorVertex> vertices, Vector2F startLocal) {
			moving.Clear();
			movingSet.Clear();
			foreach (var vertex in vertices) {
				moving.Add((vertex, vertex.ToVector(), new(vertex.U, vertex.V)));
				movingSet.Add(vertex);
			}
			moveStartLocal = startLocal;
			FitUV();
		}

		// Only the vertices move, not the image
		private void MoveVertices(Vector2F delta) {
			var uvDelta = UvDelta(delta);
			foreach (var move in moving) {
				var uv = move.UV;
				var want = uv + uvDelta;
				var got = new Vector2F(
					Math.Clamp(want.X, MathF.Min(0, uv.X), MathF.Max(1, uv.X)),
					Math.Clamp(want.Y, MathF.Min(0, uv.Y), MathF.Max(1, uv.Y)));
				var pos = move.Pos + delta;
				if (got != want)
					pos += PosDelta(got - want);
				move.Vertex.SetPos(pos);
				move.Vertex.U = got.X;
				move.Vertex.V = got.Y;
				UpdateBoneLocals(move.Vertex);
			}
			Attachment.Invalidate();
		}

		private void UpdateBoneLocals(EditorVertex vertex) {
			var world = Attachment.WorldTransform.LocalToWorld(vertex.X, vertex.Y);
			foreach (var weights in Attachment.Weights) {
				if (weights.TryGetVertexWeight(vertex, out _))
					weights.SetVertexPos(vertex, weights.Bone.WorldTransform.WorldToLocal(world));
			}
		}

		// Barycentric weights from the triangle under the point, or the nearest vertex
		private (EditorVertex, float)[] Interpolation(Vector2F point) {
			var attachment = Attachment;
			attachment.RefreshDelaunator();
			var tris = attachment.Triangles;
			for (int i = 0; i < tris.Count; i += 3) {
				EditorVertex t0 = tris[i], t1 = tris[i + 1], t2 = tris[i + 2];
				double denominator = ((double)t1.Y - t2.Y) * (t0.X - t2.X) + ((double)t2.X - t1.X) * (t0.Y - t2.Y);
				if (denominator == 0) continue;
				double w0 = (((double)t1.Y - t2.Y) * (point.X - t2.X) + ((double)t2.X - t1.X) * (point.Y - t2.Y)) / denominator;
				double w1 = (((double)t2.Y - t0.Y) * (point.X - t2.X) + ((double)t0.X - t2.X) * (point.Y - t2.Y)) / denominator;
				double w2 = 1 - w0 - w1;
				if (w0 >= -1e-5 && w1 >= -1e-5 && w2 >= -1e-5)
					return [(t0, (float)w0), (t1, (float)w1), (t2, (float)w2)];
			}
			return [(attachment.GetVertices().MinBy(x => (x.ToVector() - point).Length)!, 1)];
		}

		private EditorVertex NewVertex(Vector2F pos, (EditorVertex Vertex, float Weight)[] parts) {
			var vertex = new EditorVertex { X = pos.X, Y = pos.Y, Attachment = Attachment };
			foreach (var part in parts) {
				vertex.U += part.Vertex.U * part.Weight;
				vertex.V += part.Vertex.V * part.Weight;
			}
			foreach (var weights in Attachment.Weights) {
				float value = 0;
				foreach (var part in parts)
					value += weights.TryGetVertexWeight(part.Vertex) * part.Weight;
				if (value > 0)
					weights.SetVertexWeight(vertex, value);
			}
			UpdateBoneLocals(vertex);
			return vertex;
		}

		private EditorVertex AddVertex(Vector2F local) {
			var vertex = NewVertex(local, Interpolation(local));
			Attachment.SteinerPoints.Add(vertex);
			Attachment.Invalidate();
			return vertex;
		}

		private EditorVertex SplitEdge((EditorVertex A, EditorVertex B, bool Hull) edge, Vector2F local) {
			float t = ProjectT(local, edge.A, edge.B);
			var vertex = NewVertex(Vector2F.Lerp(t, edge.A, edge.B), [(edge.A, 1 - t), (edge.B, t)]);
			if (edge.Hull)
				Attachment.ShapeEdges.Insert(Attachment.ShapeEdges.IndexOf(edge.A) + 1, vertex);
			else {
				Attachment.SteinerPoints.Add(vertex);
				edge.A.UnconstrainFrom(edge.B);
				edge.A.ConstrainTo(vertex);
				vertex.ConstrainTo(edge.B);
			}
			Attachment.Invalidate();
			return vertex;
		}

		private bool DeleteVertex(EditorVertex vertex) {
			var attachment = Attachment;
			if (attachment.ShapeEdges.Contains(vertex) && attachment.ShapeEdges.Count <= 3) return false;
			attachment.RemoveVertex(vertex);
			foreach (var weights in attachment.Weights)
				weights.RemoveVertex(vertex);
			attachment.SelectedVertices.Remove(vertex);
			if (HoveredVertex == vertex)
				HoveredVertex = null;
			attachment.Invalidate();
			return true;
		}

		public override bool SelectMultiple => true;
		public override void Selected(ModelEditor editor, IEditorType type) {
			base.Selected(editor, type);
		}

		public Color Color { get; set; } = Color.White;

		public override RectangleF? SelectionBox {
			get {
				if (!boxing || !dragged) return null;
				var mouse = ModelEditor.Active.Editor.GetMousePos();
				return EditorPanel.ScreenBox(clickScreen, mouse);
			}
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);
			editor.Actions.TrackMesh(Attachment);
			mouseDown = true;
			dragged = boxing = false;
			edgeFrom = null;
			moving.Clear();
			movingSet.Clear();
			clickScreen = mousePos;
			var local = MouseLocal(editor, mousePos);

			var now = EngineCore.Level.Realtime;
			bool doubleClick = HoveredVertex != null && HoveredVertex == lastClickVertex && now - lastClickTime < DOUBLE_CLICK;
			lastClickVertex = doubleClick ? null : HoveredVertex;
			lastClickTime = now;

			switch (CurrentMode) {
				case EditMesh_Mode.Modify:
					if (HoveredVertex == null) {
						boxing = true;
						break;
					}
					if (doubleClick) {
						DeleteVertex(HoveredVertex);
						break;
					}
					var selected = Attachment.SelectedVertices;
					if (CtrlDown) {
						if (!selected.Remove(HoveredVertex))
							selected.Add(HoveredVertex);
						break;
					}
					if (!selected.Contains(HoveredVertex)) {
						selected.Clear();
						selected.Add(HoveredVertex);
					}
					BeginMove(selected, local);
					break;
				case EditMesh_Mode.Create:
					if (HoveredVertex != null) {
						if (doubleClick)
							DeleteVertex(HoveredVertex);
						else
							edgeFrom = HoveredVertex;
						break;
					}
					EditorVertex? created = null;
					if (HoveredEdge is { } edge)
						created = SplitEdge(edge, local);
					else if (Attachment.InsideHull(local.X, local.Y))
						created = AddVertex(local);
					if (created != null)
						BeginMove([created], local);
					break;
				case EditMesh_Mode.Delete:
					if (HoveredVertex != null)
						DeleteVertex(HoveredVertex);
					else if (HoveredEdge is { Hull: false } inner)
						inner.A.UnconstrainFrom(inner.B);
					else if (HoveredEdge == null)
						boxing = true;
					break;
				case EditMesh_Mode.New:
					ClickNew(local, doubleClick);
					break;
			}
		}

		private void ClickNew(Vector2F local, bool doubleClick) {
			if (HoveredVertex != null) {
				if (HoveredVertex == WorkingLines[0] && WorkingLines.Count >= 3) {
					if (HullSegmentAllowed(WorkingLines[WorkingLines.Count - 1], WorkingLines[0], true))
						SetMode(EditMesh_Mode.Modify);
				}
				else if (doubleClick)
					WorkingLines.Remove(HoveredVertex);
				else
					BeginMove([HoveredVertex], local);
				return;
			}
			var point = ClampToImage(local);
			if (WorkingLines.Count > 0 && !HullSegmentAllowed(WorkingLines[WorkingLines.Count - 1], point, false)) return;
			var uv = UvOf(point);
			WorkingLines.Add(new EditorVertex {
				X = point.X,
				Y = point.Y,
				U = uv.X,
				V = uv.Y,
				Attachment = Attachment
			});
		}

		private bool HullSegmentAllowed(Vector2F start, Vector2F end, bool closing) {
			int count = WorkingLines.Count;
			for (int i = 0; i < count - 1; i++) {
				if (i == count - 2 || (closing && i == 0)) continue;
				if (SegmentsCross(start, end, WorkingLines[i], WorkingLines[i + 1])) return false;
			}
			for (int i = closing ? 1 : 0; i < count - 1; i++) {
				if (DistanceToSegment(WorkingLines[i], start, end) < 0.01f) return false;
			}
			return true;
		}

		private void ApplyNew() {
			var attachment = Attachment;
			ModelEditor.Active.Actions.TrackMesh(attachment);
			attachment.ShapeEdges.Clear();
			attachment.ShapeEdges.AddRange(WorkingLines);
			attachment.SteinerPoints.Clear();
			attachment.Weights.Clear();
			attachment.SelectedVertices.Clear();
			attachment.Invalidate();
		}

		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			if (!mouseDown) return;
			if (!dragged && (mousePos - clickScreen).Length < DRAG_START) return;
			dragged = true;
			if (moving.Count == 0) return;
			var delta = MouseLocal(editor, mousePos) - moveStartLocal;
			if (CurrentMode == EditMesh_Mode.New) {
				// outline points, the mesh isn't touched yet
				foreach (var move in moving) {
					var pos = ClampToImage(move.Pos + delta);
					var uv = UvOf(pos);
					move.Vertex.SetPos(pos);
					move.Vertex.U = uv.X;
					move.Vertex.V = uv.Y;
				}
				return;
			}
			editor.Actions.TrackMesh(Attachment);
			MoveVertices(delta);
		}

		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
			if (!mouseDown) return;
			mouseDown = false;
			var screen = editor.Editor.GetMousePos();
			if (boxing)
				FinishBox(editor, screen);
			else if (edgeFrom != null && dragged)
				FinishEdge(editor, screen);
			boxing = false;
			edgeFrom = null;
			moving.Clear();
			movingSet.Clear();
		}

		private void FinishBox(ModelEditor editor, Vector2F screen) {
			var selected = Attachment.SelectedVertices;
			if (!dragged) {
				if (CurrentMode == EditMesh_Mode.Modify && !CtrlDown)
					selected.Clear();
				return;
			}

			var start = editor.Editor.ScreenToGrid(clickScreen);
			var end = editor.Editor.ScreenToGrid(screen);
			float minX = MathF.Min(start.X, end.X), maxX = MathF.Max(start.X, end.X);
			float minY = MathF.Min(start.Y, end.Y), maxY = MathF.Max(start.Y, end.Y);
			PatchAttachmentTransform();
			List<EditorVertex> inside = [];
			foreach (var vertex in Attachment.GetVertices()) {
				var world = WorldOf(vertex);
				if (world.X >= minX && world.X <= maxX && world.Y >= minY && world.Y <= maxY)
					inside.Add(vertex);
			}
			RestoreAttachmentTransform();

			if (CurrentMode == EditMesh_Mode.Delete) {
				foreach (var vertex in inside)
					DeleteVertex(vertex);
				return;
			}
			if (!CtrlDown)
				selected.Clear();
			selected.UnionWith(inside);
		}

		private void FinishEdge(ModelEditor editor, Vector2F screen) {
			var from = edgeFrom!;
			bool snap = !ShiftDown;
			var local = snap ? MouseLocal(editor, screen) : AxisLocal(editor, from, screen);

			EditorVertex to;
			if (snap && HoveredVertex != null) {
				to = HoveredVertex;
				if (to == from || Connected(from, to) || !EdgeAllowed(from, to, to.ToVector())) return;
			}
			else if (snap && HoveredEdge is { } edge) {
				var point = Vector2F.Lerp(ProjectT(local, edge.A, edge.B), edge.A, edge.B);
				if (!EdgeAllowed(from, null, point, (edge.A, edge.B))) return;
				to = SplitEdge(edge, local);
			}
			else {
				if (!Attachment.InsideHull(local.X, local.Y) || !EdgeAllowed(from, null, local)) return;
				to = AddVertex(local);
			}
			from.ConstrainTo(to);
		}

		private static Vector2F AxisSnap(Vector2F start, Vector2F to) {
			var delta = to - start;
			return MathF.Abs(delta.X) >= MathF.Abs(delta.Y) ? new(to.X, start.Y) : new(start.X, to.Y);
		}

		// shift locks a new edge horizontal or vertical
		private Vector2F AxisLocal(ModelEditor editor, EditorVertex from, Vector2F screen) {
			PatchAttachmentTransform();
			var world = AxisSnap(WorldOf(from), editor.Editor.ScreenToGrid(screen));
			var local = Attachment.WorldTransform.WorldToLocal(world);
			RestoreAttachmentTransform();
			return local;
		}

		public override void RightClicked(ModelEditor editor, Vector2F mousePos) {
			if (!mouseDown)
				CycleMode();
		}

		public bool ShouldRenderUndeformed => !meshedit_deformed.GetBool();

		private Transformation StoredAttachTransform;
		private void PatchAttachmentTransform() {
			if (!ShouldRenderUndeformed) return;

			StoredAttachTransform = Attachment.WorldTransform;
			Attachment.WorldTransform = Transformation.CalculateWorldTransformation(StoredAttachTransform.Translation, 0, Vector2F.One, Vector2F.Zero, TransformMode.Normal);
			Attachment.SuppressWorldTransform = true;
		}
		private void RestoreAttachmentTransform() {
			if (!ShouldRenderUndeformed) return;
			Attachment.WorldTransform = StoredAttachTransform;
			Attachment.SuppressWorldTransform = false;
		}

		public override bool RenderOverride() {
			bool newHull = CurrentMode == EditMesh_Mode.New;
			if (newHull || ShouldRenderUndeformed) {
				if (!newHull)
					FitUV();
				Span<Vector2F> corners = stackalloc Vector2F[4];
				for (int i = 0; i < 4; i++)
					corners[i] = PosOf(imageCorners[i]);

				Attachment.SetupWorldTransform();
				PatchAttachmentTransform();
				Attachment.RenderImage(corners);
				if (newHull)
					RenderWorkingLines();
				else
					Attachment.RenderOverlay();
				RestoreAttachmentTransform();
				return true;
			}

			if (EditorMeshAttachment.ShouldIsolate) {
				Attachment.Render();
				Attachment.RenderOverlay();

				return true;
			}

			return false;
		}

		private void RenderWorkingLines() {
			var editor = ModelEditor.Active.Editor;
			float zoom = editor.CameraZoom;
			var transform = Attachment.WorldTransform;
			int count = WorkingLines.Count;

			for (int i = 0; i < count; i++) {
				var point = transform.LocalToWorld(WorkingLines[i]);
				if (i < count - 1)
					Raylib.DrawLineV(point.ToNumerics(), transform.LocalToWorld(WorkingLines[i + 1]).ToNumerics(), new Color(150, 150, 255));
				bool hovered = HoveredVertex == WorkingLines[i];
				Raylib.DrawCircleV(point.ToNumerics(), (hovered ? 4.5f : 3f) / zoom, new Color(hovered ? 235 : 200, hovered ? 235 : 200, 255));
			}

			// next click position
			var next = HoveredVertex != null ? transform.LocalToWorld(HoveredVertex)
				: transform.LocalToWorld(ClampToImage(transform.WorldToLocal(editor.ScreenToGrid(editor.GetMousePos()))));
			if (count > 0)
				Raylib.DrawLineV(transform.LocalToWorld(WorkingLines[count - 1]).ToNumerics(), next.ToNumerics(), new Color(60, 120, 255, 160));
			if (HoveredVertex == null)
				Raylib.DrawCircleV(next.ToNumerics(), 5 / zoom, Color.Lime);
		}
		internal void DrawOverlay() {
			var editor = ModelEditor.Active.Editor;
			var zoom = editor.CameraZoom;
			var mouse = editor.ScreenToGrid(editor.GetMousePos());

			if (HoveredEdge is { } edge && (CurrentMode == EditMesh_Mode.Create || (CurrentMode == EditMesh_Mode.Delete && !edge.Hull))) {
				Color color = CurrentMode == EditMesh_Mode.Delete ? new Color(255, 60, 15) : new Color(100, 220, 100);
				Rlgl.DrawRenderBatchActive();
				Rlgl.SetLineWidth(3);
				Raylib.DrawLineV(WorldOf(edge.A).ToNumerics(), WorldOf(edge.B).ToNumerics(), color);
				Rlgl.DrawRenderBatchActive();
				Rlgl.SetLineWidth(1);
			}

			if (CurrentMode != EditMesh_Mode.Create) return;

			var target = mouse;
			if (edgeFrom != null && dragged && ShiftDown)
				target = AxisSnap(WorldOf(edgeFrom), mouse);
			else if (HoveredVertex != null)
				target = WorldOf(HoveredVertex);
			else if (HoveredEdge is { } hoveredEdge) {
				Vector2F start = WorldOf(hoveredEdge.A), end = WorldOf(hoveredEdge.B);
				target = Vector2F.Lerp(ProjectT(mouse, start, end), start, end);
			}

			if (edgeFrom != null && dragged)
				Raylib.DrawLineV(WorldOf(edgeFrom).ToNumerics(), target.ToNumerics(), new Color(245, 100, 20));
			if (HoveredVertex == null && moving.Count == 0)
				Raylib.DrawCircleV(target.ToNumerics(), 5 / zoom, new Color(100, 180, 100));
		}
	}
	public class EditorVertex : IEditorType
	{
		public override string ToString() {
			return $"EditorVertex [{X}, {Y}]";
		}
		public EditorModel GetModel() => Attachment.Slot.Bone.Model;
		public IEditorType? DeferPropertiesTo() => Attachment;
		public IEditorType? DeferTransformationsTo() => Attachment;
		public IEditorType? GetTransformParent() => Attachment;
		public float X;
		public float Y;
		public float U;
		public float V;
		public EditorVertexAttachment Attachment;


		public HashSet<EditorVertex> ConstrainedHashSet = [];
		[JsonIgnore] public bool HasConstrainedEdges => ConstrainedHashSet.Count > 0;

		/// <summary>
		/// Note that this only applies to <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.ShapeEdges"/> or 
		/// <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.SteinerPoints"/>. Shape edges inheritly are 
		/// constrained to the next <see cref="EditorVertex"/> in the array (or in the case of the last edge, the last point -> first point).
		/// </summary>
		public IEnumerable<EditorVertex> ConstrainedVertices {
			get {
				foreach (var vertex in ConstrainedHashSet)
					yield return vertex;
			}
		}

		public bool IsConstrainedTo(EditorVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			var otherContainsUs = other.ConstrainedHashSet.Contains(this);
			var weContainOther = this.ConstrainedHashSet.Contains(other);

			Debug.Assert(otherContainsUs == weContainOther);

			return otherContainsUs && weContainOther;
		}

		public void ConstrainTo(EditorVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			Debug.Assert(this != other, "Can not constrain a vertex to itself...");

			other.ConstrainedHashSet.Add(this);
			this.ConstrainedHashSet.Add(other);

			Attachment.Invalidate();
		}

		public void UnconstrainFrom(EditorVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			other.ConstrainedHashSet.Remove(this);
			this.ConstrainedHashSet.Remove(other);

			Attachment.Invalidate();
		}

		public string SingleName => "vertex";

		public string PluralName => "vertices";

		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		public void SetPos(float x, float y) {
			X = x;
			Y = y;
		}

		public bool HoverTest(Vector2F pos) {
			var dist = pos.Distance(Attachment.WorldTransform.LocalToWorld(X, Y));
			return dist < 2;
		}

		public void SetPos(Vector2F pos) => SetPos(pos.X, pos.Y);

		public Vector2F ToVector() => new(X, Y);
		public static EditorVertex FromVector(Vector2F vec, EditorVertexAttachment attachment) {
			EditorVertex vertex = new() { X = vec.X, Y = vec.Y, Attachment = attachment };

			// Bind *just* this vertex to the bone its parented to
			// Todo: auto-weigh to the bone somehow?
			attachment.SetVertexWeight(vertex, attachment.Slot.Bone, 1.0f, false);

			return vertex;
		}

		public static EditorVertex FromVector(Vector2F vec, Vector2F tex, EditorVertexAttachment attachment) {
			EditorVertex vertex = new() { X = vec.X, Y = vec.Y, U = tex.X, V = tex.Y, Attachment = attachment };

			// Bind *just* this vertex to the bone its parented to
			// Todo: auto-weigh to the bone somehow?
			attachment.SetVertexWeight(vertex, attachment.Slot.Bone, 1.0f, false);

			return vertex;
		}

		public static implicit operator Vector2F(EditorVertex v) => v.ToVector();

		public void OnMouseEntered() { }
		public void OnMouseLeft() { }

		public bool OnSelected() {
			Attachment.SelectVertex(this);
			return false;
		}

		internal void AutoUV() {
			var uvcoords = (Attachment as EditorMeshAttachment).GetVertexUV(this);
			U = uvcoords.X;
			V = uvcoords.Y;
		}
	}

	// So the reason this is structured so weirdly: Newtonsoft.Json doesn't seem to like serializing
	// MeshVertex types as keys. This weird pseudo-dictionary-only-really-o(1)-during-runtime structure
	// is *probably* good enough for the editor, but I hate it regardless and it's a mess
	public class EditorWeights
	{
		public EditorBone Bone;

		private List<EditorVertex> __vertices = [];
		private List<float> __weights = [];
		private List<Vector2F> __positions = [];

		public List<EditorVertex> Vertices { get => __vertices; set {
			Invalidated = true;
			Version++;
			__vertices = value;
		} }
		public List<float> Weights { get => __weights; set {
			Invalidated = true;
			Version++;
			__weights = value;
		} }
		public List<Vector2F> Positions { get => __positions; set {
			Invalidated = true;
			Version++;
			__positions = value;
		} }

		[JsonIgnore] private Dictionary<EditorVertex, float> TrueWeights = [];
		[JsonIgnore] private Dictionary<EditorVertex, Vector2F> TruePositions = [];
		[JsonIgnore] public bool Invalidated = true;
		[JsonIgnore] public int Version;
		/// <summary>
		/// Weights tool edits leave this bone alone
		/// </summary>
		[JsonIgnore] public bool Locked;

		public void SetVertexWeight(EditorVertex vertex, float weight) {
			bool found = false;
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Weights[i] = weight;
					found = true;
					break;
				}
			}

			if (found == false) {
				Vertices.Add(vertex);
				Weights.Add(weight);
				Positions.Add(Vector2F.Zero);
			}


			Invalidated = true; Version++;
		}
		public void SetVertexPos(EditorVertex vertex, Vector2F pos) {
			bool found = false;
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Positions[i] = pos;
					found = true;
					break;
				}
			}

			if (found == false) {
				Vertices.Add(vertex);
				Weights.Add(0);
				Positions.Add(pos);
			}

			Invalidated = true; Version++;
		}

		public bool AddVertex(EditorVertex vertex, float? weight = null, Vector2F? pos = null) {
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Weights[i] = weight ?? Weights[i];
					Positions[i] = pos ?? Positions[i];
					Invalidated = true; Version++;
					return true;
				}
			}

			return false;
		}

		public bool RemoveVertex(EditorVertex vertex) {
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Vertices.RemoveAt(i);
					Positions.RemoveAt(i);
					Weights.RemoveAt(i);
					Invalidated = true; Version++;
					return true;
				}
			}

			return false;
		}

		public void Validate() {
			if (!Invalidated) return;

			TrueWeights.Clear();
			TruePositions.Clear();

			Debug.Assert(Vertices.Count == Weights.Count && Weights.Count == Positions.Count && Vertices.Count == Positions.Count);

			for (int i = 0; i < Vertices.Count; i++) {
				TrueWeights[Vertices[i]] = Weights[i];
				TruePositions[Vertices[i]] = Positions[i];
			}

			Invalidated = false;
		}


		/// <summary>
		/// Defaults to 0
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public float TryGetVertexWeight(EditorVertex vertex) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out var value) ? value : 0;
		}
		public bool TryGetVertexWeight(EditorVertex vertex, [NotNullWhen(true)] out float weight) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out weight);
		}

		/// <summary>
		/// Defaults to <see cref="Vector2F.Zero"/>
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public Vector2F TryGetVertexPosition(EditorVertex vertex) {
			Validate();
			return TruePositions.TryGetValue(vertex, out var value) ? value : Vector2F.Zero;
		}
		public bool TryGetVertexPosition(EditorVertex vertex, [NotNullWhen(true)] out Vector2F pos) {
			Validate();
			return TruePositions.TryGetValue(vertex, out pos);
		}

		public bool TryGetVertexInfo(EditorVertex vertex, out float weight, out Vector2F pos) {
			Validate();
			var gotWeight = TryGetVertexWeight(vertex, out weight);
			return TryGetVertexPosition(vertex, out pos) || gotWeight;
		}

		public int Count {
			get {
				Debug.Assert(Weights.Count == Positions.Count);
				return Weights.Count;
			}
		}

		public bool IsEmpty => Count <= 0;

		public void Clear() {
			Vertices.Clear();
			Weights.Clear();
			Positions.Clear();
			Invalidated = true; Version++;
		}
	}

	[Nucleus.MarkForStaticConstruction]
	public class EditorMeshAttachment : EditorVertexAttachment
	{
		[JsonIgnore]
		public float LocalWidth {
			get {
				var region = Slot.Bone.Model.Images.TextureAtlas.GetRegion(Slot.Bone.Model.ResolveImage(Path)!.Name);
				if (region == null) return 512;
				region.GetBounds(out _, out _, out int w, out _);
				return w;
			}
		}
		[JsonIgnore]
		public float LocalHeight {
			get {
				var region = Slot.Bone.Model.Images.TextureAtlas.GetRegion(Slot.Bone.Model.ResolveImage(Path)!.Name);
				if (region == null) return 512;
				region.GetBounds(out _, out _, out _, out int h);
				return h;
			}
		}

		public override string SingleName => "mesh";
		public override string PluralName => "meshes";
		public override string EditorIcon => IsLinked ? "models/linkedmesh.png" : "models/mesh.png";

		public string GetPath() => Path ?? $"<{Name}>";
		public string? Path { get; set; } = null;

		/// <summary>
		/// Mesh this one shares geometry and weights with, if it's linked
		/// <br/><b>NOTE:</b> Geometry is a synced copy of the parent's, don't edit it directly.
		/// </summary>
		public EditorMeshAttachment? LinkedParent { get; set; }
		public bool InheritDeform { get; set; } = true;
		public bool Wireframe { get; set; }
		[JsonIgnore] public bool IsLinked => LinkedParent != null;
		[JsonIgnore] public override EditorVertexAttachment DeformAttachment => LinkedParent != null && InheritDeform ? LinkedParent : this;

		/// <summary>
		/// The mesh at the top of the link chain
		/// </summary>
		[JsonIgnore]
		public override EditorVertexAttachment GeometrySource {
			get {
				EditorMeshAttachment mesh = this;
				for (int i = 0; i < 16 && mesh.LinkedParent != null && mesh.LinkedParent != this; i++)
					mesh = mesh.LinkedParent;
				return mesh;
			}
		}
		[JsonIgnore] public override int VertexCount => ShapeEdges.Count + SteinerPoints.Count;

		public override void BuildProperties(Panel props, PreUIDeterminations determinations) {
			base.BuildProperties(props, determinations);

			var optionsRow = PropertiesPanel.NewRow(props, "Options", "models/mesh.png");
			var wireframe = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Wireframe", Wireframe);
			wireframe.OnCheckedChanged += (s)
				=> ModelEditor.Active.File.SetProperty(this, "wireframe", s.Checked, () => Wireframe, v => Wireframe = v);

			AddColorRow(props, () => Color, v => Color = v);

			if (LinkedParent == null) return;

			var parent = LinkedParent;
			var linkRow = PropertiesPanel.NewRow(props, "Linked", "models/linkedmesh.png");
			var parentPanel = PropertiesPanel.AddInternalPropPanel(linkRow);
			var parentButton = new Button(parentPanel);
			parentButton.Dock = Dock.Fill;
			parentButton.DockMargin = RectangleF.TLRB(3);
			parentButton.BorderSize = 0;
			parentButton.Text = parent.Slot == Slot ? parent.Name : $"{parent.Slot.Name} / {parent.Name}";
			parentButton.TooltipText = "Select the parent mesh";
			parentButton.OnButtonClick += (_, _) => ModelEditor.Active.SelectObject(parent);

			var inherit = PropertiesPanel.AddLabeledCheckbox(linkRow, "Inherit deform", InheritDeform);
			inherit.OnCheckedChanged += (s) => {
				ModelEditor.Active.File.SetProperty(this, "inherit deform", s.Checked,
					() => InheritDeform, v => InheritDeform = v);
				ModelEditor.Active.QueueAnimationUpdate();
			};
			linkRow.Thinking += (_) => linkRow.SetMouseInputEnabled(!ModelEditor.Active.AnimationMode);
		}

		/// <summary>
		/// Arbitrary points within the shapes edges.
		/// </summary>
		public List<EditorVertex> SteinerPoints = [];

		public override IEnumerable<EditorVertex> GetVertices() {
			foreach (var co in ShapeEdges) {
				co.Attachment = this;
				yield return co;
			}

			foreach (var sp in SteinerPoints) {
				sp.Attachment = this;
				yield return sp;
			}
		}

		//[JsonIgnore] public Dictionary<TriPoint, MeshVertex> triPointToMeshVertex = [];

		[JsonIgnore] public bool SuppressWorldTransform = false;

		public override Color DetermineVertexColor(bool selected, bool highlighted) {
			var hS = highlighted ? 245 : 165;
			bool deleteMode = ModelEditor.Active.File.ActiveOperator is EditMeshOperator meshOp && meshOp.CurrentMode == EditMesh_Mode.Delete;
			Color color;
			if (highlighted && deleteMode)
				color = new Color(255, 60, 15);
			else
				color = selected ? new Color(highlighted ? 180 : 0, 255, 255) : new Color(hS - 15, hS, hS);
			return color;
		}

		/// <summary>
		/// Draws the whole image as a quad, corners local in uv order (0,0) (1,0) (1,1) (0,1).
		/// </summary>
		public void RenderImage(ReadOnlySpan<Vector2F> corners) {
			var model = Slot.Bone.Model;
			ModelImage? image = model.ResolveImage(Path);
			IModelAtlasRegion? region = image != null ? model.Images.TextureAtlas.GetRegion(image.Name) : null;
			ITexture tex = region != null ? model.Images.TextureAtlas.PackedTexture : textures.GetErrorTexture();

			int regX = 0, regY = 0, regW = 512, regH = 512;
			region?.GetBounds(out regX, out regY, out regW, out regH);
			float uStart = (float)regX / tex.GetWidth(), uEnd = uStart + (float)regW / tex.GetWidth();
			float vStart = (float)regY / tex.GetHeight(), vEnd = vStart + (float)regH / tex.GetHeight();

			Span<Vector2F> points = stackalloc Vector2F[4];
			for (int i = 0; i < 4; i++)
				points[i] = WorldTransform.LocalToWorld(corners[i]);

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(tex.GetTextureHandle());
			var color = (byte)(ShouldDim ? 90 : 255);
			Rlgl.Color4ub(color, color, color, 255);

			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(points[0].X, points[0].Y, 0);
			Rlgl.TexCoord2f(uEnd, vEnd); Rlgl.Vertex3f(points[1].X, points[1].Y, 0);
			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(points[2].X, points[2].Y, 0);

			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(points[2].X, points[2].Y, 0);
			Rlgl.TexCoord2f(uStart, vStart); Rlgl.Vertex3f(points[3].X, points[3].Y, 0);
			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(points[0].X, points[0].Y, 0);

			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
		}

		[JsonIgnore] public static bool InEditMeshOperator => ModelEditor.Active.File.ActiveOperator is EditMeshOperator;
		[JsonIgnore] public bool IsActiveAttachment => ModelEditor.Active.File.ActiveOperator is EditMeshOperator op && op.Attachment == this;
		[JsonIgnore] public static bool RenderTriangles => InEditMeshOperator && EditMeshOperator.meshedit_triangles.GetBool();
		[JsonIgnore] public static bool ShouldDim => InEditMeshOperator && EditMeshOperator.meshedit_dim.GetBool();
		[JsonIgnore] public static bool ShouldIsolate => InEditMeshOperator && EditMeshOperator.meshedit_isolate.GetBool();

		public override bool RemoveVertex(EditorVertex vertex) {
			return base.RemoveVertex(vertex) || SteinerPoints.Remove(vertex);
		}

		public override void Render() {
			if (Hidden) return;
			RefreshDelaunator();

			if (!SuppressWorldTransform)
				SetupWorldTransform();

			var model = Slot.Bone.Model;

			ModelImage? image = model.ResolveImage(Path);
			if (Path == null) throw new Exception(":(");

			IModelAtlasRegion? region = image != null ? model.Images.TextureAtlas.GetRegion(image.Name) : null;
			bool succeeded = region != null;

			int regX = 0, regY = 0, regW = 512, regH = 512;
			if (succeeded) region!.GetBounds(out regX, out regY, out regW, out regH);

			float width = regH, height = regW;
			float widthDiv2 = width / 2, heightDiv2 = height / 2;
			ITexture tex = succeeded ? model.Images.TextureAtlas.PackedTexture : textures.GetErrorTexture();

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(tex.GetTextureHandle());

			var color = Slot.GetColor();

			if (IsActiveAttachment && ShouldDim) {
				color.R /= 2;
				color.G /= 2;
				color.B /= 2;
			}
			float srM = color.R / 255f, sgM = color.G / 255f, sbM = color.B / 255f, saM = color.A / 255f;
			float arM = Color.R / 255f, agM = Color.G / 255f, abM = Color.B / 255f, aaM = Color.A / 255f;

			Rlgl.Color4f(srM * arM, sgM * agM, sbM * abM, saM * aaM);
			if (ShowsWeightOverlay) {
				Rlgl.End();
				RenderWeightOverlay();
				return;
			}
			var geometry = GeometrySource;
			if (geometry != this)
				geometry.RefreshDelaunator();
			var triangles = geometry.Triangles;
			if (triangles.Count > 0) {
				float uStart, uEnd, vStart, vEnd;
				uStart = (float)regX / (float)tex.GetWidth();
				uEnd = uStart + ((float)regW / (float)tex.GetWidth());

				vStart = ((float)regY / (float)tex.GetHeight());
				vEnd = vStart + ((float)regH / (float)tex.GetHeight());

				bool block = false;
				for (int i = 0; i < triangles.Count; i += 3) {
					var av1 = triangles[i];
					var av2 = triangles[i + 1];
					var av3 = triangles[i + 2];

					Vector2F t1 = new((float)av1.U, (float)av1.V);
					Vector2F t2 = new((float)av2.U, (float)av2.V);
					Vector2F t3 = new((float)av3.U, (float)av3.V);

					float u1 = (float)NMath.Remap(t1.X, 0, 1, uStart, uEnd), v1 = (float)NMath.Remap(t1.Y, 0, 1, vEnd, vStart);
					float u2 = (float)NMath.Remap(t2.X, 0, 1, uStart, uEnd), v2 = (float)NMath.Remap(t2.Y, 0, 1, vEnd, vStart);
					float u3 = (float)NMath.Remap(t3.X, 0, 1, uStart, uEnd), v3 = (float)NMath.Remap(t3.Y, 0, 1, vEnd, vStart);

					Vector2F p1 = CalculateVertexWorldPosition(WorldTransform, av1);
					Vector2F p2 = CalculateVertexWorldPosition(WorldTransform, av2);
					Vector2F p3 = CalculateVertexWorldPosition(WorldTransform, av3);

					Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(p1.X, p1.Y, 0);
					Rlgl.TexCoord2f(u2, v2); Rlgl.Vertex3f(p2.X, p2.Y, 0);
					Rlgl.TexCoord2f(u3, v3); Rlgl.Vertex3f(p3.X, p3.Y, 0);
				}
			}

			Rlgl.End();
		}

		/// <summary>
		/// Weights tool overlay
		/// </summary>
		[JsonIgnore] public bool ShowsWeightOverlay => ModelEditor.Active.Editor.InWeightsMode && WeightsPanel.weights_overlay.GetBool()
			&& VertexWeightOperator.Target == this;

		private void RenderWeightOverlay() {
			// bones that aren't selected draw black
			bool onlySelected = WeightsPanel.weights_selected.GetBool();
			var selectedBones = ModelEditor.Active.Weights.SelectedBones;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(Rlgl.GetTextureIdDefault());
			foreach (var vertex in Triangles) {
				float r = 0, g = 0, b = 0;
				foreach (var weights in Weights) {
					float weight = weights.TryGetVertexWeight(vertex);
					if (weight <= 0 || (onlySelected && !selectedBones.Contains(weights))) continue;
					var color = WeightColor(weights);
					r += color.R * weight;
					g += color.G * weight;
					b += color.B * weight;
				}
				Rlgl.Color4ub((byte)Math.Min(r, 255), (byte)Math.Min(g, 255), (byte)Math.Min(b, 255), 255);
				var pos = CalculateVertexWorldPosition(WorldTransform, vertex);
				Rlgl.Vertex3f(pos.X, pos.Y, 0);
			}
			Rlgl.End();
		}

		static readonly HashSet<(EditorVertex, EditorVertex)> drawnLines = [];
		private static bool FirstDraw(EditorVertex from, EditorVertex to) => !drawnLines.Contains((to, from)) && drawnLines.Add((from, to));

		public void RenderTriangleLines() {
			if (!RenderTriangles) return;
			if (triangles.Count <= 0) return;

			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(2);

			drawnLines.Clear();

			for (int i = 0; i < triangles.Count; i += 3) {
				var av1 = triangles[i];
				var av2 = triangles[i + 1];
				var av3 = triangles[i + 2];

				bool ic1 = av1.IsConstrainedTo(av2);
				bool ic2 = av2.IsConstrainedTo(av3);
				bool ic3 = av3.IsConstrainedTo(av1);

				var offset = Graphics2D.Offset;
				Graphics2D.ResetDrawingOffset();

				var v1 = CalculateVertexWorldPosition(WorldTransform, av1);
				var v2 = CalculateVertexWorldPosition(WorldTransform, av2);
				var v3 = CalculateVertexWorldPosition(WorldTransform, av3);

				Graphics2D.SetDrawColor(245, 100, 20);
				if (ic1) Graphics2D.DrawLine(v1, v2);
				if (ic2) Graphics2D.DrawLine(v2, v3);
				if (ic3)
					Graphics2D.DrawLine(v3, v1);

				Graphics2D.SetDrawColor(140, 140, 160);

				if (!ic1 && FirstDraw(av1, av2))
					Graphics2D.DrawDottedLine(v1, v2, 0.5f);
				if (!ic2 && FirstDraw(av2, av3))
					Graphics2D.DrawDottedLine(v2, v3, 0.5f);
				if (!ic3 && FirstDraw(av3, av1))
					Graphics2D.DrawDottedLine(v3, v1, 0.5f);

				Graphics2D.OffsetDrawing(offset);
			}

			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(1);
		}

		public Transformation? TransformOverride = null;

		public override void RenderOverlay() {
			if (Hidden) return;

			base.RenderOverlay();
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			EditMeshOperator? meshOp = ModelEditor.Active.File.ActiveOperator as EditMeshOperator;

			RenderTriangleLines();

			Graphics2D.SetDrawColor(245, 100, 20);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			var transform = TransformOverride ?? WorldTransform;

			for (int i = 0; i < triangles.Count; i += 3) {
				EditorVertex av1 = triangles[i], av2 = triangles[i + 1], av3 = triangles[i + 2];

				//av1.Attachment = this;
				//av2.Attachment = this;
				//av3.Attachment = this;

				bool ic1 = av1.IsConstrainedTo(av2), ic2 = av2.IsConstrainedTo(av3), ic3 = av3.IsConstrainedTo(av1);

				Vector2F v1 = CalculateVertexWorldPosition(transform, av1),
						 v2 = CalculateVertexWorldPosition(transform, av2),
						 v3 = CalculateVertexWorldPosition(transform, av3);

				if (ic1) Graphics2D.DrawLine(v1, v2);
				if (ic2) Graphics2D.DrawLine(v2, v3);
				if (ic3)
					Graphics2D.DrawLine(v3, v1);
			}

			Graphics2D.SetOffset(offset);

			for (int i = 0; i < ShapeEdges.Count; i++) {
				var edge1 = ShapeEdges[i];
				var edge2 = ShapeEdges[(i + 1) % ShapeEdges.Count];
				var isHighlighted =
						((edge1.Hovered && meshOp == null)
						|| (meshOp != null && meshOp.HoveredVertex == edge1 && !meshOp.IsHoveredSteinerPoint))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(EditorVertex));

				var isSelected = SelectedVertices.Contains(edge1) || SelectedVertices.Count == 0;

				var isEdgeSelected = (SelectedVertices.Contains(edge1) && SelectedVertices.Contains(edge2)) || SelectedVertices.Count == 0;


				var lineColor = isEdgeSelected ? new Color(40, 255, 255) : new Color(20, 210, 210);

				var vertex1 = CalculateVertexWorldPosition(transform, edge1);
				var vertex2 = CalculateVertexWorldPosition(transform, edge2);

				Raylib.DrawLineV(vertex1.ToNumerics(), vertex2.ToNumerics(), lineColor);

				if (Selected || Wireframe || (ModelEditor.Active.Editor.InWeightsMode && VertexWeightOperator.Target == this)) {
					RenderVertex(edge1, isHighlighted, vertex1);
				}
			}

			for (int i = 0; i < SteinerPoints.Count; i++) {
				var point = SteinerPoints[i];
				var isHighlighted =
					((point.Hovered && meshOp == null) || (meshOp != null && meshOp.HoveredVertex == point && meshOp.IsHoveredSteinerPoint))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(EditorVertex));

				RenderVertex(point, isHighlighted);
				//Raylib.DrawCircleV(WorldTransform.LocalToWorld(point).ToNumerics(), (isHighlighted ? 3f : 2f) / camsize, new Color(isHighlighted ? 235 : 200, isHighlighted ? 235 : 200, 255));
			}

			// hack; but has to be done with the current rendering order
			if (IsActiveAttachment)
				meshOp!.DrawOverlay();
		}

		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			// linked meshes take the parent's geometry
			if (!IsLinked) {
				PropertiesPanel.OperatorButton<EditMeshOperator>(buttons, "Mesh Editor", "models/mesh.png");
				PropertiesPanel.ButtonIcon(buttons, "Reset", "models/mesh.png",
					(_, _) => ModelEditor.Active.File.ResetMesh(this));
			}
			base.BuildOperators(buttons, determinations);
		}

		public override bool HoverTest(Vector2F gridPos) {
			RefreshDelaunator();
			var geometry = GeometrySource;
			if (geometry != this)
				geometry.RefreshDelaunator();
			var triangles = geometry == this ? this.triangles : ((EditorMeshAttachment)geometry).triangles;

			var localizedPos = WorldTransform.WorldToLocal(gridPos);
			for (int i = 0; i < triangles.Count; i += 3) {
				if (localizedPos.InTriangle(new(
						new Vector2F(triangles[i].X, triangles[i].Y),
						new Vector2F(triangles[i + 1].X, triangles[i + 1].Y),
						new Vector2F(triangles[i + 2].X, triangles[i + 2].Y)
					)))
					return true;
			}

			return false;

			// var quadpoints = this.quadpoints();
			// return gridPos.TestPointInQuad(quadpoints.TL, quadpoints.TR, quadpoints.BL, quadpoints.BR);
		}

		public Vector2F GetVertexUV(Vector2F pos) {
			return new(
				(float)NMath.Remap(pos.X, LocalWidth / -2, LocalWidth / 2, 0, 1),
				(float)NMath.Remap(pos.Y, LocalHeight / -2, LocalHeight / 2, 0, 1)
			);
		}
	}
}
