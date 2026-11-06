using Newtonsoft.Json;

using Nucleus.Common.Types;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using Raylib_cs;

namespace Nucleus.ModelEditor;

public abstract partial class EditorConstraint
{
	[JsonIgnore] public abstract string EditorIcon { get; }
	[JsonIgnore] public abstract Color DisplayColor { get; }

	public bool CanHide() => true;
	public bool CanDelete() => true;
	public bool CanRename() => true;
	public IEditorType? DeferTransformationsTo() => null;
	public bool IsNameTaken(string name) => Model.GetAllConstraints().Any(x => x != this && x.Name == name);

	/// <summary>
	/// What the key button next to it in the tree keys
	/// </summary>
	[JsonIgnore] protected virtual KeyframeProperty TreeKey => KeyframeProperty.None;
	public bool CanKeyframe() => TreeKey != KeyframeProperty.None;
	public bool GetKeyframeParameters(
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEditorType? target,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out KeyframeProperty property,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out int arrayIndex) {
		target = this;
		property = TreeKey;
		arrayIndex = -1;
		return CanKeyframe();
	}
	public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameConstraint(this, newName);
	public EditorResult Remove() => ModelEditor.Active.File.RemoveConstraint(this);

	/// <summary>
	/// Does it run under this skin?
	/// </summary>
	public virtual bool IsActiveIn(EditorSkin? skin) =>
		Bones.Count > 0 && (!SkinRequired || (skin != null && skin.Constraints.Contains(this)));

	internal abstract void Solve(bool animating);

	public virtual void RenderViewport() { }

	protected bool Highlighted {
		get {
			if (Selected || Hovered) return true;
			foreach (var bone in Bones) {
				if (bone.Selected) return true;
			}
			return false;
		}
	}

	public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
		PropertiesPanel.DeleteOperator(this, props, determinations);
		PropertiesPanel.RenameOperator(this, props, determinations);
	}

	public virtual void BuildOperators(Panel buttons, PreUIDeterminations determinations) { }

	public void BuildProperties(Panel props, PreUIDeterminations determinations) {
		var file = ModelEditor.Active.File;

		var orderRow = PropertiesPanel.NewRow(props, "Order", "models/draworder.png");
		var order = ConstraintProps.Slider(orderRow, Order, 0);
		order.MinimumValue = 0;
		order.MaximumValue = Math.Max(0, Model.GetAllConstraints().Count() - 1);
		order.OnValueChanged += (_, _, v) => file.SetConstraintOrder(this, (int)Math.Round(v));
		ConstraintProps.SetupOnly(orderRow);

		var skinRow = PropertiesPanel.NewRow(props, "Skin", "models/skin.png");
		var skinRequired = PropertiesPanel.AddLabeledCheckbox(skinRow, "Skin required", SkinRequired);
		skinRequired.OnCheckedChanged += (s) =>
			file.SetProperty(this, "skin required", s.Checked, () => SkinRequired, v => SkinRequired = v);
		ConstraintProps.SetupOnly(skinRow);

		BuildProperties(props);
	}

	protected abstract void BuildProperties(Panel props);

	protected List<EditorBone> TargetCandidates() {
		var list = new List<EditorBone>();
		foreach (var bone in Model.GetAllBones()) {
			bool under = false;
			for (var ancestor = bone; ancestor != null && !under; ancestor = ancestor.Parent) {
				if (Bones.Contains(ancestor))
					under = true;
			}
			if (!under)
				list.Add(bone);
		}
		return list;
	}

	protected void SetValue<T>(string property, T value, Func<T> get, Action<T> set)
		=> ModelEditor.Active.File.SetProperty(this, property, value, get, newValue => {
			set(newValue);
			ModelEditor.Active.QueueAnimationUpdate();
		});

	protected static bool animating => ModelEditor.Active.AnimationMode;

	/// <summary>
	/// Sets the pose value in animate mode, the setup value otherwise.
	/// </summary>
	protected void Edit<T>(string property, T value, Func<T> get, Action<T> set, Action<T> setPose, KeyframeProperty key) {
		if (!animating) {
			SetValue(property, value, get, set);
			return;
		}
		setPose(value);
		ModelEditor.Active.File.PoseEdited(this, key);
	}
}

public partial class EditorIkConstraint
{
	public override string EditorIcon => "models/ikconstraint.png";
	protected override KeyframeProperty TreeKey => KeyframeProperty.Ik_Constraint;
	public override Color DisplayColor => IkConstraintTimeline.TIMELINE_COLOR_IK;

	public override bool IsActiveIn(EditorSkin? skin) => Target != null && Target.IsActive && base.IsActiveIn(skin);

	internal override void Solve(bool animating) {
		float mix = animating ? CurrentMix : Mix;
		var target = Target.WorldTransform;
		switch (Bones.Count) {
			case 1:
				_ = Bones[0].WorldTransform;
				ConstraintMath.IkApply(Bones[0], target.X, target.Y, animating ? CurrentCompress : Compress, animating ? CurrentStretch : Stretch, Uniform, mix);
				break;
			case 2:
				_ = Bones[1].WorldTransform;
				ConstraintMath.IkApply(Bones[0], Bones[1], target.X, target.Y,
					animating ? CurrentBendDirection : BendDirection,
					animating ? CurrentStretch : Stretch,
					animating ? CurrentSoftness : Softness,
					mix);
				break;
		}
	}

	public override void RenderViewport() {
		if (Target == null || Bones.Count == 0) return;
		bool lit = Highlighted || Target.Selected;
		var color = DisplayColor with { A = (byte)(lit ? 230 : 90) };

		var last = Bones[Bones.Count - 1];
		var tip = last.WorldTransform.LocalToWorld(last.Length, 0);
		var target = Target.WorldTransform.Translation;
		ViewportDraw.Line(Bones[0].WorldTransform.Translation, last.WorldTransform.Translation, lit ? 2 : 1, color);
		ViewportDraw.Dashed(tip, target, lit ? 2 : 1, color);
		ViewportDraw.Ring(target, lit ? 9 : 7, color);
		ViewportDraw.Ring(target, lit ? 4 : 3, color);
	}

	protected override void BuildProperties(Panel props) {
		var targetRow = PropertiesPanel.NewRow(props, "Target", "models/bone.png");
		ConstraintProps.BoneCombo(targetRow, Target, TargetCandidates(),
			v => SetValue("target", v, () => Target, x => Target = x));
		ConstraintProps.SetupOnly(targetRow);

		if (Bones.Count > 0)
			ConstraintProps.BoneButton(PropertiesPanel.NewRow(props, "Parent", "models/bone.png"), Bones[0]);
		if (Bones.Count > 1)
			ConstraintProps.BoneButton(PropertiesPanel.NewRow(props, "Child", "models/bone.png"), Bones[1]);

		const KeyframeProperty KEY = KeyframeProperty.Ik_Constraint;
		var mixRow = PropertiesPanel.NewRow(props, "Mix", "models/ikconstraint.png");
		ConstraintProps.Percent(mixRow, () => animating ? CurrentMix : Mix,
			v => Edit("mix", v, () => Mix, x => Mix = x, x => CurrentMix = x, KEY));

		if (Bones.Count > 1) {
			var softRow = PropertiesPanel.NewRow(props, "Softness", "models/ikconstraint.png");
			var softness = ConstraintProps.Slider(softRow, Softness, 2);
			softness.MinimumValue = 0;
			ConstraintProps.Follow(softness, () => animating ? CurrentSoftness : Softness);
			softness.OnValueChanged += (_, _, v) =>
				Edit("softness", (float)v, () => Softness, x => Softness = x, x => CurrentSoftness = x, KEY);

			var optionsRow = PropertiesPanel.NewRow(props, "Options", "models/info.png");
			var positive = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Positive", BendDirection > 0);
			ConstraintProps.FollowCheck(positive, () => (animating ? CurrentBendDirection : BendDirection) > 0,
				c => Edit("bend direction", c ? 1 : -1,
					() => BendDirection, x => BendDirection = x, x => CurrentBendDirection = x, KEY));
			var stretch = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Stretch", Stretch);
			ConstraintProps.FollowCheck(stretch, () => animating ? CurrentStretch : Stretch,
				c => Edit("stretch", c, () => Stretch, x => Stretch = x, x => CurrentStretch = x, KEY));
		}
		else {
			var optionsRow = PropertiesPanel.NewRow(props, "Options", "models/info.png");
			var compress = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Compress", Compress);
			ConstraintProps.FollowCheck(compress, () => animating ? CurrentCompress : Compress,
				c => Edit("compress", c, () => Compress, x => Compress = x, x => CurrentCompress = x, KEY));
			var stretch = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Stretch", Stretch);
			ConstraintProps.FollowCheck(stretch, () => animating ? CurrentStretch : Stretch,
				c => Edit("stretch", c, () => Stretch, x => Stretch = x, x => CurrentStretch = x, KEY));
			var uniform = PropertiesPanel.AddLabeledCheckbox(optionsRow, "Uniform", Uniform);
			uniform.OnCheckedChanged += (s) => SetValue("uniform", s.Checked, () => Uniform, x => Uniform = x);
			ConstraintProps.SetupOnly(uniform);
		}
	}
}

