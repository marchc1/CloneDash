
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor.UI.Operators
{
	public class ImageSetParentOperator : Operator
	{
		public override string Name => "Image: Set Parent";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(EditorBone), typeof(EditorSlot)];

		private ModelImage SelectedImage;

		protected override void Activated() {
			SelectedImage = UIDeterminations.Last as ModelImage ?? throw new Exception("Wtf?");
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

					DropdownSelector<EditorSlot>? dropdownSlot = null;
					if (bone.Slots.Count <= 0) {
						existingSlotPanel.Panel.Enabled = false;
						newSlotPanel.Checkbox.Checked = true;
					}
					else {
						dropdownSlot = existingSlotPanel.Panel.Add<DropdownSelector<EditorSlot>>();
						dropdownSlot.OnToString += (eSlot) => eSlot?.Name ?? "<null slot?>";
						dropdownSlot.Dock = Dock.Fill;
						dropdownSlot.Items.AddRange(bone.Slots);
						dropdownSlot.Selected = bone.Slots[0];
					}
					var newSlotName = newSlotPanel.Panel.Add<Textbox>();
					newSlotName.Dock = Dock.Fill;
					newSlotName.HelperText = "New slot name...";
					newSlotName.Text = SelectedImage.Name;

					EditorDialogs.SetupOKCancelButtons(
						boneDialog,
						true,
						() => {
							var file = ModelEditor.Active.File;
							if (existingSlotPanel.Checkbox.Checked && dropdownSlot != null && dropdownSlot.Selected != null) {
								var result = file.AddAttachment<EditorRegionAttachment>(dropdownSlot.Selected, newSlotName.Text);
								if (result.Failed) {

								}
								else {
									result.Result.Path = $"<{SelectedImage.Name}>";
								}
							}
							else {

							}
						},
						null
					);
					boneDialog.Size = new(boneDialog.Size.X, bone.Slots.Count <= 0 ? 158 : 184);
					break;
			}
		}
	}
}
