namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        public enum MDCompatLayerInitResult
        {
            OK,
            SteamNotInstalled,
            MuseDashNotInstalled,
            StreamingAssetsNotFound,
            NoteDataManagerNotFound,
            OperatingSystemNotCompatible
        }
    }
}
