using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System.Xml.Linq;

namespace Nucleus.ModelEditor
{
	public class TranslateSelectionOperator : DefaultOperator
	{
		public static void DrawDualAxis(string name, EditorPanel editorPanel, IEditorType target) {
			if (!target.CanTranslate() && name == "translate") return;
			if (!target.CanScale() && name == "scale") return;
			Texture texX = EngineCore.Level.Textures.LoadTextureFromFile($"models/gizmo_{name}_dirX.png");
			Texture texY = EngineCore.Level.Textures.LoadTextureFromFile($"models/gizmo_{name}_dirY.png");
			float size = 96f;
			Vector2F worldPos = editorPanel.GridToScreen(target.GetWorldPosition());
			float worldRot = target.GetScreenRotation();

			var offset = Graphics2D.Offset;
			Rlgl.PushMatrix();
			Rlgl.Translatef(worldPos.X + offset.X, worldPos.Y + offset.Y, 0);
			Rlgl.Rotatef(-worldRot, 0, 0, 1);

			Graphics2D.ResetDrawingOffset();

			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(texX);
			Graphics2D.DrawImage(new(-size / 2f), new(size), new(0), 0);
			Rlgl.DrawRenderBatchActive();

			Rlgl.PushMatrix();
			Rlgl.Rotatef(target.GetShearX() + -target.GetShearY(), 0, 0, 1);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(texY);
			Graphics2D.DrawImage(new(-size / 2f), new(size), new(0), 0);
			Rlgl.PopMatrix();
			Rlgl.DrawRenderBatchActive();

			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target) => DrawDualAxis("translate", editorPanel, target);

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
			delta *= -1;
			ModelEditor.Active.File.MoveSelectedWorldspace(delta);
		}
	}
}