namespace CloneDash
{
    /// <summary>
    /// Defines how the player and an entity will interact with each other
    /// </summary>
    public enum EntityInteractivity
    {
        /// <summary>
        /// No interactivity. Used for entities that are not meant to be played with, such as effects
        /// </summary>
        Noninteractive,

        /// <summary>
        /// Single-hit trigger. Used for basic enemy types<br></br>
        /// Will mean OnHit() and OnMiss() get called, when applicable
        /// </summary>
        Hit,

        /// <summary>
        /// Triggers when the player is on the same pathway as the entity. You should still try to hit this entity, but it doesnt require a key press<br></br>
        /// Will call OnHit() when the player passes by the entity
        /// </summary>
        SamePath,

        /// <summary>
        /// The player should actually avoid this entity<br></br>
        /// Will call OnPass(). When not avoided, it damages the player. This may be changed so it uses OnHit() or OnMiss() instead
        /// </summary>
        Avoid,

        /// <summary>
        /// Single-hit trigger, but requires the key to be held as well<br></br>
        /// Doesn't do much differently compared to an entity using Hit interactivity
        /// </summary>
        Sustain
    }
}
