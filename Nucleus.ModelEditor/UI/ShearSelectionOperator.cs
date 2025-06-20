using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class ShearSelectionOperator : DefaultOperator
	{
		private static void DrawCircleSector(float startAngle, float endAngle, bool isYAxis) {
			Color inner = isYAxis ? new Color(50, 255, 50, 100) : new Color(255, 50, 50, 100);
			Color outer = isYAxis ? new Color(15, 255, 15, 225) : new Color(255, 15, 15, 225);

			Raylib.DrawCircleSector(new(0, 0), 36, startAngle, endAngle, 24, inner);
			Raylib.DrawCircleSectorLines(new(0, 0), 36, startAngle, endAngle, 24, outer);
		}
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target) {
			Texture texB = EngineCore.Level.Textures.LoadTextureFromFile("models/gizmo_shear_base.png");
			Texture texX = EngineCore.Level.Textures.LoadTextureFromFile("models/gizmo_shear_dirX.png");
			Texture texY = EngineCore.Level.Textures.LoadTextureFromFile("models/gizmo_shear_dirY.png");
			float size = 96f;
			Vector2F worldPos = editorPanel.GridToScreen(target.GetWorldPosition());
			float worldRot = target.GetScreenRotation();

			var offset = Graphics2D.Offset;
			Rlgl.PushMatrix();
			Rlgl.Translatef(worldPos.X + offset.X, worldPos.Y + offset.Y, 0);
			Rlgl.Rotatef(-worldRot + target.GetShearX(), 0, 0, 1);

			Graphics2D.ResetDrawingOffset();

			Rlgl.PushMatrix();
			Rlgl.Rotatef(0, 0, 0, 1);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(texB);
			Graphics2D.DrawImage(new(-(size * 2f) / 2f), new(size * 2f), new(0), 0);
			DrawCircleSector(0, -target.GetShearX(), false);
			DrawCircleSector(180, 180 - target.GetShearX(), false);
			DrawCircleSector(90, 90 - target.GetShearY(), true);
			DrawCircleSector(270, 270 - target.GetShearY(), true);
			Rlgl.PopMatrix();
			Rlgl.DrawRenderBatchActive();

			Rlgl.PushMatrix();
			Rlgl.Rotatef(-target.GetShearX(), 0, 0, 1);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(texX);
			Graphics2D.DrawImage(new(-size / 2f), new(size), new(0), 0);
			Rlgl.PopMatrix();
			Rlgl.DrawRenderBatchActive();

			Rlgl.PushMatrix();
			Rlgl.Rotatef(-target.GetShearY(), 0, 0, 1);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(texY);
			Graphics2D.DrawImage(new(-size / 2f), new(size), new(0), 0);
			Rlgl.PopMatrix();
			Rlgl.DrawRenderBatchActive();

			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}

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