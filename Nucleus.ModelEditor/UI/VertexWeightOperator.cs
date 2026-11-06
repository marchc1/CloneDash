using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// Weights tool, works on the mesh shown in the Weights view.
	/// </summary>
	public class VertexWeightOperator : DefaultOperator
	{
		static WeightsPanel panel => ModelEditor.Active.Weights;
		public static EditorVertexAttachment? Target
			=> panel.ActiveAttachment is { Weights.Count: > 0 } attachment ? attachment : null;

		public override bool IsSelectable(IEditorType target) => target switch {
			EditorMeshAttachment mesh => mesh.LinkedParent == null,
			EditorClippingAttachment => true,
			EditorBone bone => Target?.Weights.Any(x => x.Bone == bone) ?? false,
			_ => false
		};

		private const float VERTEX_PICK = 10;
		private const float DRAG_START = 8;
		private const float BRUSH_START = 2;
		// weight per pixel dragged in Direct
		private const float DIRECT_RATE = 0.005f;

		enum DragMode { None, Box, Direct, Brush }

		EditorVertex? clickedVertex;
		EditorWeights? clickedBone;
		EditorAttachment? clickedOther;
		bool ctrlClick;
		bool started;
		DragMode dragMode;
		Vector2F boxStart, boxEnd;
		readonly Dictionary<EditorVertex, float[]> startWeights = [];
		readonly Dictionary<EditorVertex, float> applied = [];

		private static Vector2F ScreenOf(EditorPanel editorPanel, EditorVertexAttachment attachment, EditorVertex vertex)
			=> editorPanel.GridToScreen(attachment.CalculateVertexWorldPosition(attachment.WorldTransform, vertex));

		private static EditorVertex? VertexAt(EditorPanel editorPanel, EditorVertexAttachment attachment, Vector2F screen) {
			EditorVertex? best = null;
			float bestDistance = VERTEX_PICK;
			foreach (var vertex in attachment.GetVertices()) {
				float distance = (ScreenOf(editorPanel, attachment, vertex) - screen).Length;
				if (distance <= bestDistance) {
					best = vertex;
					bestDistance = distance;
				}
			}
			return best;
		}

		private static bool InsideHull(EditorPanel editorPanel, EditorVertexAttachment attachment, Vector2F screen) {
			List<Vector2F> hull = [];
			foreach (var vertex in attachment.ShapeEdges)
				hull.Add(ScreenOf(editorPanel, attachment, vertex));
			bool inside = false;
			for (int i = 0, j = hull.Count - 1; i < hull.Count; j = i++) {
				if ((hull[i].Y > screen.Y) != (hull[j].Y > screen.Y) && screen.X < (hull[j].X - hull[i].X) * (screen.Y - hull[i].Y) / (hull[j].Y - hull[i].Y) + hull[i].X)
					inside = !inside;
			}
			return inside;
		}

		public override bool ToolClick(EditorPanel editorPanel, Vector2F mouseScreen, bool ctrl) {
			var attachment = Target;
			if (attachment == null) return false;

			clickedVertex = VertexAt(editorPanel, attachment, mouseScreen);
			clickedBone = editorPanel.HoveredObject is EditorBone bone ? attachment.Weights.FirstOrDefault(x => x.Bone == bone) : null;
			clickedOther = editorPanel.HoveredObject is EditorAttachment other && other != attachment
				&& !InsideHull(editorPanel, attachment, mouseScreen) ? other : null;

			ctrlClick = ctrl;
			started = false;
			dragMode = DragMode.None;
			return true;
		}

		public override void ToolDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow) {
			var attachment = Target;
			if (attachment == null) return;
			bool direct = WeightsPanel.Mode == WeightsMode.Direct;

			if (!started) {
				float startDistance = direct || ctrlClick ? DRAG_START : BRUSH_START;
				if ((mouseScreenNow - mouseScreenStart).Length <= startDistance) return;
				started = true;
				if (ctrlClick || (direct && attachment.SelectedVertices.Count == 0))
					dragMode = DragMode.Box;
				else if (panel.SelectedBones.Count > 0) {
					dragMode = direct ? DragMode.Direct : DragMode.Brush;
					ModelEditor.Active.Actions.TrackMesh(attachment);
					MeshWeights.EnsurePositions(attachment);
					startWeights.Clear();
					applied.Clear();
					foreach (var vertex in attachment.SelectedVertices) {
						var values = new float[attachment.Weights.Count];
						for (int i = 0; i < values.Length; i++)
							values[i] = attachment.Weights[i].TryGetVertexWeight(vertex);
						startWeights[vertex] = values;
					}
				}
			}

			switch (dragMode) {
				case DragMode.Box: boxStart = mouseScreenStart; boxEnd = mouseScreenNow; break;
				case DragMode.Direct:
					AdjustDirect(attachment, (mouseScreenStart.Y - mouseScreenNow.Y) * DIRECT_RATE);
					break;
				case DragMode.Brush: Brush(editorPanel, attachment, mouseScreenNow); break;
			}
		}

		private void AdjustDirect(EditorVertexAttachment attachment, float delta) {
			foreach (var pair in startWeights) {
				for (int i = 0; i < pair.Value.Length && i < attachment.Weights.Count; i++)
					attachment.Weights[i].SetVertexWeight(pair.Key, pair.Value[i]);
				Dictionary<EditorWeights, float> targets = [];
				foreach (var weights in panel.SelectedBones)
					targets[weights] = weights.TryGetVertexWeight(pair.Key) + delta;
				MeshWeights.Adjust(attachment, pair.Key, targets);
			}
		}

		private static float BrushFalloff(float distance) {
			float radius = WeightsPanel.weights_size.GetFloat() / 2;
			float inner = radius * (1 - Math.Clamp(WeightsPanel.weights_feather.GetFloat() / 100, 0, 1));
			if (distance <= inner) return 1;
			if (distance >= radius) return 0;
			return (radius - distance) / (radius - inner);
		}

		private void Brush(EditorPanel editorPanel, EditorVertexAttachment attachment, Vector2F mouse) {
			float strength = Math.Clamp(WeightsPanel.weights_strength.GetFloat() / 100, 0, 1);
			var mode = WeightsPanel.Mode;
			IEnumerable<EditorVertex> scope = attachment.SelectedVertices.Count > 0 ? attachment.SelectedVertices : attachment.GetVertices();
			foreach (var vertex in scope.ToList()) {
				float falloff = BrushFalloff((ScreenOf(editorPanel, attachment, vertex) - mouse).Length);
				if (falloff <= 0) continue;

				Dictionary<EditorWeights, float> targets = [];
				if (mode == WeightsMode.Replace) {
					foreach (var weights in panel.SelectedBones) {
						float current = weights.TryGetVertexWeight(vertex);
						targets[weights] = NMath.Lerp(falloff, current, strength);
					}
				}
				else {
					// a stroke gives each vertex at most Strength
					float amount = strength * falloff;
					float extra = amount - applied.GetValueOrDefault(vertex);
					if (extra <= 0) continue;
					applied[vertex] = amount;
					foreach (var weights in panel.SelectedBones)
						targets[weights] = weights.TryGetVertexWeight(vertex)
							+ (mode == WeightsMode.Add ? extra : -extra);
				}
				MeshWeights.Adjust(attachment, vertex, targets);
			}
		}

		public override void ToolRelease(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenEnd, bool dragged) {
			var attachment = Target;
			if (attachment == null) return;

			if (!started) {
				if (clickedBone != null)
					panel.ClickBone(clickedBone, ctrlClick);
				else if (clickedVertex != null) {
					if (!ctrlClick)
						attachment.SelectedVertices.Clear();
					if (!attachment.SelectedVertices.Remove(clickedVertex))
						attachment.SelectedVertices.Add(clickedVertex);
				}
				// With nothing selected, clicking another mesh picks it
				else if (!ctrlClick && attachment.SelectedVertices.Count == 0 && clickedOther != null)
					ModelEditor.Active.SelectObject(clickedOther);
				else if (!ctrlClick)
					attachment.SelectedVertices.Clear();
			}
			else if (dragMode == DragMode.Box) {
				float x0 = MathF.Min(mouseScreenStart.X, boxEnd.X), x1 = MathF.Max(mouseScreenStart.X, boxEnd.X);
				float y0 = MathF.Min(mouseScreenStart.Y, boxEnd.Y), y1 = MathF.Max(mouseScreenStart.Y, boxEnd.Y);
				if (!ctrlClick)
					attachment.SelectedVertices.Clear();
				foreach (var vertex in attachment.GetVertices()) {
					var screen = ScreenOf(editorPanel, attachment, vertex);
					if (screen.X >= x0 && screen.X <= x1 && screen.Y >= y0 && screen.Y <= y1)
						attachment.SelectedVertices.Add(vertex);
				}
			}

			started = false;
			dragMode = DragMode.None;
			startWeights.Clear();
			applied.Clear();
		}

		public override RectangleF? SelectionBox => dragMode == DragMode.Box ? EditorPanel.ScreenBox(boxStart, boxEnd) : null;

		public override void PaintOverlay(EditorPanel editorPanel, float width, float height) {
			if (WeightsPanel.Mode == WeightsMode.Direct || Target == null || !editorPanel.IsHovered() || dragMode == DragMode.Box) return;
			var mouse = editorPanel.GetMousePos();
			float radius = WeightsPanel.weights_size.GetFloat() / 2;
			float inner = radius * (1 - Math.Clamp(WeightsPanel.weights_feather.GetFloat() / 100, 0, 1));
			Graphics2D.SetDrawColor(255, 255, 255, 170);
			Graphics2D.DrawRing(mouse, radius - 1, radius, 0, 360, 64);
			if (inner > 1 && inner < radius - 1)
				Graphics2D.DrawRing(mouse, inner - 1, inner, 0, 360, 64);
		}

		/// <summary>
		/// Copied weights, one bone to weight map per vertex
		/// </summary>
		private static List<Dictionary<EditorBone, float>>? clipboard;

		private static List<EditorVertex> SelectedInOrder(EditorVertexAttachment attachment) {
			List<EditorVertex> vertices = [];
			foreach (var vertex in attachment.GetVertices()) {
				if (attachment.SelectedVertices.Contains(vertex))
					vertices.Add(vertex);
			}
			return vertices;
		}

		public static void SelectAll() {
			var attachment = Target;
			if (attachment == null) return;
			attachment.SelectedVertices.UnionWith(attachment.GetVertices());
		}

		public static void Copy() {
			var attachment = Target;
			if (attachment == null || attachment.SelectedVertices.Count == 0) return;
			List<Dictionary<EditorBone, float>> copied = [];
			foreach (var vertex in SelectedInOrder(attachment)) {
				Dictionary<EditorBone, float> weights = [];
				foreach (var boneWeights in attachment.Weights)
					weights.Add(boneWeights.Bone, boneWeights.TryGetVertexWeight(vertex));
				copied.Add(weights);
			}
			clipboard = copied;
		}

		public static void Paste() {
			var attachment = Target;
			if (attachment == null || clipboard == null) return;
			var vertices = SelectedInOrder(attachment);
			if (vertices.Count != clipboard.Count) {
				Logs.Warn($"Paste weights: copied {clipboard.Count} vertices, {vertices.Count} selected");
				return;
			}

			ModelEditor.Active.Actions.TrackMesh(attachment);
			MeshWeights.EnsurePositions(attachment);
			List<Vector2F> world = [];
			foreach (var vertex in vertices)
				world.Add(attachment.CalculateVertexWorldPosition(attachment.WorldTransform, vertex));
			for (int i = 0; i < vertices.Count; i++) {
				var vertex = vertices[i];
				foreach (var weights in attachment.Weights)
					weights.SetVertexWeight(vertex, clipboard[i].GetValueOrDefault(weights.Bone));
				// bones that aren't bound here
				if (MathF.Abs(attachment.Weights.Sum(w => w.TryGetVertexWeight(vertex)) - 1) > 0.0001f)
					MeshWeights.Normalize(attachment, vertex, world[i]);
			}
		}
	}
}
