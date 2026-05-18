using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

using System.Diagnostics.CodeAnalysis;

namespace Nucleus.UI.Elements;

public class Titlebar : Panel
{
	public enum TitlebarButtonType
	{
		Close,
		Minimize,
		Maximize
	}
	public class TitlebarButton(Titlebar titlebar, TitlebarButtonType type) : Button(titlebar)
	{
		TitlebarButtonType type;
		public override void Paint(float width, float height) {
			switch (type) {
				case TitlebarButtonType.Close: PaintClose(width, height); break;
				case TitlebarButtonType.Minimize: PaintMinimize(width, height); break;
				case TitlebarButtonType.Maximize: PaintMaximize(width, height); break;
			}
		}

		private void PaintMaximize(float width, float height) {
			base.Paint(width, height);
			Graphics2D.SetDrawColor(TextColor);
			var size = new Vector2F(10);
			var pos = new Vector2F((width / 2) - (size.X / 2), (height / 2) - (size.Y / 2));
			Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
				pos, size), 1);

			if (Level.FrameState.Keyboard.ShiftDown && IsHovered()) {
				Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
				pos - new Vector2F(2), size + new Vector2F(4)), 1);
				Graphics2D.DrawLine(pos + new Vector2F(-2, -2), new(4, 4));
				Graphics2D.DrawLine(pos + new Vector2F(0, size.Y) + new Vector2F(-2, 2), new(4, height - 4));
				Graphics2D.DrawLine(pos + new Vector2F(size.X, 0) + new Vector2F(2, -2), new(width - 4, 4));
				Graphics2D.DrawLine(pos + new Vector2F(size.X, size.Y) + new Vector2F(2, 2), new(width - 4, height - 4));
			}
		}

		private void PaintMinimize(float width, float height) {
			base.Paint(width, height);

			Graphics2D.SetDrawColor(TextColor);
			Graphics2D.DrawLine(new(14, height / 2), new(width - 14, height / 2));
		}

		private void PaintClose(float width, float height) {
			base.Paint(width, height);
		}
	}
	public Titlebar(Element? parent) : base(parent) {
		ImagePadding = new(6, 6);
		Dock = Dock.Top;
		Size = new(0, this.GetParent() is UserInterface ? 34 : 42);
		if (this.GetParent() is not UserInterface)
			DockMargin = RectangleF.TLRB(4);
		TextSize = 20;

		CloseButton = new TitlebarButton(this, TitlebarButtonType.Close);
		CloseButton.Dock = Dock.Right;
		CloseButton.AutoSize = false;
		CloseButton.Size = new(48, 0);

		CloseButton.DockMargin = RectangleF.TLRB(3);
		CloseButton.OnButtonClick += (self, button) => OnClosePressed?.Invoke(self, button);

		MaximizeButton = new TitlebarButton(this, TitlebarButtonType.Maximize);
		MaximizeButton.Dock = Dock.Right;
		MaximizeButton.AutoSize = false;
		MaximizeButton.Size = new(48, 0);

		MaximizeButton.DockMargin = RectangleF.TLRB(3);
		MaximizeButton.OnButtonClick += (self, button) => OnMaximizePressed?.Invoke(self, button);
		MinimizeButton = new TitlebarButton(this, TitlebarButtonType.Minimize);
		MinimizeButton.Dock = Dock.Right;
		MinimizeButton.AutoSize = false;
		MinimizeButton.Size = new(48, 0);

		MinimizeButton.DockMargin = RectangleF.TLRB(3);
		MinimizeButton.OnButtonClick += (self, button) => OnMinimizePressed?.Invoke(self, button);

		CloseButton.Text = "X";
		MaximizeButton.Text = "";
		MinimizeButton.Text = "";

		CloseButton.BackgroundColor = CloseButton.BackgroundColor.RGBubToHSVf().SetHSVf(hue: 0, saturation: 0.54f).HSVfToRGBub();
		CloseButton.ForegroundColor = CloseButton.ForegroundColor.RGBubToHSVf().SetHSVf(hue: 0, saturation: 0.6f).HSVfToRGBub();
		CloseButton.TextColor = CloseButton.TextColor.RGBubToHSVf().SetHSVf(hue: 0, saturation: 0.3f).HSVfToRGBub();
	}

	private bool imageChanged;
	private string? imagePath;
	public new string? Image {
		get => imagePath;
		set {
			imagePath = value;
			imageChanged = true;
		}
	}
	private Anchor titlePos = Anchor.Center;
	public string Title { get; set; } = "Untitled Window";
	public Anchor TitlePos {
		get => titlePos;
		set {
			titlePos = value;
			InvalidateLayout();
		}
	}

	public event ButtonActionFn? OnClosePressed;
	public event ButtonActionFn? OnMaximizePressed;
	public event ButtonActionFn? OnMinimizePressed;

	public delegate void TitlebarDragFn(Titlebar titlebar, Vector2F delta);
	public event TitlebarDragFn? OnTitlebarDragged;

	public TitlebarButton CloseButton { get; private set; }
	public TitlebarButton MaximizeButton { get; private set; }
	public TitlebarButton MinimizeButton { get; private set; }

	Panel? ImageRenderer;

	protected override void OnThink() {
		if (IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_ALL);

		if (imageChanged) {
			if (imagePath == null) {
				if (IValidatable.IsValid(ImageRenderer))
					ImageRenderer.Remove();
			}
			else {
				if (!IValidatable.IsValid(ImageRenderer))
					setupImageRenderer();

				ImageRenderer.Image = Level.Textures.LoadTextureFromFile(imagePath);
			}
		}
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		if (IValidatable.IsValid(ImageRenderer)) {
			ImageRenderer.Size = new(height, height);
			ImageRenderer.Position = TitlePos switch {
				Anchor.CenterLeft => new(0, 0),
				Anchor.Center => new((width / 2) - (Graphics2D.GetTextSize(Title, Graphics2D.UI_FONT_NAME, TextSize).W / 2), 0),
				_ => new(0, 0),
			};
			ImageRenderer.ImageOrientation = ImageOrientation.Zoom;
			ImageRenderer.ImagePadding = ImagePadding;
			ImageRenderer.SetPaintBackgroundEnabled(false);
		}
	}

	[MemberNotNull(nameof(ImageRenderer))]
	void setupImageRenderer() {
		ImageRenderer = new Panel(this);
		ImageRenderer.SetPassthru(true);
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		OnTitlebarDragged?.Invoke(this, delta);
		return true;
	}

	public override void Paint(float width, float height) {
		Graphics2D.SetDrawColor(BackgroundColor);
		Graphics2D.DrawRectangle(0, 0, width, height);

		Graphics2D.SetDrawColor(ForegroundColor);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);

		Graphics2D.SetDrawColor(TextColor);
		var pnt = TitlePos.CalculatePosition(new(TitlePos.GetHorizontalRatio() == 0 ? 8 : 0, 0), new(width, height));
		if (imageChanged) {
			if (imagePath == null)
				base.Image = null;
			else
				base.Image = Level.Textures.LoadTextureFromFile(imagePath);

			imageChanged = false;
		}

		if (base.Image != null)
			pnt.X += height - 4;

		Graphics2D.DrawText(pnt.X, pnt.Y, Title, Graphics2D.UI_FONT_NAME, TextSize, TitlePos);
	}
}
public class Taskbar(Element? parent) : Element(parent)
{

}
public class Window : Element
{
	private string _title = "";
	public string Title {
		get => Titlebar == null ? _title : Titlebar.Title;
		set {
			if (Titlebar == null)
				_title = value;
			else
				Titlebar.Title = value;
		}
	}
	private bool __resizable = true;
	public bool Resizable {
		get => __resizable;
		set {
			__resizable = value;
			ResizeTL?.SetVisible(value);
			ResizeTR?.SetVisible(value);
			ResizeBL?.SetVisible(value);
			ResizeBR?.SetVisible(value);
		}
	}

