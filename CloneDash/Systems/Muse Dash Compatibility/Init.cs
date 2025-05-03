using AssetStudio;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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
            	result = INIT_OSX();
#elif COMPILED_LINUX
            	result = INIT_LINUX();
#else
			MDCompatLayerInitResult result = MDCompatLayerInitResult.OperatingSystemNotCompatible;
#endif
			}

			if (result != MDCompatLayerInitResult.OK) {
				CD_StaticSequentialProfiler.End(out _, out _);
				return result;
			}

			using (CD_StaticSequentialProfiler.StartStackFrame("Build Catalog")) {
				Catalog = new(Path.Combine(WhereIsMuseDashDataFolder, "StreamingAssets/aa/catalog.json"));
				Bundles = new(Catalog);
			}
			// At this point, Interlude can use Muse Dash assets, since StreamingAssets are ready
			Interlude.ShouldSelectInterludeTexture = true;
			Interlude.Spin(submessage: "Muse Dash Compat: Platform initialized...");

			byte[] serializedBytes;
			using (CD_StaticSequentialProfiler.StartStackFrame("Load NoteManagerAssetBundle")) {
				AssetsManager manager = new AssetsManager();
				NoteManagerAssetBundle = Bundles.Search("Assets/Static Resources/_Programs/GlobalConfigs/NoteDataMananger.asset");
				manager.LoadFiles(NoteManagerAssetBundle);

				var monobehavior = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;
				monobehavior.reader.Reset();
				serializedBytes = [];

				for (int i = 0, n = (int)monobehavior.reader.BaseStream.Length; i < n; i++) {
					if (
						monobehavior.reader.ReadByte() == 1 &&
						monobehavior.reader.ReadByte() == 1 &&
						monobehavior.reader.ReadByte() == 11 &&
						monobehavior.reader.ReadByte() == 0 &&
						monobehavior.reader.ReadByte() == 0 &&
						monobehavior.reader.ReadByte() == 0 &&
						monobehavior.reader.ReadByte() == 109 &&
						monobehavior.reader.ReadByte() == 0 &&
						monobehavior.reader.ReadByte() == 95 &&
						monobehavior.reader.ReadByte() == 0 &&
						monobehavior.reader.ReadByte() == 78
						) {
						// Wow! This sucks!
						// But it cuts out 400ms...
						monobehavior.reader.BaseStream.Seek(-11 + -4, SeekOrigin.Current);
						int length = monobehavior.reader.ReadInt32();
						serializedBytes = new byte[length];
						var start = monobehavior.reader.BaseStream.Position;
						var raw = (monobehavior.reader.BaseStream as MemoryStream).GetBuffer();
						for (int i2 = 0; i2 < length; i2++) {
							serializedBytes[i2] = raw[start + i2];
						}
						break;
					}
				}

				manager.Clear();
				Interlude.Spin(submessage: "Muse Dash Compat: Note conversion table loaded...");
			}
			using (CD_StaticSequentialProfiler.StartStackFrame("Deserialize NoteDataManager"))
				NoteDataManager = SerializationUtility.DeserializeValue<List<NoteConfigData>>(serializedBytes, DataFormat.Binary);
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
