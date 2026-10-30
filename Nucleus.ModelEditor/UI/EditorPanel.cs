using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.UI.Elements.Visual;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using Color = Nucleus.Common.Types.Color;
using RectangleF = Nucleus.Types.RectangleF;

namespace Nucleus.ModelEditor
{

	public class EditorPanel : View
	{
		public override string Name => "Editor";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		TransformPanel TransformRotation, TransformTranslation, TransformScale, TransformShear;
		Button LocalTransformButton, ParentTransformButton, WorldTransformButton;
		Button PoseBonesOpBtn, WeighVerticesOpBtn, CreateBonesOpBtn;
		Button BoneCompensationBtn, ImageCompensationBtn, AutoKeyBtn;
		public ViewportOptionsGrid ViewportOptions;

		CenteredObjectsPanel MainTransformsPanel;
		CenteredObjectsPanel OperatorPanel;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

		public ViewportSelectMode SelectMode { get; set; } = ViewportSelectMode.All;
		Checkerboard Checkerboard = null!;
		public EditorPanel(Element parent) : base(parent) {
			Checkerboard = new(Level);

			MainTransformsPanel = new(this);
			MainTransformsPanel.ForceHeight = false;
			MainTransformsPanel.DropOverflow = true;

			MainTransformsPanel.
			Dock = Dock.Bottom;
			MainTransformsPanel.			Size = new(128);

			OperatorPanel = new(this);
			OperatorPanel.ForceHeight = false;

			OperatorPanel.
			Dock = Dock.Bottom;
			OperatorPanel.			Size = new(128);
			OperatorPanel.SetVisible(false);

			var modePanel = new FlexPanel(MainTransformsPanel);
			modePanel.Direction = Axis.Vertical;
			modePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			modePanel.Size = new(124, 90);

			PoseBonesOpBtn = ToolbarButtons.New(modePanel, "Pose", "models/poseBones.png");
			PoseBonesOpBtn.OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.PoseBoneToTarget);

			WeighVerticesOpBtn = ToolbarButtons.New(modePanel, "Weights", "models/weights.png");
			WeighVerticesOpBtn.OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.ChangeMeshWeights);

