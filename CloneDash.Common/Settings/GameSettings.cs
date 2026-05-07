using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Settings;

public enum DeadEntityVisibility {
UseGamemodeDefaults = -1,
	FullyVisible = 0,
	Dimmed = 1,
	Invisible = 2
}
[MarkForStaticConstruction]
public static class GameSettings
{
	public static ConVar r_deadentityvisibility = new(nameof(r_deadentityvisibility), (int)DeadEntityVisibility.UseGamemodeDefaults, FCvar.Saved, "Controls the visibility of entities post-death, in applicable gamemodes with applicable entities. -1 == gamemode default, 0 == fully visible, 1 == dimmed, and 2 == invisible", -1, 2);
	public static DeadEntityVisibility DeadEntityVisibility => (DeadEntityVisibility)r_deadentityvisibility.GetInt();
}