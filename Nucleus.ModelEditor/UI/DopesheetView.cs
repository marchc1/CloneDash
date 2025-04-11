using glTFLoader.Schema;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using System.Diagnostics;
using Color = Raylib_cs.Color;

namespace Nucleus.ModelEditor.UI
{
	public class DopesheetView : View
	{
		public override string Name => "Dope Sheet";

		private double frameOffset = 0;
		private float scrollOffset = 0;
		private double zoom = DefaultZoom;
		/// <summary>
		/// The frame offset. Left == offset.
		/// </summary>
		public double FrameOffset {
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
		public double Zoom {
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
		public Panel KeyframeOverlay;
		public Panel KeyframeInfoPanel;
		public Panel KeyframeChannelsPanel;
		public NumSlider ZoomSlider;

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);
			KeyframeInfoPanel.ChildRenderOffset = new(0, -ScrollOffset);
			KeyframeChannelsPanel.ChildRenderOffset = new(0, -ScrollOffset);
		}

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
			ZoomSlider.OnValueChanged += (_, _, v) => {
				var oob = FrameOutOfBounds(GetCurFrame());
				var xpos = oob ? TimeInfoPanel.RenderBounds.W / 2 : FrameToX(GetCurFrame());
				var centerXBefore = XToFrameExact(xpos);
				Zoom = v;
				var centerXAfter = XToFrameExact(xpos);
				var deltaFrame = centerXAfter - centerXBefore;
				var frameScaling = centerXAfter / centerXBefore;
				Console.WriteLine($"{xpos}, {deltaFrame}");

				if (FrameOffset > 0) {
					FrameOffset = (FrameOffset - deltaFrame) / frameScaling;
				}
			};

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

			ButtonsAndNames.Add(out KeyframeChannelsPanel);
			KeyframeChannelsPanel.Dock = Dock.Fill;
			KeyframeChannelsPanel.DockMargin = RectangleF.TLRB(0);
			KeyframeChannelsPanel.DockPadding = RectangleF.Zero;
			KeyframeChannelsPanel.BorderSize = 0;

			// Setup buttons
			{
				Buttons.Add(out Button jumpStart); SetupButton(jumpStart, true, true, false); jumpStart.Image = Textures.LoadTextureFromFile("models/jumpStart.png");
				Buttons.Add(out Button jumpPrevious); SetupButton(jumpPrevious, true, false, true); jumpPrevious.Image = Textures.LoadTextureFromFile("models/jumpPrevious.png");
				Buttons.Add(out Button playBackward); SetupButton(playBackward, false, true, false); playBackward.Image = Textures.LoadTextureFromFile("models/playBackward.png");
				Buttons.Add(out Button playForward); SetupButton(playForward, false, false, true); playForward.Image = Textures.LoadTextureFromFile("models/playForward.png");
				Buttons.Add(out Button jumpNext); SetupButton(jumpNext, true, true, false); jumpNext.Image = Textures.LoadTextureFromFile("models/jumpNext.png");
				Buttons.Add(out Button jumpEnd); SetupButton(jumpEnd, true, false, true); jumpEnd.Image = Textures.LoadTextureFromFile("models/jumpEnd.png");
				Buttons.Add(out Button loop); SetupButton(loop, true, true, true); loop.Image = Textures.LoadTextureFromFile("models/loop.png");

				playBackward.Thinking += (s) => {
					var timeline = ModelEditor.Active.File.Timeline;

					s.Image = timeline.PlayingBackwards
								? Textures.LoadTextureFromFile("models/stop.png") : timeline.PlayingForwards
								? Textures.LoadTextureFromFile("models/backReset.png") : Textures.LoadTextureFromFile("models/playBackward.png");

					s.BackgroundColor = timeline.PlayingBackwards ? Color.SkyBlue : DefaultBackgroundColor;
				};

				playForward.Thinking += (s) => {
					var timeline = ModelEditor.Active.File.Timeline;

					s.Image = timeline.PlayingForwards
								? Textures.LoadTextureFromFile("models/stop.png") : timeline.PlayingBackwards
								? Textures.LoadTextureFromFile("models/forwardReset.png") : Textures.LoadTextureFromFile("models/playForward.png");
					s.BackgroundColor = timeline.PlayingForwards ? Color.SkyBlue : DefaultBackgroundColor;
				};

				playBackward.MouseReleaseEvent += (_, _, _) => {
					var timeline = ModelEditor.Active.File.Timeline;
					timeline.TogglePlayBackwards();
				};

				playForward.MouseReleaseEvent += (_, _, _) => {
					var timeline = ModelEditor.Active.File.Timeline;
					timeline.TogglePlayForwards();
				};
			}

