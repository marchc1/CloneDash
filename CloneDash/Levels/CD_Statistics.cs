using CloneDash.Game;
using CloneDash.Game.Sheets;
using Nucleus.Engine;

namespace CloneDash.Levels
{
    public class CD_Statistics : Level
    {
        public override void Initialize(params object[] args) {
            DashSheet sheet = args[0] as DashSheet;
            StatisticsData stats = args[1] as StatisticsData;

        }
    }
}
