using AssetStudio;
using CloneDash.Game;
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
		public static Dictionary<string, string> StreamingAssetsSearchable = [];
		public static Dictionary<string, Dictionary<string, MuseDashAsset>> StreamingAssetsSearchableTypes = [];
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

			// Trying to make searching these files a bit more efficient...
			// for now, commenting this out. But it may get used later
			// only 25ms to run, i just dont want to go through everything
			// and replace the First calls with calls to these dicts right now
			/*foreach(var filename in StreamingFiles) {
				var ext = Path.GetExtension(filename);
				if (ext != ".bundle") continue;
				var name = Path.GetFileNameWithoutExtension(filename);
				var pieces = name.Split('_');
				var md5 = pieces[pieces.Length - 1];
				bool isMD5 = Regex.IsMatch(md5, "[a-f0-9]{32}");
				var nicename = string.Join('_', pieces, 0, isMD5 ? pieces.Length - 1 : pieces.Length);

				StreamingAssetsSearchable[nicename] = filename;
				var type = pieces[0];
				if (!StreamingAssetsSearchableTypes.TryGetValue(type, out var list))
					StreamingAssetsSearchableTypes[type] = list = [];

				list.Add(nicename, new(nicename, filename));
			}*/

			byte[] serializedBytes;
			using (CD_StaticSequentialProfiler.StartStackFrame("Load NoteManagerAssetBundle")) {
				AssetsManager manager = new AssetsManager();
				manager.LoadFiles(NoteManagerAssetBundle);

				var monobehavior = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;
				var monobehavior_obj = (monobehavior.ToType()["serializationData"] as OrderedDictionary);
				var serializedList = monobehavior_obj["SerializedBytes"] as List<object>;
				serializedBytes = new byte[serializedList.Count];
				for (int i = 0; i < serializedList.Count; i++) serializedBytes[i] = (byte)serializedList[i];

				manager.Clear();
			}

			NoteDataManager = OdinSerializer.SerializationUtility.DeserializeValue<List<NoteConfigData>>(serializedBytes, DataFormat.Binary);

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

			Initialized = true;

			CD_StaticSequentialProfiler.End(out var stack, out var acumulators);

			Logs.Debug($"MuseDashCompat.Init(): profiling complete, results:\n  Stack:\n{string.Join(Environment.NewLine, stack.ToStringArray())}\n");

			return result;
		}
	}
}
