using Newtonsoft.Json;
using Nucleus.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models
{
	public class Model : IValidatable
	{
		public string Name { get; set; }
		public Bone Root { get; set; }

		[JsonIgnore] private List<Bone> allbones = [];
		[JsonIgnore] private bool allBonesInvalid = true;

		public List<Slot> Slots { get; } = [];

		public void InvalidateBonesList() => allBonesInvalid = true;
		private void addBoneAndChildrenIntoBones(Bone bone) {
			allbones.Add(bone);
			foreach (var child in bone.Children) addBoneAndChildrenIntoBones(child);
		}
		public List<Bone> GetAllBones() {
			if (allBonesInvalid) {
				allbones.Clear();
				addBoneAndChildrenIntoBones(Root);
			}

			return allbones;
		}
		public bool TryFindBone(string name, out Bone? oBone) {
			foreach (var bone in GetAllBones()) {
				if (bone.Name == name) {
					oBone = bone;
					return true;
				}
			}

			oBone = null;
			return false;
		}

		public Bone? FindBone(string name) => TryFindBone(name, out var bone) ? bone : null;

		public bool TryFindSlot(string name, out Slot? oSlot) {
			foreach (var slot in Slots) {
				if (slot.Name == name) {
					oSlot = slot;
					return true;
				}
			}

			oSlot = null;
			return false;
		}

		public Slot? FindSlot(string name) => TryFindSlot(name, out var slot) ? slot : null;

		private bool __isvalid = true;
		public void Invaldiate() => __isvalid = false;
		public bool IsValid() => __isvalid;

		public static Model New() {
			Model model = new Model();
			model.Name = "model";

			model.Root = new Bone() {
				Model = model,
				Name = "root"
			};

			model.Root.Rotation = 45;

			var boneTest = model.Root.AddBone("test");
			boneTest.Result.Translation = new(50, 0);
			boneTest.Result.Length = 250;

			return model;
		}
	}
}
