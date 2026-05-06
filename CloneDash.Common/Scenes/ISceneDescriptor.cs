using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;

namespace CloneDash.Common.Scenes;

public struct SceneMetadata
{
	public HumanLanguage Language;
	public string Name;
	public string Artists;
}

public interface ISceneInstance
{
	IGame GetGame();
	ISceneDescriptor GetScene();
}

public interface ISceneDescriptor : IUniquelyIdentifiableObject
{
	public static ReadOnlySpan<char> ConstructUUID(ReadOnlySpan<char> source, ReadOnlySpan<char> name) => $"scene/{source}/{name}";
	SceneMetadata FetchMetadata(in HumanLanguage desiredLanguage);
	T? CreateInGame<T>(IGame game) where T : ISceneInstance;
	bool SupportsGamemode(IGamemodeDescriptor gamemodeDescriptor);
}