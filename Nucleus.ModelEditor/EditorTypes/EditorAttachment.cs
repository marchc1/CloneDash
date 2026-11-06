using Newtonsoft.Json;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public abstract class EditorAttachment : IEditorType
	{
		public EditorModel GetModel() => Slot.Bone.Model;
		public IEditorType? GetTransformParent() => Hidden ? Slot.Bone : this;
		public IEditorType? DeferTransformationsTo() => GetTransformParent();
		public string Name { get; set; }
		public EditorSlot Slot { get; set; }

		public EditorSkin? Skin { get; set; }
		/// <summary>
		/// Skin placeholder name, null means the attachment's own name
		/// </summary>
		public string? Placeholder { get; set; }
		/// <summary>
		/// The name the slot and animations look this attachment up by
		/// </summary>
		[JsonIgnore] public string SkinKey => Placeholder ?? Name;

		public virtual bool CanDelete() => true;
		public EditorResult Remove() => ModelEditor.Active.File.RemoveAttachment(this);

		/// <summary>
		/// Select checkbox, off means only the tree can select it
		/// </summary>
		public bool ViewportCanSelect { get; set; } = true;
		/// <summary>
		/// Export checkbox, off leaves it and its keys out of the export
		/// </summary>
		public bool Export { get; set; } = true;
		/// <summary>
		/// Name checkbox, draws the name even with names turned off
		/// </summary>
		public bool ViewportShowName { get; set; }

		[JsonIgnore] public virtual ViewportSelectMode SelectMode => ViewportSelectMode.NotApplicable;

		public string GetName() => Name;

		public abstract string EditorIcon { get; }

		public virtual bool HoverTest(Vector2F gridPos) => false;
		public virtual bool HoverTestOpacity(Vector2F gridPos) => true;

		[JsonIgnore] public abstract string SingleName { get; }
		[JsonIgnore] public abstract string PluralName { get; }

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore]
		public bool Hidden {
			get => Slot.GetActiveAttachment() != this;
			set { }
		}

		public virtual void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}
		public virtual void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var row = PropertiesPanel.NewRow(props, "Skin", "models/skin.png");
			List<EditorSkin?> skins = [null, .. GetModel().Skins];
			PropertiesPanel.AddComboBox(row, Skin, skins,
				(s) => s?.Name ?? "(default)",
				(s) => ModelEditor.Active.File.SetAttachmentSkin(this, s));

			var file = ModelEditor.Active.File;
			var flags = PropertiesPanel.NewRow(props, "Viewport", "models/info.png");
			var select = PropertiesPanel.AddLabeledCheckbox(flags, "Select", ViewportCanSelect);
			var export = PropertiesPanel.AddLabeledCheckbox(flags, "Export", Export);
			var name = PropertiesPanel.AddLabeledCheckbox(flags, "Name", ViewportShowName);
			select.OnCheckedChanged += (s)
				=> file.SetProperty(this, "select", s.Checked, () => ViewportCanSelect, v => ViewportCanSelect = v);
			export.OnCheckedChanged += (s)
				=> file.SetProperty(this, "export", s.Checked, () => Export, v => Export = v);
			name.OnCheckedChanged += (s)
				=> file.SetProperty(this, "show name", s.Checked, () => ViewportShowName, v => ViewportShowName = v);
		}
		protected void AddColorRow(Panel props, Func<Nucleus.Common.Types.Color> get, Action<Nucleus.Common.Types.Color> set) {
			var row = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");
			var selector = PropertiesPanel.AddColorSelector(row, get());
			selector.OnColorChanged += (_, ref newColor)
				=> ModelEditor.Active.File.SetProperty(this, "color", newColor, get, set);
			if (ModelEditor.Active.AnimationMode)
				selector.SetMouseInputEnabled(false);
		}

		public virtual void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<UI.Operators.SetParentOperator>(buttons, "Set Parent",
				"models/setparent.png");
		}
		public virtual string? DetermineHeaderText(PreUIDeterminations determinations) => $"{((IEditorType)this).CapitalizedSingleName} '{Name}'";

		public virtual bool CanTranslate() => false;
		public virtual bool CanRotate() => false;
		public virtual bool CanScale() => false;
		public virtual bool CanShear() => false;
		public virtual bool CanHide() => false;

		public virtual bool GetVisible() => !Hidden;

		public virtual float GetTranslationX(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		public virtual float GetTranslationY(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		public virtual float GetRotation(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		public virtual float GetScaleX() => 0f;
		public virtual float GetScaleY() => 0f;
		public virtual float GetShearX() => 0f;
		public virtual float GetShearY() => 0f;

		public virtual void EditTranslationX(float value, UserTransformMode transform = UserTransformMode.LocalSpace) { }
		public virtual void EditTranslationY(float value, UserTransformMode transform = UserTransformMode.LocalSpace) { }
		public virtual void EditRotation(float value, bool localTo = true) { }
		public virtual void EditScaleX(float value) { }
		public virtual void EditScaleY(float value) { }
		public virtual void EditShearX(float value) { }
		public virtual void EditShearY(float value) { }

		public virtual Vector2F GetWorldPosition() => Vector2F.Zero;
		public virtual void SetWorldPosition(Vector2F pos, bool additive = false) { }
		public virtual float GetWorldRotation() => 0;
		public virtual float GetScreenRotation() => 0;

		public virtual void OnMouseEntered() { }
		public virtual void OnMouseLeft() { }
		public virtual bool OnSelected() => true;
		public virtual bool OnUnselected() => true;
		public virtual void OnHidden() {
			Slot.SetActiveAttachment(null);
			// Force selection (pull out of any operators etc)
			ModelEditor.Active.File.DeactivateOperator(true);
			//ModelEditor.Active.SelectObject(this);
		}
		public virtual void OnShown() {
			Slot.SetActiveAttachment(this);
			ModelEditor.Active.File.DeactivateOperator(true);
			ModelEditor.Active.SelectObject(this);
		}

		public virtual bool CanRename() => true;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameAttachment(this, newName);
		public virtual void Render() { }
		public virtual void RenderOverlay() { }
	}
}
