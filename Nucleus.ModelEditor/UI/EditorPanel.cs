using glTFLoader.Schema;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nucleus.ModelEditor
{

	public class EditorPanel : View
	{
		public override string Name => "Editor";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		TransformPanel TransformRotation, TransformTranslation, TransformScale, TransformShear;
		Button LocalTransformButton, ParentTransformButton, WorldTransformButton;
		Button PoseBonesOpBtn, WeighVerticesOpBtn, CreateBonesOpBtn;

		CenteredObjectsPanel MainTransformsPanel;
		CenteredObjectsPanel OperatorPanel;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

		public ViewportSelectMode SelectMode { get; set; } = ViewportSelectMode.All;
		protected override void Initialize() {
			base.Initialize();

			Add(out MainTransformsPanel);
			MainTransformsPanel.ForceHeight = false;

			MainTransformsPanel.Dock = Dock.Bottom;
			MainTransformsPanel.Size = new(128);

			Add(out OperatorPanel);
			OperatorPanel.ForceHeight = false;

			OperatorPanel.Dock = Dock.Bottom;
			OperatorPanel.DockMargin = RectangleF.TLRB(0, 0, 0, -120); // Silly way to do this, but w/e
			OperatorPanel.Size = new(128);
			OperatorPanel.Visible = false;

			var modePanel = MainTransformsPanel.Add<FlexPanel>();
			modePanel.Direction = Directional180.Vertical;
			modePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			modePanel.Size = new(98, 90);

			PoseBonesOpBtn = modePanel.Add<Button>();
			PoseBonesOpBtn.Text = "Pose";
			PoseBonesOpBtn.MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.PoseBoneToTarget);

			WeighVerticesOpBtn = modePanel.Add<Button>();
			WeighVerticesOpBtn.Text = "Weights";
			WeighVerticesOpBtn.MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.ChangeMeshWeights);

			CreateBonesOpBtn = modePanel.Add<Button>();
			CreateBonesOpBtn.Text = "Create";
			CreateBonesOpBtn.MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.CreateNewBones);

			var transformPanel = MainTransformsPanel.Add<FlexPanel>();
			transformPanel.Direction = Directional180.Vertical;
			transformPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformPanel.Size = new(280, 115);

			TransformRotation = TransformPanel.New(transformPanel, "Rotate", 1, KeyframeProperty.Bone_Rotation);
			TransformTranslation = TransformPanel.New(transformPanel, "Translate", 2, KeyframeProperty.Bone_Translation);
			TransformScale = TransformPanel.New(transformPanel, "Scale", 2, KeyframeProperty.Bone_Scale);
			TransformShear = TransformPanel.New(transformPanel, "Shear", 2, KeyframeProperty.Bone_Shear);

			TransformRotation.GetNumSlider(0).OnValueChanged += CHANGE_Rotation;
			TransformTranslation.GetNumSlider(0).OnValueChanged += CHANGE_TranslationX;
			TransformTranslation.GetNumSlider(1).OnValueChanged += CHANGE_TranslationY;
			TransformScale.GetNumSlider(0).OnValueChanged += CHANGE_ScaleX;
			TransformScale.GetNumSlider(1).OnValueChanged += CHANGE_ScaleY;
			TransformShear.GetNumSlider(0).OnValueChanged += CHANGE_ShearX;
			TransformShear.GetNumSlider(1).OnValueChanged += CHANGE_ShearY;

			TransformRotation.GetButton().MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.RotateSelection);
			TransformTranslation.GetButton().MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.TranslateSelection);
			TransformScale.GetButton().MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.ScaleSelection);
			TransformShear.GetButton().MouseReleaseEvent += (_, _, _) => SetEditorOperator(EditorDefaultOperator.ShearSelection);

			var transformModePanel = MainTransformsPanel.Add<FlexPanel>();
			transformModePanel.Direction = Directional180.Vertical;
			transformModePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformModePanel.Size = new(70, 90);

			LocalTransformButton = transformModePanel.Add<Button>();
			LocalTransformButton.Text = "Local";
			LocalTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.LocalCoordinates);

			ParentTransformButton = transformModePanel.Add<Button>();
			ParentTransformButton.Text = "Parent";
			ParentTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.ParentCoordinates);

			WorldTransformButton = transformModePanel.Add<Button>();
			WorldTransformButton.Text = "World";
			WorldTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.WorldCoordinates);

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			ModelEditor.Active.SetupAnimateModeChanged += (_, _) => Active_SelectedChanged();
			ModelEditor.Active.File.OperatorActivated += File_OperatorActivated;
			ModelEditor.Active.File.OperatorDeactivated += File_OperatorDeactivated;
			ModelEditor.Active.File.Cleared += File_Cleared; ;

			ModelEditor.Active.File.ItemTransformed += File_ItemTransformed;

			Active_SelectedChanged();

			SetTransformMode(EditorTransformMode.ParentCoordinates);
			SetEditorOperator(EditorDefaultOperator.RotateSelection);


			ModelEditor.Active.File.AnimationActivated += File_AnimationActivated;
			ModelEditor.Active.File.AnimationDeactivated += File_AnimationDeactivated;
			ModelEditor.Active.SetupAnimateModeChanged += Active_SetupAnimateModeChanged;
			ModelEditor.Active.SelectedChanged += Active_SelectedChanged1;
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
			ModelEditor.Active.Editor.TransformTranslation.GetKeyframeButton2().Enabled = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Translation);
			ModelEditor.Active.Editor.TransformScale.GetKeyframeButton2().Enabled = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Scale);
			ModelEditor.Active.Editor.TransformShear.GetKeyframeButton2().Enabled = (anim == null || bone == null) ? false : anim.DoesBoneHaveSeparatedProperty(bone, KeyframeProperty.Bone_Shear);
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

		private void File_ItemTransformed(IEditorType item, bool translation, bool rotation, bool scale, bool shear) {
			if (translation || rotation || scale || shear) {
				Active_SelectedChanged();
			}
		}

		HashSet<Type>? SelectableTypes { get; set; } = null;

		private void File_OperatorActivated(EditorFile self, Operator op) {
			MainTransformsPanel.Visible = false;
			OperatorPanel.ClearChildren();
			OperatorPanel.Visible = true;
			SelectableTypes = op.SelectableTypes == null ? null : op.SelectableTypes.ToHashSet();
			op.ChangeEditorProperties(OperatorPanel);
		}

		private void File_OperatorDeactivated(EditorFile self, Operator op, bool canceled) {
			MainTransformsPanel.Visible = true;
			OperatorPanel.ClearChildren();
			OperatorPanel.Visible = false;
			SelectableTypes = null;
		}

		public DefaultOperator? DefaultOperator { get; private set; }
		public EditorDefaultOperator DefaultOperatorType { get; private set; }

		public bool InPoseMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.PoseBoneToTarget;
		public bool InWeightsMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ChangeMeshWeights;
		public bool InCreateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.CreateNewBones;

		public bool InRotateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.RotateSelection;
		public bool InTranslateMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.TranslateSelection;
		public bool InScaleMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ScaleSelection;
		public bool InShearMode => !ModelEditor.Active.File.IsOperatorActive && DefaultOperatorType == EditorDefaultOperator.ShearSelection;

		private void SetEditorOperator(EditorDefaultOperator op) {
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

		public EditorTransformMode TransformMode { get; private set; }
		private void SetTransformMode(EditorTransformMode mode) {
			TransformMode = mode;

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
			ModelEditor.Active.File.RotateSelected((float)newValue);
		}
		private void CHANGE_TranslationX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateXSelected((float)newValue);
		}
		private void CHANGE_TranslationY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateYSelected((float)newValue);
		}
		private void CHANGE_ScaleX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleXSelected((float)newValue);
		}
		private void CHANGE_ScaleY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleYSelected((float)newValue);
		}
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

			foreach (var selected in determinations.Selected) {
				var item = selected.DeferTransformationsTo();
				if (item == null) continue;

				if (first) {
					supportRotation = item.CanRotate();
					rotV = item.GetRotation();
					TransformRotation.GetNumSlider(0).SetValueNoUpdate(rotV);

					supportTranslation = item.CanTranslate();
					posX = item.GetTranslationX();
					posY = item.GetTranslationY();
					TransformTranslation.GetNumSlider(0).SetValueNoUpdate(posX);
					TransformTranslation.GetNumSlider(1).SetValueNoUpdate(posY);

					supportScale = item.CanScale();
					scaX = item.GetScaleX();
					scaY = item.GetScaleY();
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

					supportRotation = supportRotation && item.GetRotation() == rotV;
					supportTranslation = supportTranslation && item.GetTranslationX() == posX && item.GetTranslationY() == posY;
					supportScale = supportScale && item.GetScaleX() == scaX && item.GetScaleY() == scaY;
					supportShear = supportShear && item.GetShearX() == sheX && item.GetShearY() == sheY;
				}
			}

		end:
			TransformRotation.EnableSliders = supportRotation;
			TransformRotation.InputDisabled = !supportRotation;
			TransformTranslation.EnableSliders = supportTranslation;
			TransformTranslation.InputDisabled = !supportTranslation;
			TransformScale.EnableSliders = supportScale;
			TransformScale.InputDisabled = !supportScale;
			TransformShear.EnableSliders = supportShear;
			TransformShear.InputDisabled = !supportShear;
		}

		public override void PreRender() {
			base.PreRender();
		}

		private float cameraX = 0;
		private float cameraY = 0;
		private float cameraZoom = 1;

		public float CameraX {
			get => cameraX;
			set {
				Console.WriteLine(value);
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
			if (SelectableTypes != null)
				return !IsTypeProhibitedByOperator(type.GetType());

			if (DefaultOperator != null)
				return DefaultOperator.IsSelectable(type);

			return true;
		}


		protected override void OnThink(FrameState frameState) {
			var activeOp = ModelEditor.Active.File.ActiveOperator;

			if (Hovered) {
				// Hover determination
				HoverGridPos = ScreenToGrid(GetMousePos());

				bool canHoverTest_Bones = (SelectMode & ViewportSelectMode.Bones) == ViewportSelectMode.Bones;
				bool canHoverTest_Images = (SelectMode & ViewportSelectMode.Images) == ViewportSelectMode.Images;
				bool canHoverTest_Meshes = (SelectMode & ViewportSelectMode.Meshes) == ViewportSelectMode.Meshes;

				IEditorType? hovered = null;

				foreach (var model in ModelEditor.Active.File.Models) {
					EditorAttachment? firstAttachment = null;
					foreach (var slot in model.Slots) {
						foreach (var attachment in slot.Attachments) {
							var lastHovered = hovered;

							if (!attachment.GetVisible()) continue;

							bool hovertest = attachment.HoverTest(HoverGridPos) && CanSelect(attachment);

							switch (attachment) {
								case EditorRegionAttachment:
									if (canHoverTest_Images && hovertest) hovered = attachment; break;
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
						if (hovered is MeshVertex) break;
					}

					if (hovered is not MeshVertex && canHoverTest_Bones) {
						foreach (var bone in model.GetAllBones()) {
							if (bone.HoverTest(HoverGridPos) && CanSelect(bone))
								hovered = bone;
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
			activeOp?.Think(ModelEditor.Active, HoverGridPos);
		}
		public Vector2F ScreenToGrid(Vector2F screenPos) {
			Vector2F screenCoordinates = Vector2F.Remap(screenPos, new(0), RenderBounds.Size, new(0, 0), EngineCore.GetWindowSize());
			Vector2F halfScreenSize = EngineCore.GetWindowSize() / 2;
			Vector2F centeredCoordinates = screenCoordinates - halfScreenSize;

			Vector2F withoutCamVars = centeredCoordinates * new Vector2F(RenderBounds.W / widthMultiplied, 1);
			Vector2F accountingForCamVars = (withoutCamVars / CameraZoom) + new Vector2F(CameraX, -CameraY);
			return accountingForCamVars * new Vector2F(1, -1);
		}
		public Vector2F GridToScreen(Vector2F gridPos) {
			gridPos *= new Vector2F(1, -1);
			Vector2F size = EngineCore.GetWindowSize();
			size.W *= RenderBounds.W / widthMultiplied;

			return Vector2F.Remap(
				(gridPos - new Vector2F(CameraX, -CameraY)) * CameraZoom,
				-size / 2, size / 2, new(0), RenderBounds.Size) + new Vector2F(0, RenderBounds.Pos.Y);
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

		public override void MouseClick(FrameState state, Types.MouseButton button) {
			base.MouseClick(state, button);
			ClickPos = GetMousePos();
			ClickedObject = null;
			__dragBlocked = false;

			var doesOperatorAllowClick = DefaultOperator?.GizmoClicked(this, HoveredObject, ClickPos) ?? true;
			if (HoveredObject != null && button == Types.MouseButton.Mouse1) {
				if (doesOperatorAllowClick == false) {
					__dragBlocked = true;
					return;
				}

				ClickedObject = HoveredObject;
				var operatorActive = ModelEditor.Active.File.ActiveOperator != null;
				if (operatorActive) {
					ModelEditor.Active.SelectObject(HoveredObject, state.KeyboardState.ShiftDown);
				}
				else {
					//if(ModelEditor.Active.IsObjectSelected(HoveredObject))
				}
			}

			if (button == Types.MouseButton.Mouse1)
				ModelEditor.Active.File.ActiveOperator?.Clicked(ModelEditor.Active, ClickPos);

			__startedDrag = false;
			__startDraggingOperator = false;
			CanDragObject = button == Types.MouseButton.Mouse1;
			CanDragCamera = button == Types.MouseButton.Mouse2;
		}

		[MemberNotNullWhen(true, nameof(DefaultOperator))]
		public bool CanUseDefaultOperator => DefaultOperator != null && !ModelEditor.Active.File.IsOperatorActive;

		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			base.MouseDrag(self, state, delta);
			if (__dragBlocked) return;

			var pos = GetMousePos();
			if (((pos - ClickPos).Length > __LengthUntilDragStarts || ModelEditor.Active.SelectedObjectsCount > 0) && !__startedDrag) {
				__startedDrag = true;
				__dragStartScreenspace = pos;
				__dragStartGridspace = ScreenToGrid(pos);
				if (CanDragObject) {
					// In some operators we would do something here
					if (CanUseDefaultOperator) {
						__startDraggingOperator = DefaultOperator.GizmoStartDragging(this, pos, ModelEditor.Active.FirstSelectedObject, ClickedObject);
					}
					if (!__startDraggingOperator) {
						if (HoveredObject != null) {
							if (!ModelEditor.Active.IsObjectSelected(HoveredObject)) {
								ModelEditor.Active.File.ActiveOperator?.DragStart(ModelEditor.Active, ClickPos);
								ModelEditor.Active.SelectObject(HoveredObject, state.KeyboardState.ShiftDown);
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
		}

		public override void MouseRelease(Element self, FrameState state, Types.MouseButton button) {
			base.MouseRelease(self, state, button);
			CanDragCamera = false;
			__dragBlocked = false;

			if (button == Types.MouseButton.Mouse1) {
				bool allowSelection = DefaultOperator?.GizmoReleased(this, ClickedObject, GetMousePos()) ?? true;
				ModelEditor.Active.File.ActiveOperator?.DragRelease(ModelEditor.Active, ClickPos);
				if (
					ClickedObject != null
					&& !ModelEditor.Active.IsObjectSelected(ClickedObject)
					&& allowSelection
				) {
					var activeOp = ModelEditor.Active.File.ActiveOperator;
					if (activeOp == null || !activeOp.SelectMultiple) // Kind of a hack, but the editor would
																	  // trigger a second selection otherwise
						ModelEditor.Active.SelectObject(ClickedObject, state.KeyboardState.ShiftDown);
				}
			}
		}

		public override void MouseScroll(Element self, FrameState state, Vector2F delta) {
			CameraZoom = Math.Clamp(CameraZoom + (delta.Y / 5 * CameraZoom), 0.05f, 40);
		}
		Camera3D cam;
		float widthMultiplied;
		public void Draw3DCursor() {
			//Raylib.DrawSphere(new(HoverGridPos.X, HoverGridPos.Y, 0), 2, Color.RED);
		}

		/// <summary>
		/// Draws a bone and then its children based on its posing
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

			var color = selected ? Color.SkyBlue : bone.Hovered ? bone.Color.Adjust(0, -0.3f, 0.3f) : bone.Color.Adjust(0, 0, -0.15f);
			ManagedMemory.Texture boneTex;

			if (ModelEditor.Active.Editor.InWeightsMode
			 && ModelEditor.Active.LastSelectedObject is EditorMeshAttachment meshAttachment
			 && meshAttachment.Weights.Count > 0) {
				// Override the color
				int boneIndex = meshAttachment.Weights.FindIndex(0, (x) => x.Bone == bone);
				if (boneIndex == -1)
					color = new Color(170, 170, 170, 40);
				else
					color = EditorMeshAttachment.BoneWeightListIndexToColor(boneIndex, color.A);
			}


			if (bone.Length > 0) {
				boneTex = Level.Textures.LoadTextureFromFile("models/lengthbonetex.png");
				var innerRing = Level.Textures.LoadTextureFromFile("models/bonering.png");
				var lengthMul = (float)NMath.Remap(bone.Length, 40, 150, 0.38, 1, true) * 6;
				Raylib.DrawTexturePro(innerRing, new(0, 0, innerRing.Width, innerRing.Height), new(wt.X - (lengthMul / 2), wt.Y - (lengthMul / 2), lengthMul, lengthMul), new(0), 0, color);
			}
			else
				boneTex = Level.Textures.LoadTextureFromFile("models/lengthlessbonetex.png");

			bone.GetTexCoords(byHowMuch, out var baseBottom, out var baseTop, out var tipBottom, out var tipTop, out var lengthLimit);

			if (bone.Length > 0) {
				Rlgl.Begin(DrawMode.TRIANGLES);

				Rlgl.Color4ub(color.R, color.G, color.B, color.A);

				Rlgl.SetTexture(((Texture2D)boneTex).Id);
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
					boneTex,
					new(0, 0, boneTex.Width, boneTex.Height),
					new(-size / 2f, -size / 2f, size, size),
					new(0),
					0,
					color
				);
				Rlgl.PopMatrix();
			}
			foreach (var child in bone.Children) {
				DrawBone(child);
			}
		}

		/// <summary>
		/// Draws all models in the working model list
		/// </summary>
		public void DrawModels() {
			foreach (var model in ModelEditor.Active.File.Models) {
				foreach (var slot in model.Slots) {
					slot.GetActiveAttachment()?.Render();
				}
			}

			foreach (var model in ModelEditor.Active.File.Models) {
				foreach (var bone in model.GetAllBones()) {
					DrawBone(bone);
				}
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
		public override void Paint(float width, float height) {
			cam = new Camera3D() {
				Projection = CameraProjection.CAMERA_ORTHOGRAPHIC,
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
			Raylib.BeginMode3D(cam);
			Rlgl.DisableBackfaceCulling();
			Rlgl.DisableDepthMask();

			Checkerboard.Draw();
			Raylib.DrawLine3D(new(-10000, 0, 0), new(10000, 0, 0), Color.Black);
			Raylib.DrawLine3D(new(0, -10000, 0), new(0, 10000, 0), Color.Black);

			if (!(ModelEditor.Active.File.ActiveOperator?.RenderOverride() ?? false)) {
				DrawModels();
				Draw3DCursor();
			}

			Raylib.EndMode3D();
			Surface.ResetViewport();

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
				string font = "Noto Sans", text = $"{selectedTransformable.GetName()}";
				int fontSize = 20;
				Vector2F textSize = Graphics2D.GetTextSize(text, font, fontSize) + new Vector2F(6);
				Graphics2D.SetDrawColor(10, 10, 10, 190);
				Graphics2D.DrawRectangle((width / 2) - (textSize.W / 2), (height - 140) - (textSize.H / 2), textSize.W, textSize.H);
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawText(width / 2, height - 140, text, font, fontSize, Anchor.Center);
			}

			Rlgl.DrawRenderBatchActive();

			Rlgl.EnableDepthMask();
			Rlgl.EnableBackfaceCulling();

			Graphics2D.SetDrawColor(255, 255, 255);

			Graphics2D.DrawText(new(4, height - 32), $"fps:  {EngineCore.FPS}", "Consolas", 12, Anchor.BottomLeft);
			Graphics2D.DrawText(new(4, height - 18), $"gridpos:  {HoverGridPos}", "Consolas", 12, Anchor.BottomLeft);
			Graphics2D.DrawText(new(4, height - 4), $"mousepos: {GetMousePos()}", "Consolas", 12, Anchor.BottomLeft);

			Operator? op = ModelEditor.Active.File.ActiveOperator;
			if (op != null) {
				Graphics2D.DrawText(new(width / 2, 32), $"{op.Name ?? "<NULL>"}", "Noto Sans", 32, Anchor.TopCenter);
			}
		}
	}
}
