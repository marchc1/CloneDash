using Nucleus.Core;
using Nucleus.Models;
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

		private struct PreUIDeterminations
		{
			public bool OnlySelectedOne;
			public bool AllShareAType;
			public Type? SharedType;
			public object? Last;
			public int Count;
		}

		private PreUIDeterminations GetDeterminations() {
			PreUIDeterminations determinations = new();

			var count = ModelEditor.Active.SelectedObjectsCount;
			var last = ModelEditor.Active.LastSelectedObject;
			if (ModelEditor.Active.AreAllSelectedObjectsTheSameType(out Type? type)) {
				determinations.OnlySelectedOne = count == 1;
				determinations.AllShareAType = true;
				determinations.SharedType = type;
			}

			determinations.Last = ModelEditor.Active.LastSelectedObject;
			determinations.Count = ModelEditor.Active.SelectedObjectsCount;

			return determinations;
		}

		private string DetermineHeaderText(PreUIDeterminations determinations) {
			var text = "";
			if (determinations.AllShareAType) {
				var count = determinations.Count;
				var last = determinations.Last;
				switch (last) {
					case Model model: text = count > 1 ? $"{count} models selected" : $"Model '{model.Name}'"; break;
					case Bone bone: text = count > 1 ? $"{count} bones selected" : $"Bone '{bone.Name}'"; break;
					case Slot slot: text = count > 1 ? $"{count} slots selected" : $"Slot '{slot.Name}'"; break;
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
		private void DetermineOperators(Panel buttons, PreUIDeterminations determinations) {
			switch (determinations.Last) {
				case Bone bone:

					NewMenu(buttons, [
						new("Bone", () => bone.AddBone("test")),
						new("Slot", () => {
							var name = TinyFileDialogs.InputBox("New name", "Gimme a slot name");
							if(!name.Cancelled){
								bone.AddSlot(name.Result);
							}
						}),
					]);
					break;
			}
		}

		private void ModelEditor_Active_SelectedChanged() {
			DockMargin = RectangleF.TLRB(-8, 0, 0, 4);
			this.ClearChildren();
			// Process type
			if (!ModelEditor.Active.AreObjectsSelected)
				return;

			var determinations = GetDeterminations();

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
			buttons.Size = new(32);
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
