using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public enum EditClipping_Mode
	{
		Create,
		Delete,
		New
	}
	public class EditClippingOperator : Operator
	{
		public EditClipping_Mode CurrentMode { get; private set; } = EditClipping_Mode.Create;
		public override string Name => $"Edit Clipping: {CurrentMode}";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(EditorVertex)];

		public void SetMode(EditClipping_Mode mode) {
			// Leaving New (or clicking it again) closes the polygon, a too short one is dropped
			if (CurrentMode == EditClipping_Mode.New && WorkingLines.Count >= MIN_VERTICES) {
				ApplyNew();
				if (mode == EditClipping_Mode.New)
					mode = EditClipping_Mode.Create;
			}
			CurrentMode = mode;
			UpdateButtonState();
		}

		private Button CreateButton, DeleteButton, NewButton;
		private Checkbox Triangles, Dim, Isolate, Deform;
		List<EditorVertex> WorkingLines = [];
		public List<EditorVertex> GetWorkingVertices() => WorkingLines;

		private void UpdateButtonState() {
			CreateButton.Pulsing = CurrentMode == EditClipping_Mode.Create;
			DeleteButton.Pulsing = CurrentMode == EditClipping_Mode.Delete;
			NewButton.Pulsing = CurrentMode == EditClipping_Mode.New;
			WorkingLines.Clear();
		}

		public override void ChangeEditorProperties(CenteredObjectsPanel panel) {
			panel.			Size = new(0, 182);

			var win = new Window(panel);
			win.			Size = new(330, panel.Size.H);
			win.Center();
			win.Title = "Edit Clipping";
			win.HideNonCloseButtons();

			var row1 = new FlexPanel(win);
			row1.			DockPadding = RectangleF.TLRB(0, 4, 4, 0);
			row1.			Dock = Dock.Top;
			row1.			Size = new(0, 32);
			row1.Direction = Axis.Horizontal;
			row1.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

			var row2 = new FlexPanel(win);
			row2.			DockPadding = RectangleF.TLRB(0, 4, 4, 0);
			row2.			Dock = Dock.Top;
			row2.			Size = new(0, 32);
			row2.Direction = Axis.Horizontal;
			row2.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

			CreateButton = new Button(row1); CreateButton.Text = "Create"; CreateButton.SetAutoSize(true); CreateButton.SetTextPadding(new(64, 0)); CreateButton.OnButtonClick += (_, _) => SetMode(EditClipping_Mode.Create);
			DeleteButton = new Button(row1); DeleteButton.Text = "Delete"; DeleteButton.SetAutoSize(true); DeleteButton.SetTextPadding(new(64, 0)); DeleteButton.OnButtonClick += (_, _) => SetMode(EditClipping_Mode.Delete);

			NewButton = new Button(row2); NewButton.Text = "New"; NewButton.SetAutoSize(true); NewButton.SetTextPadding(new(64, 0)); NewButton.OnButtonClick += (_, _) => SetMode(EditClipping_Mode.New);

			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}
		public EditorVertex? HoveredVertex;
		/// <summary>
		/// Edge from ShapeEdges[i] to the next, -1 for none
		/// </summary>
		public int HoveredEdge = -1;
		public EditorClippingAttachment Attachment => this.UIDeterminations.Last as EditorClippingAttachment ?? throw new Exception("Wtf?");

		public const int MIN_VERTICES = 3;
		private const double DOUBLE_CLICK = 0.3;
		private const float DRAG_START = 4;
		static bool CtrlDown => EngineCore.Level.FrameState.Keyboard.ControlDown;

		bool mouseDown;
		bool dragged;
		bool boxing;
		Vector2F clickScreen;
		Vector2F moveStartLocal;
		EditorVertex? pendingDelete;
		readonly List<(EditorVertex Vertex, Vector2F Start)> moving = [];
		double lastClickTime = -1;
		EditorVertex? lastClickVertex;

		private Vector2F MouseLocal(ModelEditor editor, Vector2F mousePos) =>
			Attachment.WorldTransform.WorldToLocal(editor.Editor.ScreenToGrid(mousePos));
		List<EditorVertex> shownVertices => CurrentMode == EditClipping_Mode.New ? WorkingLines : Attachment.ShapeEdges;

		private static float ProjectT(Vector2F p, Vector2F a, Vector2F b) {
			var ab = b - a;
			float lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
			return lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / lengthSquared, 0, 1);
		}

		public override void Think(ModelEditor editor, Vector2F mousePos) {
			Attachment.SetupWorldTransform();
			HoveredVertex = null;
			HoveredEdge = -1;
			var mouse = MouseLocal(editor, editor.Editor.GetMousePos());
			var reach = 12f / ModelEditor.Active.Editor.CameraZoom;

			float closest = float.MaxValue;
			foreach (var vertex in shownVertices) {
				if (mouseDown && moving.Any(x => x.Vertex == vertex)) continue;
				float distance = mouse.Distance(vertex.ToVector());
				if (distance < closest) {
					closest = distance;
					HoveredVertex = vertex;
				}
			}
			if (closest > reach)
				HoveredVertex = null;

			if (HoveredVertex != null || CurrentMode != EditClipping_Mode.Create) return;
			var edges = Attachment.ShapeEdges;
			closest = reach;
			for (int i = 0; i < edges.Count; i++) {
				Vector2F a = edges[i].ToVector(), b = edges[(i + 1) % edges.Count].ToVector();
				float distance = mouse.Distance(Vector2F.Lerp(ProjectT(mouse, a, b), a, b));
				if (distance < closest) {
					closest = distance;
					HoveredEdge = i;
				}
			}
		}

		public override bool SelectMultiple => true;
		public override void Selected(ModelEditor editor, IEditorType type) {
			base.Selected(editor, type);
		}

		public override RectangleF? SelectionBox => boxing && dragged ? EditorPanel.ScreenBox(clickScreen, ModelEditor.Active.Editor.GetMousePos()) : null;

		private bool DeleteVertex(EditorVertex vertex) {
			if (Attachment.ShapeEdges.Count <= MIN_VERTICES) return false;
			Attachment.SelectedVertices.Remove(vertex);
			Attachment.RemoveVertex(vertex);
			Attachment.Invalidate();
			return true;
		}

		private void BeginMove(IEnumerable<EditorVertex> vertices, Vector2F local) {
			moving.Clear();
			foreach (var vertex in vertices)
				moving.Add((vertex, vertex.ToVector()));
			moveStartLocal = local;
		}

		private void ApplyNew() {
			var attachment = Attachment;
			ModelEditor.Active.Actions.TrackMesh(attachment);
			attachment.ShapeEdges.Clear();
			attachment.Weights.Clear();
			attachment.SelectedVertices.Clear();
			attachment.ShapeEdges.AddRange(WorkingLines);
			attachment.Invalidate();
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);
			editor.Actions.TrackMesh(Attachment);
			mouseDown = true;
			dragged = boxing = false;
			pendingDelete = null;
			moving.Clear();
			clickScreen = mousePos;
			var local = MouseLocal(editor, mousePos);
			var selected = Attachment.SelectedVertices;

			var now = EngineCore.Level.Realtime;
			bool doubleClick = HoveredVertex != null && HoveredVertex == lastClickVertex && now - lastClickTime < DOUBLE_CLICK;
			lastClickVertex = doubleClick ? null : HoveredVertex;
			lastClickTime = now;

			switch (CurrentMode) {
				case EditClipping_Mode.Create:
					if (HoveredVertex != null) {
						if (doubleClick)
							DeleteVertex(HoveredVertex);
						else
							BeginMove(selected.Contains(HoveredVertex) ? selected : [HoveredVertex], local);
					}
					else if (HoveredEdge >= 0) {
						var edges = Attachment.ShapeEdges;
						Vector2F a = edges[HoveredEdge].ToVector();
						Vector2F b = edges[(HoveredEdge + 1) % edges.Count].ToVector();
						var created = EditorVertex.FromVector(Vector2F.Lerp(ProjectT(local, a, b), a, b), new(0, 0), Attachment);
						edges.Insert(HoveredEdge + 1, created);
						Attachment.Invalidate();
						BeginMove([created], local);
					}
					break;
				case EditClipping_Mode.Delete:
					if (HoveredVertex == null) {
						boxing = true;
						break;
					}
					if (CtrlDown) {
						if (!selected.Remove(HoveredVertex))
							selected.Add(HoveredVertex);
						break;
					}
					// Wait for the release, it might be a drag
					pendingDelete = HoveredVertex;
					BeginMove(selected.Contains(HoveredVertex) ? selected : [HoveredVertex], local);
					break;
				case EditClipping_Mode.New:
					if (HoveredVertex != null) {
						if (HoveredVertex == WorkingLines[0] && WorkingLines.Count >= MIN_VERTICES)
							SetMode(EditClipping_Mode.Create);
						else if (doubleClick)
							WorkingLines.Remove(HoveredVertex);
						else
							BeginMove([HoveredVertex], local);
					}
					else
						WorkingLines.Add(EditorVertex.FromVector(local, new(0, 0), Attachment));
					break;
			}
		}

		public override void DragStart(ModelEditor editor, Vector2F mousePos) {
			base.DragStart(editor, mousePos);
		}

		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			if (!mouseDown) return;
			if (!dragged && (mousePos - clickScreen).Length < DRAG_START) return;
			dragged = true;
			if (moving.Count == 0) return;
			var delta = MouseLocal(editor, mousePos) - moveStartLocal;
			if (CurrentMode != EditClipping_Mode.New)
				editor.Actions.TrackMesh(Attachment);
			foreach (var item in moving)
				item.Vertex.SetPos(item.Start + delta);
			if (CurrentMode != EditClipping_Mode.New) {
				EditorFile.UpdateVertexPositions(Attachment);
				Attachment.Invalidate();
			}
		}

		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
			if (!mouseDown) return;
			mouseDown = false;
			var selected = Attachment.SelectedVertices;

			if (pendingDelete != null && !dragged) {
				// Deleting a selected vertex takes the whole selection with it.
				var doomed = selected.Contains(pendingDelete) ? selected.ToList() : [pendingDelete];
				foreach (var vertex in doomed)
					DeleteVertex(vertex);
			}
			else if (boxing && dragged) {
				var boxStart = editor.Editor.ScreenToGrid(clickScreen);
				var boxEnd = editor.Editor.ScreenToGrid(editor.Editor.GetMousePos());
				float minX = MathF.Min(boxStart.X, boxEnd.X), maxX = MathF.Max(boxStart.X, boxEnd.X);
				float minY = MathF.Min(boxStart.Y, boxEnd.Y), maxY = MathF.Max(boxStart.Y, boxEnd.Y);
				if (!CtrlDown)
					selected.Clear();
				foreach (var vertex in Attachment.ShapeEdges) {
					var position = Attachment.CalculateVertexWorldPosition(Attachment.WorldTransform, vertex);
					if (position.X >= minX && position.X <= maxX && position.Y >= minY && position.Y <= maxY)
						selected.Add(vertex);
				}
			}
			else if (boxing && !CtrlDown)
				selected.Clear();

			boxing = false;
			pendingDelete = null;
			moving.Clear();
		}

		// right click switches between Create and Delete
		public override void RightClicked(ModelEditor editor, Vector2F mousePos) {
			if (!mouseDown)
				SetMode(CurrentMode == EditClipping_Mode.Create ? EditClipping_Mode.Delete : EditClipping_Mode.Create);
		}

		public override bool RenderOverride() {
			return false;
		}

		internal void DrawCreateGizmo() {
			var camsize = ModelEditor.Active.Editor.CameraZoom;
			var mp = ModelEditor.Active.Editor.ScreenToGrid(ModelEditor.Active.Editor.GetMousePos());

			Raylib.DrawCircleV(mp.ToNumerics(), 5 / camsize, new(100, 180, 100));
		}
	}

	public class EditorClippingAttachment : EditorVertexAttachment, IClipPolygon<EditorSlot>
	{
		public override string SingleName => "clipping";
		public override string PluralName => "clippings";
		public override string EditorIcon => "models/clipping.png";

		public EditorSlot? EndSlot { get; set; }

		public override void BuildProperties(Panel props, PreUIDeterminations determinations) {
			base.BuildProperties(props, determinations);
			var slotRow = PropertiesPanel.NewRow(props, "End Slot", "models/clip_end.png");
			var slot = PropertiesPanel.AddComboBox(slotRow, EndSlot, GetModel().Slots, (s) => {
				if (s == null) return Slot.Name;
				return s.Name;
			}, (n) => {

			});
		}

		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<EditClippingOperator>(buttons, "Clipping Editor", "models/clipping.png");
			PropertiesPanel.ButtonIcon(buttons, "Freeze", "models/clipping.png",
				(_, _) => ModelEditor.Active.File.FreezeVertexAttachment(this));
			base.BuildOperators(buttons, determinations);
		}

		public override Color DetermineVertexColor(bool selected, bool highlighted) {
			var hS = highlighted ? 245 : 165;
			bool deleteMode = ModelEditor.Active.File.ActiveOperator is EditClippingOperator clippingOp && clippingOp.CurrentMode == EditClipping_Mode.Delete;
			Color color;
			if (highlighted && deleteMode)
				color = new Color(255, 60, 15);
			else
				color = selected ? new Color(highlighted ? 180 : 0, 255, 255) : new Color(hS - 15, hS, hS);
			return color;
		}

		public override void Render() {
			if (ModelEditor.Active.Editor.ViewportOptions.Visible[(int)ViewportItemKind.Others])
				RenderOverlay();
			GetModel().Clipping.Start(this, Slot, EndSlot?.Name);
		}

		public override void RenderOverlay() {
			if (Hidden) return;
			SetupWorldTransform();

			base.RenderOverlay();
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			EditClippingOperator? clipOp = ModelEditor.Active.File.ActiveOperator as EditClippingOperator;

			Graphics2D.SetDrawColor(245, 100, 20);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			var transform = WorldTransform;

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

			var edges = (ModelEditor.Active.File.ActiveOperator is EditClippingOperator editClipOp && editClipOp.CurrentMode == EditClipping_Mode.New) ? editClipOp.GetWorkingVertices() : ShapeEdges;
			for (int i = 0; i < edges.Count; i++) {
				var edge1 = edges[i];
				var edge2 = edges[(i + 1) % edges.Count];
				var isHighlighted =
						((edge1.Hovered && clipOp == null)
						|| (clipOp != null && clipOp.HoveredVertex == edge1))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(EditorVertex));

				var isSelected = SelectedVertices.Contains(edge1) || SelectedVertices.Count == 0;

				var isEdgeSelected = (SelectedVertices.Contains(edge1) && SelectedVertices.Contains(edge2)) || SelectedVertices.Count == 0;


				var lineColor = isEdgeSelected ? new Color(195, 60, 60) : new Color(120, 45, 45);

				var vertex1 = CalculateVertexWorldPosition(transform, edge1);
				var vertex2 = CalculateVertexWorldPosition(transform, edge2);

				Raylib.DrawLineV(vertex1.ToNumerics(), vertex2.ToNumerics(), lineColor);

				if (Selected || (ModelEditor.Active.Editor.InWeightsMode && VertexWeightOperator.Target == this)) {
					RenderVertex(edge1, isHighlighted, vertex1);
				}
			}

			// hack; but has to be done with the current rendering order
			if (
				ModelEditor.Active.File.ActiveOperator is EditClippingOperator editClipOperator
				&& editClipOperator.CurrentMode == EditClipping_Mode.Create
				&& editClipOperator.HoveredEdge >= 0
				&& editClipOperator.HoveredVertex == null
			) {
				editClipOperator.DrawCreateGizmo();
			}
		}

		public int GetVerticesCount() => ShapeEdges.Count;

		public int ComputeWorldVertices(EditorSlot slot, int startAt, int length, Vector2F[] worldVertices, int offset) {
			for (int i = startAt; i < length; i++) {
				worldVertices[i + offset] = CalculateVertexWorldPosition(slot.Bone.WorldTransform, ShapeEdges[i]);
			}

			return length - startAt;
		}

		public int ComputeWorldVerticesInto(EditorSlot slot, Vector2F[] worldVertices)
			=> ComputeWorldVertices(slot, 0, ShapeEdges.Count, worldVertices, 0);
	}
}
