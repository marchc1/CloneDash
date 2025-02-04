using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Platform;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
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
			ModelEditor.Active.File.Cleared += File_Cleared;
		}

		private void File_Cleared(EditorFile file) {
			ClearProperties();
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
			else
				text = $"{determinations.Count} items selected";

			return text;
		}
		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
		Panel Props;
		private FlexPanel NewRow(Panel props, string label, string? icon = null) {
			Panel p = props.Add(new Panel() {
				Dock = Dock.Top,
				Size = new(0, 30),
				DockMargin = RectangleF.TLRB(0, 8, 8, -1),
				BorderSize = 1,
				BackgroundColor = new(30, 35, 40),
				ForegroundColor = new(120, 125, 130)
			});

			Label test = p.Add<Label>();
			test.Dock = Dock.Left;
			test.Text = label;
			test.Size = new(110);
			test.TextAlignment = Anchor.CenterLeft;
			test.TextPadding = new(32, 0);
			test.DrawBackground = true;
			test.TextSize = 19;
			test.BackgroundColor = new(60, 65, 70);

			FlexPanel inner = p.Add<FlexPanel>();
			inner.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			inner.Direction = Directional180.Horizontal;
			inner.DockPadding = RectangleF.Zero;
			inner.Dock = Dock.Fill;

			ManagedMemory.Texture? tex = null;
			if (icon != null) {
				tex = Level.Textures.LoadTextureFromFile(icon);
			}

			test.PaintOverride += (self, w, h) => {
				Label l = (self as Label);
				Graphics2D.SetDrawColor(l.BackgroundColor);
				Graphics2D.DrawRectangle(0, 0, w, h);

				if (IValidatable.IsValid(tex)) {
					Graphics2D.SetTexture(tex);
					Graphics2D.SetDrawColor(255, 255, 255);
					Graphics2D.DrawImage(new(2, (h - 24) / 2), new(24, 24));
				}

				Vector2F textDrawingPosition = Anchor.GetPositionGivenAlignment(l.TextAlignment, self.RenderBounds.Size, l.TextPadding);
				Graphics2D.SetDrawColor(l.TextColor);
				Graphics2D.DrawText(textDrawingPosition, l.Text, l.Font, l.TextSize, l.TextAlignment);
			};

			return inner;
		}
		private Panel AddInternalPropPanel(Panel prop) {
			var first = !prop.AddParent.HasChildren;

			var panel = prop.Add<Panel>();
			panel.DrawPanelBackground = false;
			panel.DockPadding = RectangleF.Zero;

			panel.SetTag<bool>("first", first);

			panel.PaintOverride += (s, w, h) => {
				if (s.GetTag<bool>("first")) return;
				Graphics2D.SetDrawColor(s.ForegroundColor);
				Graphics2D.DrawLine(0, 0, 0, h, 3);
			};

			return panel;
		}
		private Checkbox AddLabeledCheckbox(Panel prop, string text, bool @checked = false) {
			var panel = AddInternalPropPanel(prop);

			var checkbox = panel.Add<Checkbox>();
			var label = panel.Add<Label>();

			checkbox.Dock = Dock.Left;
			checkbox.DockMargin = RectangleF.TLRB(4, 6, 7, 4);
			checkbox.Checked = @checked;

			label.Dock = Dock.Fill;
			label.Text = text;
			label.TextAlignment = Anchor.CenterLeft;
			label.DockMargin = RectangleF.TLRB(4);

			return checkbox;
		}
		private NumSlider AddNumSlider(Panel prop, float currentValue = 0) {
			var panel = AddInternalPropPanel(prop);

			var numslider = panel.Add<NumSlider>();
			numslider.Dock = Dock.Fill;
			numslider.Value = currentValue;
			numslider.TextAlignment = Anchor.Center;
			numslider.BorderSize = 0;

			return numslider;
		}
		private ColorSelector AddColorSelector(Panel prop, Color? currentColor = null) {
			var panel = AddInternalPropPanel(prop);

			var selector = panel.Add<ColorSelector>();
			selector.Dock = Dock.Left;
			selector.Size = new(96);
			selector.SelectedColor = currentColor ?? Color.WHITE;
			selector.BorderSize = 0;

			return selector;
		}

		private DropdownSelector<T> AddEnumComboBox<T>(Panel prop, T? value) where T : Enum {
			var panel = AddInternalPropPanel(prop);

			var selector = panel.Add(DropdownSelector<T>.FromEnum<T>(value ?? default(T)));
			selector.Dock = Dock.Left;
			selector.Size = new(96);
			selector.Selected = value;
			selector.BorderSize = 0;

			return selector;
		}
		private void DetermineProperties(Panel props, PreUIDeterminations determinations) {
			ModelEditor editor = ModelEditor.Active;
			EditorFile file = editor.File;
			switch (determinations.Last) {
				case EditorBone bone:
					var transformRow = NewRow(props, "Transform", "models/bonetransform.png");

					var boneTransformData = bone.TransformMode.Unpack();
					var boneRotation = AddLabeledCheckbox(transformRow, "Rotation", boneTransformData.Rotation);
					var boneScale = AddLabeledCheckbox(transformRow, "Scale", boneTransformData.Scale);
					var boneReflection = AddLabeledCheckbox(transformRow, "Reflection", boneTransformData.Reflection);

					var lengthRow = NewRow(props, "Length", "models/bonelength.png");

					var boneLength = AddNumSlider(lengthRow, bone.Length);
					boneLength.MinimumValue = 0;
					boneLength.OnValueChanged += (_, _, v) => file.SetBoneLength(bone, (float)v);

					var viewportRow = NewRow(props, "Viewport", "models/info.png");

					var boneViewportIconContainer = AddInternalPropPanel(viewportRow);
					var boneViewportName = AddLabeledCheckbox(viewportRow, "Name", bone.ViewportShowName);
					var boneViewportSelectable = AddLabeledCheckbox(viewportRow, "Selectable", bone.ViewportCanSelect);

					var boneColorRow = NewRow(props, "Color", "models/colorwheel.png");
					var boneColor = AddColorSelector(boneColorRow, bone.Color);
					break;
				case EditorSlot slot:
					var slotColorRow = NewRow(props, "Color", "models/colorwheel.png");

					var slotColorSelector = AddColorSelector(slotColorRow, slot.Color);
					var slotDarkColorSelector = AddColorSelector(slotColorRow, slot.DarkColor);
					var slotTintCheck = AddLabeledCheckbox(slotColorRow, "Tint black?", slot.TintBlack);

					slotDarkColorSelector.Parent.EngineDisabled = !slot.TintBlack;

					slotColorSelector.ColorChanged += (_, c) => {
						slot.Color = c;
					};

					slotDarkColorSelector.ColorChanged += (_, c) => {
						slot.DarkColor = c;
					};

					slotTintCheck.OnCheckedChanged += (s) => {
						slot.TintBlack = s.Checked;
						slotDarkColorSelector.Parent.Enabled = slot.TintBlack;
						slotColorRow.InvalidateLayout();
					};

					var slotBlendRow = NewRow(props, "Blending", "models/blending.png");
					var blending = AddEnumComboBox<BlendMode>(slotBlendRow, slot.Blending);


					break;
			}
		}
		private struct NewItemAction
		{
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

				foreach (var action in actions) {
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

		private Button NewTopOperatorButton(Panel props, string icon) {
			var btn = props.Add<Button>();
			btn.Dock = Dock.Right;
			btn.DockMargin = RectangleF.TLRB(8, 0, 0, 8);
			btn.Size = new(32);
			btn.Text = "";
			btn.BorderSize = 0;
			btn.ImageOrientation = ImageOrientation.Centered;
			btn.Image = Level.Textures.LoadTextureFromFile(icon);
			return btn;
		}

		private void DeleteOperator(Panel props, PreUIDeterminations determinations) {
			var btn = NewTopOperatorButton(props, "models/delete.png");
			var rename = NewTopOperatorButton(props, "models/rename.png");
			var duplicate = NewTopOperatorButton(props, "models/duplicate.png");
		}

		private void DetermineTopOperators(Panel props, PreUIDeterminations determinations) {
			if (determinations.AllShareAType) {
				switch (determinations.Last) {
					case EditorBone bone:
						DeleteOperator(props, determinations);
						break;
				}
			}
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

		private void ClearProperties() {
			this.ClearChildren();
		}
		private void ModelEditor_Active_SelectedChanged() {
			DockMargin = RectangleF.TLRB(-8, 0, 0, 4);
			ClearProperties();
			// Process type
			//if (!ModelEditor.Active.AreObjectsSelected)
			//return;

			var determinations = ModelEditor.Active.GetDeterminations();
			if (determinations.Count == 0)
				return;

			var top = Add<Panel>();
			top.Size = new(48);
			top.BorderSize = 0;
			top.Dock = Dock.Top;

			var label = top.Add<Label>();
			label.Text = DetermineHeaderText(determinations);
			label.Dock = Dock.Left;
			label.Size = new(38);
			label.TextPadding = new(11);
			label.TextAlignment = Types.Anchor.TopLeft;
			label.TextSize = 22;
			label.AutoSize = true;
			this.DockPadding = RectangleF.Zero;

			DetermineTopOperators(top, determinations);

			if (determinations.AllShareAType) {
				DetermineProperties(this, determinations);

				var buttons = Add<CenteredObjectsPanel>();
				buttons.Dock = Dock.Top;
				buttons.Size = new(50);
				buttons.DockMargin = RectangleF.TLRB(8, 0, 0, 0);
				buttons.XSeparation = 8;
				buttons.YSeparation = 16;
				DetermineOperators(buttons, determinations);
				// If no operators for this type, avoid wasting the space for them
				if (!buttons.HasChildren)
					buttons.Remove();
			}
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
