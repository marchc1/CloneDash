namespace CloneDash.Common;

public interface IUniquelyIdentifiableObject
{
	/// <summary>
	/// Get the unique ID (should be unique across anything, NOT just within the context of the type!)
	/// </summary>
	ReadOnlySpan<char> GetUUID();
}

public static class IUniquelyIdentifiableObjectExts
{
	public static bool UUIDEquals(this IUniquelyIdentifiableObject? obj1, IUniquelyIdentifiableObject? obj2) {
		if (obj1 == null) return obj2 == null;
		if (obj2 == null) return false; // obj1 being not null, but obj2 being null, means no match

		return obj1.GetUUID().Equals(obj2.GetUUID(), StringComparison.Ordinal);
	}
}