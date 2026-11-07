using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct WarningStartGameEvent(DashEnemy Note) : IGameEvent;
