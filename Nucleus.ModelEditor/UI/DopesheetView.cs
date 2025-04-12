using Nucleus.Core;
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

		KeyframeOverlay.MoveToFront();
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
		keyframes.PassMouseTo(KeyframeInfoPanel); // this is only used as a background for keyframes
		keyframes.SetTag("target", target);
		keyframes.PaintOverride += Keyframes_PaintOverride;
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
		switch (target) {
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
		}
	}
	protected override void OnZoomsChanged() {
		base.OnZoomsChanged();
		KeyframeInfoPanel.InvalidateLayout();
	}
	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		KeyframeInfoPanel.ChildRenderOffset = new(0, -ScrollOffset);
	}
	public override void CreateChannels() {
		KeyframeInfoPanel.ClearChildren();
		base.CreateChannels();
	}

	private void KeyframeInfoPanel_MouseClickEvent(Element self, FrameState state, MouseButton button) {
		ResetDragDirection(button == MouseButton.Mouse2, Vector2F.Zero);
	}
	private void KeyframeInfoPanel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
		processScroll(delta);
	}

	private void KeyframeInfoPanel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		ResetDragDirection(false, Vector2F.Zero);
		DraggingFrame = false;
	}
}
