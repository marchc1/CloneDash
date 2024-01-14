namespace CloneDash.Game.Components
{
    /// <summary>
    /// WIP
    /// </summary>
    public class Statistics : DashGameComponent
    {
        public bool FullCombo { get; private set; } = true;
        public bool AllPerfect { get; private set; } = true;

        public int Perfects { get; private set; } = 0;
        public int Greats { get; private set; } = 0;
        public int Passes { get; private set; } = 0;
        public int Early { get; private set; } = 0;
        public int Late { get; private set; }

        public List<float> MillisecondAccuracies { get; private set; } = [];

        public Statistics(DashGame game) : base(game) {

        }
    }
}