			Add(out TimeInfoPanel);
			TimeInfoPanel.Dock = Dock.Top;
			TimeInfoPanel.Size = new(36);
			TimeInfoPanel.DockMargin = RectangleF.TLRB(0);
			TimeInfoPanel.DockPadding = RectangleF.Zero;
			TimeInfoPanel.PaintOverride += TopButtonsAndTimeInfo_PaintOverride;
			TimeInfoPanel.MouseClickEvent += TimeInfoPanel_MouseClickEvent;
			TimeInfoPanel.MouseDragEvent += TimeInfoPanel_MouseDragEvent;
			TimeInfoPanel.MouseReleaseEvent += TimeInfoPanel_MouseReleaseEvent;

			Add(out KeyframeInfoPanel);
			KeyframeInfoPanel.Dock = Dock.Fill;
			KeyframeInfoPanel.Size = new(36);
			KeyframeInfoPanel.DockMargin = RectangleF.TLRB(0);
			KeyframeInfoPanel.DockPadding = RectangleF.Zero;
			KeyframeInfoPanel.MouseClickEvent += KeyframeInfoPanel_MouseClickEvent;
			KeyframeInfoPanel.MouseDragEvent += KeyframeInfoPanel_MouseDragEvent;
			KeyframeInfoPanel.MouseReleaseEvent += KeyframeInfoPanel_MouseReleaseEvent;

			Add(out KeyframeOverlay);
			KeyframeOverlay.OnHoverTest += Passthru;
			KeyframeOverlay.Thinking += (s) => s.SetRenderBounds(KeyframeInfoPanel.RenderBounds);
			KeyframeOverlay.DockMargin = RectangleF.TLRB(0);
			KeyframeOverlay.DockPadding = RectangleF.Zero;
			KeyframeOverlay.PaintOverride += KeyframeInfoPanel_PaintOverride;
		}

