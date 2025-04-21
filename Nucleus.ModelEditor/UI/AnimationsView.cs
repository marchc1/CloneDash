using Nucleus.UI;
using Nucleus.UI.Elements;
using static Nucleus.Util.Util;

namespace Nucleus.ModelEditor.UI
{
	public class AnimationsView : View
	{
		public override string Name => "Animations";

		DropdownSelector<EditorModel> selector;
		ListView listitems;

		protected override void Initialize() {
			base.Initialize();

			Add(out selector);
			selector.Dock = Dock.Top;
			selector.OnSelectionChanged += Selector_OnSelectionChanged;
			selector.OnToString += Selector_OnToString;

			listitems = Add<ListView>();
			listitems.Dock = Dock.Fill;
			listitems.DrawPanelBackground = false;

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.ModelRemoved += File_ModelRemoved;
			ModelEditor.Active.File.Cleared += File_Cleared;
			ModelEditor.Active.File.AnimationAdded += File_AnimationAdded;
		}

		private void Selector_OnSelectionChanged(DropdownSelector<EditorModel> self, EditorModel oldValue, EditorModel newValue) {
			ClearAndSetupAnimationPanelFor(newValue);
		}

		private string? Selector_OnToString(EditorModel? item) => item?.Name;

		private void Active_SelectedChanged() {
			if (ModelEditor.Active.SelectedObjectsCount == 0) return;
			if (ModelEditor.Active.SelectedObjectsCount > 1) return;

			ClearAndSetupAnimationPanelFor(ModelEditor.Active.FirstSelectedObject?.GetModel());
		}
		private void ClearAndSetupAnimationPanelFor(EditorModel? model) {
			selector.Selected = model;
			listitems.ClearChildren();

			if (model == null) return;
			foreach (var anim in selector.Selected.Animations) {
				File_AnimationAdded(ModelEditor.Active.File, model, anim);
			}
		}
		private void File_ModelRemoved(EditorFile file, EditorModel model) {
			selector.Items.Remove(model);
		}

		private void File_ModelAdded(EditorFile file, EditorModel model) {
			selector.Items.Add(model);
		}

		private void File_Cleared(EditorFile file) {
			listitems.ClearChildren();
			selector.Items.Clear();
			selector.Selected = null;
		}

		AlphanumComparatorFast alphanum = new AlphanumComparatorFast();

		public void SortAnimations() {
			listitems.SortChildren((x, y) => alphanum.Compare(x.Text, y.Text));
		}

		private void File_AnimationAdded(EditorFile file, EditorModel model, EditorAnimation animation) {
			if (model != selector.Selected) return;

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
