using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

public class GraphView(Element parent) : BaseTimelineView(parent)
{
	public override string Name => "Graph Editor";
	public override bool LockDragDirection => false;
}
