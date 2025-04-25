using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
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
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.ModelEditor
{
	public class PropertiesPanel : Panel
	{
		protected override void Initialize() {
			base.Initialize();
			DrawPanelBackground = false;

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
		public static FlexPanel NewRow(Panel props, string label, string? icon = null) {
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
				tex = props.UI.Level.Textures.LoadTextureFromFile(icon);
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
		public static Panel AddInternalPropPanel(Panel prop) {
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
		public static Checkbox AddLabeledCheckbox(Panel prop, string text, bool @checked = false) {
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
		public static Textbox AddFilepath(Panel prop, string? currentPath, Action<Textbox, string> chosenPath) {
			var panel = AddInternalPropPanel(prop);

			var searchBtn = panel.Add<Button>();
			searchBtn.BorderSize = 0;
			searchBtn.Dock = Dock.Right;
			searchBtn.Size = new(24);
			searchBtn.Text = "";
			searchBtn.Image = prop.Level.Textures.LoadTextureFromFile("models/search.png");

			var path = panel.Add<Textbox>();
			path.Dock = Dock.Fill;
			path.Text = currentPath ?? "<null>";
			path.TextAlignment = Anchor.Center;
			path.BorderSize = 0;

			path.OnUserPressedEnter += (_, _, txt) => chosenPath(path, txt);
			searchBtn.MouseReleaseEvent += (_, _, _) => {
				var result = Platform.SelectFolderDialog("Select Images Folder", (Filesystem.Path["game"][0] as DiskSearchPath).RootDirectory);
				if (!result.Cancelled)
					chosenPath(path, result.Result);
			};

			return path;
		}
		public static NumSlider AddNumSlider(Panel prop, float currentValue = 0) {
			var panel = AddInternalPropPanel(prop);

			var numslider = panel.Add<NumSlider>();
			numslider.Dock = Dock.Fill;
			numslider.Value = currentValue;
			numslider.TextAlignment = Anchor.Center;
			numslider.BorderSize = 0;

			return numslider;
		}
		public static ColorSelector AddColorSelector(Panel prop, Color? currentColor = null) {
			var panel = AddInternalPropPanel(prop);

			var selector = panel.Add<ColorSelector>();
			selector.Dock = Dock.Left;
			selector.Size = new(96);
			selector.SelectedColor = currentColor ?? Color.White;
			selector.BorderSize = 0;

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

			var keyframe = panel.Add<KeyframeButton>();
			keyframe.Property = property;
			keyframe.ArrayIndex = arrayIndex;
			keyframe.Size = new(24);
			keyframe.Dock = Dock.Right;
			keyframe.MouseReleaseEvent += (_, _, _) => ModelEditor.Active.File.InsertKeyframe(target, property, 0);

			var selector = panel.Add<ColorSelector>();
			selector.Dock = Dock.Fill;
			selector.Size = new(64);
			selector.SelectedColor = currentColor ?? Color.White;
			selector.BorderSize = 0;
			ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => selector.SelectedColor = slot.GetColor();
			ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => selector.SelectedColor = slot.GetColor();


			return selector;
		}

		public static DropdownSelector<T> AddComboBox<T>(Panel prop, T? value, IEnumerable<T> options, Func<T?, string> tostring, Action<T> change) {
			var panel = AddInternalPropPanel(prop);

			var selector = panel.Add<DropdownSelector<T>>();
			selector.Items.AddRange(options);
			selector.Selected = value;
			selector.Dock = Dock.Left;
			selector.Size = new(96);
			selector.Selected = value;
			selector.BorderSize = 0;
			selector.OnToString += (t) => tostring(t);
			selector.OnSelectionChanged += (self, o, n) => change(n);

			return selector;
		}
		public static DropdownSelector<T> AddEnumComboBox<T>(Panel prop, T? value) where T : Enum {
			var panel = AddInternalPropPanel(prop);

			var selector = panel.Add(DropdownSelector<T>.FromEnum<T>(value ?? default(T)));
			selector.Dock = Dock.Left;
			selector.Size = new(96);
			selector.Selected = value;
			selector.BorderSize = 0;

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
		public static Button ButtonIcon(Panel buttons, string text, string? icon = null, Action<Element, FrameState, MouseButton>? onClicked = null) {
			var newBtn = buttons.Add<Button>();
			newBtn.Text = text;
			newBtn.AutoSize = true;
			if (icon != null) {
				var img = newBtn.Add<Panel>();
				img.OnHoverTest += Element.Passthru;
				img.DrawPanelBackground = false;
				img.ShouldDrawImage = true;
				img.Size = new(32);
				img.ImageOrientation = ImageOrientation.Zoom;
				img.Dock = Dock.Left;
				img.DockMargin = RectangleF.TLRB(2);
				img.Image = buttons.Level.Textures.LoadTextureFromFile(icon);

				newBtn.TextPadding = new(34, 0);
				newBtn.TextAlignment = Anchor.CenterLeft;
			}

			newBtn.MouseReleaseEvent += (e, fs, mb) => onClicked?.Invoke(e, fs, mb);
			return newBtn;
		}
		public static void OperatorButton<T>(Panel buttons, string text, string? icon = null) where T : Operator, new() {
			var btn = ButtonIcon(buttons, text, icon, (el, fs, mb) => {
				var btn = ((el as Button) ?? throw new Exception("never should happen im lazy"));
				Operator? ourOperator = el.GetTagSafely<Operator>("op");
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
			var button = ButtonIcon(buttons, "New...", "models/add.png", (_, fs, _) => {
				Menu menu = buttons.UI.Menu();

				foreach (var action in actions) {
					menu.AddButton(action.Text, null, () => {
						action.OnClicked?.Invoke();
					});
				}

				menu.Open(fs.MouseState.MousePos);
			});
			button.Thinking += (_) => {
				button.InputDisabled = ModelEditor.Active.AnimationMode;
			};
		}
		public static void NewSlotDialog(EditorFile file, EditorBone bone) {
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
		public static void NewClippingDialog(EditorFile file, EditorSlot slot) {
			EditorDialogs.TextInput(
				"New Clipping",
				"Enter the name for the new slot.",
				"",
				true,
				(name) => {
					var result = file.AddAttachment<EditorClippingAttachment>(slot, name);
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
					var result = file.AddSkin(model, name);
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
					var result = file.AddAnimation(anims.Model, name);
					if (result.Failed)
						EditorDialogs.ConfirmAction("Animation creation error", result.Reason, true, () => NewAnimationDialog(file, anims));
				}, null
			);
		}



		public static Button NewTopOperatorButton(Panel props, string icon) {
			var btn = props.Add<Button>();
			btn.Dock = Dock.Right;
			btn.DockMargin = RectangleF.TLRB(8, 0, 0, 8);
			btn.Size = new(32);
			btn.Text = "";
			btn.BorderSize = 0;
			btn.ImageOrientation = ImageOrientation.Centered;
			btn.Image = props.Level.Textures.LoadTextureFromFile(icon);
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
			top.DockMargin = RectangleF.TLRB(4);

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
