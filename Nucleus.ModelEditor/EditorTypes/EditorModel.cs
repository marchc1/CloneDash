using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models
{
	public class EditorModel : IValidatable
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
			}

			return allbones;
		}
		public bool TryFindBone(string name, out EditorBone? oBone) {
			foreach (var bone in GetAllBones()) {
				if (bone.Name == name) {
					oBone = bone;
					return true;
				}
			}

			oBone = null;
			return false;
		}

		public EditorBone? FindBone(string name) => TryFindBone(name, out var bone) ? bone : null;

		public bool TryFindSlot(string name, out EditorSlot? oSlot) {
			foreach (var slot in Slots) {
				if (slot.Name == name) {
					oSlot = slot;
					return true;
				}
			}

			oSlot = null;
			return false;
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

		public EditorSlot? FindSlot(string name) => TryFindSlot(name, out var slot) ? slot : null;

		private bool __isvalid = true;
		public void Invaldiate() => __isvalid = false;
		public bool IsValid() => __isvalid;
	}
}
