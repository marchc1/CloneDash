using Nucleus.Engine;
using Nucleus.ManagedMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Data
{
    public class DashChart
    {
        public string Version = "9-10-2024";
    }
    public class ChartInfo
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public decimal BPM { get; set; } = 0;
        public string Scene { get; set; } = "";
        public string[] LevelDesigners { get; set; } = [];
        public string[] SearchTags { get; set; } = [];
    }
    public class ChartSheet;
    public class SheetEntity;
    public class SheetEvent;

    public abstract class ChartSong
    {
        public MusicTrack? Track;
        protected abstract MusicTrack ProduceTrack();
        public MusicTrack GetTrack() {
            if (Track != null && IValidatable.IsValid(Track))
                return Track;

            Track = ProduceTrack();
            return Track;
        }
    }
    public class MuseDashSong : ChartSong
    {

    }

    public class ChartCover;
}
