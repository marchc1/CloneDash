namespace CloneDash.Common.Game;

public record struct FeverChangedGameEvent(float Value, float Max) : IGameEvent;
