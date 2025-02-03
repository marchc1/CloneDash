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
	public class EditorBone : IValidatable
	{
		[JsonIgnore] private float __length = 0;
		public float Length{
			get => __length;
			set => __length = Math.Max(value, 0);
		}
		public string Name { get; set; }

		private EditorModel model;
		public EditorModel Model {
			get => model;
			set {
				model = value;
				model.InvalidateBonesList();
			}
		}

		private EditorBone? parent;
		public EditorBone? Parent {
			get => parent;
			set {
				if(parent != value) {
					if (parent != null)
						parent.Children.Remove(this);
					if (value != null)
						value.Children.Add(this);
					parent = value;
				}
			}
		}
		public List<EditorBone> Children { get; set; } = [];
		public List<EditorSlot> Slots { get; set; } = [];

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
	}
}
