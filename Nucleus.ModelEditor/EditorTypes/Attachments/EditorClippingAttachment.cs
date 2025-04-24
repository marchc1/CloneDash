using Nucleus.ModelEditor.UI;
using Nucleus.Models;
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
			CurrentMode = mode;
			UpdateButtonState();
		}

		private Button CreateButton, DeleteButton, NewButton;
		private Checkbox Triangles, Dim, Isolate, Deform;
		List<EditorVertex> WorkingLines = [];
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
			win.Title = "Edit Mesh";
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

		public List<EditorVertex> ShapeEdges = [];
		public IEnumerable<EditorVertex> GetVertices() {
			foreach (var co in ShapeEdges) {
				co.Attachment = this;
				yield return co;
			}
		}
	}
}
