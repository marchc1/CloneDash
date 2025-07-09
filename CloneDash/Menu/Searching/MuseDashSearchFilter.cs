using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;

namespace CloneDash.Menu.Searching;

public class MuseDashSearchFilter : SearchFilter
{
	public string FilterText;

	public override void Populate(SongSearchDialog dialog) {
		TextInput(dialog, nameof(FilterText), "Filter by name, song author, etc..", true, true);
	}

	public override Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog) {
		dialog.SetBarText(FilterText);
		return x =>
			x is MuseDashSong mds && (
				FilterText == null ? true :
				mds.Name.ToLower().Contains(FilterText.ToLower()) ||
				mds.BaseName.ToLower().Contains(FilterText.ToLower()) ||
				mds.Author.ToLower().Contains(FilterText.ToLower())
			);
	}
}
