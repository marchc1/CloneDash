using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using static Nucleus.ModelEditor.TransformPanel;

namespace Nucleus.ModelEditor.UI;

public class DopesheetView : BaseTimelineView
{
	public override string Name => "Dope Sheet";
	public override bool LockDragDirection => true;

	class InfoPanel(DopesheetView parent) : Panel(parent)
	{
		protected override bool MouseClick(FrameState state, ButtonCode button) {
			parent.dragStart = state.Mouse.MousePos;
			parent.ResetDragDirection(button == ButtonCode.Mouse2, Vector2F.Zero);
			ModelEditor.Active.UnselectAllKeyframes();
			return true;
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			parent.processScroll(delta);
			EngineCore.Window.SetMousePosition(parent.dragStart);
			return true;
		}
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			parent.ResetDragDirection(false, Vector2F.Zero);
			parent.DraggingFrame = false;
			if (button == ButtonCode.Mouse1)
				parent.SetCurFrame();
			return true;
		}
	}

	InfoPanel KeyframeInfoPanel;
	public DopesheetView(Element parent) : base(parent) {
		KeyframeInfoPanel = new(this);
		KeyframeInfoPanel.		Dock = Dock.Fill;
		KeyframeInfoPanel.		Size = new(36);
		KeyframeInfoPanel.		DockMargin = RectangleF.TLRB(0);
		KeyframeInfoPanel.		DockPadding = RectangleF.Zero;

		var btn = TopButtonPanel;

		var copy = AddTopButton("models/copy.png");
		var cut = AddTopButton("models/cut.png");
		var remove = AddTopButton("models/remove.png");
		var paste = AddTopButton("models/paste.png");

		AddTopSpace(16);

		var curve_constant = AddTopButton("models/curve_constant.png");
		var curve_linear = AddTopButton("models/curve_linear.png");
		var curve_bezier = AddTopButton("models/curve_bezier.png");

		curve_constant.Thinking += CurveBtn_DetermineInputState;
		curve_linear.Thinking += CurveBtn_DetermineInputState;
		curve_bezier.Thinking += CurveBtn_DetermineInputState;

		AddTopSpace(16);

		var autobezier = AddTopButton("models/autobezier.png");
		AddTopSpace(16);

		LabeledNumSlider curframeNum = new(TopButtonPanel);
		curframeNum.		Dock = Dock.Left;
		curframeNum.		Text = "Frame";
		curframeNum.		Size = new(128);
		curframeNum.TextFormat = "{0:0.00}";
		// TODO: remove this, fix numslider relying on order so much
		curframeNum.Value = 1;
		curframeNum.Value = 0;

		ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => curframeNum.Value = ModelEditor.Active.File.Timeline.GetVisualPlayhead(false);
		ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => curframeNum.Value = ModelEditor.Active.File.Timeline.GetVisualPlayhead(false);

		KeyframeOverlay.MoveToFront();
	}

	private void CurveBtn_DetermineInputState(Element self) {
		self.SetMouseInputEnabled(!ModelEditor.Active.KeyframesSelected);
	}

	class KeyframeEditorButton(DopesheetView view, BaseTimelineView parent, TimelineKeyframePairs keyframe ) : Button(parent)
	{
		public override void Paint(float w, float h) {
			SetFgColor(
							ModelEditor.Active.IsKeyframeSelected(keyframe)
								? GetBgColor().Adjust(0, 1, 1.3f)
								: GetBgColor().Adjust(0, 1, -0.5f));
			var fps = ModelEditor.Active.File.Timeline.GetReferenceFPS();
			Position = new((float)parent.FrameToX(keyframe.GetTime() * fps) - 2, 0);
			base.Paint(w, h);
		}
		protected override bool MouseClick(FrameState state, ButtonCode button) {
			view.isKeyframeSelected = false;
			view.isDraggingKeyframe = false;
			view.frameStart = 0;
			view.frameDrag = 0;

			view.selected = GetTag<TimelineKeyframePairs>("keyframeInfo");

			if (view.selected == null) return true;

			view.isKeyframeSelected = true;
			view.frameStart = (view.selected?.GetTime() ?? 0) * ModelEditor.Active.File.Timeline.GetReferenceFPS();
#nullable disable
			ModelEditor.Active.SelectKeyframe(view.selected.Value);
#nullable enable
			return base.MouseClick(state, button);
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			var xy = state.Mouse.MousePos - self.GetParent()!.GetGlobalPosition();
			var frameNow = state.Keyboard.ShiftDown ? view.XToFrameExact(xy.X) : view.XToFrame(xy.X);

			if (frameNow != view.frameStart || view.isDraggingKeyframe) {
				view.isDraggingKeyframe = true;
				view.frameDrag = frameNow;
				view.selected?.SetTime(frameNow / ModelEditor.Active.File.Timeline.GetReferenceFPS());
			}

			return base.MouseDrag(self, state, delta);
		}
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			if (!view.isDraggingKeyframe)
				ModelEditor.Active.File.Timeline.SetFrame(view.frameStart);

			view.isKeyframeSelected = false;
			view.isDraggingKeyframe = false;
			return base.MouseRelease(self, state, button);
		}
	}

	public class ChannelPanels(Element parent) : BaseTimelineView(parent)
	{
		public override bool LockDragDirection => false;
		public override void Paint(float width, float height) {
			var target = GetTag<object>("target");

			// Render the background color and border
			base.Paint(width, height);

			// Render overlay lines
			var tl = ModelEditor.Active.File.Timeline;
			var xstart = defaultXOffset - FrameOffset;
			var xMajorDivisions = CalcXMajorDivisions();
			var widthPer = Zoom * xMajorDivisions;
			var frame = -xMajorDivisions * 2;
			var curframe = tl.GetPlayhead();
			float curframeX = (float)FrameToX(curframe);
			for (double x = xstart - widthPer; x < width; x += widthPer) {
				frame += xMajorDivisions;
				if (x < -widthPer || frame < 0) continue;

				var xf = (float)x;
				Graphics2D.SetDrawColor(15, 15, 15);
				Graphics2D.DrawLine(xf, 0, xf, height);
			}

			// Render specific keyframe info.
			/*switch (target) {
				case EditorBone bone:

					break;
				case EditorTimeline timeline:
					var color = timeline.Color;
					var minTime = XToFrameExact(0);
					var maxTime = XToFrameExact(width);
					foreach (var keyframeTime in timeline.GetKeyframeTimes()) {
						// Early skip frames that aren't visible. Saves the FrameToX calculation
						if (keyframeTime < minTime || keyframeTime > maxTime) continue;

						var x = (int)((float)FrameToX(keyframeTime) - 2);
						Graphics2D.SetDrawColor(color);
						Graphics2D.DrawRectangle(x, 0, 5, height);
						Graphics2D.SetDrawColor(15, 15, 15);
						Graphics2D.DrawRectangleOutline(x, 0, 5, height, 1);
					}
					break;
			}*/
		}
	}

	protected override void CreateChannelPanels(out Button header, out Panel keyframes, object? target = null) {
		base.CreateChannelPanels(out header, out keyframes, target);
		keyframes = new ChannelPanels(KeyframeInfoPanel);

		Button headerRef = header;
		keyframes.Thinking += (s) => s.SetBgColor(headerRef.GetBgColor());
		keyframes.		Dock = Dock.Top;
		keyframes.		DockMargin = RectangleF.Zero;
		keyframes.BorderSize = 1;
		keyframes.		Size = new(24);
		keyframes.SetPassthru(true);
		keyframes.SetTag("target", target);
		keyframes.Thinking += (self) => {
			//self.ChildRenderOffset = new(-(float)FrameOffset, 0);
		};

		switch (target) {
			case EditorTimeline timeline:
				foreach (var keyframe in timeline.GetKeyframes()) {
					var x = (float)FrameToX(keyframe.GetTime());
					var keyframeBtn = new KeyframeEditorButton(this, (ChannelPanels)keyframes, keyframe);
					keyframeBtn.					Size = new(5, 24);
					keyframeBtn.					Position = new(x - 2, 0);
					keyframeBtn.SetBgColor(keyframe.Timeline.Color);
					keyframeBtn.BorderSize = 1;
					keyframeBtn.SetFgColor(new Color(15, 15, 15, 255));
					keyframeBtn.					Text = "";
					keyframeBtn.SetPaintBackgroundEnabled(false);
					keyframeBtn.SetPaintBorderEnabled(false);
					keyframeBtn.SetPaintEnabled(false);

					keyframeBtn.SetTag("keyframeInfo", keyframe);
				}
				break;
		}
	}

	TimelineKeyframePairs? selected;
	bool isKeyframeSelected;
	bool isDraggingKeyframe;
	double frameStart;
	double frameDrag;

	public static readonly Color FrameDraggingColor = new(255, 90, 15);

	protected override void PaintTimeOverlay(float width, float height) {
		if (!isKeyframeSelected) return;

		var curframe = isDraggingKeyframe ? frameDrag : frameStart;
		curframe = Math.Max(0, curframe);
		var xDrag = (float)FrameToX(curframe);
		Graphics2D.SetDrawColor(FrameDraggingColor);
		Graphics2D.DrawLine(xDrag, height / 2, xDrag, height);

		string curframeText = $"{(EngineCore.Level.FrameState.Keyboard.ShiftDown ? Math.Round(curframe, 2) : curframe)}";

		RenderGradientFrameText(KeyframeInfoPanel, xDrag, height, curframeText, FrameDraggingColor);
	}

	protected override void PaintPanelOverlay(float width, float height) {
		if (!isKeyframeSelected) return;

		var curframe = isDraggingKeyframe ? frameDrag : frameStart;
		curframe = Math.Max(0, curframe);

		var xDrag = (float)FrameToX(curframe);
		Graphics2D.SetDrawColor(255, 90, 15);
		Graphics2D.DrawLine(xDrag, 0, xDrag, height);
	}

	protected override void OnZoomsChanged() {
		base.OnZoomsChanged();
		KeyframeInfoPanel.InvalidateLayout();
	}
	protected override void OnThink() {
		base.OnThink();
		KeyframeInfoPanel.		ChildRenderOffset = new(0, -ScrollOffset);
		ClipChildrenVisibility(KeyframeInfoPanel);
	}
	public override void CreateChannels() {
		KeyframeInfoPanel.ClearChildren();
		base.CreateChannels();
	}

	Vector2F dragStart;
}
