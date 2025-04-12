using glTFLoader.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nucleus.ModelEditor
{
	public class ModelEditorSerializationBinder : ISerializationBinder
	{
		private static HashSet<Type> ApprovedBindables;
		static ModelEditorSerializationBinder() {
			ApprovedBindables = [];

			// Build the type whitelist. Has to be done for security reasons
			// Includes all abstract implementors of attachments and timelines.
			// Those types will then include their typename in the JSON so they can
			// be deserialized properly

			foreach (var attachmentType in typeof(EditorAttachment).GetInheritorsOfAbstractType())
				ApprovedBindables.Add(attachmentType);

			foreach (var timelineType in typeof(EditorTimeline).GetInheritorsOfAbstractType())
				ApprovedBindables.Add(timelineType);
		}
		public Type BindToType(string? assemblyName, string typeName) {
			var resolvedTypeName = $"{typeName}, {assemblyName}";

			var type = Type.GetType(resolvedTypeName, true);
			if (!ApprovedBindables.Contains(type))
				throw new JsonSerializationException($"Type is not approved for serialization. Typename: ${resolvedTypeName}");

			return type;
		}

		public void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
			assemblyName = null;
			typeName = serializedType.AssemblyQualifiedName;
		}
	}
	/// <summary>
	/// The editor interface controller; provides an API for almost all editor operations, with equiv. callbacks for various UI systems.
	/// </summary>
	public class EditorFile
	{
		public EditorFile() {
			AnimationActivated += (_, _, _) => {
				Timeline.SetFrame(0);
			};
			AnimationDeactivated += (_, _, _) => {
				Timeline.SetFrame(0);
			};
		}
		public List<EditorModel> Models = [];

		// Camera position and zoom
		public float CameraX = 0;
		public float CameraY = 0;
		public float CameraZoom = 1;

		public TimelineManager Timeline = new();

		public EditorAnimation? ActiveAnimation;

		// ============================================================================================== //
		// Serialization
		// ============================================================================================== //

		public static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings() {
			PreserveReferencesHandling = PreserveReferencesHandling.Objects,
			SerializationBinder = new ModelEditorSerializationBinder(),
			TypeNameHandling = TypeNameHandling.Auto,
		};

		public string Serialize() {
			return JsonConvert.SerializeObject(this, SerializerSettings);
		}

		public void Deserialize(string data) {
			// Clear ourselves of old data (which tells the outliner to clear itself as well)
			Clear();

			JsonConvert.PopulateObject(data, this, SerializerSettings);

			foreach (var model in Models) {
				ModelAdded?.Invoke(this, model);
				model.Images.Scan();
				ModelImagesScanned?.Invoke(this, model);
				foreach (var bone in model.GetAllBones()) {
					BoneAdded?.Invoke(this, model, bone);
					BoneColorChanged?.Invoke(this, bone);
					foreach (var slot in bone.Slots) {
						SlotAdded?.Invoke(this, model, bone, slot);
						foreach (var attachment in slot.Attachments) {
							AttachmentAdded?.Invoke(this, slot, attachment);

							switch (attachment) {
								case EditorMeshAttachment meshAttachment:
									foreach (var weight in meshAttachment.Weights) {
										AssociateBoneToMesh(meshAttachment, weight.Bone);
									}
									break;
							}
						}
					}
				}

				foreach (var skin in model.Skins)
					SkinAdded?.Invoke(this, model, skin);

				foreach (var animation in model.Animations)
					AnimationAdded?.Invoke(this, model, animation);

				if (ActiveAnimation != null)
					SetActiveAnimation(model, ActiveAnimation);
			}

			Loaded?.Invoke(this);
		}

		/// <summary>
		/// Called when the file is cleared.
		/// </summary>
		public event FileClear? Cleared;
		/// <summary>
		/// Called when a file is fully loaded and ready
		/// </summary>
		public event FileClear? Loaded;
		public delegate void FileClear(EditorFile file);


		// ============================================================================================== //
		// Models
		// ============================================================================================== //
		/// <summary>
		/// Called when a model has been added.
		/// </summary>
		public event ModelAddRemove? ModelAdded;
		/// <summary>
		/// Called when a model is about to be removed.
		/// </summary>
		public event ModelAddRemove? ModelRemoved;
		/// <summary>
		/// Called when a model is renamed.
		/// </summary>
		public event ModelRename? ModelRenamed;

		public delegate void ModelAddRemove(EditorFile file, EditorModel model);
		public delegate void ModelRename(EditorFile file, EditorModel model, string oldName, string newName);

		public EditorReturnResult<EditorModel> AddModel(string name) {
			if (Models.FirstOrDefault(x => x.Name == name) != null)
				return new(null, $"The file already contains a model named '{name}'.");

			var model = new EditorModel();
			model.Name = name;
			Models.Add(model);

			model.Root = AddBone(model, null, "root").Result ?? throw new Exception("Wtf?");

			ModelAdded?.Invoke(this, model);
			return model;
		}
		public EditorResult RenameModel(EditorModel model, string newName) {
			if (Models.FirstOrDefault(x => x.Name == newName) != null)
				return new($"The file already contains a model named '{newName}'.");

			var oldName = model.Name;
			model.Name = newName;
			ModelRenamed?.Invoke(this, model, oldName, newName);

			return EditorResult.OK;
		}

		public EditorResult RemoveModel(string modelName) {
			EditorModel? model = Models.FirstOrDefault(x => x.Name == modelName);
			if (model != null) {
				Models.Remove(model);
				ModelRemoved?.Invoke(this, model);

				return new();
			}

			return new("Model was null.");
		}
		public EditorResult RemoveModel(EditorModel model)
			=> RemoveModel(model.Name);

		// ============================================================================================== //
		// Bones
		// ============================================================================================== //

		/// <summary>
		/// Called when a bone has been added to a model.
		/// </summary>
		public event BoneAddRemove? BoneAdded;
		/// <summary>
		/// Called when a bone is about to be removed.
		/// </summary>
		public event BoneAddRemove? BoneRemoved;
		/// <summary>
		/// Called when a bone has been renamed.
		/// </summary>
		public event BoneRename? BoneRenamed;

		public event BoneGeneric? BoneLengthChanged;
		public event BoneGeneric? BoneColorChanged;
		public event BoneGeneric? BoneIconChanged;


		public delegate void BoneAddRemove(EditorFile file, EditorModel model, EditorBone bone);
		public delegate void BoneRename(EditorFile file, EditorBone bone, string oldName, string newName);
		public delegate void BoneGeneric(EditorFile file, EditorBone bone);

		public EditorReturnResult<EditorBone> AddBone(EditorModel model, EditorBone? parent, string? name = null) {
			if (name == null) {
				// please don't have more than 100,000 unnamed bones!
				for (int i = 0; i < 100_000; i++) {
					var newName = $"bone{i + 1}";
					if (!model.TryFindBone(newName, out EditorBone? _)) {
						name = newName;
						break;
					}
				}
			}

			if (model.Root != null && model.TryFindBone(name, out EditorBone? _))
				return new(null, $"The model already contains a bone named '{name}'.");

			EditorBone bone = new EditorBone();
			if (parent != null)
				parent.Children.Add(bone);
			bone.Parent = parent;
			bone.Model = model;
			bone.Name = name;

			model.InvalidateBonesList();

			BoneAdded?.Invoke(this, model, bone);
			return bone;
		}

		public EditorResult SetBoneLength(EditorBone bone, float length) {
			bone.Length = length;
			BoneLengthChanged?.Invoke(this, bone);
			return EditorResult.OK;
		}
		public EditorResult SetBoneColor(EditorBone bone, Color color) {
			bone.Color = color;
			BoneColorChanged?.Invoke(this, bone);
			return EditorResult.OK;
		}
		public EditorResult SetBoneIcon(EditorBone bone, string? icon) {
			bone.Icon = icon;
			BoneIconChanged?.Invoke(this, bone);
			return EditorResult.OK;
		}

		public EditorResult RenameBone(EditorBone bone, string newName) {
			if (bone.Model.TryFindBone(newName, out var _))
				return new($"A bone named '{newName}' already exists.");

			string oldName = bone.Name;
			bone.Name = newName;
			BoneRenamed?.Invoke(this, bone, oldName, newName);
			return EditorResult.OK;
		}
		public EditorResult RenameBone(EditorModel model, string oldName, string newName) {
			if (!model.TryFindBone(oldName, out EditorBone? bone))
				return new($"No bone named '{oldName}' could be found in the model.");

			return RenameBone(bone, newName);
		}

		public EditorResult RemoveBone(EditorModel model, EditorBone? bone) {
			if (bone == null)
				return new("Bone was null.");

			if (model != null) {
				if (bone == model.Root)
					return new("Cannot remove the root bone.");

				var parent = bone.Parent;
				if (parent == null)
					throw new Exception("Parent should NEVER be null here!");

				BoneRemoved?.Invoke(this, model, bone);
				foreach (var child in bone.Children.ToArray()) {
					RemoveBone(model, child);
				}
				parent.Children.Remove(bone);
				model.InvalidateBonesList();

				if (bone.Selected) ModelEditor.Active.UnselectObject(bone);

				return new();
			}

			return new("Model was null.");
		}
		public EditorResult RemoveBone(EditorBone bone)
			=> RemoveBone(bone.Model, bone);
		public EditorResult RemoveBone(EditorModel model, string boneName)
			=> model.TryFindBone(boneName, out EditorBone? bone) ? RemoveBone(model, bone) : new($"Bone '{boneName}' not found.");

		// ============================================================================================== //
		// Slots
		// ============================================================================================== //

		/// <summary>
		/// Called when a slot is added to a bone.
		/// </summary>
		public event SlotAddRemove? SlotAdded;
		/// <summary>
		/// Called when a slot is removed from a bone.
		/// </summary>
		public event SlotAddRemove? SlotRemoved;
		/// <summary>
		/// Called when a slot is renamed.
		/// </summary>
		public event SlotRename? SlotRenamed;

		public delegate void SlotAddRemove(EditorFile file, EditorModel model, EditorBone bone, EditorSlot slot);
		public delegate void SlotRename(EditorFile file, EditorSlot slot, string oldName, string newName);

		public EditorReturnResult<EditorSlot> AddSlot(EditorModel model, EditorBone bone, string slotName) {
			if (model.TryFindSlot(slotName, out var _))
				return new(null, $"The model already contains a slot with the name '{slotName}'.");

			EditorSlot slot = new EditorSlot();
			slot.Name = slotName;
			slot.Bone = bone;
			model.Slots.Add(slot);
			bone.Slots.Add(slot);

			SlotAdded?.Invoke(this, model, bone, slot);

			return slot;
		}
		public EditorReturnResult<EditorSlot> AddSlot(EditorBone bone, string slotName) => AddSlot(bone.Model, bone, slotName);

		public EditorResult RenameSlot(EditorModel model, EditorSlot slot, string newName) {
			if (model.TryFindSlot(newName, out var _))
				return new($"The model already contains a slot with the name '{newName}'.");

			var oldName = slot.Name;
			slot.Name = newName;
			SlotRenamed?.Invoke(this, slot, oldName, newName);

			return EditorResult.OK;
		}
		public EditorResult RenameSlot(EditorSlot slot, string newName) => RenameSlot(slot.Bone.Model, slot, newName);

		public EditorResult RemoveSlot(EditorModel model, EditorSlot slot) {
			SlotRemoved?.Invoke(this, model, slot.Bone, slot);

			model.Slots.Remove(slot);
			slot.Bone.Slots.Remove(slot);

			if (slot.Selected) ModelEditor.Active.UnselectObject(slot);

			return EditorResult.OK;
		}
		public EditorResult RemoveSlot(EditorSlot slot) => RemoveSlot(slot.Bone.Model, slot);

		// ============================================================================================== //
		// Animations
		// ============================================================================================== //

		/// <summary>
		/// Called when an animation is added to a bone.
		/// </summary>
		public event AnimationAddRemove? AnimationAdded;
		/// <summary>
		/// Called when an animation is removed from a bone.
		/// </summary>
		public event AnimationAddRemove? AnimationRemoved;
		/// <summary>
		/// Called when an animation is renamed.
		/// </summary>
		public event AnimationRename? AnimationRenamed;
		/// <summary>
		/// Called when an animation is activated.
		/// </summary>
		public event AnimationGeneric? AnimationActivated;
		/// <summary>
		/// Called when an animation is deactivated
		/// </summary>
		public event AnimationGeneric? AnimationDeactivated;


		public delegate void AnimationAddRemove(EditorFile file, EditorModel model, EditorAnimation animation);
		public delegate void AnimationRename(EditorFile file, EditorAnimation animation, string oldName, string newName);
		public delegate void AnimationGeneric(EditorFile file, EditorModel model, EditorAnimation animation);

		public EditorReturnResult<EditorAnimation> AddAnimation(EditorModel model, string name) {
			if (model.Skins.FirstOrDefault(x => x.Name == name) != null)
				return new(null, $"The model already contains an animation named '{name}'.");

			EditorAnimation animation = new EditorAnimation();
			animation.Name = name;
			animation.Model = model;
			model.Animations.Add(animation);

			AnimationAdded?.Invoke(this, model, animation);

			return animation;
		}
		public EditorResult RenameAnimation(EditorModel model, EditorAnimation animation, string newName) {
			if (model.TryFindSkin(newName, out var _))
				return new($"The model already contains an animation with the name '{newName}'.");

			var oldName = animation.Name;
			animation.Name = newName;
			AnimationRenamed?.Invoke(this, animation, oldName, newName);

			return EditorResult.OK;
		}

		public EditorResult SetActiveAnimation(EditorModel model, EditorAnimation anim) {
			UnsetActiveAnimation(model);

			model.ActiveAnimation = anim;
			ActiveAnimation = anim;
			AnimationActivated?.Invoke(this, model, anim);

			return EditorResult.OK;
		}

		public EditorResult UnsetActiveAnimation(EditorModel model, EditorAnimation? onlyIfThis = null) {
			if (model.ActiveAnimation == null) return EditorResult.OK;
			if (onlyIfThis != null && model.ActiveAnimation != onlyIfThis) return new("Not the same animation!");

			var anim = model.ActiveAnimation;
			model.ActiveAnimation = null;
			ActiveAnimation = null;
			AnimationDeactivated?.Invoke(this, model, anim);

			return EditorResult.OK;
		}

		// ============================================================================================== //
		// Images
		// ============================================================================================== //

		/// <summary>
		/// Called when a model's images have completed scanning.
		/// </summary>
		public event ModelImagesScannedD? ModelImagesScanned;

		public delegate void ModelImagesScannedD(EditorFile file, EditorModel model);

		public EditorResult SetModelImages(EditorModel model, string filepath) {
			model.Images.Filepath = filepath;
			return RescanModelImages(model);
		}

		public EditorResult RescanModelImages(EditorModel model) {
			EditorResult scanResult = model.Images.Scan();

			if (scanResult.Succeeded) {
				ModelImagesScanned?.Invoke(this, model);
			}

			return scanResult;
		}

		// ============================================================================================== //
		// Attachments
		// ============================================================================================== //

		/// <summary>
		/// Called when an attachment is added to a slot.
		/// </summary>
		public event AttachmentAddRemove? AttachmentAdded;
		/// <summary>
		/// Called when an attachment is removed from a slot.
		/// </summary>
		public event AttachmentAddRemove? AttachmentRemoved;
		/// <summary>
		/// Called when an attachment is renamed.
		/// </summary>
		public event AttachmentRename? AttachmentRenamed;

		public delegate void AttachmentAddRemove(EditorFile file, EditorSlot slot, EditorAttachment attachment);
		public delegate void AttachmentRename(EditorFile file, EditorAttachment attachment, string oldName, string newName);

		public EditorReturnResult<T> AddAttachment<T>(EditorSlot slot, string name) where T : EditorAttachment, new() {
			if (slot.TryFindAttachment(name, out var _))
				return new(null, $"An attachment named '{name}' already exists in this slot.");

			T attachment = new T();
			attachment.Slot = slot;
			attachment.Name = name;
			slot.Attachments.Add(attachment);
			AttachmentAdded?.Invoke(this, slot, attachment);

			// Make the attachment the active attachment
			slot.SetActiveAttachment(attachment);

			return attachment;
		}

		public EditorResult RenameAttachment(EditorAttachment attachment, string newName) {
			if (attachment.Slot.TryFindAttachment(newName, out var _))
				return new("An attachment with that name already exists.");

			var oldName = attachment.Name;
			attachment.Name = newName;
			AttachmentRenamed?.Invoke(this, attachment, oldName, newName);

			return EditorResult.OK;
		}

		// ============================================================================================== //
		// Skins
		// ============================================================================================== //

		/// <summary>
		/// Called when a skin is added to a model.
		/// </summary>
		public event SkinGeneric? SkinAdded;
		/// <summary>
		/// Called when a skin is removed from a model.
		/// </summary>
		public event SkinGeneric? SkinRemoved;
		/// <summary>
		/// Called when a model sets its <see cref="EditorModel.ActiveSkin"/>
		/// </summary>
		public event SkinGeneric? SkinActivated;
		/// <summary>
		/// Called when a model sets its <see cref="EditorModel.ActiveSkin"/> to something else (or unsets it entirely), <i>and</i> <see cref="EditorModel.ActiveSkin"/> was equal to this skin
		/// </summary>
		public event SkinGeneric? SkinDeactivated;
		/// <summary>
		/// Called when a skin is renamed.
		/// </summary>
		public event SkinRename? SkinRenamed;

		public delegate void SkinGeneric(EditorFile file, EditorModel model, EditorSkin skin);
		public delegate void SkinRename(EditorFile file, EditorSkin skin, string oldName, string newName);

		public EditorReturnResult<EditorSkin> AddSkin(EditorModel model, string name) {
			if (model.Skins.FirstOrDefault(x => x.Name == name) != null)
				return new(null, $"The model already contains a skin named '{name}'.");

			EditorSkin skin = new EditorSkin();
			skin.Name = name;
			skin.Model = model;
			model.Skins.Add(skin);

			SkinAdded?.Invoke(this, model, skin);

			return skin;
		}
		public EditorResult RenameSkin(EditorModel model, EditorSkin skin, string newName) {
			if (model.TryFindSkin(newName, out var _))
				return new($"The model already contains a slot with the name '{newName}'.");

			var oldName = skin.Name;
			skin.Name = newName;
			SkinRenamed?.Invoke(this, skin, oldName, newName);

			return EditorResult.OK;
		}
		public EditorResult SetActiveSkin(EditorModel model, EditorSkin skin) {
			UnsetActiveSkin(model);

			model.ActiveSkin = skin;
			SkinActivated?.Invoke(this, model, skin);

			return EditorResult.OK;
		}
		public EditorResult UnsetActiveSkin(EditorModel model, EditorSkin? onlyIfThisSkin = null) {
			if (model.ActiveSkin == null) return EditorResult.OK;
			if (onlyIfThisSkin != null && model.ActiveSkin != onlyIfThisSkin) return new("Not the same skin!");

			var skin = model.ActiveSkin;
			model.ActiveSkin = null;
			SkinDeactivated?.Invoke(this, model, skin);

			return EditorResult.OK;
		}

		public EditorResult SetActiveSkin(EditorSkin skin) => SetActiveSkin(skin.Model, skin);
		public EditorResult UnsetActiveSkin(EditorSkin skin) => UnsetActiveSkin(skin.Model, skin);

		public EditorResult RemoveSkin(EditorModel model, EditorSkin skin) {
			UnsetActiveSkin(skin);

			SkinRemoved?.Invoke(this, model, skin);
			model.Skins.Remove(skin);

			return EditorResult.OK;
		}

		// ============================================================================================== //
		// Attachment things
		// ============================================================================================== //

		public void AutoBindVertices(EditorMeshAttachment attachment) {
			var weights = attachment.Weights;
			foreach (var weightData in weights)
				weightData.Clear();

			if (weights.Count <= 0) return;
			if (weights.Count == 1) {
				// We don't need to waste time
				var weight = weights[0];
				foreach (var vertex in attachment.GetVertices())
					attachment.SetVertexWeight(vertex, weight.Bone, 1, false); // skip validation to save time

				return;
			}

			// todo: implement *actual* autobinding

			float sharedWeight = 1f / weights.Count;
			foreach (var weightData in weights) {
				foreach (var vertex in attachment.GetVertices())
					attachment.SetVertexWeight(vertex, weightData.Bone, sharedWeight, false); // skip validation to save time
			}
		}

		public void UpdateVertexPositions(EditorMeshAttachment attachment, List<EditorBone>? onlyTheseBones = null, MeshVertex? onlyThisVertex = null) {
			var weights = attachment.Weights;
			foreach (var weightData in weights) {
				if (onlyTheseBones != null && !onlyTheseBones.Contains(weightData.Bone))
					continue;

				if (onlyThisVertex != null) {
					weightData.SetVertexPos(onlyThisVertex, weightData.Bone.WorldTransform.WorldToLocal(attachment.WorldTransform.LocalToWorld(onlyThisVertex)));
					continue;
				}

				foreach (var vertex in attachment.GetVertices()) {
					weightData.SetVertexPos(vertex, weightData.Bone.WorldTransform.WorldToLocal(attachment.WorldTransform.LocalToWorld(vertex)));
				}
			}
		}

		public void AssociateBoneToMesh(EditorMeshAttachment attachment, EditorBone bone) {
			EditorMeshWeights? weights = attachment.Weights.FirstOrDefault(x => x.Bone == bone);
			if (weights == null) {
				weights = new();
				weights.Bone = bone;
				attachment.Weights.Add(weights);
			}

			BoneRemoved += (file, model, bone) => {
				attachment.Weights.RemoveAll((x) => x.Bone == bone);
			};
		}

		public void DisassociateBoneFromMesh(EditorMeshAttachment attachment, EditorBone bone) {
			attachment.Weights.RemoveAll(x => x.Bone == bone);
		}

		// ============================================================================================== //
		// Selection modification
		// ============================================================================================== //

		public delegate void OnTransformed(IEditorType item, bool translation, bool rotation, bool scale, bool shear);
		public event OnTransformed? ItemTransformed;

		public EditorResult RotateOne(IEditorType item, float rotation, bool localTo = true) {
			if (item == null) return new("Null item.");
			if (!item.CanRotate()) return new("Cannot rotate the item; it forbids it.");
			item.EditRotation(rotation, localTo);
			ItemTransformed?.Invoke(item, false, true, false, false);
			return EditorResult.OK;
		}

		private enum TRANSFORM_MODE
		{
			Rotation = 1,
			Location = 2,
			Scale = 4,
			Shear = 8
		}

		// lots of avoidable repeatable code
		private void DoSelectionTransformation(Func<IEditorType, bool> predicate, Action<IEditorType> onEachObject, TRANSFORM_MODE transform) {
			var selected = ModelEditor.Active.SelectedObjects;
			HashSet<IEditorType> transformed = [];
			foreach (IEditorType obj in selected) {
				var transformable = obj.DeferTransformationsTo();
				if (transformable == null) continue;

				if (predicate(transformable) && !transformed.Contains(transformable)) {
					onEachObject(transformable);
					ItemTransformed?.Invoke(
						obj,
						(transform & TRANSFORM_MODE.Location) == TRANSFORM_MODE.Location,
						(transform & TRANSFORM_MODE.Rotation) == TRANSFORM_MODE.Rotation,
						(transform & TRANSFORM_MODE.Scale) == TRANSFORM_MODE.Scale,
						(transform & TRANSFORM_MODE.Shear) == TRANSFORM_MODE.Shear
					);
					transformed.Add(transformable);
				}
			}
		}

		public EditorResult RotateSelected(float value, bool additive = false) {
			DoSelectionTransformation(
				(editorObj) => editorObj.CanRotate(),
				(editorObj) => editorObj.EditRotation(additive ? editorObj.GetRotation() + value : value),
				TRANSFORM_MODE.Rotation
			);
			return EditorResult.OK;
		}
		public EditorResult MoveSelectedWorldspace(Vector2F worldOffset) {
			DoSelectionTransformation(
				(editorObj) => editorObj.CanTranslate(),
				(editorObj) => editorObj.SetWorldPosition(worldOffset, true),
				TRANSFORM_MODE.Location
			);
			return EditorResult.OK;
		}
		public EditorResult TranslateXSelected(float value, bool additive = false, UserTransformMode transform = UserTransformMode.LocalSpace) {
			DoSelectionTransformation(
				(editorObj) => editorObj.CanTranslate(),
				(editorObj) => editorObj.EditTranslationX(additive ? editorObj.GetTranslationX(transform) + value : value, transform),
				TRANSFORM_MODE.Location
			);
			return EditorResult.OK;
		}
		public EditorResult TranslateYSelected(float value, bool additive = false, UserTransformMode transform = UserTransformMode.LocalSpace) {
			DoSelectionTransformation(
				(editorObj) => editorObj.CanTranslate(),
				(editorObj) => editorObj.EditTranslationY(additive ? editorObj.GetTranslationY(transform) + value : value, transform),
				TRANSFORM_MODE.Location
			);
			return EditorResult.OK;
		}
		public EditorResult ScaleXSelected(float value, bool additive = false) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanScale()) obj.EditScaleX(value);
			return EditorResult.OK;
		}
		public EditorResult ScaleYSelected(float value, bool additive = false) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanScale()) obj.EditScaleY(value);
			return EditorResult.OK;
		}
		public EditorResult ShearXSelected(float value, bool additive = false) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanShear()) obj.EditShearX(value);
			return EditorResult.OK;
		}
		public EditorResult ShearYSelected(float value, bool additive = false) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanShear()) obj.EditShearY(value);
			return EditorResult.OK;
		}

		// ============================================================================================== //
		// General file operations
		// ============================================================================================== //

		public void Clear() {
			DeactivateOperator(true);
			Models.Clear();
			Cleared?.Invoke(this);
		}

		public void NewFile() {
			Clear();
			var mdl = AddModel("model");
		}

		public delegate void OnEditorItemVischange(EditorFile self, IEditorType editorItem, bool visible);
		public event OnEditorItemVischange? EditorItemHidden;
		public event OnEditorItemVischange? EditorItemShown;
		public event OnEditorItemVischange? EditorItemVisibilityChanged;

		public void ShowEditorItem(IEditorType item) {
			item.Hidden = false;
			item.OnShown();
			EditorItemShown?.Invoke(this, item, true);
			EditorItemVisibilityChanged?.Invoke(this, item, true);
		}

		public void HideEditorItem(IEditorType item) {
			item.Hidden = true;
			item.OnHidden();
			EditorItemHidden?.Invoke(this, item, false);
			EditorItemVisibilityChanged?.Invoke(this, item, false);
		}

		public void ToggleEditorItemVisibility(IEditorType item) {
			if (!item.Hidden) HideEditorItem(item);
			else ShowEditorItem(item);
		}


		// ============================================================================================== //
		// Custom operators
		// ============================================================================================== //

		public delegate void OnOperatorActivated(EditorFile self, Operator op);
		public delegate void OnOperatorDeactivated(EditorFile self, Operator op, bool canceled);

		public event OnOperatorActivated? OperatorActivated;
		public event OnOperatorDeactivated? OperatorDeactivating;
		public event OnOperatorDeactivated? OperatorDeactivated;
		[JsonIgnore] public Operator? ActiveOperator { get; private set; }
		[JsonIgnore] public bool IsOperatorActive => ActiveOperator != null;
		public void ActivateOperator(Operator op) {
			if (ActiveOperator != null) DeactivateOperator(true);

			if (!op.CanActivate(out string? reason)) {
				Logs.Warn($"Failed to activate operator {op.GetType().Name}: {reason ?? "No reason provided."}");
				return;
			}

			ActiveOperator = op;
			op.UIDeterminations = ModelEditor.Active.GetDeterminations();
			op.CallActivateSubscriptions(this);
			OperatorActivated?.Invoke(this, op);
		}
		public void DeactivateOperator(bool canceled) {
			if (ActiveOperator == null) return;

			Operator op = ActiveOperator;
			OperatorDeactivating?.Invoke(this, op, canceled);
			ActiveOperator.CallDeactivateSubscriptions(this, canceled);
			ActiveOperator = null;
			OperatorDeactivated?.Invoke(this, op, canceled);
		}

		public T InstantiateOperator<T>() where T : Operator, new() {
			DeactivateOperator(true);

			T op = new();
			ActivateOperator(op);
			return op;
		}

		internal void ConvertAttachmentTo<T>(EditorAttachment attachment) where T : EditorAttachment {
			bool selected = attachment.Selected;
			if (selected)
				ModelEditor.Active.UnselectObject(attachment);
			EditorAttachment? newAttachment = null;
			switch (attachment) {
				case EditorRegionAttachment regionFrom:
					switch (typeof(T).Name) {
						case "EditorMeshAttachment":
							EditorMeshAttachment meshTo = new EditorMeshAttachment();
							meshTo.Slot = attachment.Slot;
							newAttachment = meshTo;

							meshTo.Path = regionFrom.Path;
							meshTo.Position = regionFrom.Position;
							meshTo.Rotation = regionFrom.Rotation;
							meshTo.Scale = regionFrom.Scale;

							var quadpoints = regionFrom.QuadPoints(localized: false);

							meshTo.ShapeEdges.Add(MeshVertex.FromVector(quadpoints.TL, meshTo));
							meshTo.ShapeEdges.Add(MeshVertex.FromVector(quadpoints.TR, meshTo));
							meshTo.ShapeEdges.Add(MeshVertex.FromVector(quadpoints.BR, meshTo));
							meshTo.ShapeEdges.Add(MeshVertex.FromVector(quadpoints.BL, meshTo));

							break;
					}
					break;
			}

			if (newAttachment == null)
				throw new NotImplementedException($"No attachment conversion exists from {attachment.GetType().Name} -> {typeof(T).Name}.");

			// copy mutual properties
			newAttachment.Slot = attachment.Slot;
			newAttachment.Name = attachment.Name;

			List<EditorAttachment> attachments = attachment.Slot.Attachments;
			var index = attachments.IndexOf(attachment);

			if (index == -1)
				throw new InvalidOperationException("Attachment wasn't a part of the slots attachments, yet was assigned to the slot...?");

			attachments[index] = newAttachment;

			AttachmentRemoved?.Invoke(this, attachment.Slot, attachment);
			AttachmentAdded?.Invoke(this, newAttachment.Slot, newAttachment);
			if (selected)
				ModelEditor.Active.SelectObject(newAttachment);
		}

		public delegate void SeparatePropertyDelegate(EditorBone bone, KeyframeProperty property, bool separated);
		public event SeparatePropertyDelegate? PropertySeparatedOrCombined;
		public void SetDoesBoneHaveSeparatedProperty(EditorBone editorBone, KeyframeProperty property, bool separated) {
			if (ActiveAnimation == null) return;

			if (separated) {
				// Find the root timeline if it exists
				var combinedTimeline = ActiveAnimation.SearchTimelineByProperty(editorBone, property, -1, false);
				if(combinedTimeline != null && combinedTimeline is CurveTimeline2 ct2) {
					SeparateTimelines(ct2);
				}
			}
			else {
				var x = ActiveAnimation.SearchTimelineByProperty(editorBone, property, 0, false);
				var y = ActiveAnimation.SearchTimelineByProperty(editorBone, property, 1, false);

				if(x is CurveTimeline1 ct1x && y is CurveTimeline1 ct1y) {
					CombineTimelines(ct1x, ct1y);
				}
			}

			ActiveAnimation.SetDoesBoneHaveSeparatedProperty(editorBone, property, separated);
			PropertySeparatedOrCombined?.Invoke(editorBone, property, separated);
		}

		public delegate void OnTimelineDelegate(EditorAnimation animation, EditorTimeline timeline);

		public delegate void OnTimelineSeparated(EditorAnimation animation, EditorTimeline timelineOriginal, EditorTimeline timelineX, EditorTimeline timelineY);
		public delegate void OnTimelineCombined(EditorAnimation animation, EditorTimeline timelineX, EditorTimeline timelineY, EditorTimeline timelineOriginal);

		public delegate void OnKeyframeCreateDelegate(EditorAnimation animation, EditorTimeline timeline, double frame);
		public delegate void OnKeyframeDelegateDeltatime(EditorAnimation animation, EditorTimeline timeline, double frameFrom, double frameTo);
		public delegate void OnKeyframeRemoveDelegate(EditorAnimation animation, EditorTimeline timeline, double frame);

		public event OnTimelineDelegate? TimelineCreated;
		public event OnTimelineDelegate? TimelineRemoved;
		public event OnTimelineSeparated? TimelineSeparated;
		public event OnTimelineCombined? TimelineCombined;

		public event OnKeyframeCreateDelegate? KeyframeCreated;
		public event OnKeyframeDelegateDeltatime? KeyframeMoved;
		public event OnKeyframeRemoveDelegate? KeyframeRemoved;

		public void InsertKeyframe(IEditorType? type, KeyframeProperty property, int arrayIndex = -1) { 
			if (type == null) return;
			if (ActiveAnimation == null) return;

			bool created = false;
			var timeline = ActiveAnimation?.SearchTimelineByProperty(type, property, out created, arrayIndex, true);

			// See if the timeline implements IKeyframeQueryable<T> (for InsertKeyframe)
			// and IProperty<T> (for getting the active value out of the timeline-attached object).
			// I don't like the interface hell here, but it works, and is the best way I can think of. And it only has to happen here,
			// everything else ultimately uses the InsertKeyframe API without a care in the world about the interface/target type.

			// Keyframe float types
			if (timeline is IKeyframeQueryable<float> kqf && timeline is IProperty<float> kpf)
				kqf.InsertKeyframe(Timeline.Frame, kpf.GetValue());

			// Keyframe vector2f types
			else if (timeline is IKeyframeQueryable<Vector2F> kqv2 && timeline is IProperty<Vector2F> kpv2)
				kqv2.InsertKeyframe(Timeline.Frame, kpv2.GetValue());

			// Keyframe Attachment types
			else if (timeline is IKeyframeQueryable<EditorAttachment> kqa && timeline is IProperty<EditorAttachment> kpa)
				kqa.InsertKeyframe(Timeline.Frame, kpa.GetValue());

			else if (timeline is IKeyframeQueryable<Color> kqc && timeline is IProperty<Color> kpc)
				kqc.InsertKeyframe(Timeline.Frame, kpc.GetValue());

			// Something went horribly wrong (probably I forgot to implement a type here)
			else {
				Debug.Assert(false, "Can't find a way to keyframe", $"Type: {type}\nProperty:{property}\nArray Index:{arrayIndex}\nTimeline:{timeline}\nActive Animation:{ActiveAnimation}");
				return;
			}

			Debug.Assert(ActiveAnimation != null);

			if (created) {
				TimelineCreated?.Invoke(ActiveAnimation, timeline);
			}

			KeyframeCreated?.Invoke(ActiveAnimation, timeline, Timeline.Frame);
		}

		public void CombineTimelines(CurveTimeline1 x, CurveTimeline1 y) {
			var activeAnimation = ActiveAnimation;
			Debug.Assert(activeAnimation != null);

			CurveTimeline2? ct2 = null;
			switch (x) {
				case TranslateXTimeline translateX:
					if(y is not TranslateYTimeline translateY) throw new Exception("Mismatched X and Y types");
					Debug.Assert(translateX.Bone == translateY.Bone);

					TranslateTimeline translate = new() { Bone = translateX.Bone, CurveX = x.Curve, CurveY = y.Curve };
					activeAnimation.Timelines.Add(translate);
					TimelineCreated?.Invoke(activeAnimation, translate);
					ct2 = translate;
					break;
				case ScaleXTimeline scaleX:
					if (y is not ScaleYTimeline scaleY) throw new Exception("Mismatched X and Y types");
					Debug.Assert(scaleX.Bone == scaleY.Bone);

					ScaleTimeline scale = new() { Bone = scaleX.Bone, CurveX = x.Curve, CurveY = y.Curve };
					activeAnimation.Timelines.Add(scale);
					TimelineCreated?.Invoke(activeAnimation, scale);
					ct2 = scale;
					break;
				case ShearXTimeline shearX:
					if (y is not TranslateYTimeline shearY) throw new Exception("Mismatched X and Y types");
					Debug.Assert(shearX.Bone == shearY.Bone);

					TranslateTimeline shear = new() { Bone = shearX.Bone, CurveX = x.Curve, CurveY = y.Curve };
					activeAnimation.Timelines.Add(shear);
					TimelineCreated?.Invoke(activeAnimation, shear);
					ct2 = shear;
					break;

				default:
					throw new Exception("Cannot combine those timeline types");
			}

			// TODO: Evaluate both curves to determine what's missing in each curve, then create a dummy keyframe

			Debug.Assert(activeAnimation.Timelines.Remove(x));
			TimelineRemoved?.Invoke(activeAnimation, x);
			Debug.Assert(activeAnimation.Timelines.Remove(y));
			TimelineRemoved?.Invoke(activeAnimation, y);

			TimelineCombined?.Invoke(activeAnimation, x, y, ct2);
		}

		public void SeparateTimelines(CurveTimeline2 timeline) {
			var activeAnimation = ActiveAnimation;
			Debug.Assert(activeAnimation != null);

			switch (timeline) {
				case TranslateTimeline translate:
					TranslateXTimeline xTimeline = new() { Bone = translate.Bone, Curve = translate.CurveX };
					TranslateYTimeline yTimeline = new() { Bone = translate.Bone, Curve = translate.CurveX };

					activeAnimation.Timelines.Add(xTimeline);
					TimelineCreated?.Invoke(activeAnimation, xTimeline);

					activeAnimation.Timelines.Add(yTimeline);
					TimelineCreated?.Invoke(activeAnimation, yTimeline);

					Debug.Assert(activeAnimation.Timelines.Remove(translate));

					TimelineRemoved?.Invoke(activeAnimation, translate);
					TimelineSeparated?.Invoke(activeAnimation, translate, xTimeline, yTimeline);
					break;
				case ScaleTimeline scale:
					ScaleXTimeline xScTimeline = new() { Bone = scale.Bone, Curve = scale.CurveX };
					ScaleYTimeline yScTimeline = new() { Bone = scale.Bone, Curve = scale.CurveX };

					activeAnimation.Timelines.Add(xScTimeline);
					TimelineCreated?.Invoke(activeAnimation, xScTimeline);

					activeAnimation.Timelines.Add(yScTimeline);
					TimelineCreated?.Invoke(activeAnimation, yScTimeline);

					Debug.Assert(activeAnimation.Timelines.Remove(scale));

					TimelineRemoved?.Invoke(activeAnimation, scale);
					TimelineSeparated?.Invoke(activeAnimation, scale, xScTimeline, yScTimeline);
					break;
				case ShearTimeline shear:
					ShearXTimeline xShTimeline = new() { Bone = shear.Bone, Curve = shear.CurveX };
					ShearYTimeline yShTimeline = new() { Bone = shear.Bone, Curve = shear.CurveX };

					activeAnimation.Timelines.Add(xShTimeline);
					TimelineCreated?.Invoke(activeAnimation, xShTimeline);

					activeAnimation.Timelines.Add(yShTimeline);
					TimelineCreated?.Invoke(activeAnimation, yShTimeline);

					Debug.Assert(activeAnimation.Timelines.Remove(shear));

					TimelineRemoved?.Invoke(activeAnimation, shear);
					TimelineSeparated?.Invoke(activeAnimation, shear, xShTimeline, yShTimeline);
					break;

				default:
					throw new Exception("Cannot split this timeline type");
			}
		}
	}
}
