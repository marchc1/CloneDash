using Nucleus.Types;

namespace Nucleus.UI.Elements;

public enum DrawSizePreservationStrategy
{
	Minimum,
	Maximum,
	Average,
	Separate
}

public class DrawSizePreservingFillContainer : Element
{
	private readonly Element content;
	private Vector2F targetDrawSize = new(1600, 900);
	private DrawSizePreservationStrategy strategy = DrawSizePreservationStrategy.Minimum;

	public Element Content => content;

	public Vector2F TargetDrawSize {
		get => targetDrawSize;
		set {
			if (targetDrawSize == value)
				return;

			targetDrawSize = value;
			InvalidateLayout();
		}
	}

	public DrawSizePreservationStrategy Strategy {
		get => strategy;
		set {
			if (strategy == value)
				return;

			strategy = value;
			InvalidateLayout();
		}
	}

	public DrawSizePreservingFillContainer(Element? parent) : base(parent) {
		Dock = Dock.Fill;
		Clipping = false;
		SetPassthru(true);

		content = new Element(this);
		content.Dock = Dock.Fill;
		content.Clipping = false;
		content.SetPassthru(true);
		SetAddParent(content);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		Vector2F size = GetRenderBounds().Size;
		if (size.W <= 0 || size.H <= 0 || targetDrawSize.W <= 0 || targetDrawSize.H <= 0)
			return;

		Vector2F ratio = size / targetDrawSize;
		ChildScale = strategy switch {
			DrawSizePreservationStrategy.Minimum => new(MathF.Min(ratio.X, ratio.Y)),
			DrawSizePreservationStrategy.Maximum => new(MathF.Max(ratio.X, ratio.Y)),
			DrawSizePreservationStrategy.Average => new((ratio.X + ratio.Y) / 2),
			_ => ratio
		};
	}
}
