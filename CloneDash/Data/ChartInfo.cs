namespace CloneDash.Data
{
    public class ChartInfo
    {
        public decimal BPM { get; set; } = 0;
        public string Scene { get; set; } = "";
        public string Music { get; set; } = "";
        public string[] LevelDesigners { get; set; } = [];
        public string[] SearchTags { get; set; } = [];
        public string Difficulty1 { get; set; } = "0";
        public string Difficulty2 { get; set; } = "0";
        public string Difficulty3 { get; set; } = "0";
        public string Difficulty4 { get; set; } = "0";
    }
}
