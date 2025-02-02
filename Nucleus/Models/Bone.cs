using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models
{
	public class Bone : IValidatable
	{
		[JsonIgnore] private float __length = 0;
		public float Length{
			get => __length;
			set => __length = Math.Max(value, 0);
		}
		public string Name { get; set; }
		public Model Model { get; set; }
		public Bone? Parent { get; set; }
		public List<Bone> Children { get; set; } = [];
		public List<Slot> Slots { get; set; } = [];

		private bool __isvalid = true;
		public void Invaldiate() => __isvalid = false;
		public bool IsValid() => __isvalid && IValidatable.IsValid(Model);

		public float Rotation { get; set; } = 0;
		public Vector2F Translation { get; set; } = Vector2F.Zero;
		public Vector2F Scale { get; set; } = Vector2F.One;
		public Vector2F Shear { get; set; } = Vector2F.Zero;

		public bool InheritRotation { get; set; } = true;
		public bool InheritScale { get; set; } = true;
		public bool InheritReflection { get; set; } = true;

		public void EditMatrix() {
			Rlgl.Translatef(Translation.X, Translation.Y, 0);
			Rlgl.Rotatef(Rotation, 0, 0, 1);
		}

		public ReturnResult<Bone> AddBone(string name) {
			if (Model.TryFindBone(name, out Bone? bone))
				return new(null, $"The bone named '{name}' already exists.");

			Bone b = new Bone() {
				Name = name,
				Model = Model,
				Parent = this
			};
			Children.Add(b);
			Model.InvalidateBonesList();

			return new(b);
		}

		public ReturnResult<Slot> AddSlot(string name) {
			if (Model.TryFindSlot(name, out Slot? slot))
				return new(null, $"The slot named '{name}' already exists.");

			Slot s = new Slot();
			s.Name = name;
			Slots.Add(s);
			Model.Slots.Add(s);

			return new(s);
		}
	}
}
