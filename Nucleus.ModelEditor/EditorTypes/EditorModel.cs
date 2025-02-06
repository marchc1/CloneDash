using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorModel : IEditorType
	{
		public string Name { get; set; }
		public EditorBone Root { get; set; }

		public bool Export { get; set; } = true;

		[JsonIgnore] private List<EditorBone> allbones = [];
		[JsonIgnore] private bool allBonesInvalid = true;

		/// <summary>
		/// A list of slots, in order of drawing order.
		/// </summary>
		public List<EditorSlot> Slots { get; } = [];

		public void InvalidateBonesList() => allBonesInvalid = true;
		private void addBoneAndChildrenIntoBones(EditorBone bone) {
			allbones.Add(bone);
			foreach (var child in bone.Children) addBoneAndChildrenIntoBones(child);
		}
		public List<EditorBone> GetAllBones() {
			if (allBonesInvalid) {
				allbones.Clear();
				addBoneAndChildrenIntoBones(Root);
				allBonesInvalid = false;
			}

			return allbones;
		}

		public EditorBone? FindBone(string name) => GetAllBones().FirstOrDefault(x => x.Name == name);
		public EditorSlot? FindSlot(string name) => Slots.FirstOrDefault(x => x.Name == name);
		public EditorSkin? FindSkin(string name) => Skins.FirstOrDefault(x => x.Name == name);

		public bool TryFindBone(string name, [NotNullWhen(true)] out EditorBone? bone) {
			bone = FindBone(name);
			return bone != null;
		}

		public bool TryFindSlot(string name, [NotNullWhen(true)] out EditorSlot? slot) {
			slot = FindSlot(name);
			return slot != null;
		}

		public bool TryFindSkin(string name, [NotNullWhen(true)] out EditorSkin? skin) {
			skin = FindSkin(name);
			return skin != null;
		}


		private EditorSkin? activeSkin = null;
		public EditorSkin? ActiveSkin {
			get => activeSkin; 
			set => activeSkin = value;
		}
		public SkinsList Skins { get; set; } = [];

		public EditorModel() {
			Skins.Model = this;
		}

		private ModelImages? images;
		public ModelImages Images {
			get {
				if (images == null) {
					images = new();
					images.Model = this;
				}

				return images;
			}
			set => images = value;
		}

		public string GetName() => Name;
		public bool IsNameTaken(string name) => ModelEditor.Active.File.Models.FirstOrDefault(x => x.Name == name) != null;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameModel(this, newName);
		public EditorResult Remove() => ModelEditor.Active.File.RemoveModel(this);
		public bool CanDelete() => true;
		public bool HoverTest(Vector2F gridPos) => false;
		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) { }
		public void BuildProperties(Panel props, PreUIDeterminations determinations) { }
		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {}
		public string SingleName => "model";
		public string PluralName => "models";
		public ViewportSelectMode SelectMode => ViewportSelectMode.NotApplicable;

		[JsonIgnore] public bool Hovered { get; set; } = false;
		[JsonIgnore] public bool Selected { get; set; } = false;

		public bool Hidden { get; set; }
		public bool CanHide() => true;
	}
}
