using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public enum BlendMode {
		Normal,
		Additive,
		Multiply,
		Screen
	}

	public class EditorBone
	{
		[JsonIgnore] private float __length = 0;
		public float Length{
			get => __length;
			set => __length = Math.Max(value, 0);
		}
		public string Name { get; set; }

		[JsonIgnore] private EditorModel model;
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
				parent = value;
				model?.InvalidateBonesList();
			}
		}

		/// <summary>
		/// A null icon means it is automatically decided (lengthless_bone for length <= 0, lengthy_bone otherwise)
		/// </summary>
		public string? Icon { get; set; } = null;
		/// <summary>
		/// Always show this bones name when rendering in the viewport
		/// </summary>
		public bool ViewportShowName { get; set; } = false;
		/// <summary>
		/// Can the bone be selected via the viewport?
		/// </summary>
		public bool ViewportCanSelect { get; set; } = true;

		/// <summary>
		/// The color of the bone in the viewport and outliner
		/// </summary>
		public Color Color { get; set; } = Color.WHITE;

		public List<EditorBone> Children { get; set; } = [];
		public List<EditorSlot> Slots { get; set; } = [];

		public float Rotation { get; set; } = 0;
		public Vector2F Translation { get; set; } = Vector2F.Zero;
		public Vector2F Scale { get; set; } = Vector2F.One;
		public Vector2F Shear { get; set; } = Vector2F.Zero;

		public TransformMode TransformMode { get; set; } = TransformMode.Normal;

		public bool InheritRotation { get; set; } = true;
		public bool InheritScale { get; set; } = true;
		public bool InheritReflection { get; set; } = true;

		public void EditMatrix() {
			Rlgl.Translatef(Translation.X, Translation.Y, 0);
			Rlgl.Rotatef(Rotation, 0, 0, 1);
		}
	}
}
