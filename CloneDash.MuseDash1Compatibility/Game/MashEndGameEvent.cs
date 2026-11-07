using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct MashEndGameEvent(DashEnemy? Note, bool Success) : IGameEvent;
