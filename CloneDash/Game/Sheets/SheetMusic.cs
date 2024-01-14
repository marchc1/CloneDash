namespace CloneDash.Game.Sheets
{
    public struct SheetMusic {
        public MusicType StoredAs;
        public string? Filepath;
        public byte[]? Data;

        public static SheetMusic FromFilepath(string filepath) {
            return new() {
                StoredAs = MusicType.FromFile,
                Filepath = filepath,
            };
        }

        public static SheetMusic FromMemory(byte[] musicdata) {
            return new() {
                StoredAs = MusicType.FromByteArray,
                Data = musicdata,
            };
        }

        public static readonly SheetMusic Blank = new() { StoredAs = MusicType.NotSet };
    }
}
