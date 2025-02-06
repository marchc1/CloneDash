using Newtonsoft.Json;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// The core editor type definition. Defines how the type behaves with various aspects of the editor.
	/// </summary>
	public interface IEditorType
	{
		string GetName() => "";
		bool IsNameTaken(string name) => false;

		EditorResult Rename(string newName) => EditorResult.NotApplicable;
		EditorResult Remove() => EditorResult.NotApplicable;

		bool CanRename() => true;
		bool CanDelete() => false;

		[JsonIgnore] string SingleName { get; }
		[JsonIgnore] string PluralName { get; }

		[JsonIgnore] string CapitalizedSingleName => SingleName.CapitalizeFirstCharacter();
		[JsonIgnore] string CapitalizedPluralName => PluralName.CapitalizeFirstCharacter();

		[JsonIgnore] ViewportSelectMode SelectMode => ViewportSelectMode.NotApplicable;
		bool HoverTest(Vector2F gridPos) => false;

		void BuildTopOperators(Panel props, PreUIDeterminations determinations) { }
		void BuildProperties(Panel props, PreUIDeterminations determinations) { }
		void BuildOperators(Panel buttons, PreUIDeterminations determinations) { }
		string? DetermineHeaderText(PreUIDeterminations determinations) => null;

		public bool CanTranslate() => false;
		public bool CanRotate() => false;
		public bool CanScale() => false;
		public bool CanShear() => false;
		public bool CanHide() => false;

		float GetTranslationX() => 0f;
		float GetTranslationY() => 0f;
		float GetRotation() => 0f;
		float GetScaleX() => 0f;
		float GetScaleY() => 0f;
		float GetShearX() => 0f;
		float GetShearY() => 0f;

		void EditTranslationX(float value) { }
		void EditTranslationY(float value) { }
		void EditRotation(float value) { }
		void EditScaleX(float value) { }
		void EditScaleY(float value) { }
		void EditShearX(float value) { }
		void EditShearY(float value) { }

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		public bool Visible { get; set; }

		void OnMouseEntered() { }
		void OnMouseLeft() { }
		void OnSelected() { }
		void OnUnselected() { }
		void OnHidden() { }
		void OnShown() { }
	}
}
