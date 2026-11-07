using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct BossAttackChangeGameEvent(string Action) : IGameEvent;
