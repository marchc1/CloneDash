using System.Collections.Generic;

namespace AssetStudio
{
    public class AssetInfo
    {
        public int preloadIndex;
        public int preloadSize;
        public PPtr<Object> asset;

        public AssetInfo(ObjectReader reader)
        {
            preloadIndex = reader.ReadInt32();
            preloadSize = reader.ReadInt32();
            asset = new PPtr<Object>(reader);
        }
    }

    public sealed class AssetBundle : NamedObject
    {
        public List<PPtr<Object>> m_PreloadTable;
        public List<KeyValuePair<string, AssetInfo>> m_Container;
        public string m_AssetBundleName;
        public string[] m_Dependencies;
        public bool m_IsStreamedSceneAssetBundle;

        public AssetBundle(ObjectReader reader) : base(reader)
        {
            var m_PreloadTableSize = reader.ReadInt32();
            m_PreloadTable = new List<PPtr<Object>>();
            for (var i = 0; i < m_PreloadTableSize; i++)
            {
                m_PreloadTable.Add(new PPtr<Object>(reader));
            }

            var m_ContainerSize = reader.ReadInt32();
            m_Container = new List<KeyValuePair<string, AssetInfo>>();
            for (var i = 0; i < m_ContainerSize; i++)
            {
                m_Container.Add(new KeyValuePair<string, AssetInfo>(reader.ReadAlignedString(), new AssetInfo(reader)));
            }

            var m_MainAsset = new AssetInfo(reader);

            if (version == (5, 4)) //5.4.x
            {
                var m_ClassVersionMapSize = reader.ReadInt32();
                for (var i = 0; i < m_ClassVersionMapSize; i++)
                {
                    var first = reader.ReadInt32();
                    var second = reader.ReadInt32();
                }
            }

            if (version >= (4, 2)) //4.2 and up
            {
                var m_RuntimeCompatibility = reader.ReadUInt32();
            }

            if (version >= 5) //5.0 and up
            {
                m_AssetBundleName = reader.ReadAlignedString();

                m_Dependencies = reader.ReadStringArray();

                m_IsStreamedSceneAssetBundle = reader.ReadBoolean();
            }
        }
    }
}
