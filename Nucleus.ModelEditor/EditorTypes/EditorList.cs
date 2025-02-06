using Newtonsoft.Json;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// A wrapper around List&lt;<typeparamref name="T"/>&gt; that implements IEditorType so it can be used in various UI components.
	/// <br></br>
	/// The only downside is you must specify a class that implements this abstract class and specifies single/plural names.
	/// <br></br>
	/// <br></br>
	/// As an example:
	/// <br></br>
	/// <code>public class SkinsList() : EditorList&lt;EditorSkin&gt;("skins", "skins");</code>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="singleName"></param>
	/// <param name="pluralName"></param>
	public abstract class EditorList<T>(string singleName, string pluralName) : List<T>, IEditorType
	{
		public EditorModel Model { get; set; }
		[JsonIgnore] public string SingleName => singleName;
		[JsonIgnore] public string PluralName => pluralName;

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore] public bool Hidden { get; set; }

		public virtual void BuildTopOperators(Panel props, PreUIDeterminations determinations) { }
		public virtual void BuildProperties(Panel props, PreUIDeterminations determinations) { }
		public virtual void BuildOperators(Panel buttons, PreUIDeterminations determinations) { }
		public virtual string? DetermineHeaderText(PreUIDeterminations determinations) => ((IEditorType)this).CapitalizedPluralName;

		public virtual void OnMouseEntered() { }
		public virtual void OnMouseLeft() { }
		public virtual void OnSelected() { }
		public virtual void OnUnselected() { }
		public virtual void OnHidden() { }
		public virtual void OnShown() { }

		public virtual bool CanRename() => false;
		public virtual bool CanDelete() => false;
		public virtual bool CanHide() => false;
	}
}
