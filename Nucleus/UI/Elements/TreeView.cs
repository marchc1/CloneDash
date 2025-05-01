using Nucleus.Input;
using Nucleus.Types;

namespace Nucleus.UI.Elements
{
	public interface IContainsNodes
	{
		public TreeNode AddNode(string text);
	}
	public class TreeNode : Button, IContainsNodes
	{
		DirectionalLayoutPanel ChildrenPanel;
		protected override void Initialize() {
			base.Initialize();
			Parent.Add(out ChildrenPanel);
			ChildrenPanel.AutoSize = true;
			ChildrenPanel.SizeChildrensOppositeSideToEdge = true;
			ChildrenPanel.BorderSize = 0;
			ChildrenPanel.PaintOverride += (_, _, _) => { };
			ChildrenPanel.Visible = false;
			ChildrenPanel.Enabled = false;
			ChildrenPanel.Size = new(0, 0);
			ChildrenPanel.DockPadding = RectangleF.TLRB(0, 8, 0, 0);

			TextAlignment = Anchor.CenterLeft;
			TextPadding = new(8);
		}

		private bool expanded = false;

		public delegate void ExpansionStateChanged(bool expanded);

		public event ExpansionStateChanged? OnExpanded;
		public event ExpansionStateChanged? OnCollapsed;
		public event ExpansionStateChanged? OnExpandToggled;

		public bool Expanded {
			get => expanded;
			set {
				if (expanded != value) {
					expanded = value;
					ChildrenPanel.Visible = value;
					ChildrenPanel.Enabled = value;

					OnExpandToggled?.Invoke(expanded);
					if (expanded) OnExpanded?.Invoke(expanded);
					else OnCollapsed?.Invoke(expanded);
				}
			}
		}
		public void Expand() => Expanded = true;
		public void Collapse() => Expanded = false;
		public void ToggleExpanded() => Expanded = !Expanded;

		public List<TreeNode> Nodes { get; set; } = [];
		public TreeNode AddNode(string text) {
			TreeNode node = ChildrenPanel.Add<TreeNode>();
			node.Text = text;
			Nodes.Add(node);
			return node;
		}

		DateTime LastRelease;
		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			base.MouseRelease(self, state, button);

			if ((DateTime.UtcNow - LastRelease).TotalSeconds < 0.3333f) {
				ToggleExpanded();
				LastRelease = DateTime.MinValue;
			}
			else
				LastRelease = DateTime.UtcNow;
		}
	}
	public class TreeView : DirectionalLayoutPanel, IContainsNodes
	{
		protected override void Initialize() {
			base.Initialize();
			SizeChildrensOppositeSideToEdge = true;
		}

		public List<TreeNode> Nodes { get; set; } = [];
		public TreeNode AddNode(string text) {
			TreeNode node = Add<TreeNode>();
			node.Text = text;
			Nodes.Add(node);
			return node;
		}
	}
}
