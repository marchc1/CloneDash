namespace CloneDash.Game.Sheets
{
    /// <summary>
    /// WIP format, still thinking about this
    /// </summary>
    public class DashSheet
    {
        public SheetHeader Header { get; set; } = new();
        public SheetMusic Music { get; set; } = SheetMusic.Blank;
        public List<SheetEvent> Events { get; private set; } = [];
        public List<SheetEntity> Entities { get; private set; } = [];

        public static DashSheet LoadFromString(string data)
        {
            throw new Exception();
        }
        public static DashSheet LoadFromFile(string filepath) => LoadFromString(File.ReadAllText(filepath));
        //public static DashSheet LoadFromMuseDash(string mapname) => MuseDashCompatibility.ConvertAssetBundleToDashSheet(mapname);
    }
}
