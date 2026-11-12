using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor.UI
{
	public class PlaybackView : View
	{
		public override string Name => "Playback";

		public PlaybackView(Element parent) : base(parent) {
			Form form = new(this);
			form.Dock = Dock.Top;

			NumSlider fps = form.Add("Timeline FPS", row => new NumSlider(row));
			fps.MinimumValue = 0;
			fps.MaximumValue = 72;
			fps.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.FPS = (int)(float)v;
			fps.Digits = 0;
			fps.TextFormat = "{0} FPS";
			fps.Value = ModelEditor.Active.File.Timeline.FPS;

			NumSlider speed = form.Add("Speed", row => new NumSlider(row));
			speed.MinimumValue = 0.01;
			speed.MaximumValue = 3.3;
			speed.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.Speed = v;
			speed.TextFormat = "{0:P2}";
			speed.Value = ModelEditor.Active.File.Timeline.Speed;

			FlexPanel btns = form.Add(null, row => new FlexPanel(row));
			btns.SetPaintBackgroundEnabled(false);
			btns.SetPaintBorderEnabled(false);
			btns.DockPadding = RectangleF.Zero;
			btns.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			btns.Direction = Axis.Horizontal;

			var stepped = new CheckboxButton(btns);
			stepped.			Text = "Stepped";
			stepped.DockMargin = RectangleF.TLRB(0, 0, 2, 0);
			stepped.Checked = ModelEditor.Active.File.Timeline.Stepped;
			stepped.OnCheckedChanged += (checkbox) => {
				ModelEditor.Active.File.Timeline.Stepped = checkbox.Checked;
				ModelEditor.Active.QueueAnimationUpdate();
			};

			var interp = new CheckboxButton(btns);
			interp.			Text = "Interpolated";
			interp.DockMargin = RectangleF.TLRB(0, 2, 0, 0);
			interp.Checked = ModelEditor.Active.File.Timeline.Interpolated;
			interp.OnCheckedChanged += (checkbox) => {
				ModelEditor.Active.File.Timeline.Interpolated = checkbox.Checked;
				ModelEditor.Active.QueueAnimationUpdate();
			};


			ModelEditor.Active.File.Loaded += (file) => {
				fps.Value = file.Timeline.FPS;
				speed.Value = file.Timeline.Speed;
				stepped.Checked = file.Timeline.Stepped;
				interp.Checked = file.Timeline.Interpolated;
			};
		}
	}
}
