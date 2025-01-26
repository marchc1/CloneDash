using AssetStudio;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        public static bool Initialized { get; private set; } = false;

        public static MDCompatLayerInitResult InitializeCompatibilityLayer() {
            if (Initialized)
                return MDCompatLayerInitResult.OK;


#if COMPILED_WINDOWS
			MDCompatLayerInitResult result = INIT_WINDOWS();
#elif COMPILED_OSX
            MDCompatLayerInitResult result = INIT_OSX();
#elif COMPILED_LINUX
            MDCompatLayerInitResult result = INIT_LINUX();
#else
			MDCompatLayerInitResult result = MDCompatLayerInitResult.OperatingSystemNotCompatible;
#endif

            if (result != MDCompatLayerInitResult.OK)
                return result;

            AssetsManager manager = new AssetsManager();
            manager.LoadFiles(NoteManagerAssetBundle);

            var monobehavior = manager.assetsFileList[0].Objects.First(x => x.type == ClassIDType.MonoBehaviour) as MonoBehaviour;
            var monobehavior_obj = (monobehavior.ToType()["serializationData"] as OrderedDictionary);
            var serializedList = monobehavior_obj["SerializedBytes"] as List<object>;
            byte[] serializedBytes = new byte[serializedList.Count];
            for (int i = 0; i < serializedList.Count; i++) serializedBytes[i] = (byte)serializedList[i];

            NoteDataManager = OdinSerializer.SerializationUtility.DeserializeValue<List<NoteConfigData>>(serializedBytes, DataFormat.Binary);

            foreach (var notedata in NoteDataManager) {
                IDToNote[notedata.id] = notedata;
                IBMSToNote[notedata.ibms_id] = notedata;
                UIDToNote[notedata.uid] = notedata;
                if (!IBMSToDesc.ContainsKey(notedata.ibms_id))
                    IBMSToDesc[notedata.ibms_id] = [];

                IBMSToDesc[notedata.ibms_id].Add(notedata.des);
            }

            BuildDashStructures();
            manager.Clear();
            Initialized = true;
            return result;
        }
    }
}