public partial class EditorTransformConstraint
{
	public override string EditorIcon => "models/transformconstraint.png";
	protected override KeyframeProperty TreeKey => KeyframeProperty.Transform_Constraint;
	public override Color DisplayColor => TransformConstraintTimeline.TIMELINE_COLOR_TRANSFORM;

	[JsonIgnore] TransformConstraintData? solveData;

	public override bool IsActiveIn(EditorSkin? skin) => Target != null && Target.IsActive && base.IsActiveIn(skin);

	internal override void Solve(bool animating) {
		var data = solveData ??= new();
		data.Local = Local;
		data.Relative = Relative;
		data.OffsetRotation = OffsetRotation;
		data.OffsetX = OffsetX;
		data.OffsetY = OffsetY;
		data.OffsetScaleX = OffsetScaleX;
		data.OffsetScaleY = OffsetScaleY;
		data.OffsetShearY = OffsetShearY;

		_ = Target.WorldTransform;
		foreach (var bone in Bones)
			_ = bone.WorldTransform;

		if (animating)
			ConstraintMath.TransformApply(Target, Bones, data, CurrentRotateMix, CurrentTranslateMix, CurrentScaleMix, CurrentShearMix);
		else
			ConstraintMath.TransformApply(Target, Bones, data, RotateMix, TranslateMix, ScaleMix, ShearMix);
	}

	public override void RenderViewport() {
		if (Target == null) return;
		bool lit = Highlighted || Target.Selected;
		var color = DisplayColor with { A = (byte)(lit ? 230 : 80) };
		var target = Target.WorldTransform.Translation;
		foreach (var bone in Bones)
			ViewportDraw.Dashed(target, bone.WorldTransform.Translation, lit ? 2 : 1, color);
		ViewportDraw.Diamond(target, lit ? 7 : 5, color);
	}

	public void MatchOffsets() {
		if (Target == null || Bones.Count == 0) return;
		var bone = Bones[0];
		float rotation;
		float x, y;
		float scaleX, scaleY;
		float shearY;
		if (Local) {
			Target.ResetApplied();
			bone.ResetApplied();
			var targetApplied = Target.Applied;
			var boneApplied = bone.Applied;
			rotation = boneApplied.Rotation - targetApplied.Rotation;
			x = boneApplied.X - targetApplied.X;
			y = boneApplied.Y - targetApplied.Y;
			scaleX = boneApplied.ScaleX - targetApplied.ScaleX;
			scaleY = boneApplied.ScaleY - targetApplied.ScaleY;
			shearY = boneApplied.ShearY - targetApplied.ShearY;
		}
		else {
			var targetWorld = Target.WorldTransform;
			var boneWorld = bone.WorldTransform;
			var local = targetWorld.WorldToLocal(boneWorld.X, boneWorld.Y);
			x = local.X;
			y = local.Y;
			rotation = (MathF.Atan2(boneWorld.C, boneWorld.A) - MathF.Atan2(targetWorld.C, targetWorld.A)).ToDegrees();
			scaleX = MathF.Sqrt(boneWorld.A * boneWorld.A + boneWorld.C * boneWorld.C)
				- MathF.Sqrt(targetWorld.A * targetWorld.A + targetWorld.C * targetWorld.C);
			scaleY = MathF.Sqrt(boneWorld.B * boneWorld.B + boneWorld.D * boneWorld.D)
				- MathF.Sqrt(targetWorld.B * targetWorld.B + targetWorld.D * targetWorld.D);
			float boneShear = MathF.Atan2(boneWorld.D, boneWorld.B) - MathF.Atan2(boneWorld.C, boneWorld.A);
			float targetShear = MathF.Atan2(targetWorld.D, targetWorld.B) - MathF.Atan2(targetWorld.C, targetWorld.A);
			shearY = (boneShear - targetShear).ToDegrees();
		}
		// todo: relative mode wants different offsets

		rotation = Wrap180(rotation);
		shearY = Wrap180(shearY);
		SetValue("rotate offset", rotation, () => OffsetRotation, v => OffsetRotation = v);
		SetValue("x offset", x, () => OffsetX, v => OffsetX = v);
		SetValue("y offset", y, () => OffsetY, v => OffsetY = v);
		SetValue("scale x offset", scaleX, () => OffsetScaleX, v => OffsetScaleX = v);
		SetValue("scale y offset", scaleY, () => OffsetScaleY, v => OffsetScaleY = v);
		SetValue("shear y offset", shearY, () => OffsetShearY, v => OffsetShearY = v);
	}

