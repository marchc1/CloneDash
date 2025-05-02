using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

public class DopesheetView : BaseTimelineView
{
	public override string Name => "Dope Sheet";
	public override bool LockDragDirection => true;

	public Panel KeyframeInfoPanel;

	protected override void Initialize() {
		base.Initialize();
		Add(out KeyframeInfoPanel);
		KeyframeInfoPanel.Dock = Dock.Fill;
		KeyframeInfoPanel.Size = new(36);
		KeyframeInfoPanel.DockMargin = RectangleF.TLRB(0);
		KeyframeInfoPanel.DockPadding = RectangleF.Zero;
		KeyframeInfoPanel.MouseClickEvent += KeyframeInfoPanel_MouseClickEvent;
		KeyframeInfoPanel.MouseDragEvent += KeyframeInfoPanel_MouseDragEvent;
		KeyframeInfoPanel.MouseReleaseEvent += KeyframeInfoPanel_MouseReleaseEvent;

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

		TopButtonPanel.Add(out LabeledNumSlider curframeNum);
		curframeNum.Dock = Dock.Left;
		curframeNum.Text = "Frame";
		curframeNum.Size = new(128);
		curframeNum.TextFormat = "{0:0.00}";
		// TODO: remove this, fix numslider relying on order so much
		curframeNum.Value = 1;
		curframeNum.Value = 0;

		ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => curframeNum.Value = ModelEditor.Active.File.Timeline.GetVisualPlayhead(false);
		ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => curframeNum.Value = ModelEditor.Active.File.Timeline.GetVisualPlayhead(false);

		KeyframeOverlay.MoveToFront();
	}

	private void CurveBtn_DetermineInputState(Element self) {
		self.InputDisabled = !ModelEditor.Active.KeyframesSelected;
	}

	protected override void CreateChannelPanels(out Button header, out Panel keyframes, object? target = null) {
		base.CreateChannelPanels(out header, out keyframes, target);
		KeyframeInfoPanel.Add(out keyframes);

		Button headerRef = header;
		keyframes.Thinking += (s) => s.BackgroundColor = headerRef.BackgroundColor;
		keyframes.Dock = Dock.Top;
		keyframes.DockMargin = RectangleF.Zero;
		keyframes.BorderSize = 1;
		keyframes.Size = new(24);
		keyframes.OnHoverTest += Passthru;
		keyframes.SetTag("target", target);
		keyframes.PaintOverride += Keyframes_PaintOverride;
		keyframes.Thinking += (self) => {
			//self.ChildRenderOffset = new(-(float)FrameOffset, 0);
		};

		switch (target) {
			case EditorTimeline timeline:
				foreach (var keyframe in timeline.GetKeyframes()) {
					var x = (float)FrameToX(keyframe.GetTime());
					var keyframeBtn = keyframes.Add<Button>();
					keyframeBtn.Size = new(5, 24);
					keyframeBtn.Position = new(x - 2, 0);
					keyframeBtn.BackgroundColor = keyframe.Timeline.Color;
					keyframeBtn.BorderSize = 1;
					keyframeBtn.ForegroundColor = new(15, 15, 15, 255);
					keyframeBtn.Text = "";
					keyframeBtn.PaintOverride += (self, w, h) => {
						self.ForegroundColor =
							ModelEditor.Active.IsKeyframeSelected(keyframe)
								? self.BackgroundColor.Adjust(0, 1, 1.3f)
								: self.BackgroundColor.Adjust(0, 1, -0.5f);
						var fps = ModelEditor.Active.File.Timeline.GetReferenceFPS();
						self.Position = new((float)FrameToX(keyframe.GetTime() * fps) - 2, 0);
						self.Paint(w, h);
					};

					keyframeBtn.SetTag("keyframeInfo", keyframe);
					keyframeBtn.MouseClickEvent += KeyframeBtn_MouseClickEvent;
					keyframeBtn.MouseDragEvent += KeyframeBtn_MouseDragEvent;
					keyframeBtn.MouseReleasedOrLostEvent += KeyframeBtn_MouseReleaseEvent;
				}
				break;
		}
	}

	TimelineKeyframePairs? selected;
	bool isKeyframeSelected;
	bool isDraggingKeyframe;
	double frameStart;
	double frameDrag;

	private void KeyframeBtn_MouseClickEvent(Element self, FrameState state, MouseButton button) {
		isKeyframeSelected = false;
		isDraggingKeyframe = false;
		frameStart = 0;
		frameDrag = 0;

		selected = self.GetTag<TimelineKeyframePairs>("keyframeInfo");

		if (selected == null) return;

		isKeyframeSelected = true;
		frameStart = (selected?.GetTime() ?? 0) * ModelEditor.Active.File.Timeline.GetReferenceFPS();
#nullable disable
		ModelEditor.Active.SelectKeyframe(selected.Value);
#nullable enable
	}
	private void KeyframeBtn_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
		var xy = state.Mouse.MousePos - self.Parent.GetGlobalPosition();
		var frameNow = state.Keyboard.ShiftDown ? XToFrameExact(xy.X) : XToFrame(xy.X);

		if (frameNow != frameStart || isDraggingKeyframe) {
			isDraggingKeyframe = true;
			frameDrag = frameNow;
			selected?.SetTime(frameNow / ModelEditor.Active.File.Timeline.GetReferenceFPS());
		}
	}
	private void KeyframeBtn_MouseReleaseEvent(Element self, FrameState state, MouseButton button, bool lost) {
		if (!isDraggingKeyframe)
			ModelEditor.Active.File.Timeline.SetFrame(frameStart);

		isKeyframeSelected = false;
		isDraggingKeyframe = false;
	}

	public static readonly Raylib_cs.Color FrameDraggingColor = new(255, 90, 15);

	protected override void PaintTimeOverlay(float width, float height) {
		if (!isKeyframeSelected) return;

		var curframe = isDraggingKeyframe ? frameDrag : frameStart;
		curframe = Math.Max(0, curframe);
		var xDrag = (float)FrameToX(curframe);
		Graphics2D.SetDrawColor(FrameDraggingColor);
		Graphics2D.DrawLine(xDrag, height / 2, xDrag, height);

		string curframeText = $"{(EngineCore.CurrentFrameState.Keyboard.ShiftDown ? Math.Round(curframe, 2) : curframe)}";

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
	protected override void OnZoomsChanged() {
		base.OnZoomsChanged();
		KeyframeInfoPanel.InvalidateLayout();
	}
	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		KeyframeInfoPanel.ChildRenderOffset = new(0, -ScrollOffset);
		ClipChildrenVisibility(KeyframeInfoPanel);
	}
	public override void CreateChannels() {
		KeyframeInfoPanel.ClearChildren();
		base.CreateChannels();
	}

	Vector2F dragStart;
	private void KeyframeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
		dragStart = state.Mouse.MousePos;

		ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
		ModelEditor.Active.UnselectAllKeyframes();
	}

	private void KeyframeInfoPanel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
		processScroll(delta);
		EngineCore.Window.SetMousePosition(dragStart);
	}

	private void KeyframeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		ResetDragDirection(false, Vector2F.Zero);
		DraggingFrame = false;
		if (button == MouseButton.Mouse1)
			SetCurFrame();
	}
}
