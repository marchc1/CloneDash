namespace AssetStudio
{
    public static class AudioClipExtension
    {
        public static bool IsConvertSupport(this AudioClip m_AudioClip)
        {
            if (m_AudioClip.version < 5)
            {
                switch (m_AudioClip.m_Type)
                {
                    case FMODSoundType.AIFF:
                    case FMODSoundType.IT:
                    case FMODSoundType.MOD:
                    case FMODSoundType.S3M:
                    case FMODSoundType.XM:
                    case FMODSoundType.XMA:
                    case FMODSoundType.AUDIOQUEUE:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (m_AudioClip.m_CompressionFormat)
                {
                    case AudioCompressionFormat.PCM:
                    case AudioCompressionFormat.Vorbis:
                    case AudioCompressionFormat.ADPCM:
                    case AudioCompressionFormat.MP3:
                    case AudioCompressionFormat.XMA:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsLegacyConvertSupport(this AudioClip m_AudioClip)
        {
            return m_AudioClip.version < (2, 6) && m_AudioClip.m_Format != 0x05;
        }
    }
}