	private static float Wrap180(float degrees) {
		degrees %= 360;
		if (degrees > 180)
			degrees -= 360;
		else if (degrees < -180)
			degrees += 360;
		return degrees;
	}

	public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
		PropertiesPanel.ButtonIcon(buttons, "Match", "models/transformconstraint.png", (_, _) => {
			if (ModelEditor.Active.AnimationMode) return;
			MatchOffsets();
			ModelEditor.Active.SelectObject(this);
		});
	}

	protected override void BuildProperties(Panel props) {
		var targetRow = PropertiesPanel.NewRow(props, "Target", "models/bone.png");
		ConstraintProps.BoneCombo(targetRow, Target, TargetCandidates(),
			v => SetValue("target", v, () => Target, x => Target = x));
		ConstraintProps.SetupOnly(targetRow);

		ConstraintProps.BoneList(PropertiesPanel.NewRow(props, "Bones", "models/bone.png"), Bones);

		var modeRow = PropertiesPanel.NewRow(props, "Mode", "models/bonetransform.png");
		var local = PropertiesPanel.AddLabeledCheckbox(modeRow, "Local", Local);
		local.OnCheckedChanged += (s) => SetValue("local", s.Checked, () => Local, x => Local = x);
		var relative = PropertiesPanel.AddLabeledCheckbox(modeRow, "Relative", Relative);
		relative.OnCheckedChanged += (s) => SetValue("relative", s.Checked, () => Relative, x => Relative = x);
		ConstraintProps.SetupOnly(modeRow);

		const KeyframeProperty KEY = KeyframeProperty.Transform_Constraint;
		// Offsets are setup only, the mixes key together
		var rotateRow = PropertiesPanel.NewRow(props, "Rotate", "models/rotate.png");
		ConstraintProps.SetupOnly(ConstraintProps.Offset(rotateRow, "", "°", () => OffsetRotation,
			v => SetValue("rotate offset", v, () => OffsetRotation, x => OffsetRotation = x)));
		ConstraintProps.Percent(rotateRow, () => animating ? CurrentRotateMix : RotateMix,
			v => Edit("rotate mix", v, () => RotateMix, x => RotateMix = x, x => CurrentRotateMix = x, KEY));

		var translateRow = PropertiesPanel.NewRow(props, "Translate", "models/translate.png");
		ConstraintProps.SetupOnly(ConstraintProps.Offset(translateRow, "X ", "", () => OffsetX,
			v => SetValue("x offset", v, () => OffsetX, x => OffsetX = x)));
		ConstraintProps.SetupOnly(ConstraintProps.Offset(translateRow, "Y ", "", () => OffsetY,
			v => SetValue("y offset", v, () => OffsetY, x => OffsetY = x)));
		ConstraintProps.Percent(translateRow, () => animating ? CurrentTranslateMix : TranslateMix,
			v => Edit("translate mix", v, () => TranslateMix, x => TranslateMix = x, x => CurrentTranslateMix = x, KEY));

		var scaleRow = PropertiesPanel.NewRow(props, "Scale", "models/scale.png");
		ConstraintProps.SetupOnly(ConstraintProps.Offset(scaleRow, "X ", "", () => OffsetScaleX,
			v => SetValue("scale x offset", v, () => OffsetScaleX, x => OffsetScaleX = x)));
		ConstraintProps.SetupOnly(ConstraintProps.Offset(scaleRow, "Y ", "", () => OffsetScaleY,
			v => SetValue("scale y offset", v, () => OffsetScaleY, x => OffsetScaleY = x)));
		ConstraintProps.Percent(scaleRow, () => animating ? CurrentScaleMix : ScaleMix,
			v => Edit("scale mix", v, () => ScaleMix, x => ScaleMix = x, x => CurrentScaleMix = x, KEY));

		var shearRow = PropertiesPanel.NewRow(props, "Shear", "models/shear.png");
		ConstraintProps.SetupOnly(ConstraintProps.Offset(shearRow, "Y ", "°", () => OffsetShearY,
			v => SetValue("shear y offset", v, () => OffsetShearY, x => OffsetShearY = x)));
		ConstraintProps.Percent(shearRow, () => animating ? CurrentShearMix : ShearMix,
			v => Edit("shear mix", v, () => ShearMix, x => ShearMix = x, x => CurrentShearMix = x, KEY));
	}
}

public partial class EditorPathConstraint
{
	public override string EditorIcon => "models/pathconstraint.png";
	public override Color DisplayColor => Bones.Count > 0 ? Bones[0].Color : PathPositionTimeline.TIMELINE_COLOR_PATH;

