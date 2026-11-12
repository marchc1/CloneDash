using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

/// <summary>
/// Onion skin settings
/// </summary>
public class GhostingView : View
{
	public override string Name => "Ghosting";

	static GhostSettings s => Ghosting.Settings;

	public GhostingView(Element parent) : base(parent) {
		var body = new Panel(this);
		body.Dock = Dock.Fill;
		body.DockPadding = RectangleF.TLRB(4);
		body.SetPaintBackgroundEnabled(false);
		body.SetPaintBorderEnabled(false);

		var frames = PropertiesPanel.NewRow(body, "Frames", "models/keyframe.png");
		Check(frames, "Before", () => s.FramesBefore, v => s.FramesBefore = v);
		Slider(frames, () => s.BeforeFrames, v => s.BeforeFrames = v, 0, 60);
		Check(frames, "After", () => s.FramesAfter, v => s.FramesAfter = v);
		Slider(frames, () => s.AfterFrames, v => s.AfterFrames = v, 0, 60);
		var frameStep = PropertiesPanel.NewRow(body, "Frame step");
		Slider(frameStep, () => s.FrameStep, v => s.FrameStep = v, 0.1f, 30);

		var keys = PropertiesPanel.NewRow(body, "Key frames", "models/keyframe.png");
		Check(keys, "Before", () => s.KeysBefore, v => s.KeysBefore = v);
		Slider(keys, () => s.KeysBeforeFrames, v => s.KeysBeforeFrames = v, 0, 60);
		Check(keys, "After", () => s.KeysAfter, v => s.KeysAfter = v);
		Slider(keys, () => s.KeysAfterFrames, v => s.KeysAfterFrames = v, 0, 60);
		var keyStep = PropertiesPanel.NewRow(body, "Key step");
		Slider(keyStep, () => s.KeyStep, v => s.KeyStep = v, 1, 10);

		var colors = PropertiesPanel.NewRow(body, "Display", "models/colorwheel.png");
		Check(colors, "Current", () => s.Current, v => s.Current = v);
		var before = PropertiesPanel.AddColorSelector(colors, s.BeforeColor);
		before.OnColorChanged += (_, ref c) => s.BeforeColor = c;
		var after = PropertiesPanel.AddColorSelector(colors, s.AfterColor);
		after.OnColorChanged += (_, ref c) => s.AfterColor = c;

		var options = PropertiesPanel.NewRow(body, "Options", "models/info.png");
		Check(options, "Anchor", () => s.Anchor, v => s.Anchor = v);
		Check(options, "On top", () => s.OnTop, v => s.OnTop = v);
		Check(options, "Loop", () => s.Loop, v => s.Loop = v);

		var offset = PropertiesPanel.NewRow(body, "Offset", "models/translate.png");
		Slider(offset, () => s.OffsetX, v => s.OffsetX = v, -50, 50, "X ");
		Slider(offset, () => s.OffsetY, v => s.OffsetY = v, -50, 50, "Y ");

		var selection = PropertiesPanel.NewRow(body, "Selection", "models/bone.png");
		Check(selection, "Selected bones only", () => s.SelectedBones, v => s.SelectedBones = v);
	}

	private static void Check(Panel row, string text, Func<bool> get, Action<bool> set) {
		var box = PropertiesPanel.AddLabeledCheckbox(row, text, get());
		box.OnCheckedChanged += (c) => set(c.Checked);
	}

	private static void Slider(Panel row, Func<float> get, Action<float> set, float min, float max, string prefix = "") {
		var slider = PropertiesPanel.AddNumSlider(row, get());
		slider.MinimumValue = min;
		slider.MaximumValue = max;
		slider.Digits = 1;
		slider.Prefix = prefix;
		slider.SetValueNoUpdate(get());
		slider.OnValueChanged += (_, _, v) => set((float)v);
	}
}
