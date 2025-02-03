using Nucleus.Core;
using Nucleus.ModelEditor.UI;
using Nucleus.Platform;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus.ModelEditor
{
	public class PropertiesPanel : Panel
	{
		protected override void Initialize() {
			base.Initialize();
			DrawPanelBackground = false;

			ModelEditor.Active.SelectedChanged += ModelEditor_Active_SelectedChanged;
		}

		private string DetermineHeaderText(PreUIDeterminations determinations) {
			var text = "";
			if (determinations.AllShareAType) {
				var count = determinations.Count;
				var last = determinations.Last;
				switch (last) {
					case EditorModel model: text = count > 1 ? $"{count} models selected" : $"Model '{model.Name}'"; break;
					case EditorBone bone: text = count > 1 ? $"{count} bones selected" : $"Bone '{bone.Name}'"; break;
					case EditorSlot slot: text = count > 1 ? $"{count} slots selected" : $"Slot '{slot.Name}'"; break;
					case ModelImages images: text = $"Image files"; break;
				}
			}
			else text = $"{determinations.Count} items selected";
			

			return text;
		}
		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
		Panel Props;

		private void DetermineProperties(Panel items, PreUIDeterminations determinations) {

		}
		private struct NewItemAction {
			public string Text;
			public Action OnClicked;

			public NewItemAction(string text, Action clicked) {
				Text = text;
				OnClicked = clicked;
			}
		}
		private void NewMenu(Panel buttons, List<NewItemAction> actions) {
			var newBtn = buttons.Add<Button>();
			newBtn.Text = "New...";
			newBtn.Size = new(64);

			newBtn.MouseReleaseEvent += (_, fs, _) => {
				Menu menu = UI.Menu();

				foreach(var action in actions) {
					menu.AddButton(action.Text, null, () => {
						action.OnClicked?.Invoke();
					});
				}

				menu.Open(fs.MouseState.MousePos);
			};
		}
		private void NewSlotDialog(EditorFile file, EditorBone bone) {
			EditorDialogs.TextInput(
				"New Slot",
				"Enter the name for the new slot.",
				"",
				true,
				(name) => {
					var result = file.AddSlot(bone.Model, bone, name);
					if (result.Failed) 
						EditorDialogs.ConfirmAction("Slot creation error", result.Reason, true, () => NewSlotDialog(file, bone));
				}, null
			);
		}
		private void DetermineOperators(Panel buttons, PreUIDeterminations determinations) {
			ModelEditor editor = ModelEditor.Active;
			EditorFile file = editor.File;

			switch (determinations.Last) {
				case EditorBone bone:
					NewMenu(buttons, [
						new("Bone", () => file.AddBone(bone.Model, bone, null)),
						new("Slot", () => NewSlotDialog(file, bone)),
					]);
					break;
				case ModelImages images:
					var refreshBtn = buttons.Add<Button>();
					refreshBtn.Text = "Refresh";
					refreshBtn.Size = new(96);
					var openDirBtn = buttons.Add<Button>();
					openDirBtn.Text = "Open Directory";
					openDirBtn.Size = new(128);
					break;
			}
		}

		private void ModelEditor_Active_SelectedChanged() {
			DockMargin = RectangleF.TLRB(-8, 0, 0, 4);
			this.ClearChildren();
			// Process type
			//if (!ModelEditor.Active.AreObjectsSelected)
				//return;

			var determinations = ModelEditor.Active.GetDeterminations();

			var label = Add<Label>();
			label.Text = DetermineHeaderText(determinations);
			label.Dock = Dock.Top;
			label.Size = new(38);
			label.TextPadding = new(4);
			label.TextAlignment = Types.Anchor.TopLeft;
			label.TextSize = 18;

			DetermineProperties(this, determinations);

			var buttons = Add<CenteredObjectsPanel>();
			buttons.Dock = Dock.Top;
			buttons.Size = new(48);
			buttons.XSeparation = 8;
			buttons.YSeparation = 16;
			DetermineOperators(buttons, determinations);
		}

		protected override void ModifyLayout(ref RectangleF renderBounds) {
			base.ModifyLayout(ref renderBounds);
		}
		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			MainThread.RunASAP(() => {
				Size = SizeOfAllChildren;
			});
		}
		protected override void PostLayoutChildren() {
			base.PostLayoutChildren();

		}
	}
}
