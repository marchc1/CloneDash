using Nucleus.Extensions;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using static Nucleus.Util.Util;

namespace Nucleus.ModelEditor.UI
{
	public class AnimationsView : View
	{
		public override string Name => "Animations";

		DropdownSelector<EditorModel> selector;
		ListView listitems;

		public AnimationsView(Element parent) : base(parent) {
			selector = new(this);
			selector.SetDock(Dock.Top);
			selector.OnSelectionChanged += Selector_OnSelectionChanged;
			selector.OnToString += Selector_OnToString;

			listitems = new(this);
			listitems.SetDock(Dock.Fill);
			listitems.SetPaintBackgroundEnabled(false);

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.ModelRemoved += File_ModelRemoved;
			ModelEditor.Active.File.Cleared += File_Cleared;
			ModelEditor.Active.File.AnimationAdded += File_AnimationAdded;
			ModelEditor.Active.File.Loaded += File_Loaded;
		}

		private void File_Loaded(EditorFile file) {
			if (file.Models.Count <= 0) return;
			ClearAndSetupAnimationPanelFor(file.Models[0]);
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
			listitems.SortChildren((x, y) => {
				if (x is not ITextElement iteX) return 0;
				if (y is not ITextElement iteY) return 0;
				return alphanum.Compare(iteX.GetText(), iteY.GetText());
			});
		}


		public class AnimationListViewItem(EditorModel model, EditorAnimation animation, Element parent) : ListViewItem(parent)
		{
			public override void Paint(float width, float height) {
				SetBgColor(model.ActiveAnimation == animation ? DefaultBackgroundColor.Adjust(0, 0.5, 2.4) : DefaultBackgroundColor);
				base.Paint(width, height);
			}
		}
		private void File_AnimationAdded(EditorFile file, EditorModel model, EditorAnimation animation) {
			if (model != selector.Selected) return;

			var lvitem = new AnimationListViewItem(model, animation, listitems);
			var lvitemImg = new Nucleus.UI.Elements.Image(lvitem);
			lvitemImg.SetTexture(Level.Textures.LoadTextureFromFile("models/animation2.png"));
			lvitemImg.SetImageOrientation(ImageOrientation.Fit);
			lvitemImg.SetPassthru(true);
			lvitemImg.SetDock(Dock.Left);
			lvitemImg.SetSize(new(24));
			lvitem.SetText(animation.Name);

			SortAnimations();

			ModelEditor.Active.File.AnimationRenamed += (_, anim, _, name) => {
				if (anim == animation)
					lvitem.SetText(name);

				SortAnimations();
			};

			ModelEditor.Active.File.AnimationRemoved += (_, _, anim) => {
				if (anim == animation)
					lvitem.Remove();
			};

			lvitem.OnButtonClick += (_, _) => {
				file.SetActiveAnimation(model, animation);
			};
		}
	}
}
