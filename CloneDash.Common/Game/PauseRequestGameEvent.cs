namespace CloneDash.Common.Game;

public record struct PauseRequestGameEvent(bool Paused, bool FromMenu, bool Cancel) : IGameEvent;
