using CloneDash.Common.Compatibility.Valve;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;

using Nucleus;
using Nucleus.Common;
using Nucleus.Common.Localization;
using Nucleus.Core;
using Nucleus.Files;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.MuseDash;

public record MuseDashAsset(string nicename, string filename);
public static partial class MuseDash1Compatibility
{
	public static bool Initialized { get; private set; } = false;

	public static AppStatus LightInitialize() {
#if COMPILED_WINDOWS
		return INIT_WINDOWS();
#elif COMPILED_OSX
            return INIT_OSX();
#elif COMPILED_LINUX
            return INIT_LINUX();
#else
		return AppStatus.OperatingSystemNotCompatible;
#endif
	}

	static T? ReadJSON<T>(ReadOnlySpan<char> path) => JsonSerializer.Deserialize<T>(StreamingAssets.ReadText(path)!, new JsonSerializerOptions {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		IncludeFields = true
	});

	static T? ReadOptionalJSON<T>(string path) {
		try {
			return StreamingAssets.ReadText(path) == null ? default : ReadJSON<T>(path);
		}
		catch (JsonException ex) {
			Logs.Warn($"Couldn't read {path}: {ex.Message}");
			return default;
		}
	}

	static void AddElfinLocalization(ElfinConfigData elfin, CultureInfo lang, List<ElfinLocalizationData> localization, int index) {
		if (index < localization.Count)
			elfin.AddLocalization(lang, localization[index]);
	}

	static Task<AppStatus>? initializeTask;

	public static void BeginInitializeCompatibilityLayer() => initializeTask ??= Task.Run(InitializeCompatibilityData);

	public static AppStatus InitializeCompatibilityLayer() {
		if (Initialized)
			return AppStatus.OK;

		Task<AppStatus> task = initializeTask ??= Task.FromResult(InitializeCompatibilityData());
		Interlude.SpinUntilComplete(task);

		initializeTask = null;
		AppStatus result = task.GetAwaiter().GetResult();
		if (result != AppStatus.OK)
			return result;

		// At this point, Interlude can use Muse Dash assets, since StreamingAssets are ready
		Interlude.ShouldSelectInterludeTexture = true;
		Interlude.Spin(submessage: "Muse Dash Compat: Platform initialized...");

		filesystem.AddSearchPath("musedash", StreamingAssets);

		BuildDashStructures();
		Interlude.Spin(submessage: "Muse Dash Compat: Structures ready!");

		Initialized = true;

		return result;
	}

	static AppStatus InitializeCompatibilityData() {
		StaticSequentialProfiler.Start();

		AppStatus result;
		using (StaticSequentialProfiler.StartStackFrame("Platform Initialization")) {
			result = LightInitialize();
		}

		if (result != AppStatus.OK) {
			StaticSequentialProfiler.End(out _, out _);
			return result;
		}

		using (StaticSequentialProfiler.StartStackFrame("Read Addressables Catalog")) {
			StreamingAssets = new UnitySearchPathV2(Path.Combine(WhereIsMuseDashDataFolder!, $"StreamingAssets/aa"), StandalonePlatform);
		}

		using (StaticSequentialProfiler.StartStackFrame("Parallel Process Critical JSON Files"))
			Parallel.Invoke(
				() => Bosses = ReadOptionalJSON<List<BossConfigData>>("Assets/Static Resources/Data/Configs/others/boss.json") ?? [],
				() => NoteDataManager = ReadJSON<List<NoteConfigData>>("Assets/Static Resources/Data/Configs/others/notedata.json")!,
				() => Characters = ReadJSON<List<CharacterConfigData>>("Assets/Static Resources/Data/Configs/others/character.json")!,
				() => CharactersEN = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/english/character_English.json")!,
				() => CharactersZHCN = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/chineses/character_ChineseS.json")!,
				() => CharactersZHTW = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/chineset/character_ChineseT.json")!,
				() => CharactersJP = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/japanese/character_Japanese.json")!,
				() => CharactersKO = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/korean/character_Korean.json")!,
				() => Elfins = ReadOptionalJSON<List<ElfinConfigData>>("Assets/Static Resources/Data/Configs/others/elfin.json") ?? [],
				() => ElfinsEN = ReadOptionalJSON<List<ElfinLocalizationData>>("Assets/Static Resources/Data/Configs/english/elfin_English.json") ?? [],
				() => ElfinsZHCN = ReadOptionalJSON<List<ElfinLocalizationData>>("Assets/Static Resources/Data/Configs/chineses/elfin_ChineseS.json") ?? [],
				() => ElfinsZHTW = ReadOptionalJSON<List<ElfinLocalizationData>>("Assets/Static Resources/Data/Configs/chineset/elfin_ChineseT.json") ?? [],
				() => ElfinsJP = ReadOptionalJSON<List<ElfinLocalizationData>>("Assets/Static Resources/Data/Configs/japanese/elfin_Japanese.json") ?? [],
				() => ElfinsKO = ReadOptionalJSON<List<ElfinLocalizationData>>("Assets/Static Resources/Data/Configs/korean/elfin_Korean.json") ?? [],
				() => FailBarrages = ReadOptionalJSON<List<FailBarrageConfigData>>("Assets/Static Resources/Data/Configs/others/fail_barrage.json") ?? [],
				() => FailBarragesEN = ReadOptionalJSON<List<FailBarrageConfigData>>("Assets/Static Resources/Data/Configs/english/fail_barrage_English.json") ?? []
			);

		System.Diagnostics.Debug.Assert(Characters.Count == CharactersEN.Count);
		System.Diagnostics.Debug.Assert(Characters.Count == CharactersZHCN.Count);
		System.Diagnostics.Debug.Assert(Characters.Count == CharactersZHTW.Count);
		System.Diagnostics.Debug.Assert(Characters.Count == CharactersJP.Count);
		System.Diagnostics.Debug.Assert(Characters.Count == CharactersKO.Count);

		using (StaticSequentialProfiler.StartStackFrame("Parallel Process Localization"))
			Parallel.For(0, Characters.Count, static i => {
			var ch = Characters[i];
				ch.PrepareLocalization();
				ch.AddLocalization(ILocalize.English, CharactersEN[i]);
				ch.AddLocalization(ILocalize.ChineseSimplified, CharactersZHCN[i]);
				ch.AddLocalization(ILocalize.ChineseTraditional, CharactersZHTW[i]);
				ch.AddLocalization(ILocalize.Japanese, CharactersJP[i]);
				ch.AddLocalization(ILocalize.Korean, CharactersKO[i]);
			});

		for (int i = 0; i < Elfins.Count; i++) {
			var elfin = Elfins[i];
			elfin.Index = i;
			elfin.PrepareLocalization();
			AddElfinLocalization(elfin, ILocalize.English, ElfinsEN, i);
			AddElfinLocalization(elfin, ILocalize.ChineseSimplified, ElfinsZHCN, i);
			AddElfinLocalization(elfin, ILocalize.ChineseTraditional, ElfinsZHTW, i);
			AddElfinLocalization(elfin, ILocalize.Japanese, ElfinsJP, i);
			AddElfinLocalization(elfin, ILocalize.Korean, ElfinsKO, i);
		}

		using (StaticSequentialProfiler.StartStackFrame("Parallel Process NoteDataManager")) {
			// parallel process the dictionaries
			ConcurrentDictionary<string, List<string>> concurrentIBMSToDesc = [];
			ConcurrentDictionary<string, NoteConfigData> concurrentIDToNote = [];
			ConcurrentDictionary<string, NoteConfigData> concurrentIBMSToNote = [];
			ConcurrentDictionary<string, NoteConfigData> concurrentUIDToNote = [];

			Parallel.ForEach(NoteDataManager, notedata => {
				concurrentIDToNote[notedata.id] = notedata;
				concurrentIBMSToNote[notedata.ibms_id] = notedata;
				concurrentUIDToNote[notedata.uid] = notedata;
				concurrentIBMSToDesc.GetOrAdd(notedata.ibms_id, (x) => []).Add(notedata.des);
			});

			// Load the finalized dictionaries
			IBMSToDesc = concurrentIBMSToDesc.ToFrozenDictionary();
			IDToNote = concurrentIDToNote.ToFrozenDictionary();
			IBMSToNote = concurrentIBMSToNote.ToFrozenDictionary();
			UIDToNote = concurrentUIDToNote.ToFrozenDictionary();
		}

		using (StaticSequentialProfiler.StartStackFrame("ReadDashStructures"))
			ReadDashStructures();

		StaticSequentialProfiler.End(out var stack, out var accumulators);

		Logs.Debug($"MuseDashCompat.Init(): profiling complete, results:\n  Stack:\n{string.Join(Environment.NewLine, stack.ToStringArray())}\nAccumulators:\n{string.Join(Environment.NewLine, accumulators.Select(x => $"    {x.Key}: {x.Value.Timer.Elapsed.TotalMilliseconds} ms\n"))}\n");

		return result;
	}
}
