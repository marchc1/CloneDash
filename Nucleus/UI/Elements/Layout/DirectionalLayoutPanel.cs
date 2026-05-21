using Nucleus.Types;

namespace Nucleus.UI.Elements;

/// <summary>
/// Lays out all children in order, depending on direction.
/// </summary>
public class DirectionalLayoutPanel : ScrollPanel
{
	public DirectionalLayoutPanel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
		MainPanel.ChildDock = Dock.Top;
	}
	private Axis direction = Axis.Vertical;
	public Axis Direction {
		get => direction;
		set {
			direction = value;
			InvalidateLayout();
		}
	}

	public float WidthOffset { get; set; } = 0;

	private float padding = 4;
	public float Padding {
		get => padding;
		set {
			padding = value;
			InvalidateLayout();
		}
	}

	private bool sizetoedge = false;
	// big name
	public bool SizeChildrensOppositeSideToEdge {
		get => sizetoedge;
		set {
			sizetoedge = value;
			InvalidateLayout();
		}
	}

	private bool autosize = false;
	public bool AutoSize {
		get => autosize;
		set {
			autosize = value;
			InvalidateLayout();
		}
	}

	protected override void OnThink() {
		base.OnThink();
		if (AutoSize) {
			float size = 0;
			foreach (var child in GetAddParent().Children)
				size = MathF.Max(size, child.GetRenderBounds().Y + child.GetRenderBounds().H + 8);
			SetSize(new(GetSize().W, size + 8));
			MainPanel.SetSize(new(MainPanel.GetSize().W, size + 8));

			VerticalScrollbar.SetVisible(false);
			HorizontalScrollbar.SetVisible(false);
		}
	}
}
