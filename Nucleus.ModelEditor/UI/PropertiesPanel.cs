using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
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
		public PropertiesPanel(Element parent) : base(parent) {
			SetPaintBackgroundEnabled(false);
			SetBorderSize(0);
			ModelEditor.Active.SelectedChanged += ModelEditor_Active_SelectedChanged;
			ModelEditor.Active.SetupAnimateModeChanged += (_, _) => ModelEditor_Active_SelectedChanged();
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
				if (last != null) {
					text = last.DetermineHeaderText(determinations) ?? (count > 1 ? $"{count} models selected" : $"{last.CapitalizedSingleName} '{last.GetName()}'");
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

		public class InnerRow : FlexPanel
		{
			public InnerRow(Element parent) : base(parent) {

			}
		}

		public class InnerRowLabel : Label
		{
			ITexture? tex;
			public InnerRowLabel(Element parent, ITexture? tex) : base(parent) {
				this.tex = tex;
			}
			public override void Paint(float w, float h) {
				Graphics2D.SetDrawColor(GetBgColor());
				Graphics2D.DrawRectangle(0, 0, w, h);

				if (IValidatable.IsValid(tex)) {
					Graphics2D.SetTexture(tex);
					Graphics2D.SetDrawColor(255, 255, 255);
					Graphics2D.DrawImage(new(2, (h - 24) / 2), new(24, 24));
				}

				Vector2F textDrawingPosition = GetTextAlignment().GetPositionGivenAlignment(GetRenderBounds().Size, GetTextPadding());
				Graphics2D.SetDrawColor(GetTextColor());
				Graphics2D.DrawText(textDrawingPosition, GetText(), GetFont(), GetTextSize(), GetTextAlignment());
			}
		}
		public static InnerRow NewRow(Panel props, string label, string? icon = null) {
			Panel p = new Panel(props);
			p.SetDock(Dock.Top);
			p.SetSize(new(0, 30));
			p.SetDockMargin(RectangleF.TLRB(0, 8, 8, -1));
			p.SetBorderSize(1);
			p.SetBgColor(new Color(30, 35, 40));
			p.SetFgColor(new Color(120, 125, 130));


			ManagedMemory.Texture? tex = null;
			if (icon != null) {
				tex = props.UI.Level.Textures.LoadTextureFromFile(icon);
			}
			InnerRowLabel test = new(p, tex);
			test.SetDock(Dock.Left);
			test.SetText(label);
			test.SetSize(new(110));
			test.SetTextAlignment(Anchor.CenterLeft);
			test.SetTextPadding(new(32, 0));
			test.SetPaintBackgroundEnabled(true);
			test.SetTextSize(19);
			test.SetBgColor(new Color(60, 65, 70));

			InnerRow inner = new(p);
			inner.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			inner.Direction = Axis.Horizontal;
			inner.SetDockPadding(RectangleF.Zero);
			inner.SetDock(Dock.Fill);

			return inner;
		}

		class InternalPropPanel(Element parent) : Panel(parent)
		{
			public override void Paint(float w, float h) {
				if (GetTag<bool>("first")) return;
				Graphics2D.SetDrawColor(GetFgColor());
				Graphics2D.DrawLine(0, 0, 0, h, 3);
			}
		}

		public static Panel AddInternalPropPanel(Panel prop) {
			var first = !prop.GetAddParent().HasChildren();

			var panel = new InternalPropPanel(prop);
			panel.SetPaintBackgroundEnabled(false);
			panel.SetDockPadding(RectangleF.Zero);
			panel.SetTag<bool>("first", first);

			return panel;
		}
		public static Checkbox AddLabeledCheckbox(Panel prop, string text, bool @checked = false) {
			var panel = AddInternalPropPanel(prop);

			var checkbox = new Checkbox(panel);
			var label = new Label(panel);

			checkbox.SetDock(Dock.Left);
			checkbox.SetDockMargin(RectangleF.TLRB(4, 6, 7, 4));
			checkbox.Checked = @checked;

			label.SetDock(Dock.Fill);
			label.SetText(text);
			label.SetTextAlignment(Anchor.CenterLeft);
			label.SetDockMargin(RectangleF.TLRB(4));

			return checkbox;
		}
		public static Textbox AddFilepath(Panel prop, string? currentPath, Action<Textbox, string> chosenPath) {
			var panel = AddInternalPropPanel(prop);

			var searchBtn = new Button(panel);
			searchBtn.SetBorderSize(0);
			searchBtn.SetDock(Dock.Right);
			searchBtn.SetSize(new(24));
			searchBtn.SetText("");
			var searchImg = new Nucleus.UI.Elements.Image(searchBtn);
			searchImg.SetTexture(prop.Level.Textures.LoadTextureFromFile("models/search.png"));
			searchImg.SetPassthru(true);
			searchImg.SetDock(Dock.Fill);

			var path = new Textbox(panel);
			path.SetDock(Dock.Fill);
			path.SetText(currentPath ?? "<null>");
			path.SetTextAlignment(Anchor.Center);
			path.SetBorderSize(0);

			path.OnUserPressedEnter += (_, _, txt) => chosenPath(path, txt);
			searchBtn.OnButtonClick += (_, _) => {
				var result = Platform.SelectFolderDialog("Select Images Folder", (filesystem.GetSearchPathID("game").First() as DiskSearchPath)!.RootDirectory);
				if (!result.Cancelled)
					chosenPath(path, result.Result);
			};

			return path;
		}
		public static NumSlider AddNumSlider(Panel prop, float currentValue = 0) {
			var panel = AddInternalPropPanel(prop);

			var numslider = new NumSlider(panel);
			numslider.SetDock(Dock.Fill);
			numslider.Value = currentValue;
			numslider.SetTextAlignment(Anchor.Center);
			numslider.SetBorderSize(0);

			return numslider;
		}
		public static ColorSelector AddColorSelector(Panel prop, Color? currentColor = null) {
			var panel = AddInternalPropPanel(prop);

			var selector = new ColorSelector(panel);
			selector.SetDock(Dock.Left);
			selector.SetSize(new(96));
			selector.SelectedColor = (currentColor ?? Color.White);
			selector.SetBorderSize(0);

			return selector;
		}
		/// <summary>
		/// Keyframeable variant of <see cref="AddColorSelector(Panel, Color?)"/>
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="currentColor"></param>
		/// <returns></returns>
		public static ColorSelector AddColorSelector(Panel prop, IEditorType target, KeyframeProperty property, int arrayIndex, Color? currentColor = null) {
			var panel = AddInternalPropPanel(prop);

			if (target is not EditorSlot slot)
				throw new Exception("Unsupported type for this method");

			var keyframe = new KeyframeButton(panel);
			keyframe.Property = property;
			keyframe.ArrayIndex = arrayIndex;
			keyframe.SetSize(new(24));
			keyframe.SetDock(Dock.Right);
			keyframe.OnButtonClick += (_, _) => ModelEditor.Active.File.InsertKeyframe(target, property, 0);

			var selector = new ColorSelector(panel);
			selector.SetDock(Dock.Fill);
			selector.SetSize(new(64));
			selector.SelectedColor = (currentColor ?? Color.White);
			selector.SetBorderSize(0);
			ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => selector.SelectedColor = (slot.GetColor());
			ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => selector.SelectedColor = (slot.GetColor());


			return selector;
		}

		public static DropdownSelector<T> AddComboBox<T>(Panel prop, T? value, IEnumerable<T> options, Func<T?, string> tostring, Action<T> change) {
			var panel = AddInternalPropPanel(prop);

			var selector = new DropdownSelector<T>(panel);
			selector.Items.AddRange(options);
			selector.Selected = value;
			selector.SetDock(Dock.Left);
			selector.SetSize(new(96));
			selector.SetBorderSize(0);
			selector.Selected = value;
			selector.OnToString += (t) => tostring(t);
			selector.OnSelectionChanged += (self, o, n) => change(n);

			return selector;
		}
		public static DropdownSelector<T> AddEnumComboBox<T>(Panel prop, T? value) where T : Enum {
			var panel = AddInternalPropPanel(prop);

			var selector = DropdownSelector<T>.FromEnum<T>(panel, value ?? default(T));
			selector.SetDock(Dock.Left);
			selector.SetSize(new(96));
			selector.SetBorderSize(0);
			selector.Selected = value;

			return selector;
		}
		private void DetermineProperties(Panel props, PreUIDeterminations determinations) {
			determinations.Last?.DeferPropertiesTo()?.BuildProperties(props, determinations);
		}
		public struct NewItemAction
		{
			public string Text;
			public Action OnClicked;

			public NewItemAction(string text, Action clicked) {
				Text = text;
				OnClicked = clicked;
			}
		}
		public static Button ButtonIcon(Panel buttons, string text, string? icon = null, Action<Element, ButtonCode>? onClicked = null) {
			var newBtn = new Button(buttons);
			newBtn.SetText(text);
			newBtn.SetAutoSize(true);
			if (icon != null) {
				var img = new Nucleus.UI.Elements.Image(newBtn);
				img.SetPassthru(true);
				img.SetSize(new(32));
				img.SetImageOrientation(ImageOrientation.Zoom);
				img.SetDock(Dock.Left);
				img.SetDockMargin(RectangleF.TLRB(2));
				img.SetTexture(buttons.Level.Textures.LoadTextureFromFile(icon));

				newBtn.SetTextPadding(new(34, 0));
				newBtn.SetTextAlignment(Anchor.CenterLeft);
			}

			newBtn.OnButtonClick += (e, mb) => onClicked?.Invoke(e, mb);
			return newBtn;
		}
		public static void OperatorButton<T>(Panel buttons, string text, string? icon = null) where T : Operator, new() {
			var btn = ButtonIcon(buttons, text, icon, (el, mb) => {
				var btn = ((el as Button) ?? throw new Exception("never should happen im lazy"));
				Operator? ourOperator = el.GetTag<Operator>("op");
				if (ourOperator != null && ourOperator == ModelEditor.Active.File.ActiveOperator) {
					// Multiple select does not cancel this way
					ModelEditor.Active.File.DeactivateOperator(!ModelEditor.Active.File.ActiveOperator.SelectMultiple);
					btn.Pulsing = false;
				}
				else {
					T op = ModelEditor.Active.File.InstantiateOperator<T>();
					el.SetTag("op", op);
					btn.Pulsing = true;
					op.OnDeactivated += (_, _, _) => btn.Pulsing = false;
				}
			});
		}
		public static void NewMenu(Panel buttons, List<NewItemAction> actions) {
			var button = ButtonIcon(buttons, "New...", "models/add.png", (_, _) => {
				Menu menu = buttons.UI.Menu();

				foreach (var action in actions) {
					menu.AddButton(action.Text, null, () => {
						action.OnClicked?.Invoke();
					});
				}

				menu.Open(EngineCore.Level.FrameState.Mouse.MousePos);
			});
			button.Thinking += (_) => {
				button.SetMouseInputEnabled(!ModelEditor.Active.AnimationMode);
			};
		}
		public static void NewSlotDialog(EditorFile file, EditorBone bone) {
			EditorDialogs.TextInput(
				"New Slot",
				"Enter the name for the new slot.",
				"",
				true,
				(name) => {
					var result = file.AddSlot(bone.Model, bone, new(name));
					if (result.Failed)
						EditorDialogs.ConfirmAction("Slot creation error", result.Reason, true, () => NewSlotDialog(file, bone));
				}, null
			);
		}
		public static void NewClippingDialog(EditorFile file, EditorSlot slot) {
			EditorDialogs.TextInput(
				"New Clipping",
				"Enter the name for the new slot.",
				"",
				true,
				(name) => {
					var result = file.AddAttachment<EditorClippingAttachment>(slot, new(name));
					if (result.Failed)
						EditorDialogs.ConfirmAction("Slot creation error", result.Reason, true, () => NewClippingDialog(file, slot));
				}, null
			);
		}
		public static void NewSkinDialog(EditorFile file, EditorModel model) {
			EditorDialogs.TextInput(
				"New Slot",
				"Enter the name for the new slot.",
				"",
				true,
				(name) => {
					var result = file.AddSkin(model, new(name));
					if (result.Failed)
						EditorDialogs.ConfirmAction("Skin creation error", result.Reason, true, () => NewSkinDialog(file, model));
				}, null
			);
		}
		public static void NewAnimationDialog(EditorFile file, AnimationsList anims) {
			EditorDialogs.TextInput(
				"New Animation",
				"Enter the name for the new animation.",
				"",
				true,
				(name) => {
					var result = file.AddAnimation(anims.Model, new(name));
					if (result.Failed)
						EditorDialogs.ConfirmAction("Animation creation error", result.Reason, true, () => NewAnimationDialog(file, anims));
				}, null
			);
		}



		public static Button NewTopOperatorButton(Panel props, string icon) {
			var btn = new Button(props);
			btn.SetDock(Dock.Right);
			btn.SetDockMargin(RectangleF.TLRB(8, 0, 0, 8));
			btn.SetSize(new(32));
			btn.SetText("");
			btn.SetBorderSize(0);
			var btnImg = new Nucleus.UI.Elements.Image(btn);
			btnImg.SetTexture(props.Level.Textures.LoadTextureFromFile(icon));
			btnImg.SetImageOrientation(ImageOrientation.Centered);
			btnImg.SetPassthru(true);
			btnImg.SetDock(Dock.Fill);
			return btn;
		}

		public static void DeleteOperator(IEditorType obj, Panel props, PreUIDeterminations determinations) {
			var btn = NewTopOperatorButton(props, "models/delete.png");
		}
		public static void RenameOperator(IEditorType obj, Panel props, PreUIDeterminations determinations) {
			var rename = NewTopOperatorButton(props, "models/rename.png");
		}
		public static void DuplicateOperator(IEditorType obj, Panel props, PreUIDeterminations determinations) {
			var duplicate = NewTopOperatorButton(props, "models/duplicate.png");
		}

		private void DetermineTopOperators(Panel props, PreUIDeterminations determinations) {
			if (determinations.AllShareAType) {
				var last = determinations.Last?.DeferPropertiesTo();
				if (last is IEditorType editorType)
					editorType.BuildTopOperators(props, determinations);
			}
		}

		private void DetermineOperators(Panel buttons, PreUIDeterminations determinations) {
			ModelEditor editor = ModelEditor.Active;
			EditorFile file = editor.File;
			var last = determinations.Last?.DeferPropertiesTo();
			last?.BuildOperators(buttons, determinations);
		}

		private void ClearProperties() {
			this.ClearChildren();
		}
		private void ModelEditor_Active_SelectedChanged() {
			SetDockMargin(RectangleF.TLRB(-8, 0, 0, 4));
			ClearProperties();
			// Process type
			//if (!ModelEditor.Active.AreObjectsSelected)
			//return;

			var determinations = ModelEditor.Active.GetDeterminations();
			if (determinations.Count == 0)
				return;

			var top = new Panel(this);
			top.SetSize(new(48));
			top.SetBorderSize(0);
			top.SetDock(Dock.Top);
			top.SetDockMargin(RectangleF.TLRB(4));

			var label = new Label(top);
			label.SetText(DetermineHeaderText(determinations));
			label.SetDock(Dock.Left);
			label.SetSize(new(38));
			label.SetTextPadding(new(11));
			label.SetTextAlignment(Types.Anchor.TopLeft);
			label.SetTextSize(22);
			label.SetAutoSize(true);
			this.SetDockPadding(RectangleF.Zero);

			DetermineTopOperators(top, determinations);

			if (determinations.AllShareAType) {
				DetermineProperties(this, determinations);

				var buttons = new CenteredObjectsPanel(this);
				buttons.SetDock(Dock.Top);
				buttons.SetSize(new(50));
				buttons.SetDockMargin(RectangleF.TLRB(8, 0, 0, 0));
				buttons.XSeparation = 8;
				buttons.YSeparation = 16;
				DetermineOperators(buttons, determinations);
				// If no operators for this type, avoid wasting the space for them
				if (!buttons.HasChildren())
					buttons.Remove();
			}
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
		}
	}
}
