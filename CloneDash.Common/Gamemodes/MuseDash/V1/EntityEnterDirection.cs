namespace CloneDash
{
	/// <summary>
	/// Defines which direction the entity comes in from.<br></br>
	/// This really only applies to some entities; it's up to the entity definition to define if and how it wishes to implement this behavior<br></br>
	/// Only by default applies to standard enemies because that's all that really uses it 
	/// </summary>
	public enum EntityEnterDirection
	{
		/// <summary>
		/// Default behavior, which means as normal, entities come in from the right side
		/// </summary>
		RightSide,
		/// <summary>
		/// Entities will come in from the top of the screen onto the pathway
		/// </summary>
		TopDown,
		/// <summary>
		/// Entities will come in from the bottom of the screen onto the pathway
		/// </summary>
		BottomUp
	}
}
