using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Statistics;
using CloneDash.Scenes;

namespace CloneDash.Game;

public sealed record DashEnemyComponents(MuseDash1CharacterComponent Character, MuseDash1ChartComponent Chart, ComboComponent Combo, Conductor Conductor, HealthComponent Health, MuseDash1HudComponent Hud, MuseDash1JudgeComponent Judge, MashComponent Mash, INoteRules<DashEnemy> NoteRules, PathwaysComponent Pathways, MuseDash1PlayfieldComponent Playfield, MuseDash1RulesComponent Rules, MuseDash1ScenesComponent Scenes, ScrollComponent Scroll, StatisticsData Stats);
