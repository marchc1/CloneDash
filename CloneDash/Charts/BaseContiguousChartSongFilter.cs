using CloneDash.Data;
using CloneDash.Menu.Searching;

namespace CloneDash.Charts;

public class BaseContiguousChartSongFilter(BaseContiguousChartSongFilter? parent) : IChartSongFilter
{
	public string? Query = parent?.Query ?? null;
	public bool UseMin = true;
	public bool UseMax = true;
	public int MinDifficulty = 1;
	public int MaxDifficulty = 13;

	public virtual bool NameTest(ChartSong song) {
		if (Query != null) {
			if (song.Name.Contains(Query, StringComparison.InvariantCultureIgnoreCase)) return true;
			if (song.Author.Contains(Query, StringComparison.InvariantCultureIgnoreCase)) return true;

			return false;
		}

		return true;
	}
	public virtual bool Test(ChartSong song) {
		if (!NameTest(song))
			return false;

		// todo: standardize these numbers
		UseMin = MinDifficulty != 1;
		UseMax = MaxDifficulty != 13;

		if (MinDifficulty > MaxDifficulty)
			(MinDifficulty, MaxDifficulty) = (MaxDifficulty, MinDifficulty);

		if (UseMin || UseMax)
			for (int i = 0; i < 5; i++) {
				if (song.TryDifficultyInteger(i + 1, out int d)) {
					bool inRange = true;

					if (d >= MinDifficulty && d <= MaxDifficulty)
						return true;
				}
			}
		else
			return true;

		return false;
	}

	public IReadOnlyList<ChartSong> Apply(IReadOnlyList<ChartSong> songs) {
		List<ChartSong> newSongs = new List<ChartSong>(songs.Count);
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