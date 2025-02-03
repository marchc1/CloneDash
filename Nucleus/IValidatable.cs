using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Engine
{
    public interface IValidatable
    {
        public bool IsValid();

        public static bool IsValid([NotNullWhen(true)] IValidatable? item) => item != null && item.IsValid();
    }
}