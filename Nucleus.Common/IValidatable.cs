using System.Diagnostics.CodeAnalysis;

namespace Nucleus
{
	public interface IValidatable
	{
		public bool IsValid();

		public static bool IsValid<T>([NotNullWhen(true)] T? item) where T : IValidatable, allows ref struct 
			=> item != null && item.IsValid();
	}
}