using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements
{
	public enum ScrollbarAlignment
	{
		Horizontal,
		Vertical
	}

	public class Scrollbar : Panel
	{
		public float ScrollbarSize { get; set; } = 8;

		public Button Up { get; set; }
		public Button Down { get; set; }
		public Button Grip { get; set; }

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
		protected override void Initialize() {
			this.Size = new(18, 18);

			Up = this.Add<Button>();
			Down = this.Add<Button>();
			Grip = this.Add<Button>();

			Up.Size = new(18, 18);
			Down.Size = new(18, 18);

			Up.Dock = Dock.Top;
			Down.Dock = Dock.Bottom;
			Grip.Dock = Dock.Fill;

			Up.PaintOverride += Button_PaintOverride;
			Down.PaintOverride += Button_PaintOverride;
			Grip.PaintOverride += Grip_PaintOverride;

			Grip.MouseDragEvent += Grip_MouseDragEvent;

			Up.MouseScrollEvent += MouseScrolled;
			Down.MouseScrollEvent += MouseScrolled;
			Grip.MouseScrollEvent += MouseScrolled;
			this.MouseScrollEvent += MouseScrolled;

			Visible = false;
		}

		public float ScrollDelta { get; set; } = 30;

		public void MouseScrolled(Element self, Types.FrameState state, Types.Vector2F delta) {
			Scroll += delta.Y * -ScrollDelta;
			ConsumeScrollEvent();
		}

		private void Grip_MouseDragEvent(Element self, Types.FrameState state, Types.Vector2F delta) {
			// Remap the new mouse pos
			var map = state.MouseState.MousePos - self.GetGlobalPosition();
			//Console.WriteLine(map);
			var newScroll = (float)NMath.Remap(
				Alignment == ScrollbarAlignment.Horizontal ? map.X : map.Y,
				0, Alignment == ScrollbarAlignment.Horizontal ? self.RenderBounds.W : self.RenderBounds.H,
				0, MaxScroll
				);

			Scroll = newScroll;
		}

		public bool ShouldShow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W > PageSize.W : PageContents.H > PageSize.H;
		public float GetOverflow() => Alignment == ScrollbarAlignment.Horizontal ? PageContents.W / PageSize.W : PageContents.H / PageSize.H;

		private void Grip_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, TextColor, new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
			var gripThickness = 4;
			Graphics2D.SetDrawColor(fore, 200);

			var gripMinSize = 16;
			var gripSize = Math.Max((Alignment == ScrollbarAlignment.Vertical ? height : width) / GetOverflow(), gripMinSize);

			// Scrollbar height calculation
			if (Alignment == ScrollbarAlignment.Vertical)
				Graphics2D.DrawRectangle(
					(width / 2) - (gripThickness / 2),
					(float)NMath.Remap(Scroll, 0, MaxScroll, 0, height - (height / GetOverflow())),
					gripThickness,
					gripSize);
			else
				Graphics2D.DrawRectangle(
					(float)NMath.Remap(Scroll, 0, MaxScroll, 0, width - (width / GetOverflow())),
					(height / 2) - (gripThickness / 2),
					gripSize,
					gripThickness);

		}



		private void Button_PaintOverride(Element self, float width, float height) {
			var fore = MixColorBasedOnMouseState(self, TextColor, new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
			var down = self == Down;

			Graphics2D.SetDrawColor(fore, self.Hovered ? 220 : 200);
			Graphics2D.SetTexture(Alignment == ScrollbarAlignment.Vertical ?
				(down ? Level.Textures.LoadTextureFromFile("ui/down32.png") : Level.Textures.LoadTextureFromFile("ui/up32.png")) :
				(down ? Level.Textures.LoadTextureFromFile("ui/right32.png") : Level.Textures.LoadTextureFromFile("ui/left32.png")));
			Graphics2D.DrawImage(new(2), new(width - 4, height - 4));
		}

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

		}

		public override void Paint(float width, float height) {

		}

		public void Update(Vector2F contents, Vector2F size) {
			PageContents = contents;
			PageSize = size;

			var overflowing = contents.Y - size.Y;
			if (Scroll > overflowing && Scroll > 0) {
				Scroll = Math.Max(0, overflowing);
			}

			this.Visible = ShouldShow();
			this.Enabled = this.Visible;
		}
	}
}
