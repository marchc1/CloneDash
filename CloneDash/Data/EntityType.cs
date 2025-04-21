namespace CloneDash
{
	/// <summary>
	/// The default entity types.<br></br>
	/// Custom entities should use the Custom EntityType.
	/// </summary>
	public enum EntityType
    {
        /// <summary>
        /// Not set
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Effects, such as text effects
        /// </summary>
        Effect = 1,
        /// <summary>
        /// Debugging entity (currently not used)
        /// </summary>
        Debug = 2,
        /// <summary>
        /// Custom entity type not applicable to the following definitions (not used at the moment)
        /// </summary>
        Custom = 16,

        /// <summary>
        /// Basic, single-hit enemy, which damages you when you fail to hit the it
        /// </summary>
        Single = 64,
        /// <summary>
        /// Single-hit enemy that requires the key to be held (sustained) until the note is over, does not do any damage for failing to hit but still ruins your combo
        /// </summary>
        SustainBeam,
        /// <summary>
        /// Single-hit enemy that is placed on both pathways, requires both entities to be hit or you will be damaged
        /// </summary>
        Double,
        /// <summary>
        /// Multi-hit enemy, requires an initial hit and (not yet implemented) gives more score for each further hit until a maximum number of hits is achieved
        /// </summary>
        Masher,
        /// <summary>
        /// Enemy which needs to be avoided, otherwise it will damage the player
        /// </summary>
        Gear,
        /// <summary>
        /// Single-hit enemy that doesn't do any damage or combo reset when missed
        /// </summary>
        Ghost,
        /// <summary>
        /// Boss enemy. Only spawned once and responds moreso to map events
        /// </summary>
        Boss,
        /// <summary>
        /// Health pickup
        /// </summary>
        Heart,
        /// <summary>
        /// Score pickup
        /// </summary>
        Score,        
        /// <summary>
        /// Single-hit enemy that swings in from the top
        /// </summary>
        Hammer,   
        /// <summary>
        /// Single-hit enemy that comes in from the bottom-up
        /// </summary>
        Raider
    }
}
