namespace CloneDash.Common;

public interface IUniquelyIdentifiableObject
{
	/// <summary>
	/// Get the unique ID (should be unique across anything, NOT just within the context of the type!)
	/// </summary>
	ReadOnlySpan<char> GetUUID();
}