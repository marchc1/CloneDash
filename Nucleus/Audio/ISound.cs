using Nucleus.Commands;
using Nucleus.ManagedMemory;

namespace Nucleus.Audio
{
	public interface ISound : IManagedMemory
	{
		/// <summary>
		/// Attachs the current value, and any further updates to the <see cref="ConVar"/>'s value, to the sounds volume. This expects a multiplicative number (where 0 == no sound, 1 == no change).
		/// </summary>
		/// <param name="cv"></param>
		public void BindVolumeToConVar(ConVar cv);
	}
}
