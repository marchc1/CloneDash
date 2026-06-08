using CloneDash.Common.Gamemodes.MuseDash;
using Nucleus.Common.Types;

namespace CloneDash.Game;

public static class PathwayExts {
	public static readonly Color PATHWAY_TOP_COLOR = new Color(178, 255, 252, 120);
	public static readonly Color PATHWAY_BOTTOM_COLOR = new Color(248, 178, 255, 120);
	public static readonly Color PATHWAY_DUAL_COLOR = new Color(220, 160, 140, 255);

	public static Color GetColor(this PathwaySide side, int alpha = -1) {
		var c = side == PathwaySide.Top ? PATHWAY_TOP_COLOR : PATHWAY_BOTTOM_COLOR;
		return new(c.R, c.G, c.B, alpha == -1 ? c.A : alpha);
	}
}
