using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Game
{
    // WIP
    public class StatisticsData
    {
        public int Misses { get; set; } = 0;
        public int Hits { get; set; } = 0;
        public int Avoids { get; set; } = 0;

        public int NeededHits { get; set; } = 0;
        public int NeededAvoids { get; set; } = 0;

        public bool FullCombo => Misses == 0;

        public int Perfects { get; set; } = 0;
        public int Greats { get; set; } = 0;

        public List<double> MsAccuracy { get; } = [];
    }
}
