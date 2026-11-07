using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game;

public record struct CharacterSkillGameEvent(PathwayId Pathway) : IGameEvent;
