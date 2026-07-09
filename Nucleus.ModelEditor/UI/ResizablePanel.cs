using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;

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

		class ResizablePanelButton(Element parent) : Button(parent)
		{
			public event Action<Element, FrameState, Vector2F>? MouseDragEvent;
			protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
				MouseDragEvent?.Invoke(self, state, delta);
				return base.MouseDrag(self, state, delta);
			}
		}

		private ResizablePanelButton __top, __left, __right, __bottom;
		private Panel __inside;
		private float __size = 4;
		public ResizablePanel(Element parent) : base(parent) {
			DockPadding = RectangleF.TLRB(0);

			__top = new(this);
			__left = new(this);
			__right = new(this);
			__bottom = new(this);
			__inside = new(this);

			foreach (Button b in new Button[] { __top, __left, __right, __bottom }) {
				b.				Text = "";
				b.SetBgColor(Color.Blank);
				b.BorderSize = 0;
			}

			__inside.
			Dock = Dock.Fill;

			UpdateResizers();
			SetAddParent(__inside);

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

			this.
			Position = new(this.Position.X + delta.X, this.Position.Y);
			this.			Size = new(this.Size.X + -delta.X, this.Size.Y);
		}

		private void __right_MouseDragEvent(Element self, FrameState state, Vector2F delta) {

		}

		private void __bottom_MouseDragEvent(Element self, FrameState state, Vector2F delta) {

		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			__top.			Position = new(__size, 0); __top.Size = new(width - (__size * 2), __size);
			__left.			Position = new(0, __size); __left.Size = new(__size, height - (__size * 2));
			__bottom.			Position = new(__size, height - __size); __bottom.Size = new(width - (__size * 2), __size);
			__right.			Position = new(width - __size, __size); __right.Size = new(__size, height - (__size * 2));
			__inside.			DockMargin = RectangleF.TLRB(__size + 2);
		}

		private void UpdateResizers() {
			__top.SetVisible(__resizeTop);
			__left.SetVisible(__resizeLeft);
			__right.SetVisible(__resizeRight);
			__bottom.SetVisible(__resizeBottom);
		}
	}
}
