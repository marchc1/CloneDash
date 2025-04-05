using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor;

public class WeightsPanel : Panel {
	PropertiesPanel props;
	protected override void Initialize() {
		Add(out props);
		props.Dock = Dock.Fill;
		props.DockMargin = RectangleF.TLRB(4);
	}
}
