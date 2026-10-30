using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

namespace Nucleus.UI;

public class Panel : Element
{
	/// <summary>
	/// How many levels of nested panels get darker
	/// </summary>
	public const int MAX_INSET_DEPTH = 3;

	/// <summary>
	/// How much darker nested panels are than the default background
	/// </summary>
	public const float INSET_DARKEN = 0.4f;

	public Panel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
		DockPadding = RectangleF.TLRB(2);
		AddFlag(ElementFlags.PaintBackgroundEnabled | ElementFlags.PaintBorderEnabled | ElementFlags.PaintEnabled);
	}

	/// <summary>
	/// How many ancestors paint a background behind this
	/// </summary>
	public int GetInsetDepth() {
		int depth = 0;
		for (Element? ancestor = GetParent(); ancestor != null && depth <= MAX_INSET_DEPTH; ancestor = ancestor.GetParent()) {
			if (ancestor.IsPaintBackgroundEnabled() && ancestor.GetBgColor().A > 0)
				depth++;
		}
		return depth;
	}

	public override void PaintBackground(float width, float height) {
		// Custom colors and top level panels draw as normal. Nested ones are darker.
		if (HasUserBgColor || Roundness > 0) {
			base.PaintBackground(width, height);
			return;
		}

		int depth = GetInsetDepth();
		if (depth == 0) {
			base.PaintBackground(width, height);
			return;
		}
		if (depth > MAX_INSET_DEPTH)
			return;

		Graphics2D.SetDrawColor(GetBgColor().Adjust(0, 0, -INSET_DARKEN));
		Graphics2D.DrawRectangle(0, 0, width, height);
	}

	public override void Paint(float width, float height) {
	}
}
