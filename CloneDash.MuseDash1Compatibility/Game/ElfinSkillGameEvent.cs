using CloneDash.Common.Game;

namespace CloneDash.Game;

public record struct ElfinSkillGameEvent(string Effect, float? X = null, float? Y = null) : IGameEvent;