using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class EditorBone : PoseableObject, IEditorType
	{

		[JsonIgnore] public bool Hovered { get; set; } = false;
		[JsonIgnore] public bool Selected { get; set; } = false;
		public bool CanHide() => true;

		[JsonIgnore] private float __length = 0;
		public float Length {
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

		[JsonIgnore] private Vector2F q1, q2, q3, q4;

		public void GetTexCoords(float byHowMuch, out Vector2F baseBottom, out Vector2F baseTop, out Vector2F tipBottom, out Vector2F tipTop, out float lengthLimit) {
			var length = Length;
			lengthLimit = Math.Clamp(length, 0f, 230f) / 2f;

			var wt = WorldTransform;

			var xMi = 0f;
			var xMa = length;

			baseBottom = wt.LocalToWorld(new Vector2F(xMi, lengthLimit * byHowMuch));
			baseTop = wt.LocalToWorld(new Vector2F(xMi, -lengthLimit * byHowMuch));
			tipBottom = wt.LocalToWorld(new Vector2F(xMa, lengthLimit * byHowMuch));
			tipTop = wt.LocalToWorld(new Vector2F(xMa, -lengthLimit * byHowMuch));

			byHowMuch /= 5;

			q1 = wt.LocalToWorld(new Vector2F(xMi, lengthLimit * byHowMuch));
			q2 = wt.LocalToWorld(new Vector2F(xMi, -lengthLimit * byHowMuch));
			q3 = wt.LocalToWorld(new Vector2F(xMa, lengthLimit * byHowMuch));
			q4 = wt.LocalToWorld(new Vector2F(xMa, -lengthLimit * byHowMuch));
		}

		private EditorBone? parent;
		public EditorBone? Parent {
			get => parent;
			set {
				parent = value;
				model?.InvalidateBonesList();
			}
		}

		public override PoseableObject? GetParent() => Parent;
		public override IEnumerable<PoseableObject> GetChildren() => Children;

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
		public Color Color { get; set; } = new Color(170, 255);

		public List<EditorBone> Children { get; set; } = [];
		public List<EditorSlot> Slots { get; set; } = [];

		public string SingleName => "bone";
		public string PluralName => "bones";
		public ViewportSelectMode SelectMode => ViewportSelectMode.Bones;
		public bool CanDelete() {
			if (Model.Root == this)
				return false;
			return true;
		}
		public bool HoverTest(Vector2F gridPos) {
			if (Length <= 0)
				return gridPos.Distance(WorldTransform.Translation) < 16;
			else
				return gridPos.TestPointInQuad(q1, q2, q3, q4);
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
			boneColor.ColorChanged += (_, c) => {
				ModelEditor.Active.File.SetBoneColor(this, c);
			};
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

		public bool CanTranslate() => true;
		public bool CanRotate() => true;
		public bool CanScale() => true;
		public bool CanShear() => true;

		public float GetTranslationX() => Position.X;
		public float GetTranslationY() => Position.Y;
		public float GetRotation() => Rotation;
		public float GetScaleX() => Scale.X;
		public float GetScaleY() => Scale.Y;
		public float GetShearX() => Shear.X;
		public float GetShearY() => Shear.Y;

		public void EditTranslationX(float value) => SetupPositionX = value;
		public void EditTranslationY(float value) => SetupPositionY = value;
		public void EditRotation(float value) => SetupRotation = value;
		public void EditScaleX(float value) => SetupScaleX = value;
		public void EditScaleY(float value) => SetupScaleY = value;
		public void EditShearX(float value) => SetupShearX = value;
		public void EditShearY(float value) => SetupShearY = value;
		
		public bool Visible { get; set; }
	}
}