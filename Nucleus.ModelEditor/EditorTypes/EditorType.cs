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

		IEditorType? GetTransformParent() => null;

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

		public bool GetVisible() => !Hidden;

		float GetTranslationX(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		float GetTranslationY(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		float GetRotation(UserTransformMode transform = UserTransformMode.LocalSpace) => 0f;
		float GetScaleX() => 0f;
		float GetScaleY() => 0f;
		float GetShearX() => 0f;
		float GetShearY() => 0f;

		void EditTranslationX(float value, UserTransformMode transform = UserTransformMode.LocalSpace) { }
		void EditTranslationY(float value, UserTransformMode transform = UserTransformMode.LocalSpace) { }
		void EditRotation(float value, bool localTo = true) { }
		void EditScaleX(float value) { }
		void EditScaleY(float value) { }
		void EditShearX(float value) { }
		void EditShearY(float value) { }

		Vector2F GetWorldPosition() => Vector2F.Zero;
		void SetWorldPosition(Vector2F pos, bool additive = false) { }
		float GetWorldRotation() => 0;
		float GetScreenRotation() => 0;

		/// <summary>
		/// Overriden if the element doesn't really support transformation; but is the parent of something that does
		/// </summary>
		/// <returns></returns>
		IEditorType? DeferTransformationsTo() => this;
		/// <summary>
		/// Overriden if the element doesn't really have any properties, but is the parent of something that does. <br/>
		/// todo: null behavior?
		/// </summary>
		/// <returns></returns>
		IEditorType? DeferPropertiesTo() => this;


		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		public bool Hidden { get; set; }

		void OnMouseEntered() { }
		void OnMouseLeft() { }
		/// <summary>
		/// Override and return false if you wish to block selection. Generally respected.
		/// </summary>
		/// <returns></returns>
		bool OnSelected() { return true; }
		/// <summary>
		/// Override and return false if you wish to block unselection. Only respects this wish in certain places (ie. pressing the ESCAPE key)
		/// </summary>
		/// <returns></returns>
		bool OnUnselected() => true;
		void OnHidden() { }
		void OnShown() { }
	}
}
