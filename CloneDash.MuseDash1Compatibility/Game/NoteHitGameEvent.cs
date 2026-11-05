using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;

namespace CloneDash.Game;

public record struct NoteHitGameEvent(DashEnemy Note, PathwayId Pathway, bool Perfect, double Precision, bool InFever, EarlyLate EarlyLate) : IGameEvent;
