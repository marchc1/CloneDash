using Nucleus.Common.Input;
using Nucleus.Types;

namespace Nucleus.UI.Elements;

public interface IContainsNodes
{
	public TreeNode AddNode(string text);
}
public class TreeNode : Button, IContainsNodes
{
	DirectionalLayoutPanel ChildrenPanel;
	public TreeNode(Element? parent) : base(parent) {
		ChildrenPanel = new(GetParent());
		ChildrenPanel.AutoSize = true;
		ChildrenPanel.SizeChildrensOppositeSideToEdge = true;
		ChildrenPanel.BorderSize = 0;
		ChildrenPanel.SetPaintBackgroundEnabled(false);
		ChildrenPanel.SetPaintEnabled(false);
		ChildrenPanel.SetVisible(false);
		ChildrenPanel.SetSize(new(0, 0));
		ChildrenPanel.DockPadding = RectangleF.TLRB(0, 8, 0, 0);

		SetTextAlignment(Anchor.CenterLeft);
		SetTextPadding(new(8));
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
				ChildrenPanel.SetVisible(value);

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
		TreeNode node = new TreeNode(ChildrenPanel);
		node.SetText(text);
		Nodes.Add(node);
		return node;
	}

	DateTime LastRelease;
	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		base.MouseRelease(self, state, button);

		if ((DateTime.UtcNow - LastRelease).TotalSeconds < 0.3333f) {
			ToggleExpanded();
			LastRelease = DateTime.MinValue;
		}
		else
			LastRelease = DateTime.UtcNow;
		return true;
	}
}
public class TreeView : DirectionalLayoutPanel, IContainsNodes
{
	public TreeView(Element? parent) : base(parent) {
		SizeChildrensOppositeSideToEdge = true;
	}

	public List<TreeNode> Nodes { get; set; } = [];
	public TreeNode AddNode(string text) {
		TreeNode node = new TreeNode(this);
		node.SetText(text);
		Nodes.Add(node);
		return node;
	}
}
