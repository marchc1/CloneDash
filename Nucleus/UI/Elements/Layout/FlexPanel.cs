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
	/// Fits all elements into the FlexPanel's size on the Direction axis / Children.Count.
	/// </summary>
	StretchToFit
}

public class FlexPanel(Element? parent) : Panel(parent)
{
	public Axis Direction {
		get;
		set { if (field == value) return; field = value; InvalidateLayout(); }
	} = Axis.Horizontal;
	public FlexChildrenResizingMode ChildrenResizingMode {
		get;
		set { if (field == value) return; field = value; InvalidateLayout(); }
	} = FlexChildrenResizingMode.DoNotResize;

	protected override void ChildParented(Element parent, Element child) {
		base.ChildParented(parent, child);
		child.Dock = Dock.None;
	}

	protected override void PerformLayout(float width, float height) {
		if (ChildrenResizingMode == FlexChildrenResizingMode.DoNotResize)
			return;

		int visibleCount = 0;
		foreach (var child in Children)
			if (child.IsVisible())
				visibleCount++;

		if (visibleCount == 0)
			return;

		bool horiz = Direction == Axis.Horizontal;

		RectangleF dp = DockPadding;

		Vector2F elementSpacePosition = new(dp.Left, dp.Top);
		Vector2F elementSpaceBounds = new(width - dp.Right - dp.Left, height - dp.Bottom - dp.Top);

		int idx = 0;

		foreach (var child in Children) {
			if (!child.IsVisible())
				continue;

			float mLeft = child.DockMargin.X;
			float mTop = child.DockMargin.Y;
			float mRight = child.DockMargin.W;
			float mBot = child.DockMargin.H;

			float cx, cy, cw, ch;

			switch (ChildrenResizingMode) {
				case FlexChildrenResizingMode.StretchToFit:
					if (horiz) {
						float start = MathF.Round(elementSpacePosition.X + elementSpaceBounds.W * idx / visibleCount);
						float end = MathF.Round(elementSpacePosition.X + elementSpaceBounds.W * (idx + 1) / visibleCount);
						cx = start;
						cy = elementSpacePosition.Y;
						cw = end - start;
						ch = elementSpaceBounds.H;
					}
					else {
						float start = MathF.Round(elementSpacePosition.Y + elementSpaceBounds.H * idx / visibleCount);
						float end = MathF.Round(elementSpacePosition.Y + elementSpaceBounds.H * (idx + 1) / visibleCount);
						cx = elementSpacePosition.X;
						cy = start;
						cw = elementSpaceBounds.W;
						ch = end - start;
					}

					cx = cx + mLeft;
					cy = cy + mTop;

					cw = cw - mLeft - mRight;
					ch = ch - mTop - mBot;
					break;
				default:
					continue;
			}

			child.Position = new(cx, cy);
			child.Size = new(cw, ch);

			idx++;
		}
	}
}