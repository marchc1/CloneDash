using CloneDash.Common.Game;

namespace CloneDash.Scenes;

public record struct SceneChangedGameEvent(IMuseDash1SceneInstance? From, IMuseDash1SceneInstance? To) : IGameEvent;
