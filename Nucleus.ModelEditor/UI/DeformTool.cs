using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// Vertex dragging outside Edit Mesh, keys deform in animate mode
	/// </summary>
	public class DeformTool : DefaultOperator
	{
		private const float VERTEX_PICK = 10;
		private const float DRAG_START = 4;

		public static EditorVertexAttachment? Target {
			get {
				var editor = ModelEditor.Active;
				if ((editor.AnimationMode && !editor.CanInsertKeyframes()) || editor.SelectedObjectsCount != 1)
					return null;
				var panel = editor.Editor;
				if (!panel.InTranslateMode && !panel.InRotateMode && !panel.InScaleMode && !panel.InShearMode) return null;
				return editor.LastSelectedObject is EditorVertexAttachment attachment && attachment.Slot.GetActiveAttachment() == attachment ? attachment
					: null;
			}
		}

		EditorVertex? pressed;
		bool ctrlClick;
		bool started;
		bool boxing;
		Vector2F boxStart, boxEnd;
		readonly Dictionary<EditorVertex, Vector2F> startLocal = [];
		readonly Dictionary<(EditorVertex Vertex, EditorBone Bone), Vector2F> startBone = [];
		readonly Dictionary<EditorVertex, Vector2F> setupLocal = [];
		readonly Dictionary<(EditorVertex Vertex, EditorWeights Weights), Vector2F> setupBone = [];

		static bool Setup => !ModelEditor.Active.AnimationMode;

		// Linked meshes move the parent's vertices, matched by index
		private static EditorVertex GeometryKey(EditorVertexAttachment attachment, EditorVertex vertex) {
			var source = attachment.GeometrySource;
			return source == attachment ? vertex : source.VertexAt(attachment.IndexOfVertex(vertex)) ?? vertex;
		}

		private static EditorVertex DeformKey(EditorVertexAttachment attachment, EditorVertex vertex) {
			var source = attachment.DeformAttachment;
			return source == attachment ? vertex : source.VertexAt(attachment.IndexOfVertex(vertex)) ?? vertex;
		}

		private static Vector2F ScreenOf(EditorPanel panel, EditorVertexAttachment attachment, EditorVertex vertex)
			=> panel.GridToScreen(attachment.CalculateVertexWorldPosition(attachment.WorldTransform, vertex));

		private static EditorVertex? VertexAt(EditorPanel panel, EditorVertexAttachment attachment, Vector2F screen) {
			EditorVertex? best = null;
			float bestDistance = VERTEX_PICK;
			foreach (var vertex in attachment.GetVertices()) {
				float distance = (ScreenOf(panel, attachment, vertex) - screen).Length;
				if (distance <= bestDistance) {
					best = vertex;
					bestDistance = distance;
				}
			}
			return best;
		}

		public override bool ToolClick(EditorPanel editorPanel, Vector2F mouseScreen, bool ctrl) {
			var attachment = Target;
			if (attachment == null) return false;
			pressed = VertexAt(editorPanel, attachment, mouseScreen);
			ctrlClick = ctrl;
			started = boxing = false;
			if (pressed != null || ctrl) return true;
			// clicking the attachment away from its vertices = normal selection
			attachment.SelectedVertices.Clear();
			return false;
		}

		public override void ToolDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow) {
			var attachment = Target;
			if (attachment == null) return;
			if (!started) {
				if ((mouseScreenNow - mouseScreenStart).Length < DRAG_START) return;
				started = true;
				boxing = pressed == null || ctrlClick;
				if (!boxing)
					BeginMove(attachment);
			}
			if (boxing) {
				boxStart = mouseScreenStart;
				boxEnd = mouseScreenNow;
				return;
			}
			Move(attachment, editorPanel.ScreenToGrid(mouseScreenNow) - editorPanel.ScreenToGrid(mouseScreenStart));
		}

		private void BeginMove(EditorVertexAttachment attachment) {
			if (!attachment.SelectedVertices.Contains(pressed!)) {
				attachment.SelectedVertices.Clear();
				attachment.SelectedVertices.Add(pressed!);
			}
			if (Setup) {
				var geometry = attachment.GeometrySource;
				ModelEditor.Active.Actions.TrackMesh(geometry);
				setupLocal.Clear();
				setupBone.Clear();
				foreach (var vertex in attachment.SelectedVertices) {
					var key = GeometryKey(attachment, vertex);
					if (geometry.Weights.Count == 0) {
						setupLocal[key] = new(key.X, key.Y);
						continue;
					}
					foreach (var weights in geometry.Weights) {
						if (weights.TryGetVertexInfo(key, out var weight, out var pos) && weight > 0)
							setupBone[(key, weights)] = pos;
					}
				}
				return;
			}
			var source = attachment.DeformAttachment;
			source.CurrentDeform ??= [];
			source.CurrentBoneDeform ??= [];
			startLocal.Clear();
			startBone.Clear();
			foreach (var vertex in attachment.SelectedVertices) {
				var key = DeformKey(attachment, vertex);
				if (attachment.Weights.Count == 0) {
					startLocal[key] = source.CurrentDeform.GetValueOrDefault(key);
					continue;
				}
				foreach (var weights in attachment.Weights) {
					if (weights.TryGetVertexWeight(vertex) > 0)
						startBone[(key, weights.Bone)] = source.CurrentBoneDeform.GetValueOrDefault((key, weights.Bone));
				}
			}
		}

		// Weighted vertices move every bone's offset by the same world amount
		private void Move(EditorVertexAttachment attachment, Vector2F worldDelta) {
			if (Setup) {
				foreach (var pair in setupLocal) {
					var local = attachment.WorldTransform.WorldToLocal(worldDelta)
						- attachment.WorldTransform.WorldToLocal(Vector2F.Zero);
					pair.Key.X = pair.Value.X + local.X;
					pair.Key.Y = pair.Value.Y + local.Y;
				}
				foreach (var pair in setupBone) {
					var weights = pair.Key.Weights;
					var local = weights.Bone.WorldTransform.WorldToLocal(worldDelta)
						- weights.Bone.WorldTransform.WorldToLocal(Vector2F.Zero);
					weights.SetVertexPos(pair.Key.Vertex, pair.Value + local);
				}
				return;
			}
			var source = attachment.DeformAttachment;
			if (source.CurrentDeform == null || source.CurrentBoneDeform == null) return;
			foreach (var pair in startLocal) {
				var local = attachment.WorldTransform.WorldToLocal(worldDelta)
					- attachment.WorldTransform.WorldToLocal(Vector2F.Zero);
				source.CurrentDeform[pair.Key] = pair.Value + local;
			}
			foreach (var pair in startBone) {
				var bone = pair.Key.Bone;
				var local = bone.WorldTransform.WorldToLocal(worldDelta) - bone.WorldTransform.WorldToLocal(Vector2F.Zero);
				source.CurrentBoneDeform[(pair.Key.Vertex, bone)] = pair.Value + local;
			}
		}

		public override void ToolRelease(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenEnd, bool dragged) {
			var attachment = Target;
			if (attachment == null) return;
			if (!started) {
				if (pressed != null) {
					double now = EngineCore.Level.Realtime;
					// ctrl + double click selects every deformed vertex, or every undeformed one
					if (ctrlClick && pressed == lastClicked && now - lastClickTime < OutlinerPanel.DOUBLE_CLICK_TIME) {
						bool deformed = IsDeformed(attachment, pressed);
						attachment.SelectedVertices.Clear();
						foreach (var vertex in attachment.GetVertices()) {
							if (IsDeformed(attachment, vertex) == deformed)
								attachment.SelectedVertices.Add(vertex);
						}
						lastClicked = null;
					}
					else {
						if (!ctrlClick)
							attachment.SelectedVertices.Clear();
						if (!attachment.SelectedVertices.Remove(pressed))
							attachment.SelectedVertices.Add(pressed);
						lastClicked = pressed;
						lastClickTime = now;
					}
				}
			}
			else if (boxing) {
				float x0 = MathF.Min(mouseScreenStart.X, boxEnd.X), x1 = MathF.Max(mouseScreenStart.X, boxEnd.X);
				float y0 = MathF.Min(mouseScreenStart.Y, boxEnd.Y), y1 = MathF.Max(mouseScreenStart.Y, boxEnd.Y);
				if (!ctrlClick)
					attachment.SelectedVertices.Clear();
				foreach (var vertex in attachment.GetVertices()) {
					var screenPos = ScreenOf(editorPanel, attachment, vertex);
					if (screenPos.X >= x0 && screenPos.X <= x1 && screenPos.Y >= y0 && screenPos.Y <= y1)
						attachment.SelectedVertices.Add(vertex);
				}
			}
			else if (!Setup)
				ModelEditor.Active.File.PoseEdited(attachment, KeyframeProperty.Deform);

			started = boxing = false;
			pressed = null;
			startLocal.Clear();
			startBone.Clear();
			setupLocal.Clear();
			setupBone.Clear();
		}

		EditorVertex? lastClicked;
		double lastClickTime;

		public override RectangleF? SelectionBox => boxing ? EditorPanel.ScreenBox(boxStart, boxEnd) : null;

		// moved vertices show pink
		public override void PaintOverlay(EditorPanel editorPanel, float width, float height) {
			var attachment = Target;
			if (attachment == null) return;
			foreach (var vertex in attachment.GetVertices()) {
				var screenPos = ScreenOf(editorPanel, attachment, vertex);
				bool selected = attachment.SelectedVertices.Contains(vertex);
				bool deformed = IsDeformed(attachment, vertex);
				Graphics2D.SetDrawColor(0, 0, 0, 200);
				Graphics2D.DrawCircle(screenPos, selected ? 5 : 4);
				if (selected)
					Graphics2D.SetDrawColor(new Common.Types.Color(255, 255, 255));
				else if (deformed)
					Graphics2D.SetDrawColor(new Common.Types.Color(255, 110, 200));
				else
					Graphics2D.SetDrawColor(new Common.Types.Color(120, 200, 255));
				Graphics2D.DrawCircle(screenPos, selected ? 3.5f : 2.5f);
			}
		}

		private static bool IsDeformed(EditorVertexAttachment attachment, EditorVertex vertex) {
			var source = attachment.DeformAttachment;
			var key = DeformKey(attachment, vertex);
			if (attachment.Weights.Count == 0)
				return source.CurrentDeform != null && source.CurrentDeform.TryGetValue(key, out var offset) && offset.Length > 1e-4f;
			if (source.CurrentBoneDeform == null) return false;
			foreach (var weights in attachment.Weights) {
				if (source.CurrentBoneDeform.TryGetValue((key, weights.Bone), out var offset) && offset.Length > 1e-4f)
					return true;
			}
			return false;
		}
	}
}
