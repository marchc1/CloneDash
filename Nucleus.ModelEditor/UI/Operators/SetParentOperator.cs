namespace Nucleus.ModelEditor.UI.Operators
{
	/// <summary>
	/// Click the new parent in the viewport or tree
	/// </summary>
	public class SetParentOperator : Operator
	{
		public override string Name => "Set Parent";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => item is EditorAttachment ? [typeof(EditorSlot)] : [typeof(EditorBone)];

		private IEditorType? item;
		// Everything selected that's the same kind as the last pick
		private IEditorType[] items = [];

		public override bool CanActivate(out string? reason) {
			reason = ModelEditor.Active.AnimationMode ? "Set Parent only works in setup mode." : null;
			return reason == null;
		}

		protected override void Activated() {
			item = UIDeterminations.Last;
			List<IEditorType> sameKind = [];
			if (item != null) {
				foreach (var selected in UIDeterminations.Selected ?? []) {
					if (Kind(selected) == Kind(item))
						sameKind.Add(selected);
				}
			}
			items = sameKind.ToArray();
			if (items.Length == 0 && item != null)
				items = [item];
		}

		private static int Kind(IEditorType editorType) =>
			editorType switch { EditorBone => 0, EditorSlot => 1, EditorAttachment => 2, _ => 3 };

		public override bool HoverTest(IEditorType? type) =>
			type == null || items.Any(x => ModelEditor.Active.File.CanReparent(x, type));

		public override void Selected(ModelEditor editor, IEditorType type) {
			foreach (var child in items) {
				if (editor.File.CanReparent(child, type))
					editor.File.Reparent(child, type);
			}
		}
	}
}
