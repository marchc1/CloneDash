
namespace Nucleus.ModelEditor.UI.Operators
{
	public class ImageSetParentOperator : Operator
	{
		public override string Name => "Image: Set Parent";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(EditorBone), typeof(EditorSlot)];

		protected override void Activated() {

		}
		protected override void Deactivated(bool canceled) {

		}

		public override void Selected(ModelEditor editor, IEditorType type) {
			base.Selected(editor, type);
		}
	}
}
