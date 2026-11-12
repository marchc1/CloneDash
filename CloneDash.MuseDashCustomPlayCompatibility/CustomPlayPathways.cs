using CloneDash.Common.Gamemodes.MuseDash;
using Nucleus;
using Nucleus.Common.Types;

namespace CloneDash.MuseDashCustomPlayCompatibility;

[MarkForStaticConstruction]
public static class CustomPlayPathways
{
	public const string MIDDLE = "Middle";
	public const float MIDDLE_HUE_SHIFT = -0.18f;

	public static readonly Color MIDDLE_COLOR = new(186, 255, 178, 120);

	public static readonly PathwayLayout Layout = new(
		PathwayLayout.MuseDash.Definitions[0],
		new PathwayDefinition(MIDDLE, CustomPlayActions.Middle, 0.5f, [new(PathwayLayout.ASSETS_MIDDLE), new(PathwayLayout.ASSETS_AIR, MIDDLE_HUE_SHIFT)], MIDDLE_COLOR),
		PathwayLayout.MuseDash.Definitions[1]
	);

	static CustomPlayPathways() {
		PathwayLayout.Register(Layout);
	}
}
