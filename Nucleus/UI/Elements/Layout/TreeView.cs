using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI.Elements;

public interface IContainsNodes
{
	public TreeNode AddNode(string text);
}
public class TreeNode : Button, IContainsNodes
{
	public const float CHEVRON_SIZE = 16;
	public const float INDENT = 16;

	DirectionalLayoutPanel ChildrenPanel;
	public TreeNode(Element? parent) : base(parent) {
		ChildrenPanel = new(GetParent());
		ChildrenPanel.AutoSize = true;
		ChildrenPanel.SizeChildrensOppositeSideToEdge = true;
		ChildrenPanel.BorderSize = 0;
		ChildrenPanel.SetPaintBackgroundEnabled(false);
		ChildrenPanel.SetPaintBorderEnabled(false);
		ChildrenPanel.SetPaintEnabled(false);
		ChildrenPanel.SetVisible(false);
		ChildrenPanel.		Size = new(0, 0);
		ChildrenPanel.DockPadding = RectangleF.TLRB(0, INDENT, 0, 0);

		SetPaintBorderEnabled(false);
		SetTextAlignment(Anchor.CenterLeft);
		SetTextPadding(new(4 + CHEVRON_SIZE, 0));
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
		node.		Text = text;
		Nodes.Add(node);
		return node;
	}

	public TreeView? Tree {
		get {
			for (Element? element = GetParent(); element != null; element = element.GetParent()) {
				if (element is TreeView tree)
					return tree;
			}
			return null;
		}
	}

	public bool Selected => Tree?.SelectedNode == this;

	SchemeableSetting<Color> hoveredColor = SchemeableSetting<Color>.Default(new(200, 210, 230, 50));
	SchemeableSetting<Color> selectedColor = SchemeableSetting<Color>.Default(new(200, 210, 230, 30));

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		hoveredColor.SetSchemeValue(scheme.GetColor("Nucleus.ListViewItem.Hovered", new(200, 210, 230, 50)));
		selectedColor.SetSchemeValue(scheme.GetColor("Nucleus.ListViewItem.Selected", new(200, 210, 230, 30)));
	}

	public override void PaintBackground(float width, float height) {
		if (IsHovered()) {
			Graphics2D.SetDrawColor(hoveredColor.Get());
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		else if (Selected) {
			Graphics2D.SetDrawColor(selectedColor.Get());
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		if (Selected) {
			Graphics2D.SetDrawColor(GetTextColor());
			Graphics2D.DrawRectangle(0, 4, 2, height - 8);
		}

		if (Nodes.Count == 0)
			return;

		// Chevron, pointing right when collapsed and down when expanded
		Graphics2D.SetDrawColor(GetTextColor());
		float cx = 4 + CHEVRON_SIZE / 2, cy = MathF.Round(height / 2);
		if (expanded) {
			Graphics2D.DrawLine(new Vector2F(cx - 4, cy - 2), new Vector2F(cx, cy + 2), 1.5f);
			Graphics2D.DrawLine(new Vector2F(cx, cy + 2), new Vector2F(cx + 4, cy - 2), 1.5f);
		}
		else {
			Graphics2D.DrawLine(new Vector2F(cx - 2, cy - 4), new Vector2F(cx + 2, cy), 1.5f);
			Graphics2D.DrawLine(new Vector2F(cx + 2, cy), new Vector2F(cx - 2, cy + 4), 1.5f);
		}
	}

	DateTime LastRelease;
	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		base.MouseRelease(self, state, button);

		bool onChevron = Nodes.Count > 0 && GetMousePos().X < 4 + CHEVRON_SIZE + 4;
		if (onChevron) {
			ToggleExpanded();
			LastRelease = DateTime.MinValue;
			return true;
		}

		Tree?.SelectNode(this);

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
		node.		Text = text;
		Nodes.Add(node);
		return node;
	}

	public TreeNode? SelectedNode { get; private set; }
	public event Action<TreeView, TreeNode?>? OnNodeSelected;

	public void SelectNode(TreeNode? node) {
		if (SelectedNode == node) return;
		SelectedNode = node;
		OnNodeSelected?.Invoke(this, node);
	}
}
