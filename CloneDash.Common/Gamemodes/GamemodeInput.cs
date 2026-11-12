using CloneDash.Game.Input;
using Nucleus.Commands;
using Nucleus.Common.Types;

namespace CloneDash.Common.Gamemodes;

public sealed record GamemodeInput(string Name, string Set, InputAction Action, Color Color);

public sealed record GamemodeInputSwitch(string Name, ConVar ConVar);
