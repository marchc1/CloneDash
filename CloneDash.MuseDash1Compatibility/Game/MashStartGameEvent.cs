using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct MashStartGameEvent(DashEnemy Note) : IGameEvent;
