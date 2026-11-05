using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;

namespace CloneDash.Game;

public record struct SustainStateChangedGameEvent(SustainBeam Sustain, PathwayId Pathway, bool WasSustainingBefore, bool IsSustainingNow, int SustainCount) : IGameEvent;