	[JsonIgnore] PathConstraintData? solveData;
	[JsonIgnore] readonly PathScratch scratch = new();
	[JsonIgnore] bool hasHandle;
	[JsonIgnore] Vector2F handle;

	public override bool IsActiveIn(EditorSkin? skin) =>
		Target != null && Target.Bone.IsActive && base.IsActiveIn(skin);

	internal override void Solve(bool animating) {
		hasHandle = false;
		if (Target.GetActiveAttachment() is not EditorPathAttachment path || !path.HasValidShape) return;

		var data = solveData ??= new();
		data.PositionMode = PositionMode;
		data.SpacingMode = SpacingMode;
		data.RotateMode = RotateMode;
		data.OffsetRotation = OffsetRotation;

		_ = Target.Bone.WorldTransform;
		foreach (var bone in Bones)
			_ = bone.WorldTransform;

		float rotateMix = animating ? CurrentRotateMix : RotateMix;
		float translateMix = animating ? CurrentTranslateMix : TranslateMix;
		ConstraintMath.PathApply(path, Target.Bone, Bones, data, animating ? CurrentPosition : Position, animating ? CurrentSpacing : Spacing, rotateMix, translateMix, scratch);

		// First position along the path, for the handle
		if ((rotateMix > 0 || translateMix > 0) && scratch.Positions.Length >= 2) {
			handle = new(scratch.Positions[0], scratch.Positions[1]);
			hasHandle = true;
		}
	}

	public override void RenderViewport() {
		if (Target == null) return;
		bool lit = Highlighted || Target.Selected || (Target.GetActiveAttachment()?.Selected ?? false);
		var color = DisplayColor with { A = (byte)(lit ? 230 : 90) };
		if (lit) {
			foreach (var bone in Bones)
				ViewportDraw.Ring(bone.WorldTransform.Translation, 6, color);
		}
	}

	public void RenderPositionHandle() {
		if (!hasHandle || Hidden) return;
		var color = DisplayColor;
		ViewportDraw.Diamond(handle, 8, new Color(0, 0, 0, 200));
		ViewportDraw.Diamond(handle, 6, color with { A = 255 });
		// todo: drag the handle to change the position
	}

	protected override void BuildProperties(Panel props) {
		var targetRow = PropertiesPanel.NewRow(props, "Target", "models/slot.png");
		var slotCombo = PropertiesPanel.AddComboBox(targetRow, Target, Model.Slots,
			s => s?.Name ?? "(none)",
			s => SetValue("target", s, () => Target, x => Target = x));
		slotCombo.Dock = Dock.Fill;
		ConstraintProps.SetupOnly(targetRow);

		ConstraintProps.BoneList(PropertiesPanel.NewRow(props, "Bones", "models/bone.png"), Bones);

		var positionRow = PropertiesPanel.NewRow(props, "Position", "models/pathconstraint.png");
		PropertiesPanel.AddKeyButton(positionRow, this, KeyframeProperty.Path_Position);
		var positionMode = PropertiesPanel.AddEnumComboBox(positionRow, PositionMode);
		var position = ConstraintProps.Slider(positionRow, 0, 2);
		PositionDisplay(position);
		ConstraintProps.Follow(position, () => (animating ? CurrentPosition : Position) * PositionScale());
		position.OnValueChanged += (_, _, v) => Edit("position", (float)v / PositionScale(),
			() => Position, x => Position = x, x => CurrentPosition = x, KeyframeProperty.Path_Position);
		positionMode.OnSelectionChanged += (_, _, mode) => {
			SetValue("position mode", mode, () => PositionMode, x => PositionMode = x);
			PositionDisplay(position);
		};
		ConstraintProps.SetupOnly(positionMode);

		var spacingRow = PropertiesPanel.NewRow(props, "Spacing", "models/bonelength.png");
		PropertiesPanel.AddKeyButton(spacingRow, this, KeyframeProperty.Path_Spacing);
		var spacingMode = PropertiesPanel.AddEnumComboBox(spacingRow, SpacingMode);
		var spacing = ConstraintProps.Slider(spacingRow, 0, 2);
		SpacingDisplay(spacing);
		ConstraintProps.Follow(spacing, () => (animating ? CurrentSpacing : Spacing) * SpacingScale());
		spacing.OnValueChanged += (_, _, v) => Edit("spacing", (float)v / SpacingScale(),
			() => Spacing, x => Spacing = x, x => CurrentSpacing = x, KeyframeProperty.Path_Spacing);
		spacingMode.OnSelectionChanged += (_, _, mode) => {
			SetValue("spacing mode", mode, () => SpacingMode, x => SpacingMode = x);
			SpacingDisplay(spacing);
		};
		ConstraintProps.SetupOnly(spacingMode);

		// rotate and translate mixes key together
		var rotateRow = PropertiesPanel.NewRow(props, "Rotate", "models/rotate.png");
		PropertiesPanel.AddKeyButton(rotateRow, this, KeyframeProperty.Path_Mix);
		var rotateMode = PropertiesPanel.AddEnumComboBox(rotateRow, RotateMode);
		rotateMode.OnSelectionChanged += (_, _, v) => SetValue("rotate mode", v, () => RotateMode, x => RotateMode = x);
		ConstraintProps.SetupOnly(rotateMode);
		ConstraintProps.Percent(rotateRow, () => animating ? CurrentRotateMix : RotateMix,
			v => Edit("rotate mix", v,
				() => RotateMix, x => RotateMix = x, x => CurrentRotateMix = x, KeyframeProperty.Path_Mix));

		var offsetRow = PropertiesPanel.NewRow(props, "Offset", "models/rotate.png");
		ConstraintProps.Offset(offsetRow, "", "°", () => OffsetRotation,
			v => SetValue("rotate offset", v, () => OffsetRotation, x => OffsetRotation = x));
		ConstraintProps.SetupOnly(offsetRow);

		var translateRow = PropertiesPanel.NewRow(props, "Translate", "models/translate.png");
		PropertiesPanel.AddKeyButton(translateRow, this, KeyframeProperty.Path_Mix);
		ConstraintProps.Percent(translateRow, () => animating ? CurrentTranslateMix : TranslateMix,
			v => Edit("translate mix", v,
				() => TranslateMix, x => TranslateMix = x, x => CurrentTranslateMix = x, KeyframeProperty.Path_Mix));
	}

	private void SpacingDisplay(NumSlider spacing) {
		spacing.Suffix = SpacingMode == SpacingMode.Percent ? "%" : "";
		spacing.SetValueNoUpdate((animating ? CurrentSpacing : Spacing) * SpacingScale());
	}

	private float SpacingScale() => SpacingMode == SpacingMode.Percent ? 100 : 1;

	private void PositionDisplay(NumSlider position) {
		position.Suffix = PositionMode == PositionMode.Percent ? "%" : "";
		position.SetValueNoUpdate((animating ? CurrentPosition : Position) * PositionScale());
	}

	private float PositionScale() => PositionMode == PositionMode.Percent ? 100 : 1;
}

public partial class EditorEvent
{
	public bool CanHide() => true;
	public bool CanDelete() => true;
	public bool CanRename() => true;
	public IEditorType? DeferTransformationsTo() => null;
	public bool IsNameTaken(string name) => Model.Events.Any(x => x != this && x.Name == name);

	public bool CanKeyframe() => true;
	public bool GetKeyframeParameters(
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEditorType? target,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out KeyframeProperty property,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out int arrayIndex) {
		target = this;
		property = KeyframeProperty.Event;
		arrayIndex = -1;
		return true;
	}
	public EditorResult Rename(string newName) => ModelEditor.Active.File.RenameEvent(this, newName);
	public EditorResult Remove() => ModelEditor.Active.File.RemoveEvent(this);

	public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {
		PropertiesPanel.DeleteOperator(this, props, determinations);
		PropertiesPanel.RenameOperator(this, props, determinations);
	}

	public void BuildProperties(Panel props, PreUIDeterminations determinations) {
		var file = ModelEditor.Active.File;

		var intRow = PropertiesPanel.NewRow(props, "Integer", "models/info.png");
		var intSlider = ConstraintProps.Slider(intRow, Int, 0);
		ConstraintProps.Follow(intSlider, () => Animating() ? CurrentInt : Int);
		intSlider.OnValueChanged += (_, _, v) =>
			Edit("integer", (int)Math.Round(v), () => Int, x => Int = x, x => CurrentInt = x, file);

		var floatRow = PropertiesPanel.NewRow(props, "Float", "models/info.png");
		var floatSlider = ConstraintProps.Slider(floatRow, Float, 3);
		ConstraintProps.Follow(floatSlider, () => Animating() ? CurrentFloat : Float);
		floatSlider.OnValueChanged += (_, _, v) =>
			Edit("float", (float)v, () => Float, x => Float = x, x => CurrentFloat = x, file);

		var stringRow = PropertiesPanel.NewRow(props, "String", "models/rename.png");
		var stringBox = ConstraintProps.Text(stringRow, String,
			v => Edit("string", v, () => String, x => String = x, x => CurrentString = x, file));
		stringBox.Thinking += (_) => {
			if (!stringBox.IsKeyboardFocused())
				stringBox.Text = (Animating() ? CurrentString : String) ?? "";
		};

		var audioRow = PropertiesPanel.NewRow(props, "Audio", "models/event.png");
		var volumeRow = PropertiesPanel.NewRow(props, "Volume", "models/info.png");
		var volume = ConstraintProps.Slider(volumeRow, Volume, 2);
		volume.MinimumValue = 0;
		volume.MaximumValue = 1;
		ConstraintProps.Follow(volume, () => Animating() ? CurrentVolume : Volume);
		volume.OnValueChanged += (_, _, v) =>
			Edit("volume", (float)v, () => Volume, x => Volume = x, x => CurrentVolume = x, file);

		var balanceRow = PropertiesPanel.NewRow(props, "Balance", "models/info.png");
		var balance = ConstraintProps.Slider(balanceRow, Balance, 2);
		balance.MinimumValue = -1;
		balance.MaximumValue = 1;
		ConstraintProps.Follow(balance, () => Animating() ? CurrentBalance : Balance);
		balance.OnValueChanged += (_, _, v) =>
			Edit("balance", (float)v, () => Balance, x => Balance = x, x => CurrentBalance = x, file);
		ConstraintProps.Text(audioRow, AudioPath, newPath => {
			file.SetProperty(this, "audio path", string.IsNullOrEmpty(newPath) ? null : newPath,
				() => AudioPath, x => AudioPath = x);
			AudioRows(props, volumeRow, balanceRow);
		});
		AudioRows(props, volumeRow, balanceRow);
		// todo: audio playback in the editor

		ConstraintProps.SetupOnly(audioRow);
	}

	private void AudioRows(Panel props, PropertiesPanel.InnerRow volumeRow, PropertiesPanel.InnerRow balanceRow) {
		bool audio = !string.IsNullOrEmpty(AudioPath);
		volumeRow.GetParent()!.SetVisible(audio);
		balanceRow.GetParent()!.SetVisible(audio);
		props.InvalidateLayout();
	}

	private void Edit<T>(string property, T value, Func<T> get, Action<T> set, Action<T> setPose, EditorFile file) {
		if (!Animating()) {
			file.SetProperty(this, property, value, get, set);
			return;
		}
		setPose(value);
		file.PoseEdited(this, KeyframeProperty.Event);
	}

	private static bool Animating() => ModelEditor.Active.AnimationMode;
}

internal static class ConstraintProps
{
	public static NumSlider Slider(Panel row, float value, int digits) {
		var slider = PropertiesPanel.AddNumSlider(row, value);
		slider.Digits = digits;
		slider.SetValueNoUpdate(value);
		return slider;
	}

	public static void Follow(NumSlider slider, Func<float> get) {
		slider.Thinking += (_) => {
			if (slider.IsKeyboardFocused() || slider.IsDepressed()) return;
			double now = Math.Round(get() * 1.0, slider.Digits);
			if (now != slider.Value)
				slider.SetValueNoUpdate(now);
		};
	}

	/// <summary>
	/// 0-1 shown as 0-100%
	/// </summary>
	public static NumSlider Percent(Panel row, Func<float> get, Action<float> set) {
		var slider = Slider(row, get() * 100, 1);
		slider.MinimumValue = 0;
		slider.MaximumValue = 100;
		slider.Suffix = "%";
		slider.SetValueNoUpdate(get() * 100);
		Follow(slider, () => get() * 100);
		slider.OnValueChanged += (_, _, v) => set((float)(v / 100));
		return slider;
	}

	public static NumSlider Offset(Panel row, string prefix, string suffix, Func<float> get, Action<float> set) {
		var slider = Slider(row, get(), 2);
		slider.Prefix = prefix;
		slider.Suffix = suffix;
		slider.SetValueNoUpdate(get());
		Follow(slider, get);
		slider.OnValueChanged += (_, _, v) => set((float)v);
		return slider;
	}

	public static Textbox Text(Panel row, string? value, Action<string> changed) {
		var panel = PropertiesPanel.AddInternalPropPanel(row);
		var text = new Textbox(panel);
		text.Dock = Dock.Fill;
		text.BorderSize = 0;
		text.Text = value ?? "";
		text.SetTextAlignment(Anchor.Center);
		text.OnUserPressedEnter += (_, _, now) => changed(now);
		return text;
	}

	public static void BoneCombo(Panel row, EditorBone? value, List<EditorBone> options, Action<EditorBone> changed) {
		var combo = PropertiesPanel.AddComboBox(row, value, options, b => b?.Name ?? "(none)", changed);
		combo.Dock = Dock.Fill;
	}

