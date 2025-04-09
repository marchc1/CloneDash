using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Color = Raylib_cs.Color;

namespace Nucleus.ModelEditor.UI
{
	public class DopesheetView : View
	{
		public override string Name => "Dope Sheet";

		private float frameOffset = 0;
		private float scrollOffset = 0;
		private float zoom = DefaultZoom;
		/// <summary>
		/// The frame offset. Left == offset.
		/// </summary>
		public float FrameOffset {
			get => frameOffset;
			set {
				frameOffset = Math.Max(0, value);
				TimeInfoPanel.InvalidateLayout();
				KeyframeInfoPanel.InvalidateLayout();
			}
		}
		public float ScrollOffset {
			get => scrollOffset;
			set {
				scrollOffset = Math.Max(0, value);
				TimeInfoPanel.InvalidateLayout();
				KeyframeInfoPanel.InvalidateLayout();
			}
		}
		/// <summary>
		/// How many pixels per frame
		/// </summary>
		public float Zoom {
			get => zoom;
			set {
				zoom = Math.Clamp(value, MinZoom, MaxZoom);
				TimeInfoPanel.InvalidateLayout();
				KeyframeInfoPanel.InvalidateLayout();

				ZoomSlider.SetValueNoUpdate(zoom);
			}
		}

		public static float MinZoom = 0.4f;
		public static float DefaultZoom = 18f;
		public static float MaxZoom = 150f;

		public Panel ButtonsAndNames;
		public FlexPanel Buttons;
		public Panel TimeInfoPanel;
		public Panel KeyframeInfoPanel;
		public NumSlider ZoomSlider;

