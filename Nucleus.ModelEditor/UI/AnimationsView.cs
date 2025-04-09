using Nucleus.UI;
using static Nucleus.Util.Util;

namespace Nucleus.ModelEditor.UI
{
	public class AnimationsView : View
	{
		public override string Name => "Animations";

		ListView listitems;

		protected override void Initialize() {
			base.Initialize();

			listitems = Add<ListView>();
			listitems.Dock = Dock.Fill;
			listitems.DrawPanelBackground = false;

			ModelEditor.Active.File.Cleared += File_Cleared;
			ModelEditor.Active.File.AnimationAdded += File_AnimationAdded;
		}

		private void File_Cleared(EditorFile file) {
			listitems.ClearChildren();
		}

		AlphanumComparatorFast alphanum = new AlphanumComparatorFast();

		public void SortAnimations() {
			listitems.SortChildren((x, y) => alphanum.Compare(x.Text, y.Text));
		}

		private void File_AnimationAdded(EditorFile file, EditorModel model, EditorAnimation animation) {
			listitems.Add(out ListViewItem lvitem);
			lvitem.Image = Textures.LoadTextureFromFile("models/animation2.png");
			lvitem.Text = animation.Name;

			SortAnimations();

			ModelEditor.Active.File.AnimationRenamed += (_, anim, _, name) => {
				if (anim == animation)
					lvitem.Text = name;

				SortAnimations();
			};

			ModelEditor.Active.File.AnimationRemoved += (_, _, anim) => {
				if (anim == animation)
					lvitem.Remove();
			};

			lvitem.PaintOverride += (self, w, h) => {
				self.BackgroundColor = model.ActiveAnimation == animation ? DefaultBackgroundColor.Adjust(0, 0.5, 2.4) : DefaultBackgroundColor;
				self.Paint(w, h);
			};

			lvitem.MouseReleaseEvent += (_, _, _) => {
				file.SetActiveAnimation(model, animation);
			};
		}
	}
}
