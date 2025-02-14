using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class RotateSelectionOperator : DefaultOperator
	{
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target) {
			if (!target.CanRotate()) return;
			Texture tex = EngineCore.Level.Textures.LoadTextureFromFile("models/gizmo_rotate.png");
			float size = 76f;
			Vector2F worldPos = editorPanel.GridToScreen(target.GetWorldPosition());
			float worldRot = target.GetScreenRotation();

			Graphics2D.SetTexture(tex);
			Graphics2D.SetDrawColor(255, 255, 255);
			var offset = Graphics2D.Offset;
			Rlgl.PushMatrix();
			Rlgl.Translatef(worldPos.X + offset.X, worldPos.Y + offset.Y, 0);
			Rlgl.Rotatef(-worldRot, 0, 0, 1);
			Graphics2D.ResetDrawingOffset();
			Graphics2D.DrawImage(new(-size / 2f), new(size), new(0), 0);
			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}

		private IEditorType etype;
		private RevolutionManager revolutionManager;

		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			currentSelection = currentSelection?.GetTransformableEditorType();
			clicked = clicked?.GetTransformableEditorType();

			if (clicked != null && clicked != currentSelection && clicked.CanRotate()) {
				ModelEditor.Active.SelectObject(clicked);
				etype = clicked;
				revolutionManager = new(clicked.GetWorldPosition(), editorPanel.ScreenToGrid(mouseScreenStart));
				return true;
			}
			else if (currentSelection != null) {
				etype = currentSelection;
				revolutionManager = new(currentSelection.GetWorldPosition(), editorPanel.ScreenToGrid(mouseScreenStart));
				return true;
			}
			else
				return false;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			if (revolutionManager == null) return;
			var angDelta = revolutionManager.CalculateDelta(editorPanel.ScreenToGrid(mouseScreenNow));
			ModelEditor.Active.File.RotateSelected(-angDelta, true);
			Console.WriteLine($"{angDelta}, {mouseScreenNow}");
		}
	}
}