		private void SetupButton(Button button, bool smallVertical, bool leftPad, bool rightPad) {
			button.Text = "";
			button.BorderSize = 1;
			button.ImageOrientation = ImageOrientation.Fit;

			var hP = 3;
			var vY = smallVertical ? 8 : 4;
			button.DockMargin = RectangleF.TLRB(vY, leftPad ? hP : 0, rightPad ? hP : 0, vY);
		}
		protected override void Initialize() {
			base.Initialize();

			ModelEditor.Active.File.AnimationActivated += File_AnimationActivated;
			ModelEditor.Active.File.AnimationDeactivated += File_AnimationDeactivated;

			DockPadding = RectangleF.Zero;
			BackgroundColor = BackgroundColor.Adjust(0, -.4, 2);
			// Create the initial panels
			Add(out Panel topPanel);
			topPanel.Dock = Dock.Top;
			topPanel.Size = new(44);
			topPanel.DockMargin = RectangleF.TLRB(6);
			topPanel.DrawPanelBackground = true;
			topPanel.DockPadding = RectangleF.Zero;

			Add(out Panel bottomPanel);
			bottomPanel.Dock = Dock.Bottom;
			bottomPanel.Size = new(16);
			bottomPanel.DockMargin = RectangleF.TLRB(0);
			bottomPanel.BorderSize = 0;
			bottomPanel.DrawPanelBackground = true;
			bottomPanel.DockPadding = RectangleF.Zero;

			bottomPanel.Add(out ZoomSlider);
			ZoomSlider.MinimumValue = MinZoom;
			ZoomSlider.MaximumValue = MaxZoom;
			ZoomSlider.Value = Zoom;
			ZoomSlider.TextColor = Color.Blank;
			ZoomSlider.Dock = Dock.Left;
			ZoomSlider.Size = new(230);
			ZoomSlider.BackgroundColor = new(1, 3, 5);
			ZoomSlider.OnValueChanged += (_, _, v) => Zoom = (float)v;

			Add(out ButtonsAndNames);
			ButtonsAndNames.Dock = Dock.Left;
			ButtonsAndNames.Size = new(230);
			ButtonsAndNames.DockMargin = RectangleF.TLRB(0);
			ButtonsAndNames.DockPadding = RectangleF.Zero;
			ButtonsAndNames.BorderSize = 0;

			ButtonsAndNames.Add(out Buttons);
			Buttons.Dock = Dock.Top;
			Buttons.Size = new(36);
			Buttons.DockMargin = RectangleF.TLRB(0);
			Buttons.DockPadding = RectangleF.Zero;
			Buttons.BorderSize = 0;
			Buttons.Direction = Directional180.Horizontal;
			Buttons.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			Buttons.PaintOverride += Buttons_PaintOverride;

			// Setup buttons
			{
				Buttons.Add(out Button jumpStart); SetupButton(jumpStart, true, true, false); jumpStart.Image = Textures.LoadTextureFromFile("models/jumpStart.png");
				Buttons.Add(out Button jumpPrevious); SetupButton(jumpPrevious, true, false, true); jumpPrevious.Image = Textures.LoadTextureFromFile("models/jumpPrevious.png");
				Buttons.Add(out Button playBackward); SetupButton(playBackward, false, true, false); playBackward.Image = Textures.LoadTextureFromFile("models/playBackward.png");
				Buttons.Add(out Button playForward); SetupButton(playForward, false, false, true); playForward.Image = Textures.LoadTextureFromFile("models/playForward.png");
				Buttons.Add(out Button jumpNext); SetupButton(jumpNext, true, true, false); jumpNext.Image = Textures.LoadTextureFromFile("models/jumpNext.png");
				Buttons.Add(out Button jumpEnd); SetupButton(jumpEnd, true, false, true); jumpEnd.Image = Textures.LoadTextureFromFile("models/jumpEnd.png");
				Buttons.Add(out Button loop); SetupButton(loop, true, true, true); loop.Image = Textures.LoadTextureFromFile("models/loop.png");
			}

			Add(out TimeInfoPanel);
			TimeInfoPanel.Dock = Dock.Top;
			TimeInfoPanel.Size = new(36);
			TimeInfoPanel.DockMargin = RectangleF.TLRB(0);
			TimeInfoPanel.DockPadding = RectangleF.Zero;
			TimeInfoPanel.PaintOverride += TopButtonsAndTimeInfo_PaintOverride;
			TimeInfoPanel.MouseClickEvent += TimeInfoPanel_MouseClickEvent;
			TimeInfoPanel.MouseDragEvent += TimeInfoPanel_MouseDragEvent;
			TimeInfoPanel.MouseReleaseEvent += TimeInfoPanel_MouseReleaseEvent; ;

			Add(out KeyframeInfoPanel);
			KeyframeInfoPanel.Dock = Dock.Fill;
			KeyframeInfoPanel.Size = new(36);
			KeyframeInfoPanel.DockMargin = RectangleF.TLRB(0);
			KeyframeInfoPanel.DockPadding = RectangleF.Zero;
			KeyframeInfoPanel.PaintOverride += KeyframeInfoPanel_PaintOverride;
			KeyframeInfoPanel.MouseClickEvent += KeyframeInfoPanel_MouseClickEvent;
			KeyframeInfoPanel.MouseDragEvent += KeyframeInfoPanel_MouseDragEvent;
			KeyframeInfoPanel.MouseReleaseEvent += KeyframeInfoPanel_MouseReleaseEvent; ;
		}

		private void TimeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(false, Vector2F.Zero);
		}

		private void KeyframeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(false, Vector2F.Zero);
		}

		private bool DraggingX = false;
		private bool DraggingY = false;
		private bool Dragging = false;
		private Vector2F startAt = Vector2F.Zero;

		private float frameAtDragStart = 0;
		private float scrollAtDragStart = 0;

		private bool ResolvedDraggingDirection => DraggingX || DraggingY;

		private void ResetDragDirection(bool dragging, Vector2F v) {
			DraggingX = false;
			DraggingY = false;
			Dragging = dragging;
			startAt = v;

			frameAtDragStart = FrameOffset;
			scrollAtDragStart = ScrollOffset;
		}

		private void DetermineDragDirection(Vector2F delta) {
			if (ResolvedDraggingDirection) return;

			startAt += delta;
			Vector2F abs = startAt.Abs();

			bool x = abs.X > 10;
			bool y = abs.Y > 10;

			if (x || y) {
				if (x == true && y == true) // just pick x
					y = false;

				DraggingX = x;
				DraggingY = y;

				if (!x) FrameOffset = frameAtDragStart;
				if (!y) ScrollOffset = scrollAtDragStart;
			}
		}

		private void processScroll(Vector2F delta) {
			delta.X *= -1;
			DetermineDragDirection(delta);
			if (ResolvedDraggingDirection) {
				if (DraggingX) FrameOffset += delta.X;
				if (DraggingY) ScrollOffset += delta.Y;
			}
			else {
				FrameOffset += delta.X;
				ScrollOffset += delta.Y;
			}
		}
		private void TimeInfoPanel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			processScroll(delta);
		}
		private void KeyframeInfoPanel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			processScroll(delta);
		}



		private void TimeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
		}

		private void KeyframeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
		}



		private void drawGradient(float height) {
			var r = (float)NMath.Remap(frameOffset, 0f, 30f, 0f, 1f, false, true);
			var c = new Color(0, 0, 0, (int)(r * 150));
			Graphics2D.DrawGradient(new(0, 0), new(24, height), c, Color.Blank, Dock.Right);
		}

		private void Buttons_PaintOverride(Element self, float width, float height) {
			Graphics2D.SetDrawColor(130, 135, 142);
			Graphics2D.DrawLine(0, height, width, height);
		}

		// Shared offset from leftmost -> frame 0.
		private float defaultXOffset = 22;

		private void TopButtonsAndTimeInfo_PaintOverride(Element self, float width, float height) {
			var tl = ModelEditor.Active.File.Timeline;

			var curframe = Convert.ToInt32(tl.Frame);

			self.BackgroundColor = new(30, 37, 46);
			self.BorderSize = 0;
			self.Paint(width, height);

			Graphics2D.SetDrawColor(130, 135, 142);
			Graphics2D.DrawLine(0, height, width, height);

			var xstart = defaultXOffset - FrameOffset;
			int xMajorDivisions = CalcXMajorDivisions();

			Graphics2D.SetDrawColor(150, 150, 150);
			var frame = -xMajorDivisions * 2;
			var widthPer = Zoom * xMajorDivisions;
			for (float x = xstart - widthPer; x < width; x += widthPer) {
				frame += xMajorDivisions;
				if (x < -widthPer) continue;
				Graphics2D.DrawLine(x, height / 2, x, height);
				Graphics2D.DrawText(x, (height / 2) + 2, $"{frame}", "Noto Sans", 20, Anchor.BottomCenter);

				for (int sx = 0; sx < 4; sx++) {
					var lx = x + ((sx + 1) * (widthPer / 4));
					Graphics2D.DrawLine(lx, (height / 3) * 2, lx, height);
				}
			}

			drawGradient(height);
		}
		public int CalcXMajorDivisions() {
			if (zoom <= 0.9f) return 200;
			if (zoom <= 1.25f) return 100;
			if (zoom <= 3.23f) return 50;
			if (zoom <= 4.85f) return 20;
			if (zoom <= 11f) return 20;
			if (zoom <= 31.2f) return 5;
			if (zoom <= 48) return 2;
			return 1;
		}
		private void KeyframeInfoPanel_PaintOverride(Element self, float width, float height) {
			var tl = ModelEditor.Active.File.Timeline;
			var xstart = defaultXOffset - FrameOffset;

			self.BackgroundColor = new(13, 16, 20);
			self.BorderSize = 0;
			self.Paint(width, height);

			drawGradient(height);
		}

		private void File_AnimationDeactivated(EditorFile file, EditorModel model, EditorAnimation animation) {
			KeyframeInfoPanel.ClearChildren();
		}

		private void File_AnimationActivated(EditorFile file, EditorModel model, EditorAnimation animation) {
			KeyframeInfoPanel.ClearChildren();
		}
	}
}
