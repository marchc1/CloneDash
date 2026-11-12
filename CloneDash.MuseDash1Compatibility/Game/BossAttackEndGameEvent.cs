using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct BossAttackEndGameEvent(string Action) : IGameEvent;
