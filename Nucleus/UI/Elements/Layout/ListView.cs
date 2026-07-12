using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI;

public class ListView : ScrollPanel
{
	public ListView(Element? parent) : base(parent) {
		DockPadding = RectangleF.TLRB(2);
	}
	public Element? LastSelectedElement { get; private set; } = null;
	public HashSet<Element> SelectedElements { get; private set; } = [];
	public override void Paint(float width, float height) {
		Graphics2D.SetDrawColor(20, 25, 32, 127);
		Graphics2D.DrawRectangle(0, 0, width, height);
	}
	public override void PaintBorder(float width, float height) {
		Graphics2D.SetDrawColor(85, 95, 110);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);
	}
	protected override void ChildParented(Element parent, Element child) {
		base.ChildParented(parent, child);
		child.		Dock = Dock.Top;
	}
	public override bool ShouldItemBeVisible(Element e) {
		return (e as ListViewItem).ShowLVItem;
	}
}
public class ListViewItem : Button
{
	private bool __isLVIVisible = true;

	public bool ShowLVItem {
		get => __isLVIVisible;
		set => __isLVIVisible = value;
	}
	public ListViewItem(Element? parent) : base(parent) {
		SetBgColor(new Color(0, 0, 0, 0));
		SetFgColor(new Color(0, 0, 0, 0));
		this.		Clipping = false;
	}

	protected override void OnThink() {
		if (IsDepressed() || IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
	}

	public override void PaintBackground(float width, float height) {
		if (IsDepressed()) {
			Graphics2D.SetDrawColor(30, 35, 45, 65);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		else if (IsHovered()) {
			Graphics2D.SetDrawColor(200, 210, 230, 50);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
	}
}
