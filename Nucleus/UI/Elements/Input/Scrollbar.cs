using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
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
				(ITexture)(down ? textures.LoadTextureFromFile("ui/down32.png") : textures.LoadTextureFromFile("ui/up32.png")) :
				(ITexture)(down ? textures.LoadTextureFromFile("ui/right32.png") : textures.LoadTextureFromFile("ui/left32.png")));
			Graphics2D.DrawTexturedRectangle(new Vector2F(2), new Vector2F(width - 4, height - 4));
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
		bool Vertical => scrollbar.Alignment == ScrollbarAlignment.Vertical;
		float Axis(Vector2F v) => Vertical ? v.Y : v.X;
		float TrackLength => Vertical ? GetRenderBounds().H : GetRenderBounds().W;
		float ThumbLength => MathF.Min(TrackLength, MathF.Max(16, TrackLength / MathF.Max(1, scrollbar.GetOverflow())));
		float ThumbStart => scrollbar.MaxScroll <= 0 ? 0 : scrollbar.Scroll / scrollbar.MaxScroll * (TrackLength - ThumbLength);

		float grabOffset;
		protected override bool MouseClick(FrameState state, ButtonCode button) {
			base.MouseClick(state, button);
			float m = Axis(GetMousePos());
			bool onThumb = m >= ThumbStart && m <= ThumbStart + ThumbLength;
			grabOffset = onThumb ? m - ThumbStart : ThumbLength / 2;
			if (!onThumb)
				ScrollToThumbAt(m);
			return true;
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			ScrollToThumbAt(Axis(GetMousePos()));
			return true;
		}
		void ScrollToThumbAt(float mouseAxis) {
			float travel = TrackLength - ThumbLength;
			scrollbar.Scroll = travel <= 0 ? 0 : (mouseAxis - grabOffset) / travel * scrollbar.MaxScroll;
		}
		public override void Paint(float width, float height) {
			var fore = MixColorBasedOnMouseState(this, GetTextColor(), new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
			var gripThickness = 4;
			Graphics2D.SetDrawColor(fore, 200);

			float inset = GetContentInset();
			if (Vertical)
				Graphics2D.DrawRectangle((width / 2) - (gripThickness / 2), ThumbStart - inset, gripThickness, ThumbLength);
			else
				Graphics2D.DrawRectangle(ThumbStart - inset, (height / 2) - (gripThickness / 2), ThumbLength, gripThickness);
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
			float v = Math.Clamp(value, 0, MaxScroll);
			if (v == _scroll)
				return;
			_scroll = v;
			OnScrolled?.Invoke(v);
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
		this.		Size = new(18, 18);

		Up = new ScrollbarButton(this);
		Down = new ScrollbarButton(this);
		Grip = new ScrollbarGrip(this);

		Up.	Size = new(18, 18);
		Down.Size = new(18, 18);

		Up.Dock = Dock.Top;
		Down.Dock = Dock.Bottom;
		Grip.Dock = Dock.Fill;

		Up.OnButtonClick += (_, _) => Scroll -= ScrollDelta;
		Down.OnButtonClick += (_, _) => Scroll += ScrollDelta;

		SetVisible(false);
		SetPaintBackgroundEnabled(false);
	}

	internal bool MouseScrolled(Element self, FrameState state, Vector2F delta) {
		float d = Alignment == ScrollbarAlignment.Horizontal ? (delta.X != 0 ? delta.X : delta.Y) : delta.Y;
		Scroll += d * -ScrollDelta;
		return true;
	}
	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) => MouseScrolled(self, state, delta);

	public float ScrollDelta { get; set; } = 30;

	public bool ShouldShow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W > PageSize.W : PageContents.H > PageSize.H;
	public float GetOverflow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W / PageSize.W : PageContents.H / PageSize.H;

	public void Update(Vector2F contents, Vector2F size) {
		PageContents = contents;
		PageSize = size;

		var overflowing = Alignment == ScrollbarAlignment.Horizontal ? contents.X - size.X : contents.Y - size.Y;
		if (Scroll > overflowing && Scroll > 0) {
			Scroll = Math.Max(0, overflowing);
		}

		SetVisible(ShouldShow());
	}
}
