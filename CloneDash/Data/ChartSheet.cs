namespace CloneDash.Data
{
    public class ChartSheet
    {
        public ChartSong Song { get; private set; }
        public ChartSheet(ChartSong song) => Song = song;

        public double StartOffset { get; set; }
        public string Rating { get; set; }
        public List<ChartEntity> Entities { get; } = [];
        public List<ChartEvent> Events { get; } = [];
    }
}
