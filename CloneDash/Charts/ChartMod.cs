using CloneDash.Characters;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
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
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IChartSongProvider>();
		foreach (var retriever in providers)
			foreach (var characterName in retriever.GetAvailable())
				yield return characterName;
	}

	public static ISong? GetSongByName(ReadOnlySpan<char> name = default) {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IChartSongProvider>();
		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		foreach (var retriever in providers) {
			ISong? song = retriever.FindByName(name);
			if (song == null) continue;

			return song;
		}

		return null;
	}

	public static IChartSongProvider? GetChartSongProviderByName(ReadOnlySpan<char> name = default) {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IChartSongProvider>();
		if (name.IsEmpty || name.IsWhiteSpace())
			return null;

		foreach (var retriever in providers) {
			if (retriever.GetName().Equals(name, StringComparison.InvariantCultureIgnoreCase))
				return retriever;
		}

		return null;
	}
}