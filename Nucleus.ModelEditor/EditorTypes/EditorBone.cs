using Newtonsoft.Json;
using Nucleus.Common.Types;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using System.Net.Mail;

namespace Nucleus.ModelEditor
{
	public class EditorBone : PoseableObject, IEditorType, IModel4Nameable, IConstraintBone
	{
		public EditorModel GetModel() => Model;
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

		// hover quad width from the last draw
		[JsonIgnore] private float hoverWidth = 0.2f;

		public void GetTexCoords(float byHowMuch, out Vector2F baseBottom, out Vector2F baseTop, out Vector2F tipBottom, out Vector2F tipTop, out float lengthLimit) {
			var length = Length;
			lengthLimit = Math.Clamp(length, 0f, 230f) / 1f;

			var wt = WorldTransform;

			var xMi = 0f;
			var xMa = length;

			baseBottom = wt.LocalToWorld(new Vector2F(xMi, lengthLimit * byHowMuch));
			baseTop = wt.LocalToWorld(new Vector2F(xMi, -lengthLimit * byHowMuch));
			tipBottom = wt.LocalToWorld(new Vector2F(xMa, lengthLimit * byHowMuch));
			tipTop = wt.LocalToWorld(new Vector2F(xMa, -lengthLimit * byHowMuch));

			hoverWidth = byHowMuch / 5;
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

		/// <summary>
		/// Only active when the active skin lists it
		/// </summary>
		public bool SkinRequired { get; set; }
		/// <summary>
		/// False while skin required and not in the active skin
		/// </summary>
		[JsonIgnore] public bool IsActive { get; internal set; } = true;

		// Constraints
		[JsonIgnore] public BoneApplied Applied;
		IConstraintBone? IConstraintBone.ParentBone => Parent;
		float IConstraintBone.BoneLength => Length;
		TransformMode IConstraintBone.BoneTransformMode => TransformMode;
		ref BoneApplied IConstraintBone.Applied => ref Applied;
		ref Transformation IConstraintBone.World => ref worldTransform;

		public void UpdateWorldTransform(float x, float y, float rotation, float scaleX, float scaleY, float shearX, float shearY) {
			Applied = new() {
				X = x, Y = y,
				Rotation = rotation,
				ScaleX = scaleX, ScaleY = scaleY,
				ShearX = shearX, ShearY = shearY,
				Valid = true
			};
			worldTransform = Transformation.CalculateWorldTransformation(new Vector2F(x, y), rotation,
				new Vector2F(scaleX, scaleY), new Vector2F(shearX, shearY), TransformMode, Parent?.WorldTransform);
			WorldTransformValid = true;
		}

		void IConstraintBone.UpdateWorldTransform() {
			ResetApplied();
			UpdateWorldTransform(Applied.X, Applied.Y, Applied.Rotation, Applied.ScaleX, Applied.ScaleY, Applied.ShearX, Applied.ShearY);
		}

		/// <summary>
		/// Applied transform back to the posed local values
		/// </summary>
		public void ResetApplied() {
			Applied = new() {
				X = SetupPositionX + PositionX, Y = SetupPositionY + PositionY,
				Rotation = SetupRotation + Rotation,
				ScaleX = SetupScaleX * ScaleX, ScaleY = SetupScaleY * ScaleY,
				ShearX = SetupShearX + ShearX, ShearY = SetupShearY + ShearY,
				Valid = true
			};
		}

		/// <summary>
		/// World transform in the setup pose, without constraints
		/// </summary>
		public Transformation CalculateSetupWorldTransform()
			=> Transformation.CalculateWorldTransformation(SetupPosition, SetupRotation, SetupScale, SetupShear, SetupTransformMode, Parent?.CalculateSetupWorldTransform());
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
			var zoom = ModelEditor.Active.Editor.CameraZoom;
			if (Length <= 0)
				return gridPos.Distance(WorldTransform.Translation) < 1.5f * Math.Clamp(zoom, 0, 2);
			// The drawn quad lags behind at high fps.
			var world = WorldTransform;
			float width = Math.Clamp(Length, 0f, 230f) * hoverWidth;
			return gridPos.TestPointInQuad(
				world.LocalToWorld(new Vector2F(0, width)), world.LocalToWorld(new Vector2F(0, -width)),
				world.LocalToWorld(new Vector2F(Length, width)), world.LocalToWorld(new Vector2F(Length, -width)));
		}

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
			PropertiesPanel.DeleteOperator(this, props, determinations);
			PropertiesPanel.RenameOperator(this, props, determinations);
			PropertiesPanel.DuplicateOperator(this, props, determinations);
		}

