using CloneDash.Common.Compatibility;
using CloneDash.Common.Gamemodes;

namespace CloneDash.Common.Game;

public interface IContentSource : ICloneDashModDescriptor
{
	IGamemodeDescriptor GetGamemode();
	Type GetRuntimeType();
}
