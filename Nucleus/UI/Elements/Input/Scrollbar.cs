using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.Types;
namespace Nucleus.UI.Elements;

public enum ScrollbarAlignment
{
	Horizontal,
	Vertical
}

public class Scrollbar : Panel
{
	// TODO: just use images... in fact, should have an ImageButton, even
	internal class ScrollbarButton : Button
	{
		Scrollbar scrollbar;
		public ScrollbarButton(Scrollbar scrollbar) : base(scrollbar, text: "") {
			this.scrollbar = scrollbar;
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
			BorderSize = 0;
		}
		public override void Paint(float width, float height) {
			var fore = MixColorBasedOnMouseState(this, GetTextColor(), new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
			var down = this == scrollbar.Down;

			Graphics2D.SetDrawColor(fore, IsHovered() ? 220 : 200);
			Graphics2D.SetTexture(scrollbar.Alignment == ScrollbarAlignment.Vertical ?
				(ITexture)(down ? Level.Textures.LoadTextureFromFile("ui/down32.png") : Level.Textures.LoadTextureFromFile("ui/up32.png")) :
				(ITexture)(down ? Level.Textures.LoadTextureFromFile("ui/right32.png") : Level.Textures.LoadTextureFromFile("ui/left32.png")));
			Graphics2D.DrawImage(new(2), new(width - 4, height - 4));
		}
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) => scrollbar.MouseScrolled(self, state, delta);
	}
	internal class ScrollbarGrip : Button
	{
		Scrollbar scrollbar;
		public ScrollbarGrip(Scrollbar scrollbar) : base(scrollbar, text: "") {
			this.scrollbar = scrollbar;
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
			BorderSize = 0;
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			// Remap the new mouse pos
			var map = state.Mouse.MousePos - self.GetGlobalPosition();
			//Console.WriteLine(map);
			var newScroll = (float)NMath.Remap(
				scrollbar.Alignment == ScrollbarAlignment.Horizontal ? map.X : map.Y,
				0, scrollbar.Alignment == ScrollbarAlignment.Horizontal ? self.RenderBounds.W : self.RenderBounds.H,
				0, scrollbar.MaxScroll
				);

			scrollbar.Scroll = newScroll;
			return true;
		}
		public override void Paint(float width, float height) {
			var fore = MixColorBasedOnMouseState(this, GetTextColor(), new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
			var gripThickness = 4;
			Graphics2D.SetDrawColor(fore, 200);

			var gripMinSize = 16;
			var gripSize = Math.Max((scrollbar.Alignment == ScrollbarAlignment.Vertical ? height : width) / scrollbar.GetOverflow(), gripMinSize);

			// Scrollbar height calculation
			if (scrollbar.Alignment == ScrollbarAlignment.Vertical)
				Graphics2D.DrawRectangle(
					(width / 2) - (gripThickness / 2),
					(float)NMath.Remap(scrollbar.Scroll, 0, scrollbar.MaxScroll, 0, height - (height / scrollbar.GetOverflow())),
					gripThickness,
					gripSize);
			else
				Graphics2D.DrawRectangle(
					(float)NMath.Remap(scrollbar.Scroll, 0, scrollbar.MaxScroll, 0, width - (width / scrollbar.GetOverflow())),
					(height / 2) - (gripThickness / 2),
					gripSize,
					gripThickness);
		}
	}
	public float ScrollbarSize { get; set; } = 8;

	internal ScrollbarButton Up { get; set; }
	internal ScrollbarButton Down { get; set; }
	internal ScrollbarGrip Grip { get; set; }

	private float _scroll, _pageSize;

	public Vector2F PageContents { get; set; }
	public Vector2F PageSize { get; set; }

	public delegate void OnScrolledDelegate(float value);
	public event OnScrolledDelegate? OnScrolled;

	public float Scroll {
		get => _scroll;
		set {
			_scroll = value;
			OnScrolled?.Invoke(value);
			ValidateScroll();
		}
	}

	public float MaxScroll => Math.Max(
		Alignment == ScrollbarAlignment.Horizontal ? PageContents.W - PageSize.W : PageContents.H - PageSize.H, 0);

	public void ValidateScroll() {
		_scroll = Math.Clamp(_scroll, 0, MaxScroll);
	}

	private ScrollbarAlignment __alignment = ScrollbarAlignment.Vertical;
	public ScrollbarAlignment Alignment {
		get {
			return __alignment;
		}
		set {
			__alignment = value;
			if (Dock == Dock.None)
				Dock = value == ScrollbarAlignment.Vertical ? Dock.Right : Dock.Bottom;
		}
	}
	protected override void PerformLayout(float width, float height) {
		if (Alignment == ScrollbarAlignment.Vertical) {
			Up.Dock = Dock.Top;
			Down.Dock = Dock.Bottom;
		}
		else {
			Up.Dock = Dock.Left;
			Down.Dock = Dock.Right;
		}
	}
	public Scrollbar(Element? parent) : base(parent) {
		this.SetSize(new(18, 18));

		Up = new ScrollbarButton(this);
		Down = new ScrollbarButton(this);
		Grip = new ScrollbarGrip(this);

		Up.SetSize(new(18, 18));
		Down.SetSize(new(18, 18));

		Up.Dock = Dock.Top;
		Down.Dock = Dock.Bottom;
		Grip.Dock = Dock.Fill;

		SetVisible(false);
		SetPaintBackgroundEnabled(false);
	}

	internal bool MouseScrolled(Element self, FrameState state, Vector2F delta) {
		Scroll += delta.Y * -ScrollDelta;
		return true;
	}
	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) => MouseScrolled(self, state, delta);

	public float ScrollDelta { get; set; } = 30;

	public bool ShouldShow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W > PageSize.W : PageContents.H > PageSize.H;
	public float GetOverflow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W / PageSize.W : PageContents.H / PageSize.H;

	public void Update(Vector2F contents, Vector2F size) {
		PageContents = contents;
		PageSize = size;

		var overflowing = contents.Y - size.Y;
		if (Scroll > overflowing && Scroll > 0) {
			Scroll = Math.Max(0, overflowing);
		}

		SetVisible(ShouldShow());
	}
}