			CreateBonesOpBtn = ToolbarButtons.New(modePanel, "Create", "models/createBones.png");
			CreateBonesOpBtn.OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.CreateNewBones);

			var transformPanel = new FlexPanel(MainTransformsPanel);
			transformPanel.Direction = Axis.Vertical;
			transformPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformPanel.Size = new(360, 115);

			TransformRotation = TransformPanel.New(transformPanel, "Rotate", "models/rotate_color.png", 1,
				KeyframeProperty.Bone_Rotation);
			TransformTranslation = TransformPanel.New(transformPanel, "Translate", "models/translate_color.png", 2,
				KeyframeProperty.Bone_Translation);
			TransformScale = TransformPanel.New(transformPanel, "Scale", "models/scale_color.png", 2,
				KeyframeProperty.Bone_Scale);
			TransformShear = TransformPanel.New(transformPanel, "Shear", "models/shear_color.png", 2,
				KeyframeProperty.Bone_Shear);

			TransformRotation.GetNumSlider(0).OnValueChanged += CHANGE_Rotation;
			TransformTranslation.GetNumSlider(0).OnValueChanged += CHANGE_TranslationX;
			TransformTranslation.GetNumSlider(1).OnValueChanged += CHANGE_TranslationY;
			TransformScale.GetNumSlider(0).OnValueChanged += CHANGE_ScaleX;
			TransformScale.GetNumSlider(1).OnValueChanged += CHANGE_ScaleY;
			// World scale can't be typed, the fields show local scale while typing.
			for (int i = 0; i < 2; i++) {
				int axis = i;
				var slider = TransformScale.GetNumSlider(i);
				slider.FocusGained += focused => {
					typingScale = true;
					if (TransformMode == EditorTransformMode.WorldCoordinates && FirstSelectedTransformable() is IEditorType item)
						focused.SetValueNoUpdate(axis == 0 ? item.GetScaleX() : item.GetScaleY());
				};
				slider.FocusLost += _ => {
					typingScale = false;
					Active_SelectedChanged();
				};
			}
			TransformShear.GetNumSlider(0).OnValueChanged += CHANGE_ShearX;
			TransformShear.GetNumSlider(1).OnValueChanged += CHANGE_ShearY;

			TransformRotation.GetButton().OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.RotateSelection);
			TransformTranslation.GetButton().OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.TranslateSelection);
			TransformScale.GetButton().OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.ScaleSelection);
			TransformShear.GetButton().OnButtonClick += (_, _) => SetEditorOperator(EditorDefaultOperator.ShearSelection);

			var transformModePanel = new FlexPanel(MainTransformsPanel);
			transformModePanel.Direction = Axis.Vertical;
			transformModePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformModePanel.Size = new(110, 90);

			LocalTransformButton = ToolbarButtons.New(transformModePanel, "Local", "models/axes_local.png");
			LocalTransformButton.OnButtonClick += (_, _) => SetTransformMode(EditorTransformMode.LocalCoordinates);

			ParentTransformButton = ToolbarButtons.New(transformModePanel, "Parent", "models/axes_parent.png");
			ParentTransformButton.OnButtonClick += (_, _) => SetTransformMode(EditorTransformMode.ParentCoordinates);

			WorldTransformButton = ToolbarButtons.New(transformModePanel, "World", "models/axes_world.png");
			WorldTransformButton.OnButtonClick += (_, _) => SetTransformMode(EditorTransformMode.WorldCoordinates);
			// Orange reminder of which axes a typed value uses
			HighlightWhileTyping(WorldTransformButton, () => TransformMode == EditorTransformMode.WorldCoordinates && TransformRotation.GetNumSlider(0).IsKeyboardFocused());
			HighlightWhileTyping(LocalTransformButton, () => TransformMode == EditorTransformMode.WorldCoordinates
				&& (TransformScale.GetNumSlider(0).IsKeyboardFocused() || TransformScale.GetNumSlider(1).IsKeyboardFocused()));

			var compensationPanel = new FlexPanel(MainTransformsPanel);
			compensationPanel.Direction = Axis.Vertical;
			compensationPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			compensationPanel.Size = new(130, 90);

			BoneCompensationBtn = ToolbarButtons.New(compensationPanel, "Bones", "models/comp_bones.png");
			BoneCompensationBtn.OnButtonClick += (_, _) => BoneCompensation = !BoneCompensation;

			ImageCompensationBtn = ToolbarButtons.New(compensationPanel, "Images", "models/comp_images.png");
			ImageCompensationBtn.OnButtonClick += (_, _) => ImageCompensation = !ImageCompensation;

			AutoKeyBtn = ToolbarButtons.New(compensationPanel, "Auto Key", "models/autokey.png");
			AutoKeyBtn.OnButtonClick += (_, _) => AutoKey = !AutoKey;

			ViewportOptions = new ViewportOptionsGrid(MainTransformsPanel);
			ViewportOptions.Size = new(180, 90);

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			ModelEditor.Active.SetupAnimateModeChanged += (_, _) => Active_SelectedChanged();
			ModelEditor.Active.File.OperatorActivated += File_OperatorActivated;
			ModelEditor.Active.File.OperatorDeactivated += File_OperatorDeactivated;
			ModelEditor.Active.File.Cleared += File_Cleared;
			ModelEditor.Active.SetupAnimateModeChanged += (_, mode) => {
				if (mode) {
					Active_SelectedChanged();
				}
			};

			ModelEditor.Active.File.ItemTransformed += File_ItemTransformed;

			Active_SelectedChanged();

			SetTransformMode(EditorTransformMode.ParentCoordinates);
			SetEditorOperator(EditorDefaultOperator.RotateSelection);


			ModelEditor.Active.File.AnimationActivated += File_AnimationActivated;
			ModelEditor.Active.File.AnimationDeactivated += File_AnimationDeactivated;
			ModelEditor.Active.SetupAnimateModeChanged += Active_SetupAnimateModeChanged;
			ModelEditor.Active.SelectedChanged += Active_SelectedChanged1;

			ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => Active_SelectedChanged();
			ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => Active_SelectedChanged();

			var zoomStrip = new ZoomStrip(this);
			zoomStrip.Anchor = Anchor.BottomLeft;
			zoomStrip.Origin = Anchor.BottomLeft;
			zoomStrip.Position = new(8, -8);
			zoomStrip.Size = new(30, 200);

			var perfGraph1 = new PerformanceGraph(this);
			perfGraph1.Anchor = Anchor.TopRight;
			perfGraph1.Origin = Anchor.TopRight;
			perfGraph1.Position = new(-8, 8);
			perfGraph1.			Size = new(300, 32);
			perfGraph1.Mode = PerformanceGraph.GraphMode.CpuUpdateTime;

			var perfGraph2 = new PerformanceGraph(this);
			perfGraph2.Anchor = Anchor.TopRight;
			perfGraph2.Origin = Anchor.TopRight;
			perfGraph2.Position = new(-8, 8 + 32 + 8);
			perfGraph2.			Size = new(300, 32);
			perfGraph2.Mode = PerformanceGraph.GraphMode.RamUsage;

		}

		private static IEditorType? FirstSelectedTransformable() {
			foreach (var selected in ModelEditor.Active.SelectedObjects) {
				var transformable = selected.DeferTransformationsTo();
				if (transformable != null) return transformable;
			}
			return null;
		}

		private void Active_SelectedChanged1() {
			setupAnimPanels(null, null);

			var animationMode = ModelEditor.Active.AnimationMode;
			if (!animationMode) return;

			var animation = ModelEditor.Active.File.ActiveAnimation;
			if (animation == null) return;

			var selected = ModelEditor.Active.LastSelectedObject;
			if (selected == null) return;
			if (selected is not EditorBone bone) return;

			setupAnimPanels(animation, bone);
		}

		private void setupAnimPanels(EditorAnimation? anim, EditorBone? bone) {
			ModelEditor.Active.Editor.TransformTranslation.SeparatedProperties = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Translation);
			ModelEditor.Active.Editor.TransformScale.SeparatedProperties = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Scale);
			ModelEditor.Active.Editor.TransformShear.SeparatedProperties = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Shear);
		}

		private void Active_SetupAnimateModeChanged(ModelEditor editor, bool animationMode) {
			Active_SelectedChanged1();
		}

		private void File_AnimationDeactivated(EditorFile file, EditorModel model, EditorAnimation animation) {

			Active_SelectedChanged1();
		}

		private void File_AnimationActivated(EditorFile file, EditorModel model, EditorAnimation animation) {
			Active_SelectedChanged1();
		}

		private void File_Cleared(EditorFile file) {
			Active_SelectedChanged();
		}

		private bool boneCompensation;
		private bool imageCompensation;
		public bool BoneCompensation {
			get => boneCompensation;
			set {
				boneCompensation = value;
				BoneCompensationBtn.Pulsing = value;
			}
		}
		public bool ImageCompensation {
			get => imageCompensation;
			set {
				imageCompensation = value;
				ImageCompensationBtn.Pulsing = value;
			}
		}

		private bool autoKey;
		public bool AutoKey {
			get => autoKey;
			set {
				autoKey = value;
				AutoKeyBtn.Pulsing = value;
			}
		}

		private void AutoKeyItem(IEditorType item, bool translation, bool rotation, bool scale, bool shear) {
			if (!ModelEditor.Active.AnimationMode || ModelEditor.Active.Actions.Applying) return;
			if (item.DeferTransformationsTo() is not EditorBone bone) return;

			var file = ModelEditor.Active.File;
			if (file.ActiveAnimation == null) return;

			if (rotation)
				file.PoseEdited(bone, KeyframeProperty.Bone_Rotation);
			if (translation)
				file.PoseEdited(bone, KeyframeProperty.Bone_Translation);
			if (scale)
				file.PoseEdited(bone, KeyframeProperty.Bone_Scale);
			if (shear)
				file.PoseEdited(bone, KeyframeProperty.Bone_Shear);
		}

		private void File_ItemTransformed(IEditorType item, bool translation, bool rotation, bool scale, bool shear) {
			AutoKeyItem(item, translation, rotation, scale, shear);
			if (translation || rotation || scale || shear) {
				Active_SelectedChanged();
			}
		}

		HashSet<Type>? SelectableTypes { get; set; } = null;

		private void File_OperatorActivated(EditorFile self, Operator op) {
			MainTransformsPanel.SetVisible(false);
			OperatorPanel.ClearChildren();
			OperatorPanel.SetVisible(true);
			SelectableTypes = op.SelectableTypes == null ? null : op.SelectableTypes.ToHashSet();
			op.ChangeEditorProperties(OperatorPanel);
		}

		private void File_OperatorDeactivated(EditorFile self, Operator op, bool canceled) {
			MainTransformsPanel.SetVisible(true);
			OperatorPanel.ClearChildren();
			OperatorPanel.SetVisible(false);
			SelectableTypes = null;
		}

		public DefaultOperator? DefaultOperator { get; private set; }
		readonly DeformTool deformTool = new();
		// whichever tool took the last left click
		DefaultOperator? toolOp;
		public EditorDefaultOperator DefaultOperatorType { get; private set; }

		public bool InPoseMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.PoseBoneToTarget;
		public bool InWeightsMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ChangeMeshWeights;
		public bool InCreateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.CreateNewBones;

		public bool InRotateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.RotateSelection;
		public bool InTranslateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.TranslateSelection;
		public bool InScaleMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ScaleSelection;
		public bool InShearMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ShearSelection;

		EditorDefaultOperator? lastOperatorType;
		bool operatorChosen;
		public void SetEditorOperator(EditorDefaultOperator op) {
			if (operatorChosen && op != DefaultOperatorType)
				lastOperatorType = DefaultOperatorType;
			operatorChosen = true;
			PoseBonesOpBtn.Pulsing = false;
			WeighVerticesOpBtn.Pulsing = false;
			CreateBonesOpBtn.Pulsing = false;
			TransformRotation.GetButton().Pulsing = false;
			TransformTranslation.GetButton().Pulsing = false;
			TransformScale.GetButton().Pulsing = false;
			TransformShear.GetButton().Pulsing = false;

			DefaultOperator?.Deactivated();
			DefaultOperator = null;
			DefaultOperatorType = op;

			switch (op) {
				case EditorDefaultOperator.PoseBoneToTarget:
					PoseBonesOpBtn.Pulsing = true;
					DefaultOperator = new PoseBonesOperator();
					break;
				case EditorDefaultOperator.ChangeMeshWeights:
					WeighVerticesOpBtn.Pulsing = true;
					DefaultOperator = new VertexWeightOperator();
					break;
				case EditorDefaultOperator.CreateNewBones:
					CreateBonesOpBtn.Pulsing = true;
					DefaultOperator = new CreateBonesOperator();
					break;
				case EditorDefaultOperator.RotateSelection:
					TransformRotation.GetButton().Pulsing = true;
					DefaultOperator = new RotateSelectionOperator();
					break;
				case EditorDefaultOperator.TranslateSelection:
					TransformTranslation.GetButton().Pulsing = true;
					DefaultOperator = new TranslateSelectionOperator();
					break;
				case EditorDefaultOperator.ScaleSelection:
					TransformScale.GetButton().Pulsing = true;
					DefaultOperator = new ScaleSelectionOperator();
					break;
				case EditorDefaultOperator.ShearSelection:
					TransformShear.GetButton().Pulsing = true;
					DefaultOperator = new ShearSelectionOperator();
					break;
			}

			DefaultOperator?.Activated();
		}

		private static readonly Color TYPING_AXES_COLOR = new(225, 135, 40);
		private static void HighlightWhileTyping(Button button, Func<bool> when) {
			Color? normal = null;
			button.Thinking += _ => {
				if (when()) {
					normal ??= button.GetBgColor();
					button.SetBgColor(TYPING_AXES_COLOR);
				}
				else if (normal is Color original) {
					button.SetBgColor(original);
					normal = null;
				}
			};
		}

		public EditorTransformMode TransformMode { get; private set; }
		private void SetTransformMode(EditorTransformMode mode) {
			TransformMode = mode;
			if (TransformRotation != null)
				Active_SelectedChanged();

			switch (TransformMode) {
				case EditorTransformMode.LocalCoordinates:
					LocalTransformButton.Pulsing = true;
					ParentTransformButton.Pulsing = false;
					WorldTransformButton.Pulsing = false;
					return;
				case EditorTransformMode.ParentCoordinates:
					LocalTransformButton.Pulsing = false;
					ParentTransformButton.Pulsing = true;
					WorldTransformButton.Pulsing = false;
					return;
				case EditorTransformMode.WorldCoordinates:
					LocalTransformButton.Pulsing = false;
					ParentTransformButton.Pulsing = false;
					WorldTransformButton.Pulsing = true;
					return;
			}
		}

		private void CHANGE_Rotation(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.RotateSelectedAxes((float)newValue, TransformMode);
		}
		private void CHANGE_TranslationX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateSelectedAxes(0, (float)newValue, TransformMode);
		}
		private void CHANGE_TranslationY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateSelectedAxes(1, (float)newValue, TransformMode);
		}
		private void CHANGE_ScaleX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleSelectedAxes(0, (float)newValue, scaleAxes);
		}
		private void CHANGE_ScaleY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleSelectedAxes(1, (float)newValue, scaleAxes);
		}

		// World scale can't be typed, typed values are local
		bool typingScale;
		EditorTransformMode scaleAxes => typingScale && TransformMode == EditorTransformMode.WorldCoordinates ? EditorTransformMode.LocalCoordinates
			: TransformMode;

		private void CHANGE_ShearX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ShearXSelected((float)newValue);
		}
		private void CHANGE_ShearY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ShearYSelected((float)newValue);
		}
		private void Active_SelectedChanged() {
			PreUIDeterminations determinations = ModelEditor.Active.GetDeterminations();

			bool supportRotation = false;
			bool supportTranslation = false;
			bool supportScale = false;
			bool supportShear = false;

			if (!determinations.IsValid()) {
				SelectMode = ViewportSelectMode.All;
				goto end;
			}

			if (determinations.Last.SelectMode != ViewportSelectMode.NotApplicable) {
				SelectMode = determinations.Last.SelectMode;
			}

			bool first = true;
			float rotV = 0f, posX = 0f, posY = 0f, scaX = 0f, scaY = 0f, sheX = 0f, sheY = 0f;
			var axes = TransformMode;

			foreach (var selected in determinations.Selected) {
				var item = selected.DeferTransformationsTo();
				if (item == null) continue;

				if (first) {
					supportRotation = item.CanRotate();
					rotV = TransformAxes.GetRotation(item, axes);
					TransformRotation.GetNumSlider(0).SetValueNoUpdate(rotV);

					supportTranslation = item.CanTranslate();
					var pos = TransformAxes.GetTranslation(item, axes);
					posX = pos.X; posY = pos.Y;
					TransformTranslation.GetNumSlider(0).SetValueNoUpdate(posX);
					TransformTranslation.GetNumSlider(1).SetValueNoUpdate(posY);

					supportScale = item.CanScale();
					var sca = TransformAxes.GetScale(item, axes);
					scaX = sca.X; scaY = sca.Y;
					TransformScale.GetNumSlider(0).SetValueNoUpdate(scaX);
					TransformScale.GetNumSlider(1).SetValueNoUpdate(scaY);

					supportShear = item.CanShear();
					sheX = item.GetShearX();
					sheY = item.GetShearY();
					TransformShear.GetNumSlider(0).SetValueNoUpdate(sheX);
					TransformShear.GetNumSlider(1).SetValueNoUpdate(sheY);

					first = false;
				}
				else {
					// When multiple items are selected, disable support if values differ. ie.
					// 1. unsupported && equals   == don't support
					// 2. supported && not equals == don't support
					//        (which leads to the former scenario happening for the rest of the selected items)

					supportRotation = supportRotation && TransformAxes.GetRotation(item, axes) == rotV;
					supportTranslation = supportTranslation && TransformAxes.GetTranslation(item, axes) == new Vector2F(posX, posY);
					supportScale = supportScale && TransformAxes.GetScale(item, axes) == new Vector2F(scaX, scaY);
					supportShear = supportShear && item.GetShearX() == sheX && item.GetShearY() == sheY;
				}
			}

		end:
			TransformRotation.ValuesEnabled = supportRotation;
			TransformTranslation.ValuesEnabled = supportTranslation;
			TransformScale.ValuesEnabled = supportScale;
			TransformShear.ValuesEnabled = supportShear;
		}

		private float cameraX = 0;
		private float cameraY = 0;
		private float cameraZoom = 1;

		public float CameraX {
			get => cameraX;
			set {
				cameraX = value;
				ModelEditor.Active.File.CameraX = value;
			}
		}

		public float CameraY {
			get => cameraY;
			set {
				cameraY = value;
				ModelEditor.Active.File.CameraY = value;
			}
		}

		public float CameraZoom {
			get => cameraZoom;
			set {
				cameraZoom = value;
				ModelEditor.Active.File.CameraZoom = value;
			}
		}

		public Vector2F HoverGridPos { get; private set; }

		//private bool IsTypeProhibitedByOperator<T>() => SelectableTypes == null ? false : !SelectableTypes.Contains(typeof(T));
		public bool IsTypeProhibitedByOperator(Type T) => SelectableTypes == null ? false : !SelectableTypes.Contains(T);

		public bool CanSelect(IEditorType type) {
			// The item's own Select checkbox. The tree can still select it.
			if (type is EditorBone { ViewportCanSelect: false } or EditorAttachment { ViewportCanSelect: false })
				return false;
			if (SelectableTypes != null)
				return !IsTypeProhibitedByOperator(type.GetType());

			if (DefaultOperator != null)
				return DefaultOperator.IsSelectable(type);

			return true;
		}


		private void PickFilters(out bool bones, out bool images, out bool meshes) => PickFilters(out bones, out images, out meshes, out _);
		private void PickFilters(out bool bones, out bool images, out bool meshes, out bool others) {
			var options = ViewportOptions;
			// Hidden bones can still be picked, they show under the mouse.
			bool bonesOn = options.Selectable[(int)ViewportItemKind.Bones];
			bool imagesOn = options.Selectable[(int)ViewportItemKind.Images] && options.Visible[(int)ViewportItemKind.Images];
			bool othersOn = options.Selectable[(int)ViewportItemKind.Others] && options.Visible[(int)ViewportItemKind.Others];
			bones = (SelectMode & ViewportSelectMode.Bones) == ViewportSelectMode.Bones && bonesOn;
			images = (SelectMode & ViewportSelectMode.Images) == ViewportSelectMode.Images && imagesOn;
			meshes = (SelectMode & ViewportSelectMode.Meshes) == ViewportSelectMode.Meshes && imagesOn;
			others = (SelectMode & ViewportSelectMode.Other) == ViewportSelectMode.Other && othersOn;
		}

		/// <summary>
		/// Everything selectable with its origin inside a screen-space box
		/// </summary>
		public List<IEditorType> ItemsInBox(Vector2F screenA, Vector2F screenB) {
			var gridA = ScreenToGrid(screenA);
			var gridB = ScreenToGrid(screenB);
			float minX = MathF.Min(gridA.X, gridB.X), maxX = MathF.Max(gridA.X, gridB.X);
			float minY = MathF.Min(gridA.Y, gridB.Y), maxY = MathF.Max(gridA.Y, gridB.Y);

			PickFilters(out bool bones, out bool images, out bool meshes, out bool others);
			List<IEditorType> found = [];
			foreach (var model in ModelEditor.Active.File.Models) {
				if (model.Hidden) continue;
				foreach (var slot in model.Slots) {
					foreach (var attachment in slot.Attachments) {
						if (!attachment.GetVisible() || !CanSelect(attachment) || !slot.Bone.IsActive) continue;
						bool kindOn = attachment switch {
							EditorMeshAttachment => meshes,
							EditorRegionAttachment => images,
							EditorPathAttachment => others,
							_ => false
						};
						if (kindOn && Inside(attachment.GetWorldPosition(), minX, maxX, minY, maxY))
							found.Add(attachment);
					}
				}

				if (bones) {
					foreach (var bone in model.GetAllBones()) {
						if (!bone.Hidden && bone.IsActive && CanSelect(bone) && Inside(bone.GetWorldPosition(), minX, maxX, minY, maxY))
							found.Add(bone);
					}
				}
			}
			return found;
		}

		private static bool Inside(Vector2F point, float minX, float maxX, float minY, float maxY)
			=> point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;

		protected override void OnThink() {
			var activeOp = ModelEditor.Active.File.ActiveOperator;
			ModelEditor.Active.File.Timeline.AddDeltaTime(EngineCore.Level.CurtimeDelta, ModelEditor.Active.File.ActiveAnimation?.CalculateMaxTime() ?? 0);
			foreach (var model in ModelEditor.Active.File.Models)
				model.UpdateConstraints();
			CollectFiredEvents();

			if (IsHovered()) {
				// Hover determination
				HoverGridPos = ScreenToGrid(GetMousePos());

				PickFilters(out bool canHoverTest_Bones, out bool canHoverTest_Images, out bool canHoverTest_Meshes, out bool canHoverTest_Others);

				IEditorType? hovered = null;

				foreach (var model in ModelEditor.Active.File.ModelsBackToFront) {
					EditorAttachment? firstAttachment = null;
					foreach (var slot in model.Slots) {
						foreach (var attachment in slot.Attachments) {
							var lastHovered = hovered;

							if (!attachment.GetVisible() || !slot.Bone.IsActive) continue;

							bool hovertest = attachment.HoverTest(HoverGridPos) && CanSelect(attachment);

							switch (attachment) {
								case EditorRegionAttachment:
									if (canHoverTest_Images && hovertest) hovered = attachment; break;
								case EditorPathAttachment:
									if (canHoverTest_Others && hovertest)
										hovered = attachment; break;
								case EditorMeshAttachment meshAttachment:
									if (canHoverTest_Meshes && hovertest)
										hovered = attachment;

									// Test the vertices, if selected
									if (meshAttachment.Selected) {
										foreach (var vertex in meshAttachment.GetVertices()) {
											if (vertex.HoverTest(HoverGridPos)) {
												hovered = vertex;
											}
										}
									}

									break;
							}

							if (hovered == attachment) {
								if (firstAttachment == null)
									firstAttachment = attachment;
								else {
									if (!attachment.HoverTestOpacity(HoverGridPos))
										hovered = lastHovered; // reset back, only use fallback if needed
								}
							}
						}
						if (hovered is EditorVertex) break;
					}

					if (hovered is not EditorVertex && canHoverTest_Bones) {
						foreach (var bone in model.GetAllBones()) {
							if (bone.IsActive && bone.HoverTest(HoverGridPos) && CanSelect(bone))
								hovered = bone;
						}
					}
				}

				// name tags sit on top of everything
				if (hovered is not EditorVertex) {
					var mouse = GetMousePos();
					for (int i = nameTags.Count - 1; i >= 0; i--) {
						var tag = nameTags[i];
						bool kindOn = tag.Item switch {
							EditorBone => canHoverTest_Bones,
							EditorMeshAttachment => canHoverTest_Meshes,
							EditorRegionAttachment => canHoverTest_Images,
							_ => canHoverTest_Others
						};
						if (kindOn && CanSelect(tag.Item) && mouse.X >= tag.Rect.X && mouse.X <= tag.Rect.X + tag.Rect.W && mouse.Y >= tag.Rect.Y && mouse.Y <= tag.Rect.Y + tag.Rect.H) {
							hovered = tag.Item;
							break;
						}
					}
				}

				// The active operator has the final say after our determinations were made
				// If no active operator; default to true
				// If active operator; default is true, unless operator overrode the method
				if (!(activeOp?.HoverTest(hovered) ?? true)) {
					return;
				}

				if (HoveredObject != null) {
					HoveredObject.Hovered = false;
					HoveredObject.OnMouseLeft();
				}

				HoveredObject = hovered;

				if (hovered != null) {
					hovered.Hovered = true;
					hovered.OnMouseEntered();
				}
			}
			else {
				if (HoveredObject != null) {
					HoveredObject.Hovered = false;
					HoveredObject.OnMouseLeft();
				}
				HoveredObject = null;
			}
			activeOp?.Think(ModelEditor.Active, HoverGridPos);
		}
		public Vector2F ScreenToGrid(Vector2F screenPos) {
			Vector2F screenCoordinates = Vector2F.Remap(screenPos, new(0), GetRenderBounds().Size, new(0, 0), EngineCore.GetWindowSize());
			Vector2F halfScreenSize = EngineCore.GetWindowSize() / 2;
			Vector2F centeredCoordinates = screenCoordinates - halfScreenSize;

			Vector2F withoutCamVars = centeredCoordinates * new Vector2F(GetRenderBounds().W / widthMultiplied, 1);
			Vector2F accountingForCamVars = (withoutCamVars / CameraZoom) + new Vector2F(CameraX, -CameraY);
			return accountingForCamVars * new Vector2F(1, -1);
		}
		public Vector2F GridToScreen(Vector2F gridPos) {
			gridPos *= new Vector2F(1, -1);
			Vector2F size = EngineCore.GetWindowSize();
			size.W *= GetRenderBounds().W / widthMultiplied;

			return Vector2F.Remap(
				(gridPos - new Vector2F(CameraX, -CameraY)) * CameraZoom,
				-size / 2, size / 2, new(0), GetRenderBounds().Size);
		}

		Vector2F ClickPos;
		bool CanDragObject;
		bool CanDragCamera;

		private float __LengthUntilDragStarts = 8f;
		private bool __dragBlocked = false;
		private bool __startedDrag = false;
		private bool __startDraggingOperator = false;
		private Vector2F __dragStartScreenspace = Vector2F.Zero;
		private Vector2F __dragStartGridspace = Vector2F.Zero;

		public IEditorType? HoveredObject { get; private set; }
		public IEditorType? ClickedObject { get; private set; }

		bool boxPending;
		bool boxSelecting;
		bool boxAdditive;
		bool toolClick;
		Vector2F boxStart;
		double lastEmptyClick = -1;
		Vector2F lastEmptyClickPos;

		/// <summary>
		/// The box being dragged out, in panel space
		/// </summary>
		public RectangleF? SelectionBox => boxSelecting ? ScreenBox(boxStart, GetMousePos()) : null;

		public static RectangleF ScreenBox(Vector2F start, Vector2F end)
			=> RectangleF.XYWH(MathF.Min(start.X, end.X), MathF.Min(start.Y, end.Y), MathF.Abs(start.X - end.X), MathF.Abs(start.Y - end.Y));

		protected override bool MouseClick(FrameState state, ButtonCode button) {
			base.MouseClick(state, button);
			ModelEditor.Active.Dopesheet.Focused = false;
			ModelEditor.Active.Graph.Focused = false;
			ClickPos = GetMousePos();
			ClickedObject = null;
			__dragBlocked = false;
			toolClick = false;
			operatorAtPress = ModelEditor.Active.File.IsOperatorActive;

			if (button == ButtonCode.Mouse1 && CanUseDefaultOperator) {
				if (deformTool.ToolClick(this, ClickPos, state.Keyboard.ControlDown))
					toolOp = deformTool;
				else if (DefaultOperator.ToolClick(this, ClickPos, state.Keyboard.ControlDown))
					toolOp = DefaultOperator;
				else
					toolOp = null;

				if (toolOp != null) {
					toolClick = true;
					CanDragObject = CanDragCamera = false;
					return true;
				}
			}

			// ctrl+drag or middle drag box selects, ctrl+click toggles
			bool ctrl = state.Keyboard.ControlDown;
			if (!ModelEditor.Active.File.IsOperatorActive && (button == ButtonCode.Mouse3 || (button == ButtonCode.Mouse1 && ctrl))) {
				boxPending = true;
				boxSelecting = false;
				boxStart = ClickPos;
				boxAdditive = ctrl || state.Keyboard.ShiftDown;
				ClickedObject = HoveredObject;
				CanDragObject = CanDragCamera = false;
				return true;
			}

			// create tool clicks empty space on purpose
			if (button == ButtonCode.Mouse1 && HoveredObject == null && !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType != EditorDefaultOperator.CreateNewBones) {
				var now = EngineCore.Level.Realtime;
				if (lastEmptyClick >= 0 && now - lastEmptyClick < 0.4 && (ClickPos - lastEmptyClickPos).Length < 6) {
					ModelEditor.Active.UnselectAllObjects();
					lastEmptyClick = -1;
					__dragBlocked = true;
					return true;
				}
				lastEmptyClick = now;
				lastEmptyClickPos = ClickPos;
			}

			var doesOperatorAllowClick = button != ButtonCode.Mouse1 || !CanUseDefaultOperator || DefaultOperator.GizmoClicked(this, HoveredObject, ClickPos);
			if (HoveredObject != null && button == ButtonCode.Mouse1) {
				if (doesOperatorAllowClick == false) {
					__dragBlocked = true;
					return true;
				}

				ClickedObject = HoveredObject;
				var operatorActive = ModelEditor.Active.File.ActiveOperator != null;
				if (operatorActive) {
					ModelEditor.Active.SelectObject(HoveredObject, state.Keyboard.ShiftDown);
				}
				else {
					//if(ModelEditor.Active.IsObjectSelected(HoveredObject))
				}
			}

			if (button == ButtonCode.Mouse1)
				ModelEditor.Active.File.ActiveOperator?.Clicked(ModelEditor.Active, ClickPos);

			__startedDrag = false;
			__startDraggingOperator = false;
			CanDragObject = button == ButtonCode.Mouse1;
			CanDragCamera = button == ButtonCode.Mouse2;

			return true;
		}

		bool operatorAtPress;

		[MemberNotNullWhen(true, nameof(DefaultOperator))]
		public bool CanUseDefaultOperator => DefaultOperator != null && !ModelEditor.Active.File.IsOperatorActive;

		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			base.MouseDrag(self, state, delta);
			if (toolClick) {
				toolOp?.ToolDrag(this, ClickPos, GetMousePos());
				return true;
			}
			if (boxPending) {
				if ((GetMousePos() - boxStart).Length > __LengthUntilDragStarts)
					boxSelecting = true;
				return true;
			}
			if (__dragBlocked) return false;

			var pos = GetMousePos();
			if (((pos - ClickPos).Length > __LengthUntilDragStarts || ModelEditor.Active.SelectedObjectsCount > 0) && !__startedDrag) {
				__startedDrag = true;
				// From the press, not from where the drag got noticed
				__dragStartScreenspace = ClickPos;
				__dragStartGridspace = ScreenToGrid(ClickPos);
				if (CanDragObject) {
					// In some operators we would do something here
					if (CanUseDefaultOperator) {
						__startDraggingOperator = DefaultOperator.GizmoStartDragging(this, ClickPos, ModelEditor.Active.FirstSelectedObject, ClickedObject);
					}
					if (!__startDraggingOperator) {
						if (HoveredObject != null) {
							if (!ModelEditor.Active.IsObjectSelected(HoveredObject)) {
								ModelEditor.Active.File.ActiveOperator?.DragStart(ModelEditor.Active, ClickPos);
								ModelEditor.Active.SelectObject(HoveredObject, state.Keyboard.ShiftDown);
							}
						}
					}
				}
			}

			if (CanUseDefaultOperator && __startedDrag && CanDragObject && ModelEditor.Active.SelectedObjectsCount > 0) {
				DefaultOperator?.GizmoDrag(this, __dragStartScreenspace, pos, ModelEditor.Active.SelectedObjects);
			}
			if (ModelEditor.Active.File.IsOperatorActive) {
				ModelEditor.Active.File.ActiveOperator?.Drag(ModelEditor.Active, __dragStartScreenspace, GetMousePos());
			}
			if (__startedDrag && CanDragCamera) {
				var dragGridPos = ScreenToGrid(GetMousePos()) - __dragStartGridspace;
				CameraX -= dragGridPos.X;
				CameraY -= dragGridPos.Y;
			}

			return true;
		}

		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			base.MouseRelease(self, state, button);
			CanDragCamera = false;
			__dragBlocked = false;

			if (toolClick && button == ButtonCode.Mouse1) {
				toolClick = false;
				toolOp?.ToolRelease(this, ClickPos, GetMousePos(), (GetMousePos() - ClickPos).Length > __LengthUntilDragStarts);
				return true;
			}

			if (boxPending) {
				var editor = ModelEditor.Active;
				if (boxSelecting) {
					var found = ItemsInBox(boxStart, GetMousePos());
					if (!boxAdditive)
						editor.UnselectAllObjects();
					List<IEditorType> add = [];
					foreach (var item in found) {
						if (!editor.IsObjectSelected(item))
							add.Add(item);
					}
					if (add.Count > 0)
						editor.SelectObjects(add.ToArray());
				}
				else if (button == ButtonCode.Mouse1 && ClickedObject != null) {
					if (editor.IsObjectSelected(ClickedObject))
						editor.UnselectObject(ClickedObject);
					else
						editor.SelectObject(ClickedObject, true);
				}
				boxPending = boxSelecting = false;
				return true;
			}

			if (button == ButtonCode.Mouse2 && (GetMousePos() - ClickPos).Length <= __LengthUntilDragStarts) {
				if (ModelEditor.Active.File.ActiveOperator != null)
					ModelEditor.Active.File.ActiveOperator.RightClicked(ModelEditor.Active, GetMousePos());
				// right click swaps to the last tool
				else if (lastOperatorType is EditorDefaultOperator last)
					SetEditorOperator(last);
			}

			if (button == ButtonCode.Mouse1) {
				bool allowSelection = operatorAtPress || !CanUseDefaultOperator || DefaultOperator.GizmoReleased(this, ClickedObject, GetMousePos());
				ModelEditor.Active.File.ActiveOperator?.DragRelease(ModelEditor.Active, ClickPos);
				if (
					ClickedObject != null
					&& !ModelEditor.Active.IsObjectSelected(ClickedObject)
					&& allowSelection
				) {
					var activeOp = ModelEditor.Active.File.ActiveOperator;
					if (activeOp == null || !activeOp.SelectMultiple) // Kind of a hack, but the editor would
																	  // trigger a second selection otherwise
						ModelEditor.Active.SelectObject(ClickedObject, state.Keyboard.ShiftDown);
				}
			}

			return true;
		}

		public const float MIN_ZOOM = 0.05f;
		public const float MAX_ZOOM = 40;

		public bool IsWindowPointInside(Vector2F windowPos) {
			var pos = GetGlobalPosition();
			var size = GetRenderBounds().Size;
			return windowPos.X >= pos.X && windowPos.Y >= pos.Y && windowPos.X < pos.X + size.X && windowPos.Y < pos.Y + size.Y;
		}

		public void ZoomAt(float zoom) => CameraZoom = Math.Clamp(zoom, MIN_ZOOM, MAX_ZOOM);

		public void FitToView() {
			float minX = float.MaxValue, minY = float.MaxValue;
			float maxX = float.MinValue, maxY = float.MinValue;

			foreach (var model in ModelEditor.Active.File.Models) {
				if (model.Hidden) continue;
				foreach (var slot in model.Slots) {
					switch (slot.GetActiveAttachment()) {
						case EditorRegionAttachment region when !region.Hidden:
							var quad = region.QuadPoints();
							Add(quad.TL, ref minX, ref minY, ref maxX, ref maxY);
							Add(quad.TR, ref minX, ref minY, ref maxX, ref maxY);
							Add(quad.BL, ref minX, ref minY, ref maxX, ref maxY);
							Add(quad.BR, ref minX, ref minY, ref maxX, ref maxY);
							break;
						case EditorMeshAttachment mesh when !mesh.Hidden:
							foreach (var vertex in mesh.GetVertices())
								Add(mesh.CalculateVertexWorldPosition(mesh.WorldTransform, vertex), ref minX, ref minY, ref maxX, ref maxY);
							break;
					}
				}
			}
			// Bones only when there's no art, effect bones can sit far off.
			if (minX > maxX) {
				foreach (var model in ModelEditor.Active.File.Models) {
					if (!model.Hidden) {
						foreach (var bone in model.GetAllBones())
							Add(bone.GetWorldPosition(), ref minX, ref minY, ref maxX, ref maxY);
					}
				}
			}
			if (minX > maxX) return;

			// Visible grid area at zoom 1, above the toolbar
			var size = GetRenderBounds().Size;
			float toolbarHeight = MainTransformsPanel.IsVisible() ? MainTransformsPanel.GetRenderBounds().H : 0;
			var area = new Vector2F(size.W, MathF.Max(size.H - toolbarHeight, size.H * 0.5f));
			CameraZoom = 1;
			var topLeft = ScreenToGrid(Vector2F.Zero);
			var bottomRight = ScreenToGrid(area);
			float visW = MathF.Abs(bottomRight.X - topLeft.X), visH = MathF.Abs(bottomRight.Y - topLeft.Y);

			ZoomAt(0.85f * MathF.Min(visW / MathF.Max(maxX - minX, 1), visH / MathF.Max(maxY - minY, 1)));
			CameraX = (minX + maxX) / 2;
			CameraY = (minY + maxY) / 2;

			var shift = ScreenToGrid(area / 2) - ScreenToGrid(size / 2);
			CameraX -= shift.X;
			CameraY -= shift.Y;
		}

		private static void Add(Vector2F point, ref float minX, ref float minY, ref float maxX, ref float maxY) {
			if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) return;
			minX = MathF.Min(minX, point.X); maxX = MathF.Max(maxX, point.X);
			minY = MathF.Min(minY, point.Y); maxY = MathF.Max(maxY, point.Y);
		}

		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			// keep the point under the cursor fixed
			var mouse = GetMousePos();
			var before = ScreenToGrid(mouse);
			ZoomAt(CameraZoom + (delta.Y / 5 * CameraZoom));
			var after = ScreenToGrid(mouse);
			CameraX += before.X - after.X;
			CameraY += before.Y - after.Y;
			return true;
		}
		Camera3D cam;
		float widthMultiplied;
		public void Draw3DCursor() {
			//Raylib.DrawSphere(new(HoverGridPos.X, HoverGridPos.Y, 0), 2, Color.RED);
		}

		/// <summary>
		/// Draws a bone based on its posing
		/// </summary>
		/// <param name="bone"></param>
		public void DrawBone(EditorBone bone) {
			var wt = bone.WorldTransform;
			var wp = wt.Translation.ToNumerics();
			var cameraFOV = cam.FovY;
			// Depending on cameraFOV, we shrink this down a bit...
			var limitBy = 500;
			var byHowMuch = cameraFOV < limitBy ? 1 - ((limitBy - cameraFOV) / limitBy) : 1;

			bool selected = bone.Selected;
			if (!selected) {
				selected = bone.Model.Root == bone && bone.Model.Selected;

				if (!selected) {
					foreach (var slot in bone.Slots)
						if (slot.Selected) {
							selected = true;
							break;
						}
				}

			}

			Color color;
			if (selected)
				color = Color.SkyBlue;
			else if (bone.Hovered)
				color = bone.Color.Adjust(0, -0.3f, 0.3f);
			else
				color = bone.Color.Adjust(0, 0, -0.15f) with { A = (byte)(bone.Length > 0 ? 180 : 255) };
			ITexture boneTex;

			if (ModelEditor.Active.Editor.InWeightsMode
			 && VertexWeightOperator.Target is EditorVertexAttachment meshAttachment) {
				// Override the color
				var weights = meshAttachment.Weights.FirstOrDefault(x => x.Bone == bone);
				if (weights == null)
					color = new Color(170, 170, 170, 40);
				else
					color = meshAttachment.WeightColor(weights, color.A);
			}

			if (bone.Length > 0) {
				boneTex = textures.LoadTextureFromFile("models/lengthbonetex.png");
				var innerRing = textures.LoadTextureFromFile("models/bonering.png");
				var lengthMul = (float)NMath.Remap(bone.Length, 40, 150, 0.38, 1, true) * 6;
				Raylib.DrawTexturePro(innerRing.ToRaylibTexture(), new(0, 0, innerRing.GetWidth(), innerRing.GetHeight()), new(wt.X - (lengthMul / 2), wt.Y - (lengthMul / 2), lengthMul, lengthMul), new(0), 0, color);
			}
			else
				boneTex = textures.LoadTextureFromFile("models/lengthlessbonetex.png");

			bone.GetTexCoords(byHowMuch, out var baseBottom, out var baseTop, out var tipBottom, out var tipTop, out var lengthLimit);

			if (bone.Length > 0) {
				Rlgl.Begin(DrawMode.TRIANGLES);

				Rlgl.Color4ub(color.R, color.G, color.B, color.A);

				Rlgl.SetTexture(boneTex.GetTextureHandle());
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(tipBottom.X, tipBottom.Y, 0);

				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(baseTop.X, baseTop.Y, 0);

				// Inverted because for some reason disabling backface culling doesn't want to work
				// (or maybe something else is going on that I don't understand, regardless, that's
				// why this is repeated but in reverse)
				/*
				Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(tipBottom.X, tipBottom.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);

				Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(baseTop.X, baseTop.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				*/
				Rlgl.End();
			}
			else {
				var size = 32f * byHowMuch;
				Rlgl.PushMatrix();
				var wps = bone.WorldTransform.LocalToWorld(0, 0).ToNumerics();
				var wd = bone.WorldTransform.LocalToWorld(1, 0).ToNumerics();
				var dir = (wd - wps);
				var rot = MathF.Atan2(dir.Y, dir.X).ToDegrees() + 90;
				Rlgl.Translatef(wps.X, wps.Y, 0);
				Rlgl.Rotatef(rot, 0, 0, 1);
				Raylib.DrawTexturePro(
					boneTex.ToRaylibTexture(),
					new(0, 0, boneTex.GetWidth(), boneTex.GetHeight()),
					new(-size / 2f, -size / 2f, size, size),
					new(0),
					0,
					color
				);
				Rlgl.PopMatrix();
			}
		}

		/// <summary>
		/// Draws all models in the working model list
		/// </summary>
		public void DrawModels() {
			foreach (var model in ModelEditor.Active.File.ModelsBackToFront) {
				if (model.Hidden) continue;
				model.UpdateConstraints();

				bool showImages = ViewportOptions.Visible[(int)ViewportItemKind.Images];
				Ghosting.Draw(model, false, showImages);
				foreach (var slot in model.DrawOrder) {
					var attachment = slot.GetActiveAttachment();
					if (slot.Bone.IsActive && (showImages || attachment is not (EditorRegionAttachment or EditorMeshAttachment)))
						attachment?.Render();
					model.Clipping.NextSlot(slot);
				}

				model.Clipping.End();
				Ghosting.Draw(model, true, showImages);
			}

			foreach (var model in ModelEditor.Active.File.Models) {
				if (model.Hidden) continue;

				foreach (var slot in model.Slots) {
					if (slot.GetActiveAttachment()
						is EditorMeshAttachment { Wireframe: true, Selected: false, Hidden: false } wire && wire != HoveredObject && slot.Bone.IsActive)
						wire.RenderOverlay();
				}

				bool showBones = ViewportOptions.Visible[(int)ViewportItemKind.Bones];
				foreach (var bone in model.GetAllBones()) {
					if (bone.IsActive && (showBones || bone == HoveredObject || bone.Selected))
						DrawBone(bone);
				}
				if (!showBones) continue;

				DrawConstraints(model, model.IkConstraints);
				DrawConstraints(model, model.TransformConstraints);
				DrawConstraints(model, model.PathConstraints);
			}

			bool caughtHovered = false;
			foreach (var obj in ModelEditor.Active.SelectedObjects) {
				if (obj is EditorAttachment attachment)
					attachment.RenderOverlay();
				if (obj == HoveredObject)
					caughtHovered = true;
			}

			if (!caughtHovered && HoveredObject is EditorAttachment hAttachment)
				hAttachment.RenderOverlay();
		}


		private static void DrawConstraints<T>(EditorModel model, List<T> constraints) where T : EditorConstraint {
			foreach (var constraint in constraints) {
				if (!constraint.Hidden && constraint.IsActiveIn(model.ActiveSkin))
					constraint.RenderViewport();
			}
		}

		// last drawn name tags, for clicking them
		readonly List<(RectangleF Rect, IEditorType Item)> nameTags = [];

		private void DrawName(IEditorType item, Vector2F gridPos, string name, Color color, bool skipIfOverlapping = false) {
			var at = GridToScreen(gridPos);
			var size = Graphics2D.GetTextSize(name, Graphics2D.UI_FONT_NAME, 16);
			var rect = RectangleF.XYWH(at.X - size.X / 2, at.Y - size.Y, size.X, size.Y);
			// a name shown by its own Name checkbox only draws if it doesn't overlap another
			if (skipIfOverlapping && nameTags.Any(x => x.Rect.X < rect.X + rect.W && rect.X < x.Rect.X + x.Rect.W && x.Rect.Y < rect.Y + rect.H && rect.Y < x.Rect.Y + x.Rect.H))
				return;
			nameTags.Add((rect, item));
			var screen = at - new Vector2F(0, GetRenderBounds().Pos.Y);
			Graphics2D.SetDrawColor(0, 0, 0, 200);
			Graphics2D.DrawText(screen + new Vector2F(1, 1), name, Graphics2D.UI_FONT_NAME, 16, Anchor.BottomCenter);
			Graphics2D.SetDrawColor(color);
			Graphics2D.DrawText(screen, name, Graphics2D.UI_FONT_NAME, 16, Anchor.BottomCenter);
		}

		private void DrawNames() {
			var options = ViewportOptions;
			nameTags.Clear();
			bool boneNames = options.ShowNames[(int)ViewportItemKind.Bones];
			bool imageNames = options.ShowNames[(int)ViewportItemKind.Images] && options.Visible[(int)ViewportItemKind.Images];
			bool otherNames = options.ShowNames[(int)ViewportItemKind.Others] && options.Visible[(int)ViewportItemKind.Others];

			foreach (var model in ModelEditor.Active.File.Models) {
				if (model.Hidden) continue;

				foreach (var slot in model.Slots) {
					var attachment = slot.GetActiveAttachment();
					if (attachment == null || attachment.Hidden) continue;
					bool isImage = attachment is EditorRegionAttachment or EditorMeshAttachment;
					if (isImage ? imageNames : otherNames)
						DrawName(attachment, attachment.GetWorldPosition(), attachment.Name, isImage ? new Color(200, 225, 255) : new Color(255, 200, 150));
					else if (attachment.ViewportShowName && options.Visible[(int)(isImage ? ViewportItemKind.Images : ViewportItemKind.Others)])
						DrawName(attachment, attachment.GetWorldPosition(), attachment.Name, isImage ? new Color(200, 225, 255) : new Color(255, 200, 150), true);
				}

				if (!options.Visible[(int)ViewportItemKind.Bones]) continue;
				foreach (var bone in model.GetAllBones()) {
					if (bone.IsActive && (boneNames || bone.ViewportShowName))
						DrawName(bone, bone.GetWorldPosition(), bone.Name, new Color(235, 235, 235));
				}
			}
		}

		// Event keys flash their name above the model during playback.
		private const double EVENT_SHOW_TIME = 0.75;
		readonly List<(string Name, double Time, EditorModel Model)> firedEvents = [];
		double lastEventPlayhead = -1;

		private void CollectFiredEvents() {
			var timeline = ModelEditor.Active.File.Timeline;
			double now = timeline.GetPlayhead();
			double last = lastEventPlayhead;
			lastEventPlayhead = now;
			int dir = timeline.PlayDirection;
			if (dir == 0 || last < 0 || last == now || !ModelEditor.Active.AnimationMode) return;

			double realtime = EngineCore.Level.Realtime;
			foreach (var model in ModelEditor.Active.File.Models) {
				var animation = model.ActiveAnimation;
				if (animation == null || model.Hidden) continue;
				foreach (var timelineAny in animation.Timelines) {
					if (timelineAny is not EventTimeline events) continue;
					foreach (var key in events.Keys.GetKeyframes()) {
						if (!Crossed(last, now, key.Time, dir) || key.Value == null) continue;
						foreach (var eventKey in key.Value) {
							var editorEvent = eventKey.Event;
							if (editorEvent != null && !editorEvent.Hidden)
								firedEvents.Add((editorEvent.Name, realtime, model));
						}
					}
				}
			}
		}

		// wraps when the playhead loops
		private static bool Crossed(double from, double to, double t, int dir) {
			if (dir > 0) return from <= to ? (t > from && t <= to) : (t > from || t <= to);
			return from >= to ? (t < from && t >= to) : (t < from || t >= to);
		}

		private void DrawFiredEvents() {
			if (firedEvents.Count == 0) return;
			double realtime = EngineCore.Level.Realtime;
			firedEvents.RemoveAll(x => realtime - x.Time > EVENT_SHOW_TIME);

			EditorModel? stackModel = null;
			Vector2F anchor = Vector2F.Zero;
			int stack = 0;
			for (int i = firedEvents.Count - 1; i >= 0; i--) {
				var fired = firedEvents[i];
				if (fired.Model != stackModel) {
					stackModel = fired.Model;
					stack = 0;
					float top = float.MinValue;
					foreach (var bone in fired.Model.GetAllBones()) {
						if (bone.IsActive)
							top = MathF.Max(top, bone.WorldTransform.Y);
					}
					anchor = new(fired.Model.Root.WorldTransform.X, top);
				}

				var screen = GridToScreen(anchor) - new Vector2F(0, GetRenderBounds().Pos.Y + 28 + stack * 24);
				int alpha = (int)(255 * Math.Clamp(1 - (realtime - fired.Time) / EVENT_SHOW_TIME, 0, 1));
				Graphics2D.SetDrawColor(0, 0, 0, alpha * 200 / 255);
				Graphics2D.DrawText(screen + new Vector2F(1, 1), fired.Name, Graphics2D.UI_FONT_NAME, 20, Anchor.BottomCenter);
				Graphics2D.SetDrawColor(255, 230, 90, alpha);
				Graphics2D.DrawText(screen, fired.Name, Graphics2D.UI_FONT_NAME, 20, Anchor.BottomCenter);
				stack++;
			}
		}

		public override void Paint(float width, float height) {
			cam = new Camera3D() {
				Projection = CameraProjection.Orthographic,
				FovY = EngineCore.GetWindowHeight() / CameraZoom,
				Position = new(CameraX, CameraY, 500),
				Target = new(CameraX, CameraY, 0),
				Up = new(0, 1, 0),
			};

			Vector2F globalpos = Graphics2D.Offset;
			Vector2F oldSize = EngineCore.GetScreenSize();
			float currentAspectRatio = width / height;
			float intendedAspectRatio = oldSize.W / oldSize.H;
			float accomodation = intendedAspectRatio / currentAspectRatio;
			widthMultiplied = width * accomodation;

			Surface.SetViewport(
				/* X */   (int)(globalpos.X - ((widthMultiplied - width) / 2)),
				/* Y */   oldSize.H - globalpos.Y - height,
				/* W */   (int)(widthMultiplied),
				/* H */   (int)height
			);
			EngineCore.Window.BeginMode3D(cam);
			Rlgl.DisableBackfaceCulling();
			Rlgl.DisableDepthMask();

			Checkerboard.Draw();
			Raylib.DrawLine3D(new(-10000, 0, 0), new(10000, 0, 0), Color.Black);
			Raylib.DrawLine3D(new(0, -10000, 0), new(0, 10000, 0), Color.Black);

			if (!(ModelEditor.Active.File.ActiveOperator?.RenderOverride() ?? false)) {
				DrawModels();
				Draw3DCursor();
			}

			EngineCore.Window.EndMode3D();
			Surface.ResetViewport();

			DrawNames();
			DrawFiredEvents();

			IEditorType? selectedTransformable = null;
			if (ModelEditor.Active.TryGetFirstSelected(out IEditorType? selected) && !ModelEditor.Active.File.IsOperatorActive) {
				selectedTransformable = selected.DeferTransformationsTo();
			}
			else if (ModelEditor.Active.File.IsOperatorActive) {
				selectedTransformable = HoveredObject;
			}

			if (selectedTransformable != null) {
				if (!ModelEditor.Active.File.IsOperatorActive)
					DefaultOperator?.GizmoRender(this, selectedTransformable);
				string text = $"{selectedTransformable.GetName()}";
				int fontSize = 20;
				Vector2F textSize = Graphics2D.GetTextSize(text, Graphics2D.UI_FONT_NAME, fontSize) + new Vector2F(6);
				Graphics2D.SetDrawColor(10, 10, 10, 190);
				Graphics2D.DrawRectangle((width / 2) - (textSize.W / 2), (height - 140) - (textSize.H / 2), textSize.W, textSize.H);
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawText(width / 2, height - 140, text, Graphics2D.UI_FONT_NAME, fontSize, Anchor.Center);
			}

			Rlgl.DrawRenderBatchActive();

			Rlgl.EnableDepthMask();
			Rlgl.EnableBackfaceCulling();

			Graphics2D.SetDrawColor(255, 255, 255);

			if (CanUseDefaultOperator) {
				deformTool.PaintOverlay(this, width, height);
				DefaultOperator.PaintOverlay(this, width, height);
			}

			if ((SelectionBox ?? ModelEditor.Active.File.ActiveOperator?.SelectionBox ?? (CanUseDefaultOperator ? deformTool.SelectionBox ?? DefaultOperator.SelectionBox : null)) is RectangleF box) {
				Graphics2D.SetDrawColor(120, 170, 255, 40);
				Graphics2D.DrawRectangle(box);
				Graphics2D.SetDrawColor(140, 185, 255, 220);
				Graphics2D.DrawRectangleOutline(box, 1);
				Graphics2D.SetDrawColor(255, 255, 255);
			}

			Operator? op = ModelEditor.Active.File.ActiveOperator;
			if (op != null) {
				Graphics2D.DrawText(new(width / 2, 32), $"{op.Name ?? "<NULL>"}", Graphics2D.UI_FONT_NAME, 32, Anchor.TopCenter);
			}
		}
	}
}
