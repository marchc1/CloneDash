using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

using System.Diagnostics.CodeAnalysis;

using MouseButton = Nucleus.Input.MouseButton;

namespace Nucleus.UI.Elements
{
	public class Titlebar : Panel
	{
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

		public event MouseEventDelegate? OnClosePressed;
		public event MouseEventDelegate? OnMaximizePressed;
		public event MouseEventDelegate? OnMinimizePressed;
		public event MouseV2Delegate? OnTitlebarDragged;

		public Button CloseButton { get; private set; }
		public Button MaximizeButton { get; private set; }
		public Button MinimizeButton { get; private set; }

		Panel? ImageRenderer;

		protected override void OnThink(FrameState frameState) {
			if (Hovered)
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
				ImageRenderer.ImagePadding = new(6, 6);
				ImageRenderer.DrawPanelBackground = false;
			}
		}

		[MemberNotNull(nameof(ImageRenderer))]
		void setupImageRenderer() {
			ImageRenderer = Add<Panel>();
		}

		protected override void Initialize() {
			base.Initialize();
			Dock = Dock.Top;
			Size = new(0, this.Parent is UserInterface ? 34 : 42);
			if (this.Parent is not UserInterface)
				DockMargin = RectangleF.TLRB(4);
			TextSize = 20;

			CloseButton = Add<Button>();
			CloseButton.Dock = Dock.Right;
			CloseButton.AutoSize = false;
			CloseButton.Size = new(48, 0);

			CloseButton.DockMargin = RectangleF.TLRB(3);
			CloseButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
				OnClosePressed?.Invoke(this, state, button);
			};
			MaximizeButton = Add<Button>();
			MaximizeButton.Dock = Dock.Right;
			MaximizeButton.AutoSize = false;
			MaximizeButton.Size = new(48, 0);

			MaximizeButton.DockMargin = RectangleF.TLRB(3);
			MaximizeButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
				OnMaximizePressed?.Invoke(this, state, button);
			};
			MinimizeButton = Add<Button>();
			MinimizeButton.Dock = Dock.Right;
			MinimizeButton.AutoSize = false;
			MinimizeButton.Size = new(48, 0);

			MinimizeButton.DockMargin = RectangleF.TLRB(3);
			MinimizeButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
				OnMinimizePressed?.Invoke(this, state, button);
			};

			CloseButton.Text = "X";
			MaximizeButton.Text = "";
			MinimizeButton.Text = "";

			MaximizeButton.PaintOverride += (self, width, height) => {
				(self as Button).Paint(width, height);
				Graphics2D.SetDrawColor(TextColor);
				var size = new Vector2F(10);
				var pos = new Vector2F((width / 2) - (size.X / 2), (height / 2) - (size.Y / 2));
				Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
					pos, size), 1);

				if (Level.FrameState.Keyboard.ShiftDown && self.Hovered) {
					Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
					pos - new Vector2F(2), size + new Vector2F(4)), 1);
					Graphics2D.DrawLine(pos + new Vector2F(-2, -2), new(4, 4));
					Graphics2D.DrawLine(pos + new Vector2F(0, size.Y) + new Vector2F(-2, 2), new(4, height - 4));
					Graphics2D.DrawLine(pos + new Vector2F(size.X, 0) + new Vector2F(2, -2), new(width - 4, 4));
					Graphics2D.DrawLine(pos + new Vector2F(size.X, size.Y) + new Vector2F(2, 2), new(width - 4, height - 4));
				}
			};
			MinimizeButton.PaintOverride += (self, width, height) => {
				(self as Button).Paint(width, height);
				Graphics2D.SetDrawColor(TextColor);
				Graphics2D.DrawLine(new(14, height / 2), new(width - 14, height / 2));
			};

			CloseButton.BackgroundColor = CloseButton.BackgroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.54f).ToRGB();
			CloseButton.ForegroundColor = CloseButton.ForegroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.6f).ToRGB();
			CloseButton.TextColor = CloseButton.TextColor.ToHSV().SetHSV(hue: 0, saturation: 0.3f).ToRGB();
		}

		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			OnTitlebarDragged?.Invoke(self, state, delta);
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
	public class Taskbar : Element
	{

	}
	public class Window : Element
	{
		public static List<WeakReference<Window>> Windows { get; } = [];

		private string _title = "Untitled Window";
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
				if (ResizeTL != null) ResizeTL.Enabled = value;
				if (ResizeTR != null) ResizeTR.Enabled = value;
				if (ResizeBL != null) ResizeBL.Enabled = value;
				if (ResizeBR != null) ResizeBR.Enabled = value;
			}
		}

		public Window() {
			Position = new(64, 64);
			Size = new(640, 480);
			Windows.Add(new(this));
		}
		~Window() {
			MainThread.RunASAP(() => Windows.RemoveAll((x) => x.TryGetTarget(out Window? window) == true && window == this), ThreadExecutionTime.AfterFrame);
		}
		public override void OnRemoval() {
			base.OnRemoval();
			Windows.RemoveAll((x) => x.TryGetTarget(out Window? window) == true && window == this);
		}
		bool opening = true;
		bool closing = false;
		double closeTime;
		public void Close() {
			closing = true;
			closeTime = Lifetime;
			Backdrop = false;
			UsesRenderTarget = true;
		}

		public Titlebar Titlebar { get; private set; }

		public Button ResizeTL { get; private set; }
		public Button ResizeTR { get; private set; }
		public Button ResizeBL { get; private set; }
		public Button ResizeBR { get; private set; }
		public static float CornerSize => 8;
		protected override void Initialize() {
			Titlebar = Add<Titlebar>();
			Titlebar.Title = _title;
			Titlebar.OnClosePressed += Titlebar_OnTitlebarClosePressed;
			Titlebar.OnTitlebarDragged += dragWindow;

			MakePopup();
			Panel ap = this.Add<Panel>();
			ap.Dock = Dock.Fill;
			ap.Size = new(0, 36);
			ap.PaintOverride += delegate (Element self, float width, float height) {
				Graphics2D.SetDrawColor(BackgroundColor);
				Graphics2D.DrawRectangle(0, 0, width, height);

				Graphics2D.SetDrawColor(ForegroundColor);
				Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
			};
			ap.DockMargin = RectangleF.TLRB(4, 8, 8, 4);

			ResizeTL = Add<Button>();
			ResizeTL.Size = new(24, 24);
			ResizeTL.Origin = Anchor.TopLeft;
			ResizeTL.Anchor = Anchor.TopLeft;
			ResizeTL.Enabled = Resizable;

			ResizeTR = Add<Button>();
			ResizeTR.Size = new(24, 24);
			ResizeTR.Origin = Anchor.TopRight;
			ResizeTR.Anchor = Anchor.TopRight;
			ResizeTR.Enabled = Resizable;

			ResizeBL = Add<Button>();
			ResizeBL.Size = new(24, 24);
			ResizeBL.Origin = Anchor.BottomLeft;
			ResizeBL.Anchor = Anchor.BottomLeft;
			ResizeBL.Enabled = Resizable;

			ResizeBR = Add<Button>();
			ResizeBR.Size = new(24, 24);
			ResizeBR.Origin = Anchor.BottomRight;
			ResizeBR.Anchor = Anchor.BottomRight;
			ResizeBR.Enabled = Resizable;

			ResizeTL.OnHoverTest += (self, bounds, mouse) => {
				var bounds1 = RectangleF.FromPosAndSize(bounds.Pos, new(CornerSize, bounds.H));
				var bounds2 = RectangleF.FromPosAndSize(bounds.Pos, new(bounds.W, CornerSize));

				return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
			};

			ResizeTR.OnHoverTest += (self, bounds, mouse) => {
				var bounds1 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(bounds.W - CornerSize, 0), new(CornerSize, bounds.H));
				var bounds2 = RectangleF.FromPosAndSize(bounds.Pos, new(bounds.W, CornerSize));

				return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
			};

			ResizeBL.OnHoverTest += (self, bounds, mouse) => {
				var bounds1 = RectangleF.FromPosAndSize(bounds.Pos, new(CornerSize, bounds.H));
				var bounds2 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(0, bounds.H - CornerSize), new(bounds.W, CornerSize));

				return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
			};

			ResizeBR.OnHoverTest += (self, bounds, mouse) => {
				var bounds1 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(bounds.W - CornerSize, 0), new(CornerSize, bounds.H));
				var bounds2 = RectangleF.FromPosAndSize(bounds.Pos + new Vector2F(0, bounds.H - CornerSize), new(bounds.W, CornerSize));

				return bounds1.ContainsPoint(mouse) || bounds2.ContainsPoint(mouse);
			};

			ResizeBL.Position = new(4, 0);
			ResizeBR.Position = new(-4, 0);

			ResizeTL.MouseDragEvent += ResizeTL_MouseDragEvent;
			ResizeTR.MouseDragEvent += ResizeTR_MouseDragEvent;
			ResizeBL.MouseDragEvent += ResizeBL_MouseDragEvent;
			ResizeBR.MouseDragEvent += ResizeBR_MouseDragEvent;

			ResizeTL.Text = "";
			ResizeTR.Text = "";
			ResizeBL.Text = "";
			ResizeBR.Text = "";

			ResizeTL.PaintOverride += ResizeTL_PaintOverride;
			ResizeTR.PaintOverride += ResizeTR_PaintOverride;
			ResizeBL.PaintOverride += ResizeBL_PaintOverride;
			ResizeBR.PaintOverride += ResizeBR_PaintOverride;


			this.AddParent = ap;
			this.UsesRenderTarget = true;
		}
		private void ResizeBR_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
			Graphics2D.SetDrawColor(fore);
			Graphics2D.DrawRectangle(width / 2, height - 2, width / 2, 2);
			Graphics2D.DrawRectangle(width - 2, height / 2, 2, height / 2);
		}

		private void ResizeBL_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
			Graphics2D.SetDrawColor(fore);
			Graphics2D.DrawRectangle(0, height - 2, width / 2, 2);
			Graphics2D.DrawRectangle(0, height / 2, 2, height / 2);
		}

		private void ResizeTR_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
			Graphics2D.SetDrawColor(fore);
			Graphics2D.DrawRectangle(width / 2, 0, width / 2, 2);
			Graphics2D.DrawRectangle(width - 2, 0, 2, height / 2);
		}

		private void ResizeTL_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
			Graphics2D.SetDrawColor(fore);
			Graphics2D.DrawRectangle(0, 0, width / 2, 2);
			Graphics2D.DrawRectangle(0, 0, 2, height / 2);
		}

		private void ResizeTL_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			this.Position += delta;
			this.Size -= delta;
		}

		private void ResizeTR_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			this.Position += delta.Mutate(zeroX: true);
			this.Size -= delta.Mutate(negateX: true);
		}
		private void ResizeBL_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			this.Position += delta.Mutate(zeroY: true);
			this.Size -= delta.Mutate(negateY: true);
		}
		private void ResizeBR_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			this.Size += delta;
		}

		private void Titlebar_OnTitlebarClosePressed(Element self, FrameState state, MouseButton button) {
			this.Close();
		}

		private void dragWindow(Element self, FrameState state, Vector2F delta) {
			this.Position += delta;
		}

		protected override void OnThink(FrameState frameState) {
			if (closing) {
				if ((Lifetime - closeTime) >= CLOSE_TIME) {
					Remove();
					return;
				}
			}
			else if (Lifetime >= OPEN_TIME) {
				opening = false;
				UsesRenderTarget = false;
			}
		}
		static float OPEN_TIME => 0.5f;
		static float CLOSE_TIME => 0.25f;
		public bool Closing => closing;
		public override void PreRender() {
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
				float mulY = 1-(NMath.Ease.InBack(originalMul) * -2);

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
		public override void PostRender() {
			if (closing) {
				Rlgl.PopMatrix();
			}
			else if (opening) {
				Rlgl.PopMatrix();
				Rlgl.PopMatrix();
				EngineCore.Window.EndMode2D();
			}
		}
		public override void PostRenderChildren() {
			if (InputDisabled) {
				Graphics2D.SetDrawColor(0, 0, 0, 155);
				Graphics2D.DrawRectangle(4, 4, RenderBounds.Width - 8, 34);
				Graphics2D.DrawRectangle(4, 4 + 34 + 8, RenderBounds.Width - 8, RenderBounds.Height - 8 - 34 - 8);
			}
		}

		public void AttachWindowAndLockInput(Window window) {
			InputDisabled = true;
			window.Removed += delegate (Element self) {
				InputDisabled = false;
			};
		}

		public void HideNonCloseButtons() {
			Titlebar.MaximizeButton.Visible = false;
			Titlebar.MinimizeButton.Visible = false;
		}

		public void HideAllButtons() {
			Titlebar.MaximizeButton.Visible = false;
			Titlebar.MinimizeButton.Visible = false;
			Titlebar.CloseButton.Visible = false;
		}
	}
}
