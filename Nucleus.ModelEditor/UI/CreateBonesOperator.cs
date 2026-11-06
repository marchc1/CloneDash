using Nucleus.Common.Types;
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
		// Slots of the attachments ctrl+clicked before the bone, in click order
		private readonly List<EditorSlot> carried = [];
		// alt: the selected bone gets placed again instead of a new one
		private EditorBone? recreate;
		Vector2F gridDragLast;

		public override bool GizmoStartDragging(EditorPanel editorPanel, Vector2F mouseScreenStart, IEditorType? currentSelection, IEditorType? clicked) {
			if (parentBone == null) return false;
			MakeBone(editorPanel.ScreenToGrid(mouseScreenStart));
			return bone != null;
		}

		public override void GizmoDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow, IEnumerable<IEditorType> targets) {
			if (bone == null) return;
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
			recreate = null;

			gridDragLast = editorPanel.ScreenToGrid(mouseScreenStart);

			if (ModelEditor.Active.AnimationMode) return false;
			if (ModelEditor.Active.SelectedObjectsCount <= 0 && target is EditorBone)
				return false;

			var selected = ModelEditor.Active.SelectedObjects.ToList();
			EditorBone? lastSelectedBone = null;
			for (int i = selected.Count - 1; i >= 0; i--) {
				if (selected[i] is EditorBone selectedBone) {
					lastSelectedBone = selectedBone;
					break;
				}
			}
			if (EngineCore.Level.FrameState.Keyboard.AltDown && lastSelectedBone is { Parent: not null } again) {
				recreate = again;
				parentBone = again.Parent;
				carried.Clear();
				return true;
			}
			var root = ModelEditor.Active.File.Models[0].Root;
			EditorBone? test;
			if (selected.Count <= 0)
				test = root;
			else
				test = lastSelectedBone ?? (selected.Any(x => x is EditorAttachment) ? root : null);
			if (test is not EditorBone boneTarget) return false;
			parentBone = boneTarget;
			carried.Clear();
			var carriedSlots = new HashSet<EditorSlot>();
			foreach (var selectedObject in selected) {
				if (selectedObject is EditorAttachment attachment && carriedSlots.Add(attachment.Slot))
					carried.Add(attachment.Slot);
			}

			ModelEditor.Active.SelectObject(parentBone);

			return true;
		}

		private void MakeBone(Vector2F point) {
			if (parentBone == null) return;
			if (bone != null) return;
			bone = recreate ?? ModelEditor.Active.File.AddBone(parentBone.Model, parentBone).ResultOrThrow;
			ModelEditor.Active.SelectObject(bone);

			var bonePos = parentBone.WorldTransform.WorldToLocal(point);
			ModelEditor.Active.File.TranslateXSelected(bonePos.X);
			ModelEditor.Active.File.TranslateYSelected(bonePos.Y);
			if (recreate != null)
				ModelEditor.Active.File.SetBoneLength(bone, 0);
		}

		// The slots move to the new bone, which takes the first one's name
		private void Carry() {
			if (bone == null || carried.Count == 0) return;
			var file = ModelEditor.Active.File;
			var name = carried[0].Name;
			if (bone.Model.FindBone(name) == null)
				file.RenameBone(bone, name);
			foreach (var slot in carried)
				file.Reparent(slot, bone);
			carried.Clear();
			ModelEditor.Active.SelectObject(bone);
		}

		public override bool GizmoReleased(EditorPanel editorPanel, IEditorType? target, Vector2F mouseScreenStart) {
			// a click on a bone picks the next parent, anything else makes a bone
			if (target is EditorBone && bone == null && carried.Count == 0 && recreate == null)
				return true;

			if (parentBone == null || ModelEditor.Active.AnimationMode) return true;
			MakeBone(editorPanel.ScreenToGrid(mouseScreenStart));
			Carry();
			// shift leaves the parent selected for the next sibling
			if (recreate == null && EngineCore.Level.FrameState.Keyboard.ShiftDown)
				ModelEditor.Active.SelectObject(parentBone);
			return false;
		}
	}
}
