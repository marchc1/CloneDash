using System.Collections.Generic;

namespace AssetStudio
{
    public sealed class PreloadData : NamedObject
    {
        public List<PPtr<Object>> m_Assets;

        public PreloadData(ObjectReader reader) : base(reader)
        {
            var m_PreloadTableSize = reader.ReadInt32();
            m_Assets = new List<PPtr<Object>>();
            for (var i = 0; i < m_PreloadTableSize; i++)
            {
                m_Assets.Add(new PPtr<Object>(reader));
            }

            /*
            if (version[0] >= 5) //5.0 and up
            {
                var m_DependenciesSize = reader.ReadInt32();
                var m_Dependencies = new string[m_DependenciesSize];

                for (var i = 0; i < m_DependenciesSize; i++)
                {
                    m_Dependencies[i] = reader.ReadAlignedString();
                }
            }

            if (version[0] > 2018 || (version[0] == 2018 && version[1] >= 2)) //2018.2 and up
            {
                var m_ExplicitDataLayout = reader.ReadBoolean();
            }
            */
        }
    }
}
