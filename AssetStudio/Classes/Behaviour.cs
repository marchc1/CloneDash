namespace AssetStudio
{
    public abstract class Behaviour : Component
    {
        public byte m_Enabled;

        public Behaviour() { }

        protected Behaviour(ObjectReader reader) : base(reader)
        {
            m_Enabled = reader.ReadByte();
            reader.AlignStream();
        }
    }
}