		private void TimeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(false, Vector2F.Zero);
			DraggingFrame = false;
		}

		private void KeyframeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(false, Vector2F.Zero);
			DraggingFrame = false;
		}

		private bool DraggingX = false;
		private bool DraggingY = false;
		private bool Dragging = false;
		private bool DraggingFrame = false;
		private Vector2F startAt = Vector2F.Zero;

		private double frameAtDragStart = 0;
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
			if (!Dragging) return;
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
			if (!Dragging) return;
			delta *= -1;
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
			if (DraggingFrame)
				SetCurFrame();
		}
		private void KeyframeInfoPanel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			processScroll(delta);
		}



		private void TimeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
			DraggingFrame = button == MouseButton.Mouse1;

			if (DraggingFrame)
				SetCurFrame();
		}

		private void KeyframeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
			ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
		}

		public double GetCurFrame() => ModelEditor.Active.File.Timeline.Frame;
		public void SetCurFrame() {
			var xLocal = TimeInfoPanel.GetMousePos();
			ModelEditor.Active.File.Timeline.SetFrame(XToFrame(xLocal.X));
		}

		private void drawGradient(float height) {
			var r = (float)NMath.Remap(frameOffset, 0f, 30f, 0f, 1f, false, true);
			var c = new Color(0, 0, 0, (int)(r * 150));
			Graphics2D.DrawGradient(new(0, 0), new(12, height), c, Color.Blank, Dock.Right);
		}

		private void Buttons_PaintOverride(Element self, float width, float height) {
			Graphics2D.SetDrawColor(130, 135, 142);
			Graphics2D.DrawLine(0, height, width, height);
		}

		// Shared offset from leftmost -> frame 0.
		private float defaultXOffset = 22;
		public static Color FrameMarkerColor => new(0, 255, 255);
		private void TopButtonsAndTimeInfo_PaintOverride(Element self, float width, float height) {
			var tl = ModelEditor.Active.File.Timeline;

			var curframe = tl.Frame;

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
			float curframeX = (float)FrameToX(curframe);

			var curframeText = $"{Math.Round(curframe)}";

			Vector2F frameTextSize = Graphics2D.GetTextSize(curframeText, "Noto Sans", 20);

			for (double x = xstart - widthPer; x < width; x += widthPer) {
				frame += xMajorDivisions;
				if (x < -widthPer || frame < 0) continue;

				var xf = (float)x;

				if (curframe != frame) {
					Graphics2D.DrawLine(xf, height / 2, xf, height);
				}

				var closeness = Math.Abs(curframeX - x);
				if (closeness > (frameTextSize.X * 1.5f))
					Graphics2D.DrawText(xf, (height / 2) + 2, $"{frame}", "Noto Sans", 20, Anchor.BottomCenter);

				var maxMinor = xMajorDivisions == 2 ? 1 : xMajorDivisions == 1 ? 0 : 4;
				for (int sx = 0; sx < maxMinor; sx++) {
					var lx = x + ((sx + 1) * (widthPer / (maxMinor + 1)));
					Graphics2D.DrawLine((float)lx, (height / 3) * 2, (float)lx, height);
				}
			}

			var textX = (float)Math.Clamp(curframeX, 10, width - 10);
			Graphics2D.SetDrawColor(self.BackgroundColor);

			var rectPos = new Vector2F(textX - (frameTextSize.W / 2), (height / 2) - frameTextSize.H);
			Graphics2D.DrawRectangle(rectPos, frameTextSize);
			var colorGradientEnd = new Color(self.BackgroundColor.R, self.BackgroundColor.G, self.BackgroundColor.B, (byte)0);
			Graphics2D.DrawGradient(rectPos - new Vector2F(12, 0), new(12, frameTextSize.H), self.BackgroundColor, colorGradientEnd, Dock.Left);
			Graphics2D.DrawGradient(rectPos + new Vector2F(frameTextSize.W, 0), new(12, frameTextSize.H), self.BackgroundColor, colorGradientEnd, Dock.Right);

			Graphics2D.SetDrawColor(FrameMarkerColor);

			Graphics2D.DrawLine(curframeX, height / 2, curframeX, height);
			Graphics2D.DrawText(textX, (height / 2) + 2, curframeText, "Noto Sans", 20, Anchor.BottomCenter);
			int tX = 4;
			var oob = FrameOutOfBounds(curframe);
			if (!oob) {
				Graphics2D.DrawTriangle(new(curframeX, height / 1.4f), new(curframeX + tX, height / 2), new(curframeX - tX, height / 2));
			}

			drawGradient(height);
		}

		public double FrameToX(double frame)
			=> (defaultXOffset - FrameOffset) + (frame * Zoom);
		public double FrameToX(int frame)
			=> (defaultXOffset - FrameOffset) + (frame * Zoom);

		public int XToFrame(double x)
			=> (int)Math.Round((x - defaultXOffset + FrameOffset) / Zoom);

		public double XToFrameExact(double x)
			=> (x - defaultXOffset + FrameOffset) / Zoom;

		public bool FrameOutOfBounds(double frame) {
			var x = FrameToX(frame);
			return x <= 14 || x >= TimeInfoPanel.RenderBounds.W;
		}
		public bool FrameOutOfBounds(int frame) {
			var x = FrameToX(frame);
			return x <= 14 || x >= TimeInfoPanel.RenderBounds.W;
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

			var curframe = GetCurFrame();
			float curframeX = (float)FrameToX(curframe);

			Graphics2D.SetDrawColor(FrameMarkerColor);
			Graphics2D.DrawLine(curframeX, 0, curframeX, height);

			drawGradient(height);
		}

		/// <summary>
		/// Determines if the dopesheet listens to hooks relating
		/// </summary>
		public bool ShouldListenToHooks { get; private set; }

		private void File_AnimationDeactivated(EditorFile file, EditorModel model, EditorAnimation animation) {
			KeyframeInfoPanel.ClearChildren();
			KeyframeChannelsPanel.ClearChildren();
			ShouldListenToHooks = false;
		}

		private void File_AnimationActivated(EditorFile file, EditorModel model, EditorAnimation animation) {
			KeyframeInfoPanel.ClearChildren();
			ShouldListenToHooks = true;
			CreateChannels();
		}

		public void SetupHooks() {
			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			ModelEditor.Active.File.TimelineCreated += (_, _) => Active_SelectedChanged();
		}
		public static Color HEADER_SELECTED_COLOR => new(115, 145, 145);
		public static Color HEADER_UNSELECTED_COLOR => new(104, 119, 119);


		private void CreateChannelPanels(out Button header, out Panel keyframes, object? target = null) {
			KeyframeChannelsPanel.Add(out header);
			KeyframeInfoPanel.Add(out keyframes);

			keyframes.Dock = Dock.Top;
			keyframes.DockMargin = RectangleF.Zero;
			keyframes.BorderSize = 1;
			keyframes.Size = new(24);
			keyframes.PassMouseTo(KeyframeInfoPanel); // this is only used as a background for keyframes
			keyframes.SetTag("target", target);
			keyframes.PaintOverride += Keyframes_PaintOverride;

			header.Dock = Dock.Top;
			header.DockMargin = RectangleF.Zero;
			header.BorderSize = 1;
			header.Size = new(24);
			header.ForegroundColor = new(10, 10, 10);
			header.TextAlignment = Anchor.CenterLeft;

		}
		public void SetupBoneChannel(object target) {
			CreateChannelPanels(out Button header, out Panel keyframes, target);
			switch (target) {
				case EditorAnimation animation:
					header.BackgroundColor = HEADER_SELECTED_COLOR;
					header.TextPadding = new(8, 0);
					header.Text = animation.Name;
					header.TextSize = 17;

					keyframes.BackgroundColor = HEADER_SELECTED_COLOR;

					if (ModelEditor.Active.SelectedObjectsCount > 0) {
						HashSet<EditorBone> foundBones = [];

						foreach (var selected in ModelEditor.Active.SelectedObjects) {
							EditorBone? representingBone = null;

							if (selected is EditorBone bone)
								representingBone = bone;
							else if (selected is EditorSlot slot)
								representingBone = slot.Bone;

							if (representingBone != null && foundBones.Add(representingBone))
								SetupBoneChannel(representingBone);
						}
					}
					else {
						List<EditorBone> bones = animation.GetAffectedBones();
						foreach (var bone in bones) {
							SetupBoneChannel(bone);
						}
					}
					break;
				case EditorBone bone:
					keyframes.BackgroundColor = bone.Selected ? HEADER_SELECTED_COLOR : HEADER_UNSELECTED_COLOR;
					header.BackgroundColor = bone.Selected ? HEADER_SELECTED_COLOR : HEADER_UNSELECTED_COLOR;

					header.Text = bone.Name;
					header.TextPadding = new(24, 0);
					header.TextSize = 16;
					header.Image = Textures.LoadTextureFromFile("models/bone.png");
					header.ImageOrientation = ImageOrientation.Centered;
					header.ImageFollowsText = true;

					header.MouseClickEvent += (_, _, _) => {
						ModelEditor.Active.SelectObject(bone);
					};

					// Get timelines in order
					var anim = ModelEditor.Active.File.ActiveAnimation;
					Debug.Assert(anim != null);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Rotation, -1);

					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Translation, -1);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Translation, 0);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Translation, 1);

					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Scale, -1);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Scale, 0);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Scale, 1);

					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Shear, -1);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Shear, 0);
					SearchPropertyThenCreatePanel(anim, bone, KeyframeProperty.Bone_Shear, 1);
					break;
			}
		}

		private void SearchPropertyThenCreatePanel(EditorAnimation anim, EditorBone bone, KeyframeProperty property, int arrayIndex = -1) {
			var timeline = anim.SearchTimelineByProperty(bone, property, arrayIndex, false);
			if (timeline == null) return;

			CreateChannelPanels(out Button header, out Panel keyframes, timeline);

			header.Text = $"{property switch {
				KeyframeProperty.Bone_Rotation => "Rotate",
				KeyframeProperty.Bone_Translation => "Translate",
				KeyframeProperty.Bone_Scale => "Scale",
				KeyframeProperty.Bone_Shear => "Shear",
				_ => "N/A",
			}}{(arrayIndex == -1 ? "" : $" {arrayIndex switch {
				0 => "X",
				1 => "Y",
				_ => throw new Exception($"Inavlid array index (expected 0 for X, 1 for Y, but got {arrayIndex})")
			}}")}";

			header.Image = Textures.LoadTextureFromFile($"models/{property switch {
				KeyframeProperty.Bone_Rotation => "rotate_color",
				KeyframeProperty.Bone_Translation => "translate_color",
				KeyframeProperty.Bone_Scale => "scale_color",
				KeyframeProperty.Bone_Shear => "shear_color",
				_ => "N/A",
			}}{(arrayIndex == -1 ? "" : $"_{arrayIndex switch {
				0 => "x",
				1 => "y",
				_ => throw new Exception($"Inavlid array index (expected 0 for X, 1 for Y, but got {arrayIndex})")
			}}")}.png");

			header.ImageFollowsText = true;
			header.ImageOrientation = ImageOrientation.Centered;
			header.TextPadding = new(38, 0);
			header.ImagePadding = new(4);

			header.Thinking += (s) => {
				bool selected = ModelEditor.Active.SelectedObjectsCount > 0 && property switch {
					KeyframeProperty.Bone_Rotation => ModelEditor.Active.Editor.DefaultOperatorType == EditorDefaultOperator.RotateSelection,
					KeyframeProperty.Bone_Translation => ModelEditor.Active.Editor.DefaultOperatorType == EditorDefaultOperator.TranslateSelection,
					KeyframeProperty.Bone_Scale => ModelEditor.Active.Editor.DefaultOperatorType == EditorDefaultOperator.ScaleSelection,
					KeyframeProperty.Bone_Shear => ModelEditor.Active.Editor.DefaultOperatorType == EditorDefaultOperator.ShearSelection,
					_ => false
				};
				var selectedInt = selected ? 80 : 45;
				var color = new Color(selectedInt, selectedInt + 3, selectedInt + 7);
				header.BackgroundColor = color;
				keyframes.BackgroundColor = color;
			};

		}

		private void Keyframes_PaintOverride(Element self, float width, float height) {
			var target = self.GetTag<object>("target");

			// Render the background color and border
			self.Paint(width, height);

			// Render overlay lines
			var tl = ModelEditor.Active.File.Timeline;
			var xstart = defaultXOffset - FrameOffset;
			var xMajorDivisions = CalcXMajorDivisions();
			var widthPer = Zoom * xMajorDivisions;
			var frame = -xMajorDivisions * 2;
			var curframe = tl.Frame;
			float curframeX = (float)FrameToX(curframe);
			for (double x = xstart - widthPer; x < width; x += widthPer) {
				frame += xMajorDivisions;
				if (x < -widthPer || frame < 0) continue;

				var xf = (float)x;
				Graphics2D.SetDrawColor(15, 15, 15);
				Graphics2D.DrawLine(xf, 0, xf, height);
			}

			// Render specific keyframe info.
			switch (target) {
				case EditorBone bone:

					break;
			}
		}

		public void CreateChannels() {
			KeyframeChannelsPanel.ClearChildren();
			KeyframeInfoPanel.ClearChildren();

			var animation = ModelEditor.Active.File.ActiveAnimation;
			if (animation == null) return;

			SetupBoneChannel(animation);
		}

		private void Active_SelectedChanged() {
			if (!ShouldListenToHooks) return;
			CreateChannels();
		}
	}
}
