using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class CreateBonesOperator : DefaultOperator
	{
		private static void DrawCircleSector(float startAngle, float endAngle, bool isYAxis) {
			Color inner = isYAxis ? new Color(50, 255, 50, 100) : new Color(255, 50, 50, 100);
			Color outer = isYAxis ? new Color(15, 255, 15, 225) : new Color(255, 15, 15, 225);

			Raylib.DrawCircleSector(new(0, 0), 36, startAngle, endAngle, 24, inner);
			Raylib.DrawCircleSectorLines(new(0, 0), 36, startAngle, endAngle, 24, outer);
		}
		public override void GizmoRender(EditorPanel editorPanel, IEditorType target) {

		}

		private EditorBone? parentBone;
		private EditorBone? bone;
		Vector2F gridDragLast;

		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			if (parentBone == null) throw new Exception("Wtf?");
			MakeBone();
			var bonePos = parentBone.WorldTransform.WorldToLocal(editorPanel.ScreenToGrid(mouseScreenStart));
			ModelEditor.Active.File.TranslateXSelected(bonePos.X);
			ModelEditor.Active.File.TranslateYSelected(bonePos.Y);

			return bone != null;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			var bonePos = bone.WorldTransform.LocalToWorld(0, 0);
			var boneEnd = editorPanel.ScreenToGrid(mouseScreenNow);
			var length = boneEnd.Distance(bonePos);
			var delta = boneEnd - bonePos;
			var rotation = MathF.Atan2(delta.Y, delta.X).ToDegrees();

			ModelEditor.Active.File.SetBoneLength(bone, length);
			ModelEditor.Active.File.RotateSelected(bone.WorldTransform.WorldToLocalRotation(rotation));
		}

		public override void GizmoEndDragging(EditorPanel editorPanel, Vector2F mouseScreenEnd, IEditorType target) {
			base.GizmoEndDragging(editorPanel, mouseScreenEnd, target);
		}

		public override bool GizmoClicked(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) {
			bone = null;

			gridDragLast = editorPanel.ScreenToGrid(mouseScreenStart);

			if (ModelEditor.Active.SelectedObjectsCount <= 0 && target != null)
				return false;

			var test = ModelEditor.Active.SelectedObjectsCount <= 0 ? ModelEditor.Active.File.Models[0].Root : ModelEditor.Active.LastSelectedObject;
			if (test is not EditorBone boneTarget) return false;
			parentBone = boneTarget;

			ModelEditor.Active.SelectObject(parentBone);

			return true;
		}

		private void MakeBone() {
			if (parentBone == null) return;
			if (bone != null) return;
			bone = ModelEditor.Active.File.AddBone(parentBone.Model, parentBone).ResultOrThrow;
			ModelEditor.Active.SelectObject(bone);
		}

		public override bool GizmoReleased(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) {
			if (target != null)
				return true;

			MakeBone();
			return false;
		}
	}
}