		public Vector2F GetWorldPosition() => WorldTransform.Translation;
		public void SetWorldPosition(Vector2F pos, bool additive = false) {
			Vector2F posF;

			Transformation? wt = Parent == null ? null : Parent.WorldTransform;
			if (additive) {
				var wp = GetWorldPosition();
				posF = wt == null ? (wp - pos) : (wt ?? throw new Exception()).WorldToLocal(wp - pos);
			}
			else posF = wt == null ? pos : (wt ?? throw new Exception()).WorldToLocal(pos);


			if (AnimationMode) Position = posF - SetupPosition;
			else SetupPosition = posF;
		}
		public float GetWorldRotation() => WorldTransform.LocalToWorldRotation(0) + GetRotation();
		public float GetScreenRotation() {
			var wp = WorldTransform.Translation;
			var wl = WorldTransform.LocalToWorld(ScaleX, 0);
			var d = (wl - wp);
			var r = MathF.Atan2(d.Y, d.X).ToDegrees();
			return r;
		}

		public void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var separateRow = PropertiesPanel.NewRow(props, "Separate", "models/separate.png");
			props.Thinking += (_) => {
				separateRow.GetParent()!.SetVisible(ModelEditor.Active.AnimationMode);
				separateRow.SetMouseInputEnabled(ModelEditor.Active.File.ActiveAnimation != null);
			};


