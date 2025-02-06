
using Nucleus.UI;

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
			switch (type) {
				case EditorBone bone:
					var boneDialog = EditorDialogs.CreateDialogWindow("Image: Set Parent");
					EditorDialogs.SetupDescription(boneDialog, "How should the attachment be parented?");

					var existingSlotPanel = EditorDialogs.CreateOptionPanel(boneDialog, true, "Use an existing slot:");
					var newSlotPanel = EditorDialogs.CreateOptionPanel(boneDialog, false, "Use a new slot:");

					existingSlotPanel.Checkbox.Radio = true;
					newSlotPanel.Checkbox.Radio = true;

					existingSlotPanel.Checkbox.LinkRadioButton(newSlotPanel.Checkbox);

					EditorDialogs.SetupOKCancelButtons(
						boneDialog, 
						true, 
						() => {

						},
						() => {

						}
					);
					boneDialog.Size = new(boneDialog.Size.X, 184);
					break;
			}
		}
	}
}
