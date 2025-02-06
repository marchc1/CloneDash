using Newtonsoft.Json;
using Nucleus.UI;
using Nucleus.Util;

namespace Nucleus.ModelEditor
{
	public interface IEditorType
	{
		string GetName();
		bool IsNameTaken(string name);

		EditorResult Rename(string newName);
		EditorResult Remove();

		bool CanRename() => true;
		bool CanDelete();

		[JsonIgnore] string SingleName { get; }
		[JsonIgnore] string PluralName { get; }

		[JsonIgnore] string CapitalizedSingleName => SingleName.CapitalizeFirstCharacter();
		[JsonIgnore] string CapitalizedPluralName => PluralName.CapitalizeFirstCharacter();

		[JsonIgnore] ViewportSelectMode SelectMode { get; }
		bool HoverTest();

		void BuildTopOperators(Panel props, PreUIDeterminations determinations);
		void BuildProperties(Panel props, PreUIDeterminations determinations);
		void BuildOperators(Panel buttons, PreUIDeterminations determinations);
		string? DetermineHeaderText(PreUIDeterminations determinations) => null;
	}
}
