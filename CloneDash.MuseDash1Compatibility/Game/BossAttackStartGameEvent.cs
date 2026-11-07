using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct BossAttackStartGameEvent(string Action) : IGameEvent;
