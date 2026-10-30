using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System.Xml.Linq;

namespace Nucleus.ModelEditor
{
	public class TranslateSelectionOperator : DefaultOperator
	{
		public enum GizmoAxis { None, X, Y }

		/// <summary>
		/// Which gizmo arrow is under a point in panel space
		/// </summary>
		public static GizmoAxis PickAxis(EditorPanel editorPanel, IEditorType target, Vector2F point) {
			var offset = point - editorPanel.GridToScreen(target.GetWorldPosition());
			var sinCos = MathF.SinCos(TransformAxes.GizmoRotation(target, editorPanel.TransformMode) * MathF.PI / 180);
			// Along the X arrow, and along the Y arrow (screen y points down)
			float alongX = offset.X * sinCos.Cos - offset.Y * sinCos.Sin;
			float alongY = -(offset.X * sinCos.Sin + offset.Y * sinCos.Cos);

			// Arrows reach ~40px out at 96px gizmo size and are ~24px thick
			if (alongX >= 2 && alongX <= 44 && MathF.Abs(alongY) <= 12) return GizmoAxis.X;
			if (alongY >= 2 && alongY <= 44 && MathF.Abs(alongX) <= 12) return GizmoAxis.Y;
			return GizmoAxis.None;
		}

		public static Vector2F AxisDirection(EditorPanel editorPanel, IEditorType target, GizmoAxis axis) {
			var sinCos = MathF.SinCos(TransformAxes.GizmoRotation(target, editorPanel.TransformMode) * MathF.PI / 180);
			return axis == GizmoAxis.Y ? new Vector2F(-sinCos.Sin, sinCos.Cos) : new Vector2F(sinCos.Cos, sinCos.Sin);
		}

		public static void DrawDualAxis(string name, EditorPanel editorPanel, IEditorType target, GizmoAxis highlight = GizmoAxis.None) {
			if (!target.CanTranslate() && name == "translate") return;
			if (!target.CanScale() && name == "scale") return;
			ITexture texX = textures.LoadTextureFromFile($"models/gizmo_{name}_dirX.png");
			ITexture texY = textures.LoadTextureFromFile($"models/gizmo_{name}_dirY.png");
			float size = 96f;
			Vector2F worldPos = editorPanel.GridToScreen(target.GetWorldPosition());
			var axes = editorPanel.TransformMode;
			float worldRot = TransformAxes.GizmoRotation(target, axes);

			var offset = Graphics2D.Offset;
			Rlgl.PushMatrix();
			Rlgl.Translatef(worldPos.X + offset.X, worldPos.Y + offset.Y, 0);
			Rlgl.Rotatef(-worldRot, 0, 0, 1);

			Graphics2D.ResetDrawingOffset();

			// the arrow that isn't hovered/dragged fades back
			Graphics2D.SetDrawColor(255, 255, 255, highlight == GizmoAxis.Y ? 120 : 255);
			Graphics2D.SetTexture((ITexture)texX);
			Graphics2D.DrawTexturedRectangle(new Vector2F(-size / 2f), new(size));
			Rlgl.DrawRenderBatchActive();

			Rlgl.PushMatrix();
			if (axes == EditorTransformMode.LocalCoordinates)
				Rlgl.Rotatef(target.GetShearX() + -target.GetShearY(), 0, 0, 1);
			Graphics2D.SetDrawColor(255, 255, 255, highlight == GizmoAxis.X ? 120 : 255);
			Graphics2D.SetTexture((ITexture)texY);
			Graphics2D.DrawTexturedRectangle(new Vector2F(-size / 2f), new(size));
			Rlgl.PopMatrix();
			Rlgl.DrawRenderBatchActive();

			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target)
			=> DrawDualAxis("translate", editorPanel, target,
				dragging ? axis : PickAxis(editorPanel, target, editorPanel.GetMousePos()));

		private IEditorType etype;

		Vector2F gridDragLast;
		Vector2F pressedAt;
		GizmoAxis axis;
		bool dragging;

		public override bool GizmoClicked(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) {
			pressedAt = mouseScreenStart;
			return base.GizmoClicked(editorPanel, target, mouseScreenStart);
		}

		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			gridDragLast = editorPanel.ScreenToGrid(mouseScreenStart);
			var transformable = currentSelection?.DeferTransformationsTo();
			axis = transformable == null ? GizmoAxis.None : PickAxis(editorPanel, transformable, pressedAt);

			if (axis == GizmoAxis.None && clicked != null && clicked != currentSelection && clicked.CanRotate()) {
				ModelEditor.Active.SelectObject(clicked);
				etype = clicked;
			}
			else if (currentSelection != null)
				etype = currentSelection;
			else
				return false;

			dragging = true;
			return true;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			var gridDrag = editorPanel.ScreenToGrid(mouseScreenNow);
			var delta = gridDrag - gridDragLast;
			gridDragLast = gridDrag;

			// resolve 
			if (axis != GizmoAxis.None && etype?.DeferTransformationsTo() is IEditorType transformable) {
				var direction = AxisDirection(editorPanel, transformable, axis);
				delta = direction * (delta.X * direction.X + delta.Y * direction.Y);
			}

			delta *= -1;
			ModelEditor.Active.File.MoveSelectedWorldspace(delta);
		}

		public override void GizmoEndDragging(EditorPanel editorPanel, Vector2F mouseScreenEnd, IEditorType target) {
			dragging = false;
			axis = GizmoAxis.None;
			base.GizmoEndDragging(editorPanel, mouseScreenEnd, target);
		}
	}
}