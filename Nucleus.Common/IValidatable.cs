using System.Diagnostics.CodeAnalysis;

namespace Nucleus;

public interface IValidatable
{
	public static bool IsValid<T>([NotNullWhen(true)] in T? item) where T : IValidatable, allows ref struct
		=> item != null && item.IsValid();

	public bool IsValid();
}