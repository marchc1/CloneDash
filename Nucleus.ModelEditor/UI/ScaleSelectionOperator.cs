using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	public class ScaleSelectionOperator : DefaultOperator
	{
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target)
			=> TranslateSelectionOperator.DrawDualAxis("scale", editorPanel, target,
				dragging ? axis : TranslateSelectionOperator.PickAxis(editorPanel, target, editorPanel.GetMousePos()));

		Vector2F pressedAt;
		Vector2F gridStart;
		Vector2F origin;
		Vector2F dirX, dirY;
		TranslateSelectionOperator.GizmoAxis axis;
		bool dragging;
		readonly Dictionary<IEditorType, Vector2F> startScales = [];

		public override bool GizmoClicked(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) {
			pressedAt = mouseScreenStart;
			return base.GizmoClicked(editorPanel, target, mouseScreenStart);
		}

		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			var transformable = currentSelection?.DeferTransformationsTo();
			axis = transformable == null ? TranslateSelectionOperator.GizmoAxis.None
				: TranslateSelectionOperator.PickAxis(editorPanel, transformable, pressedAt);

			// gizmo arrows take priority over whatever is under them
			if (axis == TranslateSelectionOperator.GizmoAxis.None && clicked != null && clicked != currentSelection && clicked.CanScale()) {
				ModelEditor.Active.SelectObject(clicked);
				transformable = clicked.DeferTransformationsTo();
			}
			if (transformable == null) return false;
			gridStart = editorPanel.ScreenToGrid(pressedAt);
			origin = transformable.GetWorldPosition();
			dirX = TranslateSelectionOperator.AxisDirection(editorPanel, transformable, TranslateSelectionOperator.GizmoAxis.X);
			dirY = TranslateSelectionOperator.AxisDirection(editorPanel, transformable, TranslateSelectionOperator.GizmoAxis.Y);

			startScales.Clear();
			foreach (var selected in ModelEditor.Active.SelectedObjects) {
				if (selected.DeferTransformationsTo() is IEditorType item && item.CanScale())
					startScales[item] = new(item.GetScaleX(), item.GetScaleY());
			}

			dragging = true;
			return true;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			var from = gridStart - origin;
			var to = editorPanel.ScreenToGrid(mouseScreenNow) - origin;

			float factorX = 1, factorY = 1;
			switch (axis) {
				case TranslateSelectionOperator.GizmoAxis.X:
					factorX = Ratio(Dot(to, dirX), Dot(from, dirX));
					break;
				case TranslateSelectionOperator.GizmoAxis.Y:
					factorY = Ratio(Dot(to, dirY), Dot(from, dirY));
					break;
				default:
					factorX = factorY = Ratio(to.Length, from.Length);
					break;
			}

			ModelEditor.Active.File.SetSelectedScale(item => startScales.TryGetValue(item, out var start) ? new(start.X * factorX, start.Y * factorY) : new(item.GetScaleX(), item.GetScaleY()));
		}

		private static float Ratio(float now, float start) => MathF.Abs(start) < 1e-3f ? 1 : now / start;

		private static float Dot(Vector2F a, Vector2F b) => a.X * b.X + a.Y * b.Y;

		public override void GizmoEndDragging(EditorPanel editorPanel, Vector2F mouseScreenEnd, IEditorType target) {
			dragging = false;
			axis = TranslateSelectionOperator.GizmoAxis.None;
			startScales.Clear();
			base.GizmoEndDragging(editorPanel, mouseScreenEnd, target);
		}
	}

}