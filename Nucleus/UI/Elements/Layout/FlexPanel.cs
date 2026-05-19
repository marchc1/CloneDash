using Nucleus.Types;

namespace Nucleus.UI;

/// <summary>
/// FlexPanel's resizing mode. Todo: better explanations
/// </summary>
public enum FlexChildrenResizingMode
{
	/// <summary>
	/// Does not perform a resizing operation on the children of the FlexPanel.
	/// </summary>
	DoNotResize,
	/// <summary>
	/// Fits the opposite direction (opposite to FlexDirection) to match the FlexPanel's size on that axis.
	/// </summary>
	FitToOppositeDirection,
	/// <summary>
	/// Stretches the opposite direction (opposite to FlexDirection)  to match the FlexPanel's size on the opposite axis.
	/// </summary>
	StretchToOppositeDirection,
	/// <summary>
	/// Fits all elements into the FlexPanel's size on the Direction axis / Children.Count.
	/// </summary>
	StretchToFit
}

public class FlexPanel(Element? parent) : Panel(parent)
{
	public Directional180 Direction { get; set; } = Directional180.Horizontal;
	public FlexChildrenResizingMode ChildrenResizingMode { get; set; } = FlexChildrenResizingMode.DoNotResize;

	public float Spacing { get; set; } = 0;

	protected override void ChildParented(Element parent, Element child) {
		base.ChildParented(parent, child);
		child.Dock = Dock.None;
	}

	protected override void PerformLayout(float width, float height) {
		int visibleCount = 0;
		foreach (var child in Children)
			if (child.IsVisible())
				visibleCount++;

		if (visibleCount == 0)
			return;

		bool horiz = Direction == Directional180.Horizontal;

		var dp = DockPadding;
		float ax = dp.X;
		float ay = dp.Y;
		float aw = width - dp.X - dp.W;
		float ah = height - dp.Y - dp.H;

		float totalSpacing = Spacing * (visibleCount - 1);

		float slotSize = horiz ? (aw - totalSpacing) / visibleCount : (ah - totalSpacing) / visibleCount;

		int idx = 0;
		float cursor = 0;

		foreach (var child in Children) {
			if (!child.IsVisible())
				continue;

			float mLeft = child.DockMargin.X;
			float mTop = child.DockMargin.Y;
			float mRight = child.DockMargin.W;
			float mBot = child.DockMargin.H;

			Vector2F naturalSize = child.DynamicallySized
				? child.Size * new Vector2F(width, height)
				: child.Size;

			float cx, cy, cw, ch;

			switch (ChildrenResizingMode) {
				case FlexChildrenResizingMode.StretchToFit: {
						if (horiz) {
							cx = ax + cursor + mLeft;
							cy = ay + mTop;
							cw = slotSize - mLeft - mRight;
							ch = ah - mTop - mBot;
							cursor += slotSize + Spacing;
						}
						else {
							cx = ax + mLeft;
							cy = ay + cursor + mTop;
							cw = aw - mLeft - mRight;
							ch = slotSize - mTop - mBot;
							cursor += slotSize + Spacing;
						}
						break;
					}

				case FlexChildrenResizingMode.FitToOppositeDirection: {
						if (horiz) {
							float childW = naturalSize.W;
							cx = ax + cursor + mLeft;
							cy = ay + mTop;
							cw = childW;
							ch = ah - mTop - mBot;
							cursor += childW + mLeft + mRight + Spacing;
						}
						else {
							float childH = naturalSize.H;
							cx = ax + mLeft;
							cy = ay + cursor + mTop;
							cw = aw - mLeft - mRight;
							ch = childH;
							cursor += childH + mTop + mBot + Spacing;
						}
						break;
					}

				case FlexChildrenResizingMode.StretchToOppositeDirection: {
						float sq = horiz ? (ah - mTop - mBot) : (aw - mLeft - mRight);
						if (horiz) {
							cx = ax + cursor + mLeft;
							cy = ay + mTop;
							cw = sq;
							ch = sq;
							cursor += sq + mLeft + mRight + Spacing;
						}
						else {
							cx = ax + mLeft;
							cy = ay + cursor + mTop;
							cw = sq;
							ch = sq;
							cursor += sq + mTop + mBot + Spacing;
						}
						break;
					}

				default: {
						float childW = naturalSize.W;
						float childH = naturalSize.H;
						if (horiz) {
							float slotCenter = ax + cursor + slotSize / 2;
							cx = slotCenter - childW / 2;
							cy = ay + (ah - childH) / 2;
							cw = childW;
							ch = childH;
							cursor += slotSize + Spacing;
						}
						else {
							float slotCenter = ay + cursor + slotSize / 2;
							cx = ax + (aw - childW) / 2;
							cy = slotCenter - childH / 2;
							cw = childW;
							ch = childH;
							cursor += slotSize + Spacing;
						}
						break;
					}
			}

			LayoutChild(child, new RectangleF(cx, cy, cw, ch));

			idx++;
		}
	}
}