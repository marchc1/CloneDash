using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI
{
	public class RowLabelContainer : Panel, ITextElement
	{
		Label label;
		public RowLabelContainer(Element parent) : base(parent) {
			label = new(this);
			Dock = Dock.Top;
			BorderSize = 0;
			label.			Text = "";
			label.			Dock = Dock.Left;
			label.SetAutoSize(true);
			label.SetTextPadding(new(16));
		}

		protected override void ChildParented(Element parent, Element child) {
			base.ChildParented(parent, child);
			child.			Dock = Dock.Fill;
			child.MoveToFront();
		}

		public ReadOnlySpan<char> Font { get => ((ITextElement)label).Font; set => ((ITextElement)label).Font = value; }
		public ReadOnlySpan<char> Text { get => ((ITextElement)label).Text; set => ((ITextElement)label).Text = value; }

		public float TextSize { get => ((ITextElement)label).TextSize; set => ((ITextElement)label).TextSize = value; }
	}
	public class PlaybackView : View
	{
		public override string Name => "Playback";

		public PlaybackView(Element parent) : base(parent) {
			RowLabelContainer row1 = new(this);
			row1.			Text = "Timeline FPS";
			NumSlider fps = new(row1);
			fps.MinimumValue = 0;
			fps.MaximumValue = 72;
			fps.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.FPS = (int)(float)v;
			fps.Digits = 0;
			fps.TextFormat = "{0} FPS";
			fps.Value = ModelEditor.Active.File.Timeline.FPS;

			RowLabelContainer row2 = new(this);
			row2.			Text = "Speed";
			NumSlider speed =new(row2);
			speed.MinimumValue = 0.01;
			speed.MaximumValue = 3.3;
			speed.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.Speed = v;
			speed.TextFormat = "{0:P2}";
			speed.Value = ModelEditor.Active.File.Timeline.Speed;

			FlexPanel btns = new(this);
			btns.			Dock = Dock.Top;
			btns.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			btns.Direction = Axis.Horizontal;

			var stepped = new CheckboxButton(btns);
			stepped.			Text = "Stepped";
			stepped.SetAutoSize(true);
			stepped.			DockMargin = RectangleF.TLRB(-1, 2, 2, -1);
			stepped.Checked = ModelEditor.Active.File.Timeline.Stepped;
			stepped.OnCheckedChanged += (c) => ModelEditor.Active.File.Timeline.Stepped = c.Checked;

			var interp = new CheckboxButton(btns);
			interp.			Text = "Interpolated";
			interp.SetAutoSize(true);
			interp.			DockMargin = RectangleF.TLRB(-1, 2, 2, -1);
			interp.Checked = ModelEditor.Active.File.Timeline.Interpolated;
			interp.OnCheckedChanged += (c) => ModelEditor.Active.File.Timeline.Interpolated = c.Checked;


			ModelEditor.Active.File.Loaded += (file) => {
				fps.Value = file.Timeline.FPS;
				speed.Value = file.Timeline.Speed;
				stepped.Checked = file.Timeline.Stepped;
				interp.Checked = file.Timeline.Interpolated;
			};
		}
	}
}
