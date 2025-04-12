using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlendMode = Nucleus.Models.BlendMode;

namespace Nucleus.ModelEditor
{
	public class EditorSlot : IEditorType
	{
		public EditorModel GetModel() => Bone.Model;
		public EditorBone Bone { get; set; }
		public string Name { get; set; }

		public Color SetupColor { get; set; } = Color.White;
		public bool TintBlack { get; set; } = false;
		public Color SetupDarkColor { get; set; } = Color.Black;
		public BlendMode SetupBlending { get; set; } = BlendMode.Normal;
		public EditorAttachment? SetupActiveAttachment { get; set; } = null;

		public List<EditorAttachment> Attachments { get; set; } = [];
		public Color Color { get; set; } = Color.White;
		public Color DarkColor { get; set; } = Color.Black;
		public BlendMode Blending { get; set; } = BlendMode.Normal;
		public EditorAttachment? ActiveAttachment { get; set; } = null;

		public EditorAttachment? GetActiveAttachment() 
			=> AnimationMode ? ActiveAttachment : SetupActiveAttachment;

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

		public EditorAttachment? FindAttachment(string name) => Attachments.FirstOrDefault(x => x.Name == name);
		public bool TryFindAttachment(string name, out EditorAttachment? attachment) {
			attachment = FindAttachment(name);
			return attachment != null;
		}

		public void ResetToSetupPose() {
			Color = SetupColor;
			DarkColor = SetupDarkColor;
			Blending = SetupBlending;

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
			var slotColorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");

			var slotColorSelector = PropertiesPanel.AddColorSelector(slotColorRow, AnimationMode ? Color : SetupColor);
			var slotDarkColorSelector = PropertiesPanel.AddColorSelector(slotColorRow, AnimationMode ? DarkColor : SetupDarkColor);
			var slotTintCheck = PropertiesPanel.AddLabeledCheckbox(slotColorRow, "Tint black?", TintBlack);

			slotDarkColorSelector.Parent.EngineDisabled = !TintBlack;

			slotColorSelector.ColorChanged += (_, c) => {
				if(AnimationMode) Color = c;
				else SetupColor = c;
			};

			slotDarkColorSelector.ColorChanged += (_, c) => {
				if (AnimationMode) DarkColor = c;
				else SetupDarkColor = c;
			};

			slotTintCheck.OnCheckedChanged += (s) => {
				TintBlack = s.Checked;
				slotDarkColorSelector.Parent.Enabled = TintBlack;
				slotColorRow.InvalidateLayout();
			};

			var slotBlendRow = PropertiesPanel.NewRow(props, "Blending", "models/blending.png");
			var blending = PropertiesPanel.AddEnumComboBox(slotBlendRow, AnimationMode ? Blending : SetupBlending);

			blending.OnSelectionChanged += (_, _, v) => {
				if (AnimationMode) Blending = v;
				else SetupBlending = v;
			};
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, []);
			PropertiesPanel.ButtonIcon(buttons, "Set Parent", "models/setparent.png");
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
