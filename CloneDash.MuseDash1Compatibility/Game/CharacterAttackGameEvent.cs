using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Input;

namespace CloneDash.Game;

public record struct CharacterAttackGameEvent(PathwayId Pathway, bool Air, PollResult Result) : IGameEvent;
