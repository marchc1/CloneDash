using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class MiscSettings
{
    public static ConVar LastSelected = new("ss_last_selected", "0-0", FCvar.Saved, "The last picked song ID.");
}