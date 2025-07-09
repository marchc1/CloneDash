using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	public class ScaleSelectionOperator : DefaultOperator
	{
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target) => TranslateSelectionOperator.DrawDualAxis("scale", editorPanel, target);

		private IEditorType etype;

		Vector2F gridDragLast;
		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			gridDragLast = editorPanel.ScreenToGrid(mouseScreenStart);

			if (clicked != null && clicked != currentSelection && clicked.CanRotate()) {
				ModelEditor.Active.SelectObject(clicked);
				etype = clicked;
				return true;
			}
			else if (currentSelection != null) {
				etype = currentSelection;
				return true;
			}
			else
				return false;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			var gridDrag = editorPanel.ScreenToGrid(mouseScreenNow);
			var delta = gridDrag - gridDragLast;
			gridDragLast = gridDrag;

			// resolve 

			ModelEditor.Active.File.TranslateXSelected(delta.X, true);
			ModelEditor.Active.File.TranslateYSelected(delta.Y, true);
		}
	}

}