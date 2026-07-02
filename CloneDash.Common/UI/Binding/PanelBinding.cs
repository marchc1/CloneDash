using Nucleus.Common.Input;

namespace CloneDash.Common.UI.Binding
{
	public class PanelBinding
	{
		public string Label { get; }
		public List<(ButtonCode[] buttons, Action action)> Bindings { get; }

		public PanelBinding(string label, params List<(ButtonCode[] buttons, Action action)> bindings) {
			Label = label;
			Bindings = bindings;
		}
	}
}