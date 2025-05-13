namespace CloneDash
{
	/// <summary>
	/// Used to define which pathway a certain entity may need, an event may occur on, etc...<br></br><br></br>
	/// Can define a specific pathway, both, or no patway.
	/// </summary>
	public enum PathwaySide
	{
		/// <summary>
		/// Doesn't apply to any pathway.
		/// </summary>
		None,
		/// <summary>
		/// The top pathway.
		/// </summary>
		Top,
		/// <summary>
		/// The bottom pathway.
		/// </summary>
		Bottom,
		/// <summary>
		/// Applies to both pathways.
		/// </summary>
		Both
	}
}
