namespace CloneDash.Common.Gamemodes.MuseDash;

public class PathwaysComponent(PathwayLayout layout)
{
	public PathwaySet AirSidePathways {
		get {
			var set = PathwaySet.None;
			for (int i = 0; i < Layout.Count; i++)
				if (Layout.IsAirSide(Layout.PathwayAt(i)))
					set = set.With(Layout.PathwayAt(i));
			return set;
		}
	}

	public PathwayId GroundPathway => Layout.PathwayAt(Layout.Count - 1);

	public PathwaySet GroundSidePathways => Layout.All & PathwaySet.FromBits(~AirSidePathways.Bits);

	public PathwayId JumpPathway => Layout.PathwayAt(0);

	public PathwayLayout Layout { get; } = layout;

	public PathwayId PrimaryPathway(PathwaySet pathways) {
		var clamped = Layout.Clamp(pathways);
		return clamped.Count == 1 ? clamped.First : GroundPathway;
	}
}
