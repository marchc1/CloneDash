using Newtonsoft.Json;
using Nucleus.Common.Types;
using Nucleus.Engine;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlendMode = Nucleus.Models.BlendMode;

namespace Nucleus.ModelEditor
{
	public class EditorSlot : IEditorType, IModel4Nameable
	{
		public EditorModel GetModel() => Bone.Model;
		public EditorBone Bone { get; set; }
		public string Name { get; set; }

		public Color SetupColor { get; set; } = Color.White;
		public bool TintBlack { get; set; } = false;
		public Color SetupDarkColor { get; set; } = Color.Black;
		public EditorAttachment? SetupActiveAttachment { get; set; } = null;

		public List<EditorAttachment> Attachments { get; set; } = [];
		public Color Color { get; set; } = Color.White;
		public Color DarkColor { get; set; } = Color.Black;
		public BlendMode Blending { get; set; } = BlendMode.Normal;
		public EditorAttachment? ActiveAttachment { get; set; } = null;

		public Color GetColor() => AnimationMode ? Color : SetupColor;
		public Color GetDarkColor() => AnimationMode ? DarkColor : SetupDarkColor;

		public EditorAttachment? GetActiveAttachment() => Resolve(AnimationMode ? ActiveAttachment : SetupActiveAttachment);

		/// <summary>
		/// What an attachment reference shows as under the model's active skin
		/// </summary>
		public EditorAttachment? Resolve(EditorAttachment? attachment) {
			if (attachment == null) return null;
			var skin = Bone.Model.ActiveSkin;
			var key = attachment.SkinKey;

			// active skin first, then the default skin
			if (skin != null) {
				if (attachment.Skin == skin && Attachments.Contains(attachment)) return attachment;
				foreach (var other in Attachments) {
					if (other.Skin == skin && other.SkinKey == key)
						return other;
				}
			}
			if (attachment.Skin == null) return Attachments.Contains(attachment) ? attachment : null;
			foreach (var other in Attachments) {
				if (other.Skin == null && other.SkinKey == key)
					return other;
			}
			return null;
		}

		public void SetActiveAttachment(EditorAttachment? attachment) {
			if (attachment != null && !Attachments.Contains(attachment))
				throw new Exception();

			if (AnimationMode) {
				ActiveAttachment = attachment;
			}
			else {
				SetupActiveAttachment = attachment;
			}
		}

		public EditorAttachment? FindAttachment(string name) => Attachments.FirstOrDefault(x => x.Name == name && x.Skin == null)
				?? Attachments.FirstOrDefault(x => x.Name == name);
		/// <summary>
		/// Names only have to be unique within a skin.
		/// </summary>
		public bool IsAttachmentNameTaken(string name, EditorSkin? skin) => Attachments.Any(x => x.Name == name && x.Skin == skin);
		public bool TryFindAttachment(string name, out EditorAttachment? attachment) {
			attachment = FindAttachment(name);
			return attachment != null;
		}

		public void ResetToSetupPose() {
			Color = SetupColor;
			DarkColor = SetupDarkColor;

			// active attachment can be null
			//if (SetupActiveAttachment == null)
				//SetupActiveAttachment = Attachments.FirstOrDefault();

			ActiveAttachment = SetupActiveAttachment;
		}

		public string SingleName => "slot";
		public string PluralName => "slots";
		public ViewportSelectMode SelectMode => ViewportSelectMode.NotApplicable;
		public bool CanDelete() => true;
		public bool HoverTest(Vector2F gridPos) => false;

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}

		public IEditorType? DeferTransformationsTo() => Bone;

		private bool AnimationMode => ModelEditor.Active.AnimationMode;

		public void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var file = ModelEditor.Active.File;
			bool animating = AnimationMode;
			bool separated = animating && file.ActiveAnimation?.SeparatedColors.Contains(this) == true;

			var slotColorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");

