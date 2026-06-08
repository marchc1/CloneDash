using System.Text.Json.Serialization;

namespace AssetStudio
{
    public class GLTextureSettings
    {
        public int m_FilterMode;
        public int m_Aniso;
        public float m_MipBias;
        public int m_WrapMode;
        [JsonInclude]
        private int m_WrapU { set => m_WrapMode = value; }

        public GLTextureSettings() { }

        public GLTextureSettings(ObjectReader reader)
        {
            var version = reader.version;

            m_FilterMode = reader.ReadInt32();
            m_Aniso = reader.ReadInt32();
            m_MipBias = reader.ReadSingle();
            if (version >= 2017)//2017.x and up
            {
                m_WrapMode = reader.ReadInt32(); //m_WrapU
                var m_WrapV = reader.ReadInt32();
                var m_WrapW = reader.ReadInt32();
            }
            else
            {
                m_WrapMode = reader.ReadInt32();
            }
        }
    }
}