	internal class WindowResizerButton(Window window, Anchor anchor) : Button(window)
	{
		public override bool HoverTest(RectangleF bounds, Vector2F mouse) {
			RectangleF bounds1, bounds2;
			switch (anchor) {
				case Anchor.TopLeft:
					bounds1 = RectangleF.FromPosAndSize(bounds.Pos, new(CornerSize, bounds.H));
					bounds2 = RectangleF.FromPosAndSize(bounds.Pos, new(bounds.W, CornerSize));

					return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
				case Anchor.TopRight:
					bounds1 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(bounds.W - CornerSize, 0), new(CornerSize, bounds.H));
					bounds2 = RectangleF.FromPosAndSize(bounds.Pos, new(bounds.W, CornerSize));

					return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
				case Anchor.BottomLeft:
					bounds1 = RectangleF.FromPosAndSize(bounds.Pos, new(CornerSize, bounds.H));
					bounds2 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(0, bounds.H - CornerSize), new(bounds.W, CornerSize));

					return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
				case Anchor.BottomRight:
					bounds1 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(bounds.W - CornerSize, 0), new(CornerSize, bounds.H));
					bounds2 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(0, bounds.H - CornerSize), new(bounds.W, CornerSize));

					return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
				default:
					return false; // unsupported anchor
			}
		}

		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			switch (anchor) {
				case Anchor.TopLeft:
					window.Position += delta;
					window.Size -= delta;
					break;
				case Anchor.TopRight:
					window.Position += delta.Mutate(zeroX: true);
					window.Size -= delta.Mutate(negateX: true);
					break;
				case Anchor.BottomLeft:
					window.Position += delta.Mutate(zeroY: true);
					window.Size -= delta.Mutate(negateY: true);
					break;
				case Anchor.BottomRight:
					window.Size += delta;
					break;
				default:
					break;
			}
			return true;
		}
		public override void Paint(float width, float height) {
			Color fore;
			switch (anchor) {
				case Anchor.TopLeft:
					fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
					Graphics2D.SetDrawColor(fore);
					Graphics2D.DrawRectangle(0, 0, width / 2, 2);
					Graphics2D.DrawRectangle(0, 0, 2, height / 2);
					break;
				case Anchor.TopRight:
					fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
					Graphics2D.SetDrawColor(fore);
					Graphics2D.DrawRectangle(width / 2, 0, width / 2, 2);
					Graphics2D.DrawRectangle(width - 2, 0, 2, height / 2);
					break;
				case Anchor.BottomLeft:
					fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
					Graphics2D.SetDrawColor(fore);
					Graphics2D.DrawRectangle(0, height - 2, width / 2, 2);
					Graphics2D.DrawRectangle(0, height / 2, 2, height / 2);
					break;
				case Anchor.BottomRight:
					fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
					Graphics2D.SetDrawColor(fore);
					Graphics2D.DrawRectangle(width / 2, height - 2, width / 2, 2);
					Graphics2D.DrawRectangle(width - 2, height / 2, 2, height / 2);
					break;
				default:
					break;
			}
		}
	}

	public Window(Element? element, ReadOnlySpan<char> title = "Untitled Window", ReadOnlySpan<char> name = default) : base(element, name) {
		Position = new(64, 64);
		Size = new(640, 480);
		_title = new(title);

		Titlebar = new Titlebar(this);
		Titlebar.Title = _title;
		Titlebar.OnClosePressed += Titlebar_OnTitlebarClosePressed;
		Titlebar.OnTitlebarDragged += dragWindow;

		MakePopup();
		Panel ap = new(this);
		ap.Dock = Dock.Fill;
		ap.Size = new(0, 36);
		ap.DockMargin = RectangleF.TLRB(4, 8, 8, 4);

		ResizeTL = new WindowResizerButton(this, Anchor.TopLeft);
		ResizeTL.Size = new(24, 24);
		ResizeTL.Origin = Anchor.TopLeft;
		ResizeTL.Anchor = Anchor.TopLeft;
		ResizeTL.SetVisible(Resizable);

		ResizeTR = new WindowResizerButton(this, Anchor.TopRight);
		ResizeTR.Size = new(24, 24);
		ResizeTR.Origin = Anchor.TopRight;
		ResizeTR.Anchor = Anchor.TopRight;
		ResizeTR.SetVisible(Resizable);

		ResizeBL = new WindowResizerButton(this, Anchor.BottomLeft);
		ResizeBL.Size = new(24, 24);
		ResizeBL.Origin = Anchor.BottomLeft;
		ResizeBL.Anchor = Anchor.BottomLeft;
		ResizeBL.SetVisible(Resizable);

		ResizeBR = new WindowResizerButton(this, Anchor.BottomRight);
		ResizeBR.Size = new(24, 24);
		ResizeBR.Origin = Anchor.BottomRight;
		ResizeBR.Anchor = Anchor.BottomRight;
		ResizeBR.SetVisible(Resizable);

		ResizeBL.Position = new(4, 0);
		ResizeBR.Position = new(-4, 0);

		ResizeTL.Text = "";
		ResizeTR.Text = "";
		ResizeBL.Text = "";
		ResizeBR.Text = "";

		this.AddParent = ap;
		SetUseRenderTarget(true);
	}
	protected override void OnRemoval() {
		base.OnRemoval();
	}
	bool opening = true;
	bool closing = false;
	double closeTime;
	public void Close() {
		closing = true;
		closeTime = Lifetime;
		Backdrop = false;
		SetUseRenderTarget(true);
	}

	public Titlebar Titlebar { get; private set; }

	internal WindowResizerButton ResizeTL { get; private set; }
	internal WindowResizerButton ResizeTR { get; private set; }
	internal WindowResizerButton ResizeBL { get; private set; }
	internal WindowResizerButton ResizeBR { get; private set; }
	public static float CornerSize => 8;

	private void Titlebar_OnTitlebarClosePressed(Button self, ButtonCode button) {
		this.Close();
	}

	private void dragWindow(Element self, Vector2F delta) {
		this.Position += delta;
	}

	protected override void OnThink() {
		if (closing) {
			if ((Lifetime - closeTime) >= CLOSE_TIME) {
				Remove();
				return;
			}
		}
		else if (Lifetime >= OPEN_TIME && opening) {
			opening = false;
			SetUseRenderTarget(false);
		}
	}
	static float OPEN_TIME => 0.5f;
	static float CLOSE_TIME => 0.25f;
	public bool Closing => closing;
	public override void PreRenderRT() {
		if (!closing && !opening) return;
		float t = (float)(closing ? Lifetime - closeTime : Lifetime);

		float mul = NMath.Remap(t, 0, closing ? CLOSE_TIME : OPEN_TIME, 0, 1, true, false);
		float mulf = ((closing ? CLOSE_TIME : OPEN_TIME) + (mul / 2)) - 1;

		if (closing) {
			float originalMul = mul;
			mul = 1 - NMath.Ease.InCubic(mul);
			mulf = NMath.Ease.OutCubic(mulf);

			Rlgl.PushMatrix();

			float mulX = 1 - (NMath.Ease.InBack(originalMul) * 0.2f);
			float mulY = 1 - (NMath.Ease.InBack(originalMul) * -2);

			Vector2F sizeOffset = new(
				((RenderBounds.X + (RenderBounds.W / 2)) * 0.5f * (mulX - 1)),
				((RenderBounds.Y + (RenderBounds.H / 2)) * 0.5f * (mulY - 1))
			);

			Rlgl.Translatef(sizeOffset.X, sizeOffset.Y, 0);
			Rlgl.Scalef(1.0f + ((1 - mulX) * 0.5f), 1.0f + ((1 - mulY) * 0.5f), 1);
		}
		else {
			mul = NMath.Ease.OutCubic(mul);
			mulf = NMath.Ease.InCubic(mulf);

			EngineCore.Window.BeginMode2D(new Camera2D() {
				Offset = new((RenderBounds.X * -mulf) + ((RenderBounds.W / 2) * -mulf), (RenderBounds.Y * -mulf) + ((RenderBounds.H / 2) * -mulf)),
				Rotation = 0,
				Target = new(0, mulf),
				Zoom = 1.0f + mulf
			});

			Rlgl.PushMatrix();
			Rlgl.Scalef(1, 1.0f + ((1 - NMath.Ease.OutCubic(mul)) * 0.5f), 1);
		}

		Opacity = mul;
	}
	public override void PostRenderRT() {
		if (closing) {
			Rlgl.PopMatrix();
		}
		else if (opening) {
			Rlgl.PopMatrix();
			Rlgl.PopMatrix();
			EngineCore.Window.EndMode2D();
		}
	}
	public override void PostChildPaint() {
		if (!IsMouseInputEnabled()) {
			Graphics2D.SetDrawColor(0, 0, 0, 155);
			Graphics2D.DrawRectangle(4, 4, RenderBounds.Width - 8, 34);
			Graphics2D.DrawRectangle(4, 4 + 34 + 8, RenderBounds.Width - 8, RenderBounds.Height - 8 - 34 - 8);
		}
	}



	public void AttachWindowAndLockInput(Window window) {
		bool kb = IsKeyboardInputEnabled(), m = IsMouseInputEnabled();

		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		window.Removed += delegate (Element self) {
			SetKeyboardInputEnabled(kb);
			SetMouseInputEnabled(m);
		};
	}

	public void HideNonCloseButtons() {
		Titlebar.MaximizeButton.SetVisible(false);
		Titlebar.MinimizeButton.SetVisible(false);
	}

	public void HideAllButtons() {
		Titlebar.MaximizeButton.SetVisible(false);
		Titlebar.MinimizeButton.SetVisible(false);
		Titlebar.CloseButton.SetVisible(false);
	}
}
