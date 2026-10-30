using Nucleus.Types;

namespace Nucleus.ModelEditor.UI.Operators
{
	public enum NewConstraintKind
	{
		IK,
		Transform,
		Path
	}

	/// <summary>
	/// Picks the target for a new constraint on the selected bones.
	/// </summary>
	public class NewConstraintOperator : Operator
	{
		public NewConstraintKind Kind = NewConstraintKind.IK;

		public override string Name => Kind switch {
			NewConstraintKind.IK => "New IK Constraint",
			NewConstraintKind.Transform => "New Transform Constraint",
			_ => "New Path Constraint"
		};
		public override bool OverrideSelection => true;
		public override bool AcceptsEmptyClick => Kind != NewConstraintKind.Path;
		public override Type[]? SelectableTypes => Kind == NewConstraintKind.Path ? [typeof(EditorSlot), typeof(EditorPathAttachment)] : [typeof(EditorBone)];

		List<EditorBone> bones = [];

		private static List<EditorBone> SelectedBones() => ModelEditor.Active.SelectedObjects.OfType<EditorBone>().ToList();

		/// <summary>
		/// Why <paramref name="kind"/> can't be made from the current selection, or null
		/// </summary>
		public static string? Problem(NewConstraintKind kind) {
			if (ModelEditor.Active.AnimationMode) return "Constraints are made in setup mode.";
			var selected = SelectedBones();
			if (selected.Count == 0) return "Select the bones to constrain first.";
			if (kind == NewConstraintKind.IK) {
				if (selected.Count > 2) return "An IK constraint takes one bone, or a bone and one of its children.";
				if (selected.Count == 2 && selected[0].Parent != selected[1] && selected[1].Parent != selected[0])
					return "For two bones, select a bone and one of its children.";
			}
			return null;
		}

		public override bool CanActivate(out string? reason) {
			reason = Problem(Kind);
			return reason == null;
		}

		protected override void Activated() {
			bones = SelectedBones();
			// IK runs parent first
			if (Kind == NewConstraintKind.IK && bones.Count == 2 && bones[0].Parent == bones[1])
				bones.Reverse();
		}

		private bool DescendsFromConstrained(EditorBone bone) {
			for (var current = bone; current != null; current = current.Parent) {
				if (bones.Contains(current)) return true;
			}
			return false;
		}

		public override bool HoverTest(IEditorType? type) => type switch {
			null => true,
			EditorBone bone => Kind == NewConstraintKind.IK ? bones.Contains(bone) || !DescendsFromConstrained(bone) : !DescendsFromConstrained(bone),
			EditorSlot or EditorPathAttachment => Kind == NewConstraintKind.Path,
			_ => false
		};

		public override void Selected(ModelEditor editor, IEditorType type) {
			if (!HoverTest(type)) return;
			switch (type) {
				case EditorBone bone when bones.Contains(bone):
					var tip = bone.WorldTransform.LocalToWorld(bone.Length, 0);
					Create(editor, NewTarget(editor, tip));
					break;
				case EditorBone bone:
					Create(editor, bone);
					break;
				case EditorSlot slot:
					CreatePath(editor, slot);
					break;
				case EditorPathAttachment path:
					CreatePath(editor, path.Slot);
					break;
			}
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			if (Kind == NewConstraintKind.Path || editor.File.ActiveOperator != this) return;
			var target = NewTarget(editor, editor.Editor.ScreenToGrid(mousePos));
			Create(editor, target);
			Deactivate();
		}

		// new target bones go under the root
		private EditorBone NewTarget(ModelEditor editor, Vector2F world) {
			var model = bones[0].Model;
			var bone = editor.File.AddBone(model, model.Root, null).ResultOrThrow;
			var local = model.Root.WorldTransform.WorldToLocal(world);
			editor.Actions.TrackTransform(bone);
			bone.EditTranslationX(local.X);
			bone.EditTranslationY(local.Y);
			editor.File.NotifyTransformed(bone);
			return bone;
		}

		private void Create(ModelEditor editor, EditorBone target) {
			var model = bones[0].Model;
			EditorConstraint constraint = Kind == NewConstraintKind.IK
				? new EditorIkConstraint { Target = target, Name = EditorFile.UniqueConstraintName(model, "ik") }
				: new EditorTransformConstraint {
					Target = target,
					Name = EditorFile.UniqueConstraintName(model, "transform")
				};
			Finish(editor, constraint);
		}

		private void CreatePath(ModelEditor editor, EditorSlot slot) => Finish(editor, new EditorPathConstraint {
			Target = slot,
			Name = EditorFile.UniqueConstraintName(bones[0].Model, "path")
		});

		private void Finish(ModelEditor editor, EditorConstraint constraint) {
			constraint.Model = bones[0].Model;
			constraint.Bones = [.. bones];
			editor.File.AddConstraint(constraint);
			MainThread.RunASAP(() => editor.SelectObject(constraint));
		}
	}
}
