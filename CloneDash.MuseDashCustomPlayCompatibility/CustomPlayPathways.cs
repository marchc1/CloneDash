using CloneDash.Common.Gamemodes.MuseDash;
using Nucleus.Common.Types;

namespace CloneDash.MuseDashCustomPlayCompatibility;

public static class CustomPlayPathways
{
	public const string MIDDLE = "Middle";

	public static readonly Color MIDDLE_COLOR = new(186, 255, 178, 120);

	public static readonly PathwayLayout Layout = new(
		PathwayLayout.MuseDash.Definitions[0],
		new PathwayDefinition(MIDDLE, CustomPlayActions.Middle, 0.5f, [new(PathwayLayout.ASSETS_MIDDLE), new(PathwayLayout.ASSETS_AIR)], MIDDLE_COLOR),
		PathwayLayout.MuseDash.Definitions[1]
	);
}
