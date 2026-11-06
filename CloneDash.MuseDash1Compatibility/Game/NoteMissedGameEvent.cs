using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct NoteMissedGameEvent(DashEnemy Note) : IGameEvent;
