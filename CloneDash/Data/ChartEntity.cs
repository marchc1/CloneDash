namespace CloneDash.Data
{
    public class ChartEntity
    {
		/// <summary>
		/// Entity type
		/// </summary>
        public EntityType Type;
		/// <summary>
		/// Entity pathway
		/// </summary>
        public PathwaySide Pathway;
		/// <summary>
		/// Vertical direction (or right-side) for this entity.
		/// </summary>
        public EntityEnterDirection EnterDirection;
		/// <summary>
		/// Dependant on entity logic and only really applciable to <see cref="Game.Entities.SingleHitEnemy"/>
		/// </summary>
        public EntityVariant Variant;

        public double HitTime;
        public double ShowTime;
		public double Length;

		/// <summary>
		/// How much fever this entity can give to the palyer.
		/// </summary>
        public int Fever;
		/// <summary>
		/// How much score this entity can give to the player.
		/// </summary>
        public int Score;
		/// <summary>
		/// How fast (from a range of 1 -> 3) does the entity go. This eventually gets plugged into the speeds parameters
		/// of whatever pathway animation is played
		/// </summary>
        public int Speed;
		/// <summary>
		/// How much health this entity can give to the player.
		/// </summary>
        public int Health;
		/// <summary>
		/// How much damage this entity can deal to the player.
		/// </summary>
        public int Damage;

		/// <summary>
		/// Is the entity vertically flipped.
		/// </summary>
        public bool Flipped;
		/// <summary>
		/// Is the entity related to the boss in any way.
		/// </summary>
        public bool RelatedToBoss;

        public string? DebuggingInfo { get; internal set; }
    }
}
