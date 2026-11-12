namespace CloneDash.Common.Game;

public record struct FeverStartGameEvent(double Duration) : IGameEvent;