			var separateTranslation = PropertiesPanel.AddLabeledCheckbox(separateRow, "Translate", ModelEditor.Active.File.ActiveAnimation?.DoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Translation) ?? false);
			var separateScale = PropertiesPanel.AddLabeledCheckbox(separateRow, "Scale", ModelEditor.Active.File.ActiveAnimation?.DoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Scale) ?? false);
			var separateShear = PropertiesPanel.AddLabeledCheckbox(separateRow, "Shear", ModelEditor.Active.File.ActiveAnimation?.DoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Shear) ?? false);

			separateTranslation.OnCheckedChanged += (s) => ModelEditor.Active.File.SetDoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Translation, s.Checked);
			separateScale.OnCheckedChanged += (s) => ModelEditor.Active.File.SetDoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Scale, s.Checked);
			separateShear.OnCheckedChanged += (s) => ModelEditor.Active.File.SetDoesBoneHaveSeparatedProperty(this, KeyframeProperty.Bone_Shear, s.Checked);

			var transformRow = PropertiesPanel.NewRow(props, "Transform", "models/bonetransform.png");

			var boneTransformData = SetupTransformMode.Unpack();
			var boneRotation = PropertiesPanel.AddLabeledCheckbox(transformRow, "Rotation", boneTransformData.Rotation);
			var boneScale = PropertiesPanel.AddLabeledCheckbox(transformRow, "Scale", boneTransformData.Scale);
			var boneReflection = PropertiesPanel.AddLabeledCheckbox(transformRow, "Reflection", boneTransformData.Reflection);
			Checkbox.CheckboxClicked transformChanged = _ => {
				var mode = TransformTools.Pack(boneRotation.Checked, boneScale.Checked, boneReflection.Checked);
				ModelEditor.Active.File.SetProperty(this, "transform", mode, () => SetupTransformMode,
					v => SetupTransformMode = v);
				var unpacked = mode.Unpack();
				boneRotation.Checked = unpacked.Rotation;
				boneScale.Checked = unpacked.Scale;
				boneReflection.Checked = unpacked.Reflection;
			};
			boneRotation.OnCheckedChanged += transformChanged;
			boneScale.OnCheckedChanged += transformChanged;
			boneReflection.OnCheckedChanged += transformChanged;

			var lengthRow = PropertiesPanel.NewRow(props, "Length", "models/bonelength.png");

			var boneLength = PropertiesPanel.AddNumSlider(lengthRow, Length);
			boneLength.MinimumValue = 0;
			boneLength.OnValueChanged += (_, _, v) => ModelEditor.Active.File.SetBoneLength(this, (float)v);

			var viewportRow = PropertiesPanel.NewRow(props, "Viewport", "models/info.png");

			var boneViewportIconContainer = PropertiesPanel.AddInternalPropPanel(viewportRow);
			var boneViewportName = PropertiesPanel.AddLabeledCheckbox(viewportRow, "Name", ViewportShowName);
			var boneViewportSelectable = PropertiesPanel.AddLabeledCheckbox(viewportRow, "Selectable", ViewportCanSelect);
			boneViewportName.OnCheckedChanged += (s) => ModelEditor.Active.File.SetProperty(this, "show name",
				s.Checked, () => ViewportShowName, v => ViewportShowName = v);
			boneViewportSelectable.OnCheckedChanged += (s) => ModelEditor.Active.File.SetProperty(this, "selectable",
				s.Checked, () => ViewportCanSelect, v => ViewportCanSelect = v);

			var skinRow = PropertiesPanel.NewRow(props, "Skin", "models/skin.png");
			var skinRequired = PropertiesPanel.AddLabeledCheckbox(skinRow, "Skin required", SkinRequired);
			skinRequired.OnCheckedChanged += (s) => ModelEditor.Active.File.SetProperty(this, "skin required",
				s.Checked, () => SkinRequired, v => SkinRequired = v);
			skinRow.Thinking += (_) => skinRow.SetMouseInputEnabled(!ModelEditor.Active.AnimationMode);

			var boneColorRow = PropertiesPanel.NewRow(props, "Color", "models/colorwheel.png");
			var boneColor = PropertiesPanel.AddColorSelector(boneColorRow, Color);
			boneColor.SelectedColor = Color;
			boneColor.OnColorChanged += (_, ref color) => ModelEditor.Active.File.SetBoneColor(this, color);
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.NewMenu(buttons, [
						new("Bone", () => {
							var bone = ModelEditor.Active.File.AddBone(Model, this, null);
							ModelEditor.Active.SelectObject(bone.ResultOrThrow);
						}),
						new("Slot", () => PropertiesPanel.NewSlotDialog(ModelEditor.Active.File, this)),
						new("IK Constraint", () => NewConstraint(UI.Operators.NewConstraintKind.IK)),
						new("Transform Constraint", () => NewConstraint(UI.Operators.NewConstraintKind.Transform)),
						new("Path Constraint", () => NewConstraint(UI.Operators.NewConstraintKind.Path)),
					]);
			if (Parent != null)
				PropertiesPanel.OperatorButton<UI.Operators.SetParentOperator>(buttons, "Set Parent",
					"models/setparent.png");
		}

		private static void NewConstraint(UI.Operators.NewConstraintKind kind) {
			var problem = UI.Operators.NewConstraintOperator.Problem(kind);
			if (problem != null) {
				UI.EditorDialogs.ConfirmAction("New constraint", problem);
				return;
			}
			ModelEditor.Active.File.ActivateOperator(new UI.Operators.NewConstraintOperator { Kind = kind });
		}

		public string? GetName() => Name;
		IEditorType? IEditorType.GetTransformParent() => Parent;
		public bool IsNameTaken(string name) => Model.GetAllBones().FirstOrDefault(x => x.Name == name) != null;
		public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameBone(this, newName);
		public EditorResult Remove() => ModelEditor.Active.File.RemoveBone(this);

		public bool CanTranslate() => true;
		public bool CanRotate() => true;
		public bool CanScale() => true;
		public bool CanShear() => true;

		public float GetTranslationX(UserTransformMode transform = UserTransformMode.LocalSpace)
			=> transform == UserTransformMode.WorldSpace ? WorldTransform.X : SetupPositionX + PositionX;
		public float GetTranslationY(UserTransformMode transform = UserTransformMode.LocalSpace)
			=> transform == UserTransformMode.WorldSpace ? WorldTransform.Y : SetupPositionY + PositionY;

		public float GetRotation(UserTransformMode transform = UserTransformMode.LocalSpace) => transform == UserTransformMode.WorldSpace ? GetWorldRotation() : SetupRotation + Rotation;
		public float GetScaleX() => SetupScaleX * ScaleX;
		public float GetScaleY() => SetupScaleY * ScaleY;
		public float GetShearX() => SetupShearX + ShearX;
		public float GetShearY() => SetupShearY + ShearY;

		private bool AnimationMode => ModelEditor.Active.AnimationMode;

		public void EditTranslationX(float value, UserTransformMode transform = UserTransformMode.LocalSpace) {
			if (AnimationMode) PositionX = value - SetupPositionX;
			else SetupPositionX = value;
		}

		public void EditTranslationY(float value, UserTransformMode transform = UserTransformMode.LocalSpace) {
			if (AnimationMode) PositionY = value - SetupPositionY;
			else SetupPositionY = value;
		}

		public void EditRotation(float value, bool localCoordinates = true) {
			if (!localCoordinates)
				value = WorldTransform.WorldToLocalRotation(value);

			if (AnimationMode)
				Rotation = value - SetupRotation;
			else
				SetupRotation = value is >= 0 and < 360 ? value : (float)(((double)value % 360 + 360) % 360);
		}
		public void EditScaleX(float value) {
			if (AnimationMode)
				ScaleX = SetupScaleX == 0 ? value : value / SetupScaleX;
			else
				SetupScaleX = value;
		}
		public void EditScaleY(float value) {
			if (AnimationMode)
				ScaleY = SetupScaleY == 0 ? value : value / SetupScaleY;
			else
				SetupScaleY = value;
		}
		public void EditShearX(float value) {
			if (AnimationMode)
				ShearX = value - SetupShearX;
			else
				SetupShearX = value;
		}
		public void EditShearY(float value) {
			if (AnimationMode)
				ShearY = value - SetupShearY;
			else
				SetupShearY = value;
		}

		public bool Hidden { get; set; }
	}
}