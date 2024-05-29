namespace CloneDash.Game.Sheets
{
    /// <summary>
    /// Where music comes from
    /// </summary>
    public enum MusicType
    {
        /// <summary>
        /// Unknown/not set for some reason
        /// </summary>
        NotSet,
        /// <summary>
        /// Came from a file on the disk
        /// </summary>
        FromFile,
        /// <summary>
        /// Came from a byte[] in memory
        /// </summary>
        FromByteArray
    }
}
