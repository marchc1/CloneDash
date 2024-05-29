namespace CloneDash.Game.Sheets
{
    public struct SheetHeader
    {
        public SheetVersion SheetVersion;

        public string Title;
        public string Author;
        public string SheetAuthor;

        public double StartOffset;

        public List<TempoChange> TempoChanges;

        public SheetHeader()
        {
            SheetVersion = SheetVersion.Version0_0_1;
            Title = "Unknown Track";
            Author = "Unknown Author";
            SheetAuthor = "Unknown Sheet Author";
            StartOffset = 0;
            TempoChanges = [];
        }
    }
}
