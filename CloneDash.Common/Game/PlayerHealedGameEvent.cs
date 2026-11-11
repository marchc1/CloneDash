namespace CloneDash.Common.Game;

public record struct PlayerHealedGameEvent(float Health) : IGameEvent;
