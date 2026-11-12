using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	public abstract class DefaultOperator
	{
		protected class TestClass
		{
			public Func<IEditorType, bool>? TestFunc;
		}

		public virtual void Activated() { }
		public virtual void Deactivated() { }

		protected bool Test(Vector2F q1, Vector2F q2, Vector2F q3, Vector2F q4, Vector2F mp) {
			return mp.TestPointInQuad(q1, q2, q3, q4);
		}

		/// <summary>
		/// Return false to block dragging
		/// </summary>
		/// <param name="editorPanel"></param>
		/// <param name="target"></param>
		/// <param name="mouseScreenStart"></param>
		/// <returns></returns>
		public virtual bool GizmoClicked(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) => true;
		/// <summary>
		/// Return false to block selection
		/// </summary>
		/// <param name="editorPanel"></param>
		/// <param name="target"></param>
		/// <param name="mouseScreenStart"></param>
		/// <returns></returns>
		public virtual bool GizmoReleased(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) => true;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="editorPanel"></param>
		/// <param name="mouseGridStart"></param>
		/// <param name="target"></param>
		/// <returns>If the gizmo should be activated.</returns>
		public virtual bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) => true;
		public virtual void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) { }
		public virtual void GizmoEndDragging(EditorPanel editorPanel, Vector2F mouseScreenEnd, IEditorType target) { }
		public virtual void GizmoRender(EditorPanel editorPanel, IEditorType target) { }

		public virtual bool IsSelectable(IEditorType target) => true;

		/// <summary>
		/// true = the tool takes this click (and its drag/release) from selection
		/// </summary>
		public virtual bool ToolClick(EditorPanel editorPanel, Vector2F mouseScreen, bool ctrl) => false;
		public virtual void ToolDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow) { }
		public virtual void ToolRelease(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenEnd,
			bool dragged) { }
		/// <summary>
		/// Drawn over the viewport in panel space
		/// </summary>
		public virtual void PaintOverlay(EditorPanel editorPanel, float width, float height) { }
		public virtual RectangleF? SelectionBox => null;
	}
}