	public static void BoneButton(Panel row, EditorBone bone) {
		var panel = PropertiesPanel.AddInternalPropPanel(row);
		var button = new Button(panel);
		button.Dock = Dock.Fill;
		button.DockMargin = RectangleF.TLRB(3);
		button.BorderSize = 0;
		button.Text = bone.Name;
		button.TooltipText = "Select bone";
		button.OnButtonClick += (_, _) => ModelEditor.Active.SelectObject(bone);
	}

	/// <summary>
	/// Button for one bone, select box for more
	/// </summary>
	public static void BoneList(Panel row, List<EditorBone> bones) {
		if (bones.Count == 1) {
			BoneButton(row, bones[0]);
			return;
		}
		var combo = PropertiesPanel.AddComboBox(row, bones.FirstOrDefault(), bones,
			b => b == null ? "(none)" : $"{b.Name}  ({bones.Count} bones)",
			bone => {
				if (bone != null)
					ModelEditor.Active.SelectObject(bone);
			});
		combo.Dock = Dock.Fill;
		// todo: change the constrained bones (and reorder them for paths)
	}

	public static void SetupOnly(Element row) =>
		row.Thinking += (_) => row.SetMouseInputEnabled(!ModelEditor.Active.AnimationMode);

	private static bool syncing;

	public static void FollowCheck(Checkbox box, Func<bool> get, Action<bool> changed) {
		box.OnCheckedChanged += (checkbox) => {
			if (!syncing)
				changed(checkbox.Checked);
		};
		box.Thinking += (_) => {
			bool now = get();
			if (box.Checked == now) return;
			syncing = true;
			box.Checked = now;
			syncing = false;
		};
	}
}

/// <summary>
/// Viewport drawing in grid space with sizes in screen pixels
/// </summary>
internal static class ViewportDraw
{
	static float zoom => ModelEditor.Active.Editor.CameraZoom;

	public static void Line(Vector2F start, Vector2F end, float thicknessPx, Color color)
		=> Raylib.DrawLineEx(start.ToNumerics(), end.ToNumerics(), thicknessPx / zoom, color);

	public static void Dashed(Vector2F start, Vector2F end, float thicknessPx, Color color, float dashPx = 6, float gapPx = 4) {
		var delta = end - start;
		float length = delta.Length;
		if (length <= 0.0001f) return;
		var direction = delta / length;
		float dash = dashPx / zoom;
		float step = (dashPx + gapPx) / zoom;
		for (float distance = 0; distance < length; distance += step) {
			Line(start + direction * distance, start + direction * MathF.Min(distance + dash, length), thicknessPx, color);
		}
	}

	public static void Ring(Vector2F center, float radiusPx, Color color) =>
		Raylib.DrawCircleLinesV(center.ToNumerics(), radiusPx / zoom, color);
	public static void Dot(Vector2F center, float radiusPx, Color color) =>
		Raylib.DrawCircleV(center.ToNumerics(), radiusPx / zoom, color);

	public static void Square(Vector2F center, float halfPx, Color color) {
		float half = halfPx / zoom;
		Raylib.DrawRectangleV(new(center.X - half, center.Y - half), new(half * 2, half * 2), color);
	}

	public static void Diamond(Vector2F center, float radiusPx, Color color) {
		float radius = radiusPx / zoom;
		var top = new System.Numerics.Vector2(center.X, center.Y + radius);
		var bottom = new System.Numerics.Vector2(center.X, center.Y - radius);
		var left = new System.Numerics.Vector2(center.X - radius, center.Y);
		var right = new System.Numerics.Vector2(center.X + radius, center.Y);
		Raylib.DrawTriangle(top, left, right, color);
		Raylib.DrawTriangle(bottom, right, left, color);
	}
}

public static class ConstraintTimelines
{
	public static EditorConstraint? ConstraintOf(EditorTimeline timeline) => timeline switch {
		IkConstraintTimeline ik => ik.Constraint,
		TransformConstraintTimeline transform => transform.Constraint,
		PathPositionTimeline position => position.Constraint,
		PathSpacingTimeline spacing => spacing.Constraint,
		PathMixTimeline mix => mix.Constraint,
		_ => null
	};

	public static string Label(EditorTimeline timeline) => timeline switch {
		IkConstraintTimeline => "IK",
		TransformConstraintTimeline => "Transform",
		PathPositionTimeline => "Position",
		PathSpacingTimeline => "Spacing",
		PathMixTimeline => "Mix",
		_ => "?"
	};
}
