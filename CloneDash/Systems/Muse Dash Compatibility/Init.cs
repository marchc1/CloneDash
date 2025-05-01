using AssetStudio;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloneDash
{
	public record MuseDashAsset(string nicename, string filename);
	public static partial class MuseDashCompatibility
	{
		public static UnityAssetCatalog Catalog;
		public static UnityBundleSearcher Bundles;

		public static bool Initialized { get; private set; } = false;

		public static MDCompatLayerInitResult InitializeCompatibilityLayer() {
			if (Initialized)
				return MDCompatLayerInitResult.OK;

			CD_StaticSequentialProfiler.Start();

			MDCompatLayerInitResult result;
			using (CD_StaticSequentialProfiler.StartStackFrame("Platform Initialization")) {
#if COMPILED_WINDOWS
				result = INIT_WINDOWS();
#elif COMPILED_OSX
            MDCompatLayerInitResult result = INIT_OSX();
#elif COMPILED_LINUX
            MDCompatLayerInitResult result = INIT_LINUX();
#else
			MDCompatLayerInitResult result = MDCompatLayerInitResult.OperatingSystemNotCompatible;
#endif
			}

			if (result != MDCompatLayerInitResult.OK) {
				CD_StaticSequentialProfiler.End(out _, out _);
				return result;
			}

			Catalog = new(Path.Combine(WhereIsMuseDashInstalled, "MuseDash_Data/StreamingAssets/aa/catalog.json"));
			Bundles = new(Catalog);
			// At this point, Interlude can use Muse Dash assets, since StreamingAssets are ready
			Interlude.ShouldSelectInterludeTexture = true;
			Interlude.Spin(submessage: "Muse Dash Compat: Platform initialized...");

			byte[] serializedBytes;
			using (CD_StaticSequentialProfiler.StartStackFrame("Load NoteManagerAssetBundle")) {
				AssetsManager manager = new AssetsManager();
				NoteManagerAssetBundle = Bundles.Search("Assets/Static Resources/_Programs/GlobalConfigs/NoteDataMananger.asset");
				manager.LoadFiles(NoteManagerAssetBundle);
				using (Bundles.Open("Assets/Static Resources/_Programs/GlobalConfigs/NoteDataMananger.asset")) {

				}
				var monobehavior = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;
				var monobehavior_obj = (monobehavior.ToType()["serializationData"] as OrderedDictionary);
				var serializedList = monobehavior_obj["SerializedBytes"] as List<object>;
				serializedBytes = new byte[serializedList.Count];
				for (int i = 0; i < serializedList.Count; i++) serializedBytes[i] = (byte)serializedList[i];

				manager.Clear();
				Interlude.Spin(submessage: "Muse Dash Compat: Note conversion table loaded...");
			}

			NoteDataManager = SerializationUtility.DeserializeValue<List<NoteConfigData>>(serializedBytes, DataFormat.Binary);
			Interlude.Spin(submessage: "Muse Dash Compat: Deserialized note config...");

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
