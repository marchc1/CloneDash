
using Nucleus;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class OffsetSettings
{
	public static ConVar clonedash_visualoffset = ConVar.Register(nameof(clonedash_visualoffset), 0, ConsoleFlags.Saved);
	public static ConVar clonedash_judgementoffset = ConVar.Register(nameof(clonedash_judgementoffset), 0, ConsoleFlags.Saved);

	public static double VisualOffset => clonedash_visualoffset.GetDouble();
	public static double JudgementOffset => clonedash_judgementoffset.GetDouble();
}