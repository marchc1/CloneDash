using Nucleus;
using Nucleus.ManagedMemory;

namespace CloneDash.Data
{
	public class ChartCover : IValidatable
	{
		public Texture? Texture { get; set; }
		/// <summary>
		/// Thanks, Unity
		/// </summary>
		public bool Flipped { get; set; }

		public bool IsValid() => IValidatable.IsValid(Texture);
	}
}
