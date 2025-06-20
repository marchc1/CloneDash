using CloneDash.Compatibility.Unity;
using CloneDash.Game;

using Nucleus;
using Nucleus.Files;

namespace CloneDash.Compatibility.MuseDash
{
	public record MuseDashAsset(string nicename, string filename);
	public static partial class MuseDashCompatibility
	{
		public static bool Initialized { get; private set; } = false;

		public static MDCompatLayerInitResult LightInitialize() {
#if COMPILED_WINDOWS
			return INIT_WINDOWS();
#elif COMPILED_OSX
            return INIT_OSX();
#elif COMPILED_LINUX
            return INIT_LINUX();
#else
			return MDCompatLayerInitResult.OperatingSystemNotCompatible;
#endif
		}
		public static MDCompatLayerInitResult InitializeCompatibilityLayer() {
			if (Initialized)
				return MDCompatLayerInitResult.OK;

			CD_StaticSequentialProfiler.Start();

			MDCompatLayerInitResult result;
			using (CD_StaticSequentialProfiler.StartStackFrame("Platform Initialization")) {
				result = LightInitialize();
			}

			if (result != MDCompatLayerInitResult.OK) {
				CD_StaticSequentialProfiler.End(out _, out _);
				return result;
			}

			// At this point, Interlude can use Muse Dash assets, since StreamingAssets are ready
			Interlude.ShouldSelectInterludeTexture = true;
			Interlude.Spin(submessage: "Muse Dash Compat: Platform initialized...");

			using (CD_StaticSequentialProfiler.StartStackFrame("Mount to Filesystem")) {
				using (Stream stream = Filesystem.Open("assets", "mdlut.dat") ?? throw new Exception("Cannot find the mdlut.dat file"))
					StreamingAssets = Filesystem.AddSearchPath("musedash", new UnitySearchPath(Path.Combine(WhereIsMuseDashDataFolder!, $"StreamingAssets/aa/{StandalonePlatform}"), stream));
			}

			using (CD_StaticSequentialProfiler.StartStackFrame("Deserialize NoteDataManager"))
				NoteDataManager = Filesystem.ReadJSON<List<NoteConfigData>>("musedash", "Assets/Static Resources/Data/Configs/others/notedata.json");

			using (CD_StaticSequentialProfiler.StartStackFrame("Deserialize Characters")) {
				Characters = Filesystem.ReadJSON<List<CharacterConfigData>>("musedash", "Assets/Static Resources/Data/Configs/others/character.json");
				CharactersEN = Filesystem.ReadJSON<List<CharacterLocalizationData>>("musedash", "Assets/Static Resources/Data/Configs/english/character_English.json");
				System.Diagnostics.Debug.Assert(Characters.Count == CharactersEN.Count);
				for (int i = 0, c = Characters.Count; i < c; i++) {
					Characters[i].Localization["english"] = CharactersEN[i];
				}
			}

			Interlude.Spin(submessage: "Muse Dash Compat: Deserialized note config...");

			using (CD_StaticSequentialProfiler.StartStackFrame("Process NoteDataManager"))
				foreach (var notedata in NoteDataManager) {
					IDToNote[notedata.id] = notedata;
					IBMSToNote[notedata.ibms_id] = notedata;
					UIDToNote[notedata.uid] = notedata;
					if (!IBMSToDesc.ContainsKey(notedata.ibms_id))
						IBMSToDesc[notedata.ibms_id] = [];

					IBMSToDesc[notedata.ibms_id].Add(notedata.des);
				}

			using (CD_StaticSequentialProfiler.StartStackFrame("BuildDashStructures"))
				BuildDashStructures();
			Interlude.Spin(submessage: "Muse Dash Compat: Structures ready!");

			Initialized = true;

			CD_StaticSequentialProfiler.End(out var stack, out var acumulators);

			Logs.Debug($"MuseDashCompat.Init(): profiling complete, results:\n  Stack:\n{string.Join(Environment.NewLine, stack.ToStringArray())}\n");

			return result;
		}
	}
}
