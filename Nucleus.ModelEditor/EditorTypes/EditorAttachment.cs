using Newtonsoft.Json;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public abstract class EditorAttachment : IEditorType
	{
		public string Name { get; set; }
		public EditorSlot Slot { get; set; }

		public string GetName() => Name;

		public abstract string EditorIcon { get; }

		[JsonIgnore] public abstract string SingleName { get; }
		[JsonIgnore] public abstract string PluralName { get; }

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore] public bool Hidden { get; set; }

		public virtual void BuildTopOperators(Panel props, PreUIDeterminations determinations) { }
		public virtual void BuildProperties(Panel props, PreUIDeterminations determinations) { }
		public virtual void BuildOperators(Panel buttons, PreUIDeterminations determinations) { }
		public virtual string? DetermineHeaderText(PreUIDeterminations determinations) => $"{((IEditorType)this).CapitalizedSingleName} '{Name}'";

		public virtual bool CanTranslate() => false;
		public virtual bool CanRotate() => false;
		public virtual bool CanScale() => false;
		public virtual bool CanShear() => false;
		public virtual bool CanHide() => false;

		public virtual bool GetVisible() => !Hidden;

		public virtual float GetTranslationX() => 0f;
		public virtual float GetTranslationY() => 0f;
		public virtual float GetRotation() => 0f;
		public virtual float GetScaleX() => 0f;
		public virtual float GetScaleY() => 0f;
		public virtual float GetShearX() => 0f;
		public virtual float GetShearY() => 0f;

		public virtual void EditTranslationX(float value) { }
		public virtual void EditTranslationY(float value) { }
		public virtual void EditRotation(float value) { }
		public virtual void EditScaleX(float value) { }
		public virtual void EditScaleY(float value) { }
		public virtual void EditShearX(float value) { }
		public virtual void EditShearY(float value) { }

		public virtual void OnMouseEntered() { }
		public virtual void OnMouseLeft() { }
		public virtual void OnSelected() { }
		public virtual void OnUnselected() { }
		public virtual void OnHidden() { }
		public virtual void OnShown() { }

		public virtual bool CanRename() => false;
		public virtual bool CanDelete() => false;

		public virtual void Render() {

		}
	}
}
