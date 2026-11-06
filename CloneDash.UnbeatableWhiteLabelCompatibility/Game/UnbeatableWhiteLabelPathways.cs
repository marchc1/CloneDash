using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Settings;
using CloneDash.Unbeatable.Internal;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public static class UnbeatableWhiteLabelPathways
{
	public const float HEIGHT_OFFSET = 3f;
	public const float PLAYER_REST_X = 0.9f;
	public const float SPAWN_X = 15f;
	public const float TARGET_X = 1.9f;

	public static readonly PathwayId Top = new(0);
	public static readonly PathwayId Mid = new(1);
	public static readonly PathwayId Low = new(2);

	public static readonly PathwayLayout Layout = new(
		new PathwayDefinition("Top", UnbeatableWhiteLabelActions.Top, 1, [], new(255, 140, 170, 255)),
		new PathwayDefinition("Mid", UnbeatableWhiteLabelActions.Mid, 0.5f, [], new(255, 210, 120, 255)),
		new PathwayDefinition("Low", UnbeatableWhiteLabelActions.Low, 0, [], new(130, 200, 255, 255))
	);

	public static PathwayId FromHeight(Height height) => height switch {
		Height.Top => Top,
		Height.Mid => Mid,
		_ => Low
	};

	public static float GetPathwayY(PathwayId pathway) => 0.5f + HEIGHT_OFFSET * Layout[pathway].NormalizedY;

	public static PathwayId Opposite(PathwayId pathway) => pathway == Top ? Low : pathway == Low ? Top : pathway;
}
