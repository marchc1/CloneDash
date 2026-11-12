namespace CloneDash.Common.Game;

public record struct ScoreChangedGameEvent(int Score) : IGameEvent;
