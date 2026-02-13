namespace Nucleus.Files;

public enum SearchPathAdd
{
	/// <summary>
	/// Adds the search path to the start; ie. it will be searched first.
	/// </summary>
	ToHead,

	/// <summary>
	/// Adds the search path to the end; ie. it will be searched last.
	/// </summary>
	ToTail
};
