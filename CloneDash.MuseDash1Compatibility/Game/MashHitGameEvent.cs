using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct MashHitGameEvent(DashEnemy Note) : IGameEvent;
