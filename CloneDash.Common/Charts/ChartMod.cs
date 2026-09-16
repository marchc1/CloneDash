using CloneDash.Characters;
using CloneDash.Common.Songs;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;

namespace CloneDash.Charts;

[MarkForStaticConstruction]
public static class ChartMod
{
	static IChartSongProvider[]? providers;

	public static IEnumerable<string> GetAvailableChartSongs() {
		foreach (var retriever in GetAll())
			foreach (var songName in retriever.GetAvailable())
				yield return songName;
	}

	public static ISong? GetSongByName(ReadOnlySpan<char> name = default) {
		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		foreach (var retriever in GetAll()){ 
			ISong? song = retriever.FindByName(name);
			if (song == null) continue;

			return song;
		}

		return null;
	}

	public static IChartSongProvider? GetChartSongProviderByName(ReadOnlySpan<char> name = default) {
		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		foreach (var retriever in GetAll()){ 
			if (retriever.GetName().Equals(name, StringComparison.InvariantCultureIgnoreCase))
				return retriever;
		}

		return null;
	}

	public static ReadOnlySpan<IChartSongProvider> GetAll() {
		if (providers == null) {
			providers = ReflectionTools.InstantiateAllInheritorsOfInterface<IChartSongProvider>();
			providers.Sort(static (a, b) => a.GetSortIndex().CompareTo(b.GetSortIndex()));
		}

		return providers;
	}
}