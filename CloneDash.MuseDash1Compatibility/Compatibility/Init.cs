using CloneDash.Common.Compatibility.Valve;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;

using Nucleus;
using Nucleus.Common;
using Nucleus.Core;
using Nucleus.Files;
using System.Collections.Concurrent;
using System.Collections.Frozen;
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

	public static char[] CodepointsInUse = null!;

	static T? ReadJSON<T>(ReadOnlySpan<char> path) => JsonSerializer.Deserialize<T>(StreamingAssets.ReadText(path)!, new JsonSerializerOptions {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		IncludeFields = true
	});

	public static AppStatus InitializeCompatibilityLayer() {
		if (Initialized)
			return AppStatus.OK;

		StaticSequentialProfiler.Start();

		AppStatus result;
		using (StaticSequentialProfiler.StartStackFrame("Platform Initialization")) {
			result = LightInitialize();
		}

		if (result != AppStatus.OK) {
			StaticSequentialProfiler.End(out _, out _);
			return result;
		}

		// At this point, Interlude can use Muse Dash assets, since StreamingAssets are ready
		Interlude.ShouldSelectInterludeTexture = true;
		Interlude.Spin(submessage: "Muse Dash Compat: Platform initialized...");

		using (StaticSequentialProfiler.StartStackFrame("Mount to Filesystem")) {
			StreamingAssets = filesystem.AddSearchPath("musedash", new UnitySearchPathV2(Path.Combine(WhereIsMuseDashDataFolder!, $"StreamingAssets/aa"), StandalonePlatform));
		}

		using (StaticSequentialProfiler.StartStackFrame("Parallel Process Critical JSON Files"))
			Parallel.Invoke(
				() => NoteDataManager = ReadJSON<List<NoteConfigData>>("Assets/Static Resources/Data/Configs/others/notedata.json")!,
				() => Characters = ReadJSON<List<CharacterConfigData>>("Assets/Static Resources/Data/Configs/others/character.json")!,
				() => CharactersEN = ReadJSON<List<CharacterLocalizationData>>("Assets/Static Resources/Data/Configs/english/character_English.json")!
			);

		System.Diagnostics.Debug.Assert(Characters.Count == CharactersEN.Count);

		using (StaticSequentialProfiler.StartStackFrame("Parallel Process Localization"))
			Parallel.For(0, Characters.Count, static i => Characters[i].Localization["english"] = CharactersEN[i]);

		Interlude.Spin(submessage: "Muse Dash Compat: Deserialized note config...");

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

		using (StaticSequentialProfiler.StartStackFrame("BuildDashStructures"))
			BuildDashStructures();
		Interlude.Spin(submessage: "Muse Dash Compat: Structures ready!");

		using (StaticSequentialProfiler.StartStackFrame("Graphics2D.RegisterCodepoints"))
			Graphics2D.RegisterCodepoints(MuseDash1Compatibility.CodepointsInUse);
		Interlude.Spin(submessage: "Muse Dash Compat: Fonts ready!");

		Initialized = true;

		StaticSequentialProfiler.End(out var stack, out var accumulators);

		Logs.Debug($"MuseDashCompat.Init(): profiling complete, results:\n  Stack:\n{string.Join(Environment.NewLine, stack.ToStringArray())}\nAccumulators:\n{string.Join(Environment.NewLine, accumulators.Select(x => $"    {x.Key}: {x.Value.Timer.Elapsed.TotalMilliseconds} ms\n"))}\n");

		return result;
	}
}
