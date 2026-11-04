using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Songs;
using Nucleus.Common.Types;

namespace CloneDash.Common.Gamemodes;

public interface IGamemodeProvider : ICloneDashModProvider<IGamemodeDescriptor> {

}

/// <summary>
/// Describes a gamemode.
/// </summary>
public interface IGamemodeDescriptor : ICloneDashModDescriptor
{
	RhythmGameLevel Load(ISongChart chart, RhythmGameLoadParameters parms);
}
