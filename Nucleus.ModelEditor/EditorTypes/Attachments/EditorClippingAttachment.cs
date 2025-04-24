using Nucleus.Core;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Poly2Tri;
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
			panel.Size = new(0, 182);

			var win = panel.Add<Window>();
			win.Size = new(330, panel.Size.H);
			win.Center();
			win.Title = "Edit Clipping";
			win.HideNonCloseButtons();

			var row1 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });
			var row2 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });

			CreateButton = row1.Add(new Button() { Text = "Create", AutoSize = true, TextPadding = new(32, 0) }); CreateButton.MouseReleaseEvent += (_, _, _) => SetMode(EditClipping_Mode.Create);
			DeleteButton = row1.Add(new Button() { Text = "Delete", AutoSize = true, TextPadding = new(32, 0) }); DeleteButton.MouseReleaseEvent += (_, _, _) => SetMode(EditClipping_Mode.Delete);

			NewButton = row2.Add(new Button() { Text = "New", AutoSize = true, TextPadding = new(32, 0) }); NewButton.MouseReleaseEvent += (_, _, _) => SetMode(EditClipping_Mode.New);
			
			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}

		public EditorVertex? AttachedVertex;
		public EditorVertex? HoveredVertex;
		/// <summary>
		/// In the context of <see cref="EditClipping_Mode.Create"/>, this is the created vertex.
		/// <br/>
		/// In the context of <see cref="EditClipping_Mode.Modify"/>, this is the clicked vertex.
		/// </summary>
		public EditorVertex? ClickedVertex;
		public EditorClippingAttachment Attachment => this.UIDeterminations.Last as EditorClippingAttachment ?? throw new Exception("Wtf?");

		public override void Think(ModelEditor editor, Vector2F mousePos) {
			EditorPanel editorPanel = editor.Editor;
			float closestVertexDist = 100000000;

			HoveredVertex = null;

			System.Numerics.Vector2 mp = (Attachment.WorldTransform.WorldToLocal(mousePos)).ToNumerics();

			var camsize = ModelEditor.Active.Editor.CameraZoom;
			var dist = 32f / camsize;
			var array = (CurrentMode == EditClipping_Mode.New ? WorkingLines : Attachment.ShapeEdges);
			for (int i = 0; i < array.Count; i++) {
				var vertex = array[i];
				var vertexDistance = mp.ToNucleus().Distance(vertex.ToVector());
				if (vertexDistance < closestVertexDist && vertex != ClickedVertex) {
					closestVertexDist = vertexDistance;
					HoveredVertex = vertex;
				}
			}

			if (closestVertexDist > dist) {
				HoveredVertex = null;
			}
		}

		public override bool SelectMultiple => true;
		public override void Selected(ModelEditor editor, IEditorType type) {
			base.Selected(editor, type);
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);

			// Overrides, for the create vertex mode, but designed this way in case
			// other things need it
			EditorVertex? clickVertexOverride = null;

			switch (CurrentMode) {
				case EditClipping_Mode.Create:
					break;
				case EditClipping_Mode.Delete:
					if (HoveredVertex != null) {
						Attachment.RemoveVertex(HoveredVertex);
						HoveredVertex = null;
						Attachment.Invalidate();
					}
					break;
				case EditClipping_Mode.New:
					if (WorkingLines.Count > 0 && HoveredVertex == WorkingLines.First()) {
						Attachment.ShapeEdges.Clear();
						Attachment.Weights.Clear();
						Attachment.ShapeEdges.AddRange(WorkingLines);
						Attachment.Invalidate();
						SetMode(EditClipping_Mode.Create);
					}
					else {
						Attachment.SetupWorldTransform();

						var gridpos = ModelEditor.Active.Editor.ScreenToGrid(mousePos);
						var attachpos = Attachment.WorldTransform.Translation;
						var attachClickPos = gridpos - attachpos;
						
						var newClickP = Attachment.WorldTransform.WorldToLocal(attachClickPos + Attachment.WorldTransform.Translation);
						WorkingLines.Add(EditorVertex.FromVector(newClickP, new(0, 0), Attachment));
					}
					break;
			}

			ClickedVertex = clickVertexOverride ?? HoveredVertex;
		}
		public override void DragStart(ModelEditor editor, Vector2F mousePos) {
			base.DragStart(editor, mousePos);
		}
		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			switch (CurrentMode) {
				case EditClipping_Mode.Create:
					if (ClickedVertex != null) {
						var mouseGridPos = editor.Editor.ScreenToGrid(mousePos);
						var localized = Attachment.WorldTransform.WorldToLocal(mouseGridPos);

						ClickedVertex.SetPos(localized);
						EditorFile.UpdateVertexPositions(ClickedVertex.Attachment);

						Attachment.Invalidate();
					}
					break;
			}
		}

		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
			switch (CurrentMode) {
				case EditClipping_Mode.Create:
					if (HoveredVertex != ClickedVertex && HoveredVertex != null && ClickedVertex != null && AttachedVertex != null) {
						// Remove the clicked vertex
						ClickedVertex.Attachment.RemoveVertex(ClickedVertex);
						// Instead, constraint AttachedVertex to HoveredVertex
						AttachedVertex.ConstrainTo(HoveredVertex);
					}
					break;
			}

			AttachedVertex = null;
			ClickedVertex = null;
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

	public class EditorClippingAttachment : EditorVertexAttachment
	{
		public override string SingleName => "clipping";
		public override string PluralName => "clippings";
		public override string EditorIcon => "models/clipping.png";

		public EditorSlot? EndSlot { get; set; }

		public override void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var slotRow = PropertiesPanel.NewRow(props, "Image", "models/clip_end.png");
			var slot = PropertiesPanel.AddComboBox(slotRow, EndSlot, GetModel().Slots, (s) => {
				if (s == null) return Slot.Name;
				return s.Name;
			}, (n) => {

			});
		}

		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<EditClippingOperator>(buttons, "Clipping Editor", "models/clipping.png");
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
			RenderOverlay();
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

			foreach (var tri in Triangles) {
				TriPoint tp1 = tri.Points[0], tp2 = tri.Points[1], tp3 = tri.Points[2];
				EditorVertex? av1 = tp1.AssociatedObject as EditorVertex,
							av2 = tp2.AssociatedObject as EditorVertex,
							av3 = tp3.AssociatedObject as EditorVertex;

				if (av1 == null || av2 == null || av3 == null) continue;

				//av1.Attachment = this;
				//av2.Attachment = this;
				//av3.Attachment = this;

				bool ic1 = av1.IsConstrainedTo(av2), ic2 = av2.IsConstrainedTo(av3), ic3 = av3.IsConstrainedTo(av1);

				Vector2F v1 = CalculateVertexWorldPosition(transform, av1),
						 v2 = CalculateVertexWorldPosition(transform, av2),
						 v3 = CalculateVertexWorldPosition(transform, av3);

				if (ic1) Graphics2D.DrawLine(v1, v2);
				if (ic2) Graphics2D.DrawLine(v2, v3);
				if (ic3) Graphics2D.DrawLine(v1, v1);
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

				if (Selected) {
					RenderVertex(edge1, isHighlighted, vertex1);
				}
			}

			// hack; but has to be done with the current rendering order
			if (
				ModelEditor.Active.File.ActiveOperator is EditClippingOperator editClipOperator
				&& editClipOperator.CurrentMode == EditClipping_Mode.Create
				&& editClipOperator.HoveredVertex == null
			) {
				editClipOperator.DrawCreateGizmo();
			}
		}
	}
}
