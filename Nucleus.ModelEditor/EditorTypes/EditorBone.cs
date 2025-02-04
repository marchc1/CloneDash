using Newtonsoft.Json;
using Nucleus.Engine;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
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

	public class EditorBone : IEditorType
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

		public string SingleName => "bone";
		public string PluralName => "bones";
		public ViewportSelectMode SelectMode => ViewportSelectMode.Bones;
		public bool CanDelete() {
			if (Model.Root == this)
				return false;
			return true;
		}
		public bool HoverTest() {
			return false;
		}

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}

		public void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var transformRow = PropertiesPanel.NewRow(props, "Transform", "models/bonetransform.png");

			var boneTransformData = TransformMode.Unpack();
			var boneRotation = PropertiesPanel.AddLabeledCheckbox(transformRow, "Rotation", boneTransformData.Rotation);
			var boneScale = PropertiesPanel.AddLabeledCheckbox(transformRow, "Scale", boneTransformData.Scale);
			var boneReflection = PropertiesPanel.AddLabeledCheckbox(transformRow, "Reflection", boneTransformData.Reflection);

			var lengthRow = PropertiesPanel.NewRow(props, "Length", "models/bonelength.png");

			var boneLength = PropertiesPanel.AddNumSlider(lengthRow, Length);
			boneLength.MinimumValue = 0;
			boneLength.OnValueChanged += (_, _, v) => ModelEditor.Active.File.SetBoneLength(this, (float)v);

			var viewportRow = PropertiesPanel.NewRow(props, "Viewport", "models/info.png");

			var boneViewportIconContainer = PropertiesPanel.AddInternalPropPanel(viewportRow);
			var boneViewportName = PropertiesPanel.AddLabeledCheckbox(viewportRow, "Name", ViewportShowName);
			var boneViewportSelectable = PropertiesPanel.AddLabeledCheckbox(viewportRow, "Selectable", ViewportCanSelect);

			var boneColorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");
			var boneColor = PropertiesPanel.AddColorSelector(boneColorRow, Color);
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
						new("Bone", () => ModelEditor.Active.File.AddBone(Model, this, null)),
						new("Slot", () => PropertiesPanel.NewSlotDialog(ModelEditor.Active.File, this)),
					]);
		}

		public string? GetName() => Name;
		public bool IsNameTaken(string name) => Model.GetAllBones().FirstOrDefault(x => x.Name == name) != null;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameBone(this, newName);
		public EditorResult Remove() => ModelEditor.Active.File.RemoveBone(this);
	}
}
