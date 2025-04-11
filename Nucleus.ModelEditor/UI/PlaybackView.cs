using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI
{
	public class RowLabelContainer : Panel {
		Label label;

		protected override void Initialize() {
			base.Initialize();
			Add(out label);
			Dock = Dock.Top;
			BorderSize = 0;
			label.Text = Text;
			label.Dock = Dock.Left;
			label.AutoSize = true;
		}

		public override void TextChanged(string oldText, string newText) {
			label.Text = newText;
		}

		public override void ChildParented(Element parent, Element child) {
			base.ChildParented(parent, child);
			child.Dock = Dock.Fill;
			child.MoveToFront();
		}
	}
	public class PlaybackView : View
	{
		public override string Name => "Playback";

		protected override void Initialize() {
			base.Initialize();

			Add(out RowLabelContainer row1);
			row1.Text = "Timeline FPS";
			row1.Add(out NumSlider fps);
			fps.MinimumValue = 0;
			fps.MaximumValue = 72;
			fps.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.FPS = (int)(float)v;
			fps.Digits = 0;
			fps.TextFormat = "{0} FPS";
			fps.Value = ModelEditor.Active.File.Timeline.FPS;

			Add(out RowLabelContainer row2);
			row2.Text = "Speed";
			row2.Add(out NumSlider speed);
			speed.MinimumValue = 0.01;
			speed.MaximumValue = 3.3;
			speed.OnValueChanged += (_, _, v) => ModelEditor.Active.File.Timeline.Speed = v;
			speed.TextFormat = "{0:P2}";
			speed.Value = ModelEditor.Active.File.Timeline.Speed;

			Add(out FlexPanel btns);
			btns.Dock = Dock.Top;
			btns.ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection;
			btns.Direction = Types.Directional180.Horizontal;

			var stepped = btns.Add<CheckboxButton>();
			stepped.Text = "Stepped";
			stepped.AutoSize = true;
			stepped.DockMargin = RectangleF.TLRB(-1, 2, 2, -1);
			stepped.Checked = ModelEditor.Active.File.Timeline.Stepped;
			stepped.OnCheckedChanged += (c) => ModelEditor.Active.File.Timeline.Stepped = c.Checked;

			var interp = btns.Add<CheckboxButton>();
			interp.Text = "Interpolated";
			interp.AutoSize = true;
			interp.DockMargin = RectangleF.TLRB(-1, 2, 2, -1);
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
