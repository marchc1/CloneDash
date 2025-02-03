using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorModel
	{
		public string Name { get; set; }
		public EditorBone Root { get; set; }

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

		public bool TryFindBone(string name, [NotNullWhen(true)] out EditorBone? bone) {
			bone = FindBone(name);
			return bone != null;
		}

		public bool TryFindSlot(string name, [NotNullWhen(true)] out EditorSlot? slot) {
			slot = FindSlot(name);
			return slot != null;
		}

		private ModelImages? images;
		public ModelImages Images {
			get {
				if(images == null) {
					images = new(this);
				}

				return images;
			}
			set => images = value;
		}

	}
}
