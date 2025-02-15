using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor.UI
{
	public class ResizablePanel : Panel
	{
		private bool __resizeTop = true, __resizeBottom = true, __resizeLeft = true, __resizeRight = true;
		public bool CanResizeTop {
			get => __resizeTop; set {
				__resizeTop = value;
				UpdateResizers();
			}
		}
		public bool CanResizeLeft {
			get => __resizeLeft; set {
				__resizeLeft = value;
				UpdateResizers();
			}
		}
		public bool CanResizeRight {
			get => __resizeRight; set {
				__resizeRight = value;
				UpdateResizers();
			}
		}
		public bool CanResizeBottom {
			get => __resizeBottom; set {
				__resizeBottom = value;
				UpdateResizers();
			}
		}

		private Button __top, __left, __right, __bottom;
		private Panel __inside;
		private float __size = 4;
		protected override void Initialize() {
			base.Initialize();
			DockPadding = RectangleF.TLRB(0);

			Add(out __top);
			Add(out __left);
			Add(out __right);
			Add(out __bottom);
			Add(out __inside);

			foreach(Button b in new Button[] { __top, __left, __right, __bottom }) {
				b.Text = "";
				b.BackgroundColor = Color.BLANK;
				b.BorderSize = 0;
			}

			__inside.Dock = Dock.Fill;

			UpdateResizers();
			AddParent = __inside;

			__top.MouseDragEvent += __top_MouseDragEvent;
			__left.MouseDragEvent += __left_MouseDragEvent;
			__right.MouseDragEvent += __right_MouseDragEvent;
			__bottom.MouseDragEvent += __bottom_MouseDragEvent;
		}

		public float MinimumWidth { get; set; } = 384;
		public float MinimumHeight { get; set; } = 384;

		private bool overflowCheckX(float deltaX) {
			if (this.Size.X - deltaX < MinimumWidth)
				return true;
			return false;
		}

		private void __top_MouseDragEvent(Element self, FrameState state, Vector2F delta) {

		}

		private void __left_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			if (overflowCheckX(delta.X)) return;

			this.Position = new(this.Position.X + delta.X, this.Position.Y);
			this.Size = new(this.Size.X + -delta.X, this.Size.Y);
		}

		private void __right_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			
		}

		private void __bottom_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			__top.Position = new(__size, 0); __top.Size = new(width - (__size * 2), __size);
			__left.Position = new(0, __size); __left.Size = new(__size, height - (__size * 2));
			__bottom.Position = new(__size, height - __size); __bottom.Size = new(width - (__size * 2), __size);
			__right.Position = new(width - __size, __size); __right.Size = new(__size, height - (__size * 2));
			__inside.DockMargin = RectangleF.TLRB(__size + 2);
		}

		private void UpdateResizers() {
			__top.Visible = __resizeTop;
			__left.Visible = __resizeLeft;
			__right.Visible = __resizeRight;
			__bottom.Visible = __resizeBottom;
		}
	}
}
