using Nucleus;
using Nucleus.ManagedMemory;

namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public class MD1_SongCover : IValidatable
{
	public Texture? Texture { get; set; }
	/// <summary>
	/// Thanks, Unity
	/// </summary>
	public bool Flipped { get; set; }

	public bool IsValid() => IValidatable.IsValid(Texture);
}
