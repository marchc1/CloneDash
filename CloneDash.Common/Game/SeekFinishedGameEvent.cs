namespace CloneDash.Common.Game;

public record struct SeekFinishedGameEvent(double Time) : IGameEvent;
