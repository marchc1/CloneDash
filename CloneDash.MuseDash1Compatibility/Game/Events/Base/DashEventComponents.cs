using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;
using CloneDash.Scenes;

namespace CloneDash.Game;

public sealed record DashEventComponents(Boss Boss, MuseDash1ChartComponent Chart, ScreenEffectsComponent Effects, PathwaysComponent Pathways, MuseDash1ScenesComponent Scenes);
