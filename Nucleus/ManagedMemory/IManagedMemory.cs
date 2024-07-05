using Nucleus.Engine;

namespace Nucleus.ManagedMemory
{
    public interface IManagedMemory : IValidatable, IDisposable
    {
        public ulong UsedBits { get; }
        public ulong UsedBytes => UsedBits / 8;
    }
}
