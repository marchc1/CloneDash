using Nucleus.Common.Util;

namespace Poly2Tri;

internal struct SweepBasin
{
	public Node? LeftNode;
	public Node? BottomNode;
	public Node? RightNode;
	public double Width;
	public bool LeftHighest;
}
