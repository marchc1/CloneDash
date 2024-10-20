namespace CloneDash.Data
{
    public struct ChartEntity
    {
        public EntityType Type;
        public PathwaySide Pathway;
        public EntityEnterDirection EnterDirection;

        public double HitTime;
        public double ShowTime;

        public int Fever;
        public int Score;
        public int Health;
        public int Damage;
        internal double Length;

        public bool RelatedToBoss;

        public string DebuggingInfo { get; internal set; }
    }
}