			// separated, the left button keys alpha and the right one keys color
			List<ColorSelector> colorSelectors = separated ? [
					PropertiesPanel.AddColorSelector(slotColorRow, this, KeyframeProperty.Slot_Color, 1, Color),
					PropertiesPanel.AddColorSelector(slotColorRow, this, KeyframeProperty.Slot_Color, 0, Color)
				] : [
					PropertiesPanel.AddColorSelector(slotColorRow, this, KeyframeProperty.Slot_Color, -1, animating ? Color : SetupColor)
				];
			if (separated) {
				colorSelectors[0].Swatch = ColorSwatch.WithAlpha;
				colorSelectors[1].Swatch = ColorSwatch.Opaque;
			}
			var slotDarkColorSelector = PropertiesPanel.AddColorSelector(slotColorRow, AnimationMode ? DarkColor : SetupDarkColor);
			var slotTintCheck = PropertiesPanel.AddLabeledCheckbox(slotColorRow, "Tint black?", TintBlack);

			slotDarkColorSelector.GetParent()!.SetVisible(TintBlack);

			foreach (var selector in colorSelectors) {
				selector.SelectedColor = GetColor();
				selector.OnColorChanged += (self, ref newColor) => {
					var before = animating ? Color : SetupColor;
					file.SetProperty(this, "color", newColor,
						() => animating ? Color : SetupColor,
						value => {
							if (!animating)
								SetupColor = value;
							Color = value;
						});
					if (animating)
						file.SlotColorEdited(this, before, newColor);
					foreach (var other in colorSelectors) {
						if (other != self)
							other.SetColorNoUpdate(newColor);
					}
				};
			}

			slotDarkColorSelector.SelectedColor = GetDarkColor();
			slotDarkColorSelector.OnColorChanged += (_, ref newColor) => file.SetProperty(this, "dark color", newColor,
				() => animating ? DarkColor : SetupDarkColor,
				value => {
					if (!animating)
						SetupDarkColor = value;
					DarkColor = value;
				});

			slotTintCheck.OnCheckedChanged += (s) => {
				file.SetProperty(this, "tint black", s.Checked, () => TintBlack, v => TintBlack = v);
				slotDarkColorSelector.GetParent()!.SetVisible(TintBlack);
				slotColorRow.InvalidateLayout();
			};

			if (animating) {
				var separateRow = PropertiesPanel.NewRow(props, "Separate", "models/separate.png");
				var alpha = PropertiesPanel.AddLabeledCheckbox(separateRow, "Alpha", separated);
				alpha.OnCheckedChanged += (s) => file.SetSlotColorSeparated(this, s.Checked);
			}

			var slotBlendRow = PropertiesPanel.NewRow(props, "Blending", "models/blending.png");
			var blending = PropertiesPanel.AddEnumComboBox(slotBlendRow, Blending);
			// think hook needed; todo
			blending.OnSelectionChanged += (_, _, v) => {
				if (!AnimationMode)
					file.SetProperty(this, "blending", v, () => Blending, b => Blending = b);
			};
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
				new("Clipping", () => PropertiesPanel.NewClippingDialog(ModelEditor.Active.File, this))
				]);
			PropertiesPanel.OperatorButton<UI.Operators.SetParentOperator>(buttons, "Set Parent",
				"models/setparent.png");
		}

		public string? GetName() => Name;
		public bool IsNameTaken(string name) => Bone.Model.FindSlot(name) != null;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameSlot(this, newName);
		public EditorResult Remove() => ModelEditor.Active.File.RemoveSlot(this);

		[JsonIgnore] public bool Hovered { get; set; } = false;
		[JsonIgnore] public bool Selected { get; set; } = false;

		public bool Hidden { get; set; }

		public bool CanHide() => true;

		public bool CanKeyframe() => true;
		public bool GetKeyframeParameters([NotNullWhen(true)] out IEditorType? target, [NotNullWhen(true)] out KeyframeProperty property, [NotNullWhen(true)] out int arrayIndex) {
			arrayIndex = -1;
			property = KeyframeProperty.Slot_Attachment;
			target = this;
			return CanKeyframe();
		}
	}
}
