namespace CloneDash.Common.Game;

public record struct PlayerDamagedGameEvent(bool Hurt) : IGameEvent;
