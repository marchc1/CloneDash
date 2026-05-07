using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Menu.Searching;

namespace CloneDash.Charts;

public class BaseContiguousChartSongFilter(BaseContiguousChartSongFilter? parent) : IChartSongFilter
{
	public string? Query = parent?.Query ?? null;
	public bool UseMin = true;
	public bool UseMax = true;
	public int MinDifficulty = 1;
	public int MaxDifficulty = 13;

	public virtual bool NameTest(ISong song) {
		if (Query != null) {
			var metadata = song.FetchMetadata(HumanLanguage.GetCurrentLanguage());
			if (metadata.Name.Contains(Query, StringComparison.InvariantCultureIgnoreCase)) return true;
			if (metadata.Author.Contains(Query, StringComparison.InvariantCultureIgnoreCase)) return true;

			return false;
		}

		return true;
	}
	public virtual bool Test(ISong song) {
		if (!NameTest(song))
			return false;

		// todo: standardize these numbers
		UseMin = MinDifficulty != 1;
		UseMax = MaxDifficulty != 13;

		if (MinDifficulty > MaxDifficulty)
			(MinDifficulty, MaxDifficulty) = (MaxDifficulty, MinDifficulty);

		if (UseMin || UseMax) {
			if (song is not IHasLowToHighDifficulties idiff)
				return false;

			Span<int> difficulties = stackalloc int[idiff.GetDifficultyCount()];
			idiff.GetDifficulties(difficulties);
			for (int i = 0; i < difficulties.Length; i++) {
				int d = difficulties[i];
				if (d >= MinDifficulty && d <= MaxDifficulty)
					return true;
			}
		}
		else
			return true;

		return false;
	}

	public IReadOnlyList<ISong> Apply(IReadOnlyList<ISong> songs) {
		List<ISong> newSongs = new List<ISong>(songs.Count);
		for (int i = 0; i < songs.Count; i++) {
			var song = songs[i];
			if (Test(song))
				newSongs.Add(song);
		}
		return newSongs;
	}

	public virtual void PopulateFields(SongSearchDialog dialog) {
		var q = dialog.TextboxInput(nameof(Query), "Search Query", Query);
		q.DemandKeyboardFocus();
		q.Caret.MovePosition(Query ?? "", Query?.Length ?? 0);
		dialog.NumberCarouselInput(nameof(MinDifficulty), "Min. Difficulty", MinDifficulty, 1, 13);
		dialog.NumberCarouselInput(nameof(MaxDifficulty), "Max. Difficulty", MaxDifficulty, 1, 13);
	}
}