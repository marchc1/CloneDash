using CloneDash.Common;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;

namespace CloneDash.Common.Scenes;

public struct SceneMetadata
{
	public string Name;
	public string Artists;
}

public interface ISceneInstance
{
	RhythmGameLevel GetGame();
	ISceneDescriptor GetScene();
}

public interface ISceneDescriptor : ICloneDashModDescriptor
{
	SceneMetadata FetchMetadata();
	T? CreateInGame<T>(RhythmGameLevel game) where T : ISceneInstance;
	bool SupportsGamemode(IGamemodeDescriptor gamemodeDescriptor);
}