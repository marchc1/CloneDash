using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game;

public record struct BonusCollectedGameEvent(DashEnemy Note, PathwayId Pathway) : IGameEvent;
