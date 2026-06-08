using CloneDash.Common.Game;
using CloneDash.Common.Songs;
using Nucleus.Common.Types;

namespace CloneDash.Common.Gamemodes;

public struct GameLoadGenericParameters{
	public int? StartMeasure;
	public bool Autoplay;
}

/// <summary>
/// Describes a gamemode.
/// </summary>
public interface IGamemodeDescriptor : IUniquelyIdentifiableObject
{
	IGame Load(ISongChart chart, in GameLoadGenericParameters parameters);
}
