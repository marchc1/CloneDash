using System.Diagnostics.CodeAnalysis;

namespace Nucleus
{
	public interface IValidatable
	{
		public bool IsValid();

		public static bool IsValid([NotNullWhen(true)] IValidatable? item) => item != null && item.IsValid();
	}
}