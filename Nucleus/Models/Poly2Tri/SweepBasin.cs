using Nucleus.Common.Util;

namespace Poly2Tri;

internal class SweepBasin : IPoolableObject
{
	public Node? LeftNode;
	public Node? BottomNode;
	public Node? RightNode;
	public double Width;
	public bool LeftHighest;

	public void Clear() {
		LeftNode = null;
		BottomNode = null;
		RightNode = null;
		Width = 0;
		LeftHighest = false;
	}

	public void Init() => Clear();
	public void Reset() => Clear();
}
