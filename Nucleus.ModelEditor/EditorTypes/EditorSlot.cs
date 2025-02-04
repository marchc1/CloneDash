using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.Models;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorSlot : IEditorType
	{
		[JsonIgnore] public EditorBone Bone { get; set; }
		public string Name { get; set; }
		public List<EditorAttachment> Attachments { get; } = [];

		public Color Color { get; set; } = Color.WHITE;
		public bool TintBlack { get; set; } = false;
		public Color DarkColor { get; set; } = Color.BLACK;
		public BlendMode Blending { get; set; } = BlendMode.Normal;

		public EditorAttachment? FindAttachment(string name) {
			return Attachments.FirstOrDefault(x => x.Name == name);
		}
		public bool TryFindAttachment(string name, [NotNullWhen(true)] out EditorAttachment? attachment) {
			attachment = FindAttachment(name);
			return attachment != null;
		}

		public string SingleName => "slot";
		public string PluralName => "slots";
		public ViewportSelectMode SelectMode => ViewportSelectMode.NotApplicable;
		public bool CanDelete() => true;
		public bool HoverTest() => false;

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}

		public void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var slotColorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");

			var slotColorSelector = PropertiesPanel.AddColorSelector(slotColorRow, Color);
			var slotDarkColorSelector = PropertiesPanel.AddColorSelector(slotColorRow, DarkColor);
			var slotTintCheck = PropertiesPanel.AddLabeledCheckbox(slotColorRow, "Tint black?", TintBlack);

			slotDarkColorSelector.Parent.EngineDisabled = !TintBlack;

			slotColorSelector.ColorChanged += (_, c) => {
				Color = c;
			};

			slotDarkColorSelector.ColorChanged += (_, c) => {
				DarkColor = c;
			};

			slotTintCheck.OnCheckedChanged += (s) => {
				TintBlack = s.Checked;
				slotDarkColorSelector.Parent.Enabled = TintBlack;
				slotColorRow.InvalidateLayout();
			};

			var slotBlendRow = PropertiesPanel.NewRow(props, "Blending", "models/blending.png");
			var blending = PropertiesPanel.AddEnumComboBox<BlendMode>(slotBlendRow, Blending);
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			throw new NotImplementedException();
		}

		public string? GetName() => Name;
		public bool IsNameTaken(string name) => Bone.Model.FindSlot(name) != null;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameSlot(this, newName);
		public EditorResult Remove() => ModelEditor.Active.File.RemoveSlot(this);
	}
}
