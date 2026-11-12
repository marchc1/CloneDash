using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Nucleus.Common.Types;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.Util;
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

			foreach (var constraintType in typeof(EditorConstraint).GetInheritorsOfAbstractType())
				ApprovedBindables.Add(constraintType);
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
	public partial class EditorFile
	{
		public EditorFile() {
			Timeline.FrameChanged += (_, _) => edited.Clear();
			AnimationActivated += (_, model, _) => {
				edited.Clear();
				model.ResetToSetupPose();
				Timeline.SetFrame(0);
				ModelEditor.Active?.QueueAnimationUpdate();
			};
			AnimationDeactivated += (_, model, _) => {
				edited.Clear();
				model.ResetToSetupPose();
				Timeline.SetFrame(0);
				ModelEditor.Active?.QueueAnimationUpdate();
			};
		}
		/// <summary>
		/// Bump when the .nm4src layout changes, and add a step to <see cref="Migrate(int)"/>
		/// </summary>
		public const int FORMAT_VERSION = 1;
		public int FormatVersion = FORMAT_VERSION;

		public List<EditorModel> Models = [];
		[JsonIgnore] public IEnumerable<EditorModel> ModelsBackToFront => Enumerable.Reverse(Models);
		public ExportSettings Export { get; set; } = new();
		[JsonIgnore] public string? DiskPath { get; set; }
		/// <summary>
		/// Relative images paths start here
		/// </summary>
		[JsonIgnore] public string? ProjectFolder => DiskPath == null ? null : Path.GetDirectoryName(DiskPath);

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
			Converters = new List<JsonConverter>() { new Vector2FJsonConverter() },
			TypeNameHandling = TypeNameHandling.Auto,
			MaxDepth = 1024
		};

		public string Serialize() {
			return JsonConvert.SerializeObject(this, SerializerSettings);
		}

		public void Deserialize(string data, string? diskPath = null) {
			// Clear ourselves of old data (which tells the outliner to clear itself as well)
			Clear();
			DiskPath = diskPath;

			// files from before versioning have no field
			FormatVersion = 0;
			JsonConvert.PopulateObject(data, this, SerializerSettings);
			if (FormatVersion > FORMAT_VERSION)
				Logs.Warn($"This file was saved by a newer editor (format {FormatVersion}, this editor reads up to {FORMAT_VERSION}). Some data may be lost.");
			Migrate(FormatVersion);
			FormatVersion = FORMAT_VERSION;

			foreach (var model in Models) {
				ModelAdded?.Invoke(this, model);
				model.Images.Scan(ProjectFolder);
				ModelImagesScanned?.Invoke(this, model);
				foreach (var bone in model.GetAllBones()) {
					BoneAdded?.Invoke(this, model, bone);
					BoneColorChanged?.Invoke(this, bone);
					foreach (var slot in bone.Slots) {
						SlotAdded?.Invoke(this, model, bone, slot);
						foreach (var attachment in slot.Attachments) {
							AttachmentAdded?.Invoke(this, slot, attachment);
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

		// one step per version
		private void Migrate(int from) {
			if (from < 1) {
				// 0: curve handles were 0-1 fractions of the segment
				foreach (var animation in Models.SelectMany(x => x.Animations)) {
					foreach (var timeline in animation.Timelines) {
						foreach (var curve in timeline.GetCurves().OfType<FCurve<float>>())
							LegacyCurves.ConvertNormalizedHandles(curve, timeline is RotationTimeline);
					}
				}
			}
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
		public event Action<EditorFile>? ModelsReordered;

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
			int index = Models.Count - 1;
			Actions.Push($"Create {name}", () => RestoreModel(model, index), () => RemoveModel(model));
			return model;
		}

		public void AppendModel(EditorModel model) {
			if (Models.Contains(model)) return;

			// Rename the model if it would conflict with another model in the file.
			if (Models.FirstOrDefault(x => x.Name == model.Name) != null) {
				for (int i = 2; i < 10000; i++) {
					var newName = $"{model.Name} ({i})";
					if (Models.FirstOrDefault(x => x.Name == newName) != null)
						continue;

					model.Name = newName;
				}
			}

			Models.Add(model);
			AnnounceModel(model);
			int index = Models.Count - 1;
			Actions.Push($"Import {model.Name}", () => RestoreModel(model, index), () => RemoveModel(model));
		}


		public EditorResult RenameModel(EditorModel model, string newName) {
			if (Models.FirstOrDefault(x => x.Name == newName) != null)
				return new($"The file already contains a model named '{newName}'.");

			var oldName = model.Name;
			model.Name = newName;
			ModelRenamed?.Invoke(this, model, oldName, newName);
			Actions.Push(("rename", model), $"Rename {oldName}",
				() => RenameModel(model, newName), () => RenameModel(model, oldName));

			return EditorResult.OK;
		}

		private void MoveModel(EditorModel model, int index) {
			Models.Remove(model);
			Models.Insert(Math.Min(index, Models.Count), model);
			ModelsReordered?.Invoke(this);
		}

		public EditorResult RemoveModel(string modelName) {
			EditorModel? model = Models.FirstOrDefault(x => x.Name == modelName);
			return model == null ? new("Model was null.") : RemoveModel(model);
		}
		public EditorResult RemoveModel(EditorModel model) {
			int index = Models.IndexOf(model);
			if (index == -1) return new("Model isn't in this file.");

			Models.RemoveAt(index);
			ModelRemoved?.Invoke(this, model);
			Actions.Push($"Delete {model.Name}", () => RemoveModel(model), () => RestoreModel(model, index));
			return new();
		}

		// ============================================================================================== //
		// Undo helpers
		// ============================================================================================== //

		private static ActionStack Actions => ModelEditor.Active.Actions;

		public void SetProperty<T>(object owner, string property, T value, Func<T> get, Action<T> set) {
			var old = get();
			if (EqualityComparer<T>.Default.Equals(old, value)) return;
			set(value);
			var name = owner is IEditorType item ? $"Change {item.GetName()} {property}" : $"Change {property}";
			Actions.Push((property, owner), name, () => set(value), () => set(old));
		}

		private void RestoreModel(EditorModel model, int index) {
			Models.Insert(Math.Min(index, Models.Count), model);
			AnnounceModel(model);
			ModelsReordered?.Invoke(this);
		}

		// Same events Deserialize sends for a model
		private void AnnounceModel(EditorModel model) {
			ModelAdded?.Invoke(this, model);
			ModelImagesScanned?.Invoke(this, model);
			AnnounceBone(model, model.Root);
			foreach (var skin in model.Skins)
				SkinAdded?.Invoke(this, model, skin);
			foreach (var animation in model.Animations)
				AnimationAdded?.Invoke(this, model, animation);
		}

		private void AnnounceBone(EditorModel model, EditorBone bone) {
			BoneAdded?.Invoke(this, model, bone);
			BoneColorChanged?.Invoke(this, bone);
			foreach (var slot in bone.Slots)
				AnnounceSlot(model, slot);
			foreach (var child in bone.Children)
				AnnounceBone(model, child);
		}

		private void AnnounceSlot(EditorModel model, EditorSlot slot) {
			SlotAdded?.Invoke(this, model, slot.Bone, slot);
			foreach (var attachment in slot.Attachments)
				AttachmentAdded?.Invoke(this, slot, attachment);
		}

		private record WeightsBackup(EditorVertexAttachment Attachment, EditorWeights[] Weights);

		// Removing a bone strips its weights from every mesh.
		private static List<WeightsBackup> BackupWeights(EditorModel model) {
			List<WeightsBackup> backup = [];
			foreach (var slot in model.Slots) {
				foreach (var attachment in slot.Attachments) {
					if (attachment is EditorVertexAttachment vertexAttachment)
						backup.Add(new WeightsBackup(vertexAttachment, vertexAttachment.Weights.ToArray()));
				}
			}
			return backup;
		}

		private static void RestoreWeights(List<WeightsBackup> backup) {
			foreach (var item in backup) {
				item.Attachment.Weights.Clear();
				item.Attachment.Weights.AddRange(item.Weights);
			}
		}

		private static bool IsUnder(EditorBone bone, EditorBone ancestor) {
			for (EditorBone? current = bone; current != null; current = current.Parent) {
				if (current == ancestor) return true;
			}
			return false;
		}

		private void RestoreBone(EditorModel model, EditorBone bone, EditorBone parent, int index, List<(EditorSlot Slot, int Index)> slots, List<WeightsBackup> weights) {
			parent.Children.Insert(Math.Min(index, parent.Children.Count), bone);
			bone.Parent = parent;
			foreach (var item in slots.OrderBy(x => x.Index))
				model.Slots.Insert(Math.Min(item.Index, model.Slots.Count), item.Slot);
			model.InvalidateBonesList();
			RestoreWeights(weights);
			AnnounceBone(model, bone);
		}

		private void RestoreSlot(EditorModel model, EditorSlot slot, int modelIndex, int boneIndex) {
			model.Slots.Insert(Math.Min(modelIndex, model.Slots.Count), slot);
			slot.Bone.Slots.Insert(Math.Min(boneIndex, slot.Bone.Slots.Count), slot);
			AnnounceSlot(model, slot);
		}

		private void UnselectTree(IEditorType item) {
			if (item.Selected)
				ModelEditor.Active.UnselectObject(item);
			switch (item) {
				case EditorBone bone:
					foreach (var slot in bone.Slots)
						UnselectTree(slot);
					break;
				case EditorSlot slot:
					foreach (var attachment in slot.Attachments)
						UnselectTree(attachment);
					break;
			}
		}

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
			if (parent != null) {
				int index = parent.Children.Count - 1;
				Actions.Push($"Create {name}",
					() => RestoreBone(model, bone, parent, index, [], []), () => RemoveBone(model, bone));
			}
			return bone;
		}

		public EditorResult SetBoneLength(EditorBone bone, float length) {
			var old = bone.Length;
			Actions.Push(("length", bone), $"Change {bone.Name} length",
				() => SetBoneLength(bone, length), () => SetBoneLength(bone, old));
			bone.Length = length;
			BoneLengthChanged?.Invoke(this, bone);
			return EditorResult.OK;
		}
		public EditorResult SetBoneColor(EditorBone bone, Color color) {
			var old = bone.Color;
			Actions.Push(("color", bone), $"Change {bone.Name} color",
				() => SetBoneColor(bone, color), () => SetBoneColor(bone, old));
			bone.Color = color;
			BoneColorChanged?.Invoke(this, bone);
			return EditorResult.OK;
		}
		public EditorResult SetBoneIcon(EditorBone bone, string? icon) {
			var old = bone.Icon;
			Actions.Push(("icon", bone), $"Change {bone.Name} icon",
				() => SetBoneIcon(bone, icon), () => SetBoneIcon(bone, old));
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
			Actions.Push(("rename", bone), $"Rename {oldName}",
				() => RenameBone(bone, newName), () => RenameBone(bone, oldName));
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

				int index = parent.Children.IndexOf(bone);
				var weights = BackupWeights(model);
				List<(EditorSlot Slot, int Index)> slots = [];
				for (int i = 0; i < model.Slots.Count; i++) {
					if (IsUnder(model.Slots[i].Bone, bone))
						slots.Add((model.Slots[i], i));
				}

				RemoveBoneTree(model, bone);
				model.InvalidateBonesList();

				Actions.Push($"Delete {bone.Name}",
					() => RemoveBone(model, bone), () => RestoreBone(model, bone, parent, index, slots, weights));
				return new();
			}

			return new("Model was null.");
		}

		// subtree is left intact for undo
		private void RemoveBoneTree(EditorModel model, EditorBone bone, bool detach = true) {
			UnselectTree(bone);
			foreach (var slot in bone.Slots) {
				SlotRemoved?.Invoke(this, model, bone, slot);
				model.Slots.Remove(slot);
			}

			foreach (var modelSlot in model.Slots) {
				foreach (var attachment in modelSlot.Attachments) {
					if (attachment is EditorVertexAttachment vertexAttachment)
						vertexAttachment.Weights.RemoveAll(x => x.Bone == bone);
				}
			}
			BoneRemoved?.Invoke(this, model, bone);
			foreach (var child in bone.Children.ToArray())
				RemoveBoneTree(model, child, false);
			if (detach)
				bone.Parent?.Children.Remove(bone);
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
			int modelIndex = model.Slots.Count - 1;
			int boneIndex = bone.Slots.Count - 1;
			Actions.Push($"Create {slotName}",
				() => RestoreSlot(model, slot, modelIndex, boneIndex), () => RemoveSlot(model, slot));

			return slot;
		}
		public EditorReturnResult<EditorSlot> AddSlot(EditorBone bone, string slotName) => AddSlot(bone.Model, bone, slotName);

		public EditorResult RenameSlot(EditorModel model, EditorSlot slot, string newName) {
			if (model.TryFindSlot(newName, out var _))
				return new($"The model already contains a slot with the name '{newName}'.");

			var oldName = slot.Name;
			slot.Name = newName;
			SlotRenamed?.Invoke(this, slot, oldName, newName);
			Actions.Push(("rename", slot), $"Rename {oldName}",
				() => RenameSlot(model, slot, newName), () => RenameSlot(model, slot, oldName));

			return EditorResult.OK;
		}
		public EditorResult RenameSlot(EditorSlot slot, string newName) => RenameSlot(slot.Bone.Model, slot, newName);

		public EditorResult RemoveSlot(EditorModel model, EditorSlot slot) {
			int modelIndex = model.Slots.IndexOf(slot);
			int boneIndex = slot.Bone.Slots.IndexOf(slot);
			UnselectTree(slot);
			SlotRemoved?.Invoke(this, model, slot.Bone, slot);

			model.Slots.Remove(slot);
			slot.Bone.Slots.Remove(slot);

			Actions.Push($"Delete {slot.Name}",
				() => RemoveSlot(model, slot), () => RestoreSlot(model, slot, modelIndex, boneIndex));
			return EditorResult.OK;
		}
		public EditorResult RemoveSlot(EditorSlot slot) => RemoveSlot(slot.Bone.Model, slot);

		public event ModelGeneric? DrawOrderChanged;
		public delegate void ModelGeneric(EditorFile file, EditorModel model);

		public EditorResult SetSlotDrawOrder(EditorSlot slot, int index) {
			var model = slot.Bone.Model;

			// animate mode changes the pose, not setup
			if (ModelEditor.Active.AnimationMode && ActiveAnimation != null) {
				var order = model.DrawOrder.ToList();
				int from = order.IndexOf(slot);
				if (from < 0) return new("Slot isn't in its model.");
				index = Math.Clamp(index, 0, order.Count - 1);
				if (index == from) return EditorResult.OK;
				order.RemoveAt(from);
				order.Insert(index, slot);
				model.AnimatedDrawOrder = order;
				DrawOrderChanged?.Invoke(this, model);
				PoseEdited(model, KeyframeProperty.Model_DrawOrder);
				return EditorResult.OK;
			}

			int old = model.Slots.IndexOf(slot);
			if (old < 0) return new("Slot isn't in its model.");
			index = Math.Clamp(index, 0, model.Slots.Count - 1);
			if (index == old) return EditorResult.OK;

			model.Slots.RemoveAt(old);
			model.Slots.Insert(index, slot);
			DrawOrderChanged?.Invoke(this, model);
			Actions.Push($"Draw order {slot.Name}", () => SetSlotDrawOrder(slot, index), () => SetSlotDrawOrder(slot, old));
			return EditorResult.OK;
		}

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
			if (model.Animations.Any(x => x.Name == name))
				return new(null, $"The model already contains an animation named '{name}'.");

			EditorAnimation animation = new EditorAnimation();
			animation.Name = name;
			animation.Model = model;
			int index = model.Animations.Count;
			RestoreAnimation(model, animation, index);
			Actions.Push($"Create {name}",
				() => RestoreAnimation(model, animation, index), () => TakeAnimation(model, animation));

			return animation;
		}
		public EditorResult RemoveAnimation(EditorModel model, EditorAnimation animation) {
			int index = model.Animations.IndexOf(animation);
			if (index < 0) return new("The animation isn't in this model.");
			bool wasActive = model.ActiveAnimation == animation;

			TakeAnimation(model, animation);
			Actions.Push($"Delete {animation.Name}", () => TakeAnimation(model, animation), () => {
				RestoreAnimation(model, animation, index);
				if (wasActive)
					SetActiveAnimation(model, animation);
			});
			return EditorResult.OK;
		}
		private void RestoreAnimation(EditorModel model, EditorAnimation animation, int index) {
			model.Animations.Insert(Math.Min(index, model.Animations.Count), animation);
			AnimationAdded?.Invoke(this, model, animation);
		}
		private void TakeAnimation(EditorModel model, EditorAnimation animation) {
			UnsetActiveAnimation(model, animation);
			if (animation.Selected)
				ModelEditor.Active.UnselectObject(animation);
			model.Animations.Remove(animation);
			AnimationRemoved?.Invoke(this, model, animation);
		}
		public EditorResult RenameAnimation(EditorModel model, EditorAnimation animation, string newName) {
			if (newName == animation.Name) return EditorResult.OK;
			if (model.Animations.Any(x => x.Name == newName))
				return new($"The model already contains an animation with the name '{newName}'.");

			var oldName = animation.Name;
			animation.Name = newName;
			AnimationRenamed?.Invoke(this, animation, oldName, newName);
			Actions.Push(("rename", animation), $"Rename {oldName}",
				() => RenameAnimation(model, animation, newName), () => RenameAnimation(model, animation, oldName));

			return EditorResult.OK;
		}

		public event Action<EditorModel>? AnimationFoldersChanged;

		/// <summary>
		/// Every animation folder path, made by hand or from animation names
		/// </summary>
		public HashSet<string> AnimationFolderPaths(EditorModel model) {
			HashSet<string> paths = [];
			foreach (var folder in model.AnimationFolders)
				Add(folder, paths);
			foreach (var animation in model.Animations)
				Add(AnimationFolder.Parent(animation.Name), paths);
			return paths;
		}

		private static void Add(string path, HashSet<string> paths) {
			for (; path.Length > 0; path = AnimationFolder.Parent(path)) {
				if (!paths.Add(path)) break;
			}
		}

		public EditorResult AddAnimationFolder(EditorModel model, string path) {
			if (string.IsNullOrWhiteSpace(AnimationFolder.Leaf(path))) return new("The folder needs a name.");
			if (AnimationFolderPaths(model).Contains(path)) return new($"There's already a folder named '{path}'.");
			Action add = () => {
				model.AnimationFolders.Add(path);
				AnimationFoldersChanged?.Invoke(model);
			};
			Action remove = () => {
				model.AnimationFolders.Remove(path);
				AnimationFoldersChanged?.Invoke(model);
			};
			add();
			Actions.Push($"Create folder {path}", add, remove);
			return EditorResult.OK;
		}

		public EditorResult RenameAnimationFolder(EditorModel model, string path, string newName) {
			if (string.IsNullOrWhiteSpace(newName) || newName.Contains('/'))
				return new("Folder names can't be empty or contain '/'.");
			var newPath = AnimationFolder.Join(AnimationFolder.Parent(path), newName);
			if (newPath == path) return EditorResult.OK;
			if (AnimationFolderPaths(model).Contains(newPath)) return new($"There's already a folder named '{newPath}'.");

			Func<string, string> moved = name => {
				if (name == path) return newPath;
				if (name.StartsWith(path + "/")) return newPath + name.Substring(path.Length);
				return name;
			};
			List<(EditorAnimation Animation, string Old, string New)> animations = [];
			foreach (var animation in model.Animations) {
				if (animation.Name.StartsWith(path + "/"))
					animations.Add((animation, animation.Name, moved(animation.Name)));
			}
			var oldFolders = model.AnimationFolders.ToList();
			List<string> newFolders = [];
			foreach (var folder in oldFolders)
				newFolders.Add(moved(folder));
			Apply(true, model, animations, oldFolders, newFolders);
			Actions.Push($"Rename folder {path}",
				() => Apply(true, model, animations, oldFolders, newFolders),
				() => Apply(false, model, animations, oldFolders, newFolders));
			return EditorResult.OK;
		}

		private void Apply(bool forward, EditorModel model, List<(EditorAnimation Animation, string Old, string New)> animations, List<string> oldFolders, List<string> newFolders) {
			model.AnimationFolders.Clear();
			model.AnimationFolders.AddRange(forward ? newFolders : oldFolders);
			foreach (var item in animations) {
				item.Animation.Name = forward ? item.New : item.Old;
				AnimationRenamed?.Invoke(this, item.Animation, forward ? item.Old : item.New, item.Animation.Name);
			}
			AnimationFoldersChanged?.Invoke(model);
		}

		/// <summary>
		/// Bakes the rotation and scale into the vertices, leaving rotation 0 and scale 1.
		/// </summary>
		public void FreezeVertexAttachment(EditorVertexAttachment attachment) {
			Actions.TrackMesh(attachment);
			Actions.TrackTransform(attachment);
			float rad = attachment.GetRotation() * MathF.PI / 180;
			float cos = MathF.Cos(rad);
			float sin = MathF.Sin(rad);
			float scaleX = attachment.GetScaleX(), scaleY = attachment.GetScaleY();
			foreach (var vertex in attachment.ShapeEdges) {
				float x = vertex.X * scaleX, y = vertex.Y * scaleY;
				vertex.SetPos(new(x * cos - y * sin, x * sin + y * cos));
			}
			attachment.EditRotation(0);
			attachment.EditScaleX(1);
			attachment.EditScaleY(1);
			attachment.Invalidate();
		}

		public void ReversePath(EditorPathAttachment path) {
			Actions.TrackMesh(path);
			// Knots are in/knot/out triplets, reversing the list reverses them too
			path.ShapeEdges.Reverse();

			// Stored lengths are cumulative per curve, and the curves now run the other way
			var oldLengths = path.Lengths.ToList();
			int curves = oldLengths.Count;
			if (curves > 0) {
				List<float> segments = [];
				for (int i = 0; i < oldLengths.Count; i++)
					segments.Add(oldLengths[i] - (i == 0 ? 0 : oldLengths[i - 1]));
				// closing curve of a closed path is always last
				List<float> order = [];
				if (path.Closed) {
					for (int i = curves - 2; i >= 0; i--)
						order.Add(segments[i]);
					order.Add(segments[segments.Count - 1]);
				}
				else {
					for (int i = segments.Count - 1; i >= 0; i--)
						order.Add(segments[i]);
				}
				float total = 0;
				List<float> newLengths = [];
				foreach (var segment in order) {
					total += segment;
					newLengths.Add(total);
				}
				Set(newLengths, path);
				Actions.Push($"Reverse {path.Name}", () => Set(newLengths, path), () => Set(oldLengths, path));
			}
			path.Invalidate();
		}

		private static void Set(List<float> lengths, EditorPathAttachment path) {
			path.Lengths.Clear();
			path.Lengths.AddRange(lengths);
		}

		/// <summary>
		/// Puts vertices back where their uvs put them and removes the weights and deform keys.
		/// </summary>
		public void ResetMesh(EditorMeshAttachment mesh) {
			if (mesh.IsLinked) return;
			Actions.TrackMesh(mesh);
			var uvMap = EditMeshOperator.FitUVMap(mesh);
			foreach (var vertex in mesh.GetVertices()) {
				var pos = EditMeshOperator.PosOf(uvMap, new(vertex.U, vertex.V));
				vertex.X = pos.X;
				vertex.Y = pos.Y;
			}
			mesh.Weights.Clear();
			mesh.Invalidate();

			foreach (var animation in mesh.Slot.Bone.Model.Animations) {
				List<DeformTimeline> deforms = [];
				foreach (var timeline in animation.Timelines) {
					if (timeline is DeformTimeline deformTimeline && deformTimeline.Attachment == mesh)
						deforms.Add(deformTimeline);
				}
				if (deforms.Count == 0) continue;
				Actions.TrackAnimation(animation);
				foreach (var deform in deforms) {
					animation.Timelines.Remove(deform);
					TimelineRemoved?.Invoke(animation, deform);
				}
			}
			mesh.CurrentDeform = null;
			mesh.CurrentBoneDeform = null;
			ModelEditor.Active.QueueAnimationUpdate();
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

		/// <summary>
		/// Makes a slot and region attachment for an image under a bone, upright at a world position. Setup mode only.
		/// </summary>
		public EditorReturnResult<EditorRegionAttachment> CreateRegionFromImage(ModelImage image, EditorBone bone, Vector2F world) {
			if (ModelEditor.Active.AnimationMode) return new(null, "Images can only be added in setup mode.");
			var model = bone.Model;
			var name = image.Name.Contains('/') ? image.Name.Substring(image.Name.LastIndexOf('/') + 1) : image.Name;
			var slotName = model.TryFindSlot(name, out _) ? UniqueName(name, n => model.TryFindSlot(n, out _)) : name;

			var slot = AddSlot(model, bone, slotName);
			if (slot.Failed) return new(null, slot.Reason);
			var region = AddAttachment<EditorRegionAttachment>(slot.ResultOrThrow, name);
			if (region.Failed) return new(null, region.Reason);

			var attachment = region.ResultOrThrow;
			attachment.Path = image.Name;
			Actions.TrackTransform(attachment);
			Compensation.Restore(new(attachment, new Transformation(1, 0, 0, 1, world.X, world.Y, 0, 0)));
			return attachment;
		}

		/// <summary>
		/// Can an image from the tree be dropped onto a bone, slot or attachment of its own model?
		/// </summary>
		public bool CanDropImage(ModelImage image, IEditorType target) {
			if (ModelEditor.Active.AnimationMode || target is not (EditorBone or EditorSlot or EditorAttachment))
				return false;
			return target.GetModel() is EditorModel model && model.Images.ImageLookup.ContainsValue(image);
		}

		public EditorReturnResult<EditorRegionAttachment> DropImageOnto(ModelImage image, IEditorType target) {
			if (!CanDropImage(image, target)) return new(null, "Can't drop that image there.");
			var name = image.Name.Contains('/') ? image.Name.Substring(image.Name.LastIndexOf('/') + 1) : image.Name;
			EditorSlot slot;
			if (target is EditorBone bone) {
				var slotName = bone.Model.TryFindSlot(name, out _) ? UniqueName(name, n => bone.Model.TryFindSlot(n, out _)) : name;
				var made = AddSlot(bone, slotName);
				if (made.Failed) return new(null, made.Reason);
				slot = made.ResultOrThrow;
			}
			else
				slot = target as EditorSlot ?? ((EditorAttachment)target).Slot;

			var attachmentName = slot.Attachments.Any(a => a.Name == name) ? UniqueName(name, n => slot.Attachments.Any(a => a.Name == n)) : name;
			var region = AddAttachment<EditorRegionAttachment>(slot, attachmentName);
			if (region.Failed) return new(null, region.Reason);
			var attachment = region.ResultOrThrow;
			attachment.Path = image.Name;
			if (target is EditorAttachment from) {
				Actions.TrackTransform(attachment);
				attachment.EditTranslationX(from.GetTranslationX());
				attachment.EditTranslationY(from.GetTranslationY());
				attachment.EditRotation(from.GetRotation());
				attachment.EditScaleX(from.GetScaleX());
				attachment.EditScaleY(from.GetScaleY());
			}
			return attachment;
		}

		public EditorResult RescanModelImages(EditorModel model) {
			EditorResult scanResult = model.Images.Scan(ProjectFolder);

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
			if (slot.IsAttachmentNameTaken(name, null))
				return new(null, $"An attachment named '{name}' already exists in this slot.");

			T attachment = new T();
			attachment.Slot = slot;
			attachment.Name = name;
			slot.Attachments.Add(attachment);
			AttachmentAdded?.Invoke(this, slot, attachment);

			var oldSetup = slot.SetupActiveAttachment;
			var oldActive = slot.ActiveAttachment;

			// Make the attachment the active attachment
			slot.SetActiveAttachment(attachment);

			var newSetup = slot.SetupActiveAttachment;
			var newActive = slot.ActiveAttachment;
			int index = slot.Attachments.Count - 1;
			Actions.Push($"Create {name}", () => {
				slot.Attachments.Insert(Math.Min(index, slot.Attachments.Count), attachment);
				AttachmentAdded?.Invoke(this, slot, attachment);
				slot.SetupActiveAttachment = newSetup;
				slot.ActiveAttachment = newActive;
			}, () => {
				UnselectTree(attachment);
				slot.Attachments.Remove(attachment);
				AttachmentRemoved?.Invoke(this, slot, attachment);
				slot.SetupActiveAttachment = oldSetup;
				slot.ActiveAttachment = oldActive;
			});

			return attachment;
		}

		private void InsertAttachment(EditorSlot slot, EditorAttachment attachment, int index, bool setupActive, bool active) {
			slot.Attachments.Insert(Math.Min(index, slot.Attachments.Count), attachment);
			AttachmentAdded?.Invoke(this, slot, attachment);
			if (setupActive)
				slot.SetupActiveAttachment = attachment;
			if (active)
				slot.ActiveAttachment = attachment;
		}

		private void TakeAttachment(EditorAttachment attachment) {
			var slot = attachment.Slot;
			UnselectTree(attachment);
			if (slot.SetupActiveAttachment == attachment)
				slot.SetupActiveAttachment = null;
			if (slot.ActiveAttachment == attachment)
				slot.ActiveAttachment = null;
			slot.Attachments.Remove(attachment);
			AttachmentRemoved?.Invoke(this, slot, attachment);
		}

		public EditorResult RemoveAttachment(EditorAttachment attachment) {
			var slot = attachment.Slot;
			int index = slot.Attachments.IndexOf(attachment);
			if (index < 0) return new("Attachment isn't in its slot.");
			bool setup = slot.SetupActiveAttachment == attachment;
			bool active = slot.ActiveAttachment == attachment;

			TakeAttachment(attachment);
			Actions.Push($"Delete {attachment.Name}",
				() => TakeAttachment(attachment), () => InsertAttachment(slot, attachment, index, setup, active));
			return EditorResult.OK;
		}

		public event AttachmentGeneric? AttachmentSkinChanged;
		public delegate void AttachmentGeneric(EditorFile file, EditorAttachment attachment);

		public EditorResult SetAttachmentSkin(EditorAttachment attachment, EditorSkin? skin) {
			if (attachment.Skin == skin) return EditorResult.OK;
			if (attachment.Slot.IsAttachmentNameTaken(attachment.Name, skin))
				return new($"That skin already has an attachment named '{attachment.Name}' in this slot.");
			var oldSkin = attachment.Skin;
			var oldPlaceholder = attachment.Placeholder;
			ApplyAttachmentSkin(skin, skin == null ? null : oldPlaceholder, attachment);
			var newPlaceholder = attachment.Placeholder;
			Actions.Push($"Set skin of {attachment.Name}",
				() => ApplyAttachmentSkin(skin, newPlaceholder, attachment),
				() => ApplyAttachmentSkin(oldSkin, oldPlaceholder, attachment));
			return EditorResult.OK;
		}

		private void ApplyAttachmentSkin(EditorSkin? skin, string? placeholder, EditorAttachment attachment) {
			attachment.Skin = skin;
			attachment.Placeholder = placeholder;
			AttachmentSkinChanged?.Invoke(this, attachment);
		}

		public EditorResult RenameAttachment(EditorAttachment attachment, string newName) {
			if (attachment.Slot.IsAttachmentNameTaken(newName, attachment.Skin))
				return new("An attachment with that name already exists.");

			var oldName = attachment.Name;
			attachment.Name = newName;
			AttachmentRenamed?.Invoke(this, attachment, oldName, newName);
			Actions.Push(("rename", attachment), $"Rename {oldName}",
				() => RenameAttachment(attachment, newName), () => RenameAttachment(attachment, oldName));

			return EditorResult.OK;
		}

		// ============================================================================================== //
		// Reparenting
		// ============================================================================================== //

		/// <summary>
		/// Called when a bone gets a new parent, a slot a new bone, or an attachment a new slot
		/// </summary>
		public event ItemGeneric? ItemReparented;
		public delegate void ItemGeneric(EditorFile file, IEditorType item);

		public bool CanReparent(IEditorType item, IEditorType target) => item switch {
			EditorBone bone => target is EditorBone parent && bone.Parent != null && parent != bone.Parent
				&& !IsUnder(parent, bone) && parent.Model == bone.Model,
			EditorSlot slot => target is EditorBone bone && bone != slot.Bone && bone.Model == slot.Bone.Model,
			EditorAttachment attachment => target is EditorSlot slot && slot != attachment.Slot && slot.Bone.Model == attachment.Slot.Bone.Model,
			EditorModel model => target is EditorModel other && other != model,
			EditorAnimation animation => target is AnimationFolder folder
				? folder.Model == animation.Model && folder.Path != AnimationFolder.Parent(animation.Name)
				: target is AnimationsList list && list.Model == animation.Model
					&& AnimationFolder.Parent(animation.Name) != "",
			_ => false
		};

		private void MoveBone(EditorBone bone, EditorBone parent, int index) {
			bone.Parent!.Children.Remove(bone);
			parent.Children.Insert(Math.Min(index, parent.Children.Count), bone);
			bone.Parent = parent;
			bone.Model.InvalidateBonesList();
			bone.InvalidateTransform();
			ItemReparented?.Invoke(this, bone);
		}

		private void MoveSlot(EditorSlot slot, EditorBone bone, int index) {
			slot.Bone.Slots.Remove(slot);
			bone.Slots.Insert(Math.Min(index, bone.Slots.Count), slot);
			slot.Bone = bone;
			ItemReparented?.Invoke(this, slot);
		}

		private void MoveAttachment(EditorAttachment attachment, EditorSlot slot, int index, bool setupActive, bool active) {
			var old = attachment.Slot;
			if (old.SetupActiveAttachment == attachment)
				old.SetupActiveAttachment = null;
			if (old.ActiveAttachment == attachment)
				old.ActiveAttachment = null;
			old.Attachments.Remove(attachment);
			slot.Attachments.Insert(Math.Min(index, slot.Attachments.Count), attachment);
			attachment.Slot = slot;
			if (setupActive)
				slot.SetupActiveAttachment = attachment;
			if (active)
				slot.ActiveAttachment = attachment;
			ItemReparented?.Invoke(this, attachment);
		}

		/// <summary>
		/// Moves an item under a new parent without moving it on screen. Setup mode only.
		/// </summary>
		public EditorResult Reparent(IEditorType item, IEditorType target) {
			if (ModelEditor.Active.AnimationMode && item is not EditorAnimation)
				return new("Reparenting only works in setup mode.");
			if (!CanReparent(item, target)) return new("Can't reparent that there.");

			switch (item) {
				case EditorBone bone: {
					var parent = (EditorBone)target;
					var old = bone.Parent!;
					int oldIndex = old.Children.IndexOf(bone);
					int index = parent.Children.Count;
					var world = bone.WorldTransform;

					Actions.TrackTransform(bone);
					MoveBone(bone, parent, index);
					Compensation.Restore(new(bone, world));
					NotifyTransformed(bone);
					Actions.Push($"Set parent of {bone.Name}",
						() => MoveBone(bone, parent, index), () => MoveBone(bone, old, oldIndex));
					break;
				}
				case EditorSlot slot: {
					var bone = (EditorBone)target;
					var old = slot.Bone;
					int oldIndex = old.Slots.IndexOf(slot);
					int index = bone.Slots.Count;
					List<Compensation.Held> held = [];
					foreach (var slotAttachment in slot.Attachments) {
						if (Compensation.Hold(slotAttachment) is Compensation.Held heldAttachment)
							held.Add(heldAttachment);
					}

					foreach (var heldItem in held)
						Actions.TrackTransform(heldItem.Item);
					MoveSlot(slot, bone, index);
					foreach (var heldItem in held)
						Compensation.Restore(heldItem);
					Actions.Push($"Set bone of {slot.Name}",
						() => MoveSlot(slot, bone, index), () => MoveSlot(slot, old, oldIndex));
					break;
				}
				case EditorAttachment attachment: {
					var slot = (EditorSlot)target;
					if (slot.IsAttachmentNameTaken(attachment.Name, attachment.Skin))
						return new($"'{slot.Name}' already has an attachment named '{attachment.Name}'.");
					var old = attachment.Slot;
					int oldIndex = old.Attachments.IndexOf(attachment);
					int index = slot.Attachments.Count;
					bool wasSetup = old.SetupActiveAttachment == attachment;
					bool wasActive = old.ActiveAttachment == attachment;
					var held = Compensation.Hold(attachment);

					if (held != null)
						Actions.TrackTransform(attachment);
					MoveAttachment(attachment, slot, index, false, false);
					if (held != null)
						Compensation.Restore(held.Value);
					Actions.Push($"Set slot of {attachment.Name}",
						() => MoveAttachment(attachment, slot, index, false, false),
						() => MoveAttachment(attachment, old, oldIndex, wasSetup, wasActive));
					break;
				}
				// Animations are moved between folders by dragging them
				case EditorAnimation animation: {
					var folder = (target as AnimationFolder)?.Path ?? "";
					return RenameAnimation(animation.Model, animation, AnimationFolder.Join(folder, AnimationFolder.Leaf(animation.Name)));
				}
				// Dragging skeletons in the tree changes their draw order.
				case EditorModel model: {
					int oldIndex = Models.IndexOf(model);
					int index = Models.IndexOf((EditorModel)target);
					MoveModel(model, index);
					Actions.Push($"Move {model.Name}", () => MoveModel(model, index), () => MoveModel(model, oldIndex));
					break;
				}
			}
			return EditorResult.OK;
		}

		// ============================================================================================== //
		// Duplicating
		// ============================================================================================== //

		private static string UniqueName(string name, Func<string, bool> taken) {
			var stem = name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			if (stem.Length == 0)
				stem = name;
			for (int i = 2; ; i++) {
				var candidate = $"{stem}{i}";
				if (!taken(candidate)) return candidate;
			}
		}

		/// <summary>
		/// Deep copy of a model, through the file format
		/// </summary>
		private static EditorModel CloneModel(EditorModel model)
			=> JsonConvert.DeserializeObject<EditorModel>(JsonConvert.SerializeObject(model, SerializerSettings), SerializerSettings)!;

		// Points references that left the copied piece back at the real model.
		private static void RemapOutside(EditorModel model, IEnumerable<EditorAttachment> attachments, HashSet<EditorBone> copiedBones, HashSet<EditorSlot> copiedSlots) {
			foreach (var attachment in attachments) {
				if (attachment.Skin != null)
					attachment.Skin = model.Skins.FirstOrDefault(x => x.Name == attachment.Skin.Name);
				if (attachment is EditorVertexAttachment vertices) {
					foreach (var weights in vertices.Weights) {
						if (!copiedBones.Contains(weights.Bone))
							weights.Bone = model.FindBone(weights.Bone.Name) ?? weights.Bone;
					}
				}
				if (attachment is EditorClippingAttachment clip && clip.EndSlot != null && !copiedSlots.Contains(clip.EndSlot))
					clip.EndSlot = model.TryFindSlot(clip.EndSlot.Name, out var end) ? end : null;
			}
		}

		public EditorResult Duplicate(IEditorType item) {
			switch (item) {
				case EditorSkin skin:
					return DuplicateSkin(skin);
				case EditorBone bone: {
					var model = bone.Model;
					var parent = bone.Parent;
					if (parent == null) return new("Cannot duplicate the root bone.");

					var copy = CloneModel(model).FindBone(bone.Name)!;
					List<EditorBone> bones = [];
					Collect(copy, bones);

					List<EditorSlot> slots = [];
					foreach (var copiedBone in bones)
						slots.AddRange(copiedBone.Slots);
					List<EditorAttachment> attachments = [];
					foreach (var copiedSlot in slots)
						attachments.AddRange(copiedSlot.Attachments);
					RemapOutside(model, attachments, bones.ToHashSet(), slots.ToHashSet());

					HashSet<string> names = [];
					foreach (var modelBone in model.GetAllBones())
						names.Add(modelBone.Name);
					foreach (var copiedBone in bones) {
						copiedBone.Name = UniqueName(copiedBone.Name, n => names.Contains(n));
						names.Add(copiedBone.Name);
						copiedBone.Model = model;
					}
					HashSet<string> slotNames = [];
					foreach (var modelSlot in model.Slots)
						slotNames.Add(modelSlot.Name);
					// Each copied slot draws right above its original.
					List<(EditorSlot Slot, int Index)> placed = [];
					foreach (var copiedSlot in slots) {
						var original = model.Slots.FindIndex(x => x.Name == copiedSlot.Name);
						copiedSlot.Name = UniqueName(copiedSlot.Name, n => slotNames.Contains(n));
						slotNames.Add(copiedSlot.Name);
						placed.Add((copiedSlot, original < 0 ? model.Slots.Count : original + 1));
					}

					copy.Parent = parent;
					int index = parent.Children.IndexOf(bone) + 1;
					List<(EditorSlot Slot, int Index)> order = [];
					var drawOrder = model.Slots.ToList();
					foreach (var placement in placed.OrderByDescending(x => x.Index))
						drawOrder.Insert(Math.Min(placement.Index, drawOrder.Count), placement.Slot);
					foreach (var copiedSlot in slots)
						order.Add((copiedSlot, drawOrder.IndexOf(copiedSlot)));

					RestoreBone(model, copy, parent, index, order, []);
					List<WeightsBackup> weights = [];
					foreach (var copiedAttachment in attachments) {
						if (copiedAttachment is EditorVertexAttachment vertexAttachment)
							weights.Add(new WeightsBackup(vertexAttachment, vertexAttachment.Weights.ToArray()));
					}
					Actions.Push($"Duplicate {bone.Name}",
						() => RestoreBone(model, copy, parent, index, order, weights), () => RemoveBone(model, copy));
					ModelEditor.Active.SelectObject(copy);
					return EditorResult.OK;
				}
				case EditorSlot slot: {
					var model = slot.Bone.Model;
					var clone = CloneModel(model);
					clone.TryFindSlot(slot.Name, out var copy);
					var attachments = copy!.Attachments.ToList();
					RemapOutside(model, attachments, [], [copy]);

					copy.Bone = slot.Bone;
					copy.Name = UniqueName(slot.Name, n => model.TryFindSlot(n, out _));
					int modelIndex = model.Slots.IndexOf(slot) + 1;
					int boneIndex = slot.Bone.Slots.IndexOf(slot) + 1;

					RestoreSlot(model, copy, modelIndex, boneIndex);
					Actions.Push($"Duplicate {slot.Name}",
						() => RestoreSlot(model, copy, modelIndex, boneIndex), () => RemoveSlot(model, copy));
					ModelEditor.Active.SelectObject(copy);
					return EditorResult.OK;
				}
				case EditorAttachment attachment: {
					var slot = attachment.Slot;
					var model = slot.Bone.Model;
					var clone = CloneModel(model);
					clone.TryFindSlot(slot.Name, out var cloneSlot);
					var copy = cloneSlot!.Attachments[slot.Attachments.IndexOf(attachment)];
					RemapOutside(model, [copy], [], []);

					copy.Slot = slot;
					copy.Name = UniqueName(attachment.Name, n => slot.IsAttachmentNameTaken(n, attachment.Skin));
					int index = slot.Attachments.IndexOf(attachment) + 1;

					Action add = () => {
						slot.Attachments.Insert(Math.Min(index, slot.Attachments.Count), copy);
						AttachmentAdded?.Invoke(this, slot, copy);
					};
					add();
					Actions.Push($"Duplicate {attachment.Name}", add, () => {
						UnselectTree(copy);
						if (slot.SetupActiveAttachment == copy)
							slot.SetupActiveAttachment = null;
						if (slot.ActiveAttachment == copy)
							slot.ActiveAttachment = null;
						slot.Attachments.Remove(copy);
						AttachmentRemoved?.Invoke(this, slot, copy);
					});
					ModelEditor.Active.SelectObject(copy);
					return EditorResult.OK;
				}
			}
			return new("Can't duplicate that.");
		}

		private static void Collect(EditorBone bone, List<EditorBone> bones) {
			bones.Add(bone);
			foreach (var child in bone.Children)
				Collect(child, bones);
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
			int index = model.Skins.Count - 1;
			Actions.Push($"Create {name}", () => RestoreSkin(model, skin, index, false), () => RemoveSkin(model, skin));

			return skin;
		}
		public EditorResult RenameSkin(EditorModel model, EditorSkin skin, string newName) {
			if (newName == skin.Name) return EditorResult.OK;
			if (model.TryFindSkin(newName, out var _))
				return new($"The model already contains a skin with the name '{newName}'.");

			var oldName = skin.Name;
			skin.Name = newName;
			SkinRenamed?.Invoke(this, skin, oldName, newName);
			Actions.Push(("rename", skin), $"Rename {oldName}",
				() => RenameSkin(model, skin, newName), () => RenameSkin(model, skin, oldName));

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
			bool wasActive = model.ActiveSkin == skin;
			int index = model.Skins.IndexOf(skin);
			UnsetActiveSkin(skin);
			if (skin.Selected)
				ModelEditor.Active.UnselectObject(skin);

			// the skin's attachments go with it
			List<(EditorAttachment Attachment, EditorSlot Slot, int Index, bool Setup, bool Active)> owned = [];
			foreach (var attachment in skin.GetAttachments())
				owned.Add((attachment, attachment.Slot, attachment.Slot.Attachments.IndexOf(attachment),
					attachment.Slot.SetupActiveAttachment == attachment, attachment.Slot.ActiveAttachment == attachment));
			foreach (var item in owned)
				TakeAttachment(item.Attachment);

			SkinRemoved?.Invoke(this, model, skin);
			model.Skins.Remove(skin);

			Actions.Push($"Delete {skin.Name}", () => RemoveSkin(model, skin), () => {
				RestoreSkin(model, skin, index, wasActive);
				foreach (var item in owned.OrderBy(x => x.Index))
					InsertAttachment(item.Slot, item.Attachment, item.Index, item.Setup, item.Active);
			});
			return EditorResult.OK;
		}

		public EditorResult DuplicateSkin(EditorSkin skin) {
			var model = skin.Model;
			var copy = new EditorSkin() {
				Name = UniqueName(skin.Name, n => model.Skins.Any(x => x.Name == n)),
				Model = model
			};
			int index = model.Skins.IndexOf(skin) + 1;
			RestoreSkin(model, copy, index, false);

			var clone = CloneModel(model);
			List<EditorAttachment> made = [];
			foreach (var attachment in skin.GetAttachments().ToList()) {
				clone.TryFindSlot(attachment.Slot.Name, out var cloneSlot);
				var dup = cloneSlot!.Attachments[attachment.Slot.Attachments.IndexOf(attachment)];
				RemapOutside(model, [dup], [], []);
				dup.Slot = attachment.Slot;
				dup.Skin = copy;
				dup.Placeholder = attachment.Placeholder;

				InsertAttachment(attachment.Slot, dup, attachment.Slot.Attachments.Count, false, false);
				made.Add(dup);
			}

			Actions.Push($"Duplicate {skin.Name}", () => {
				RestoreSkin(model, copy, index, false);
				foreach (var attachment in made)
					InsertAttachment(attachment.Slot, attachment, attachment.Slot.Attachments.Count, false, false);
			}, () => {
				foreach (var attachment in made)
					TakeAttachment(attachment);
				UnsetActiveSkin(copy);
				if (copy.Selected)
					ModelEditor.Active.UnselectObject(copy);
				SkinRemoved?.Invoke(this, model, copy);
				model.Skins.Remove(copy);
			});
			ModelEditor.Active.SelectObject(copy);
			return EditorResult.OK;
		}

		private void RestoreSkin(EditorModel model, EditorSkin skin, int index, bool activate) {
			model.Skins.Insert(Math.Min(index, model.Skins.Count), skin);
			SkinAdded?.Invoke(this, model, skin);
			if (activate)
				SetActiveSkin(model, skin);
		}

		// ============================================================================================== //
		// Attachment things
		// ============================================================================================== //

		public static void UpdateVertexPositions(EditorVertexAttachment attachment, List<EditorBone>? onlyTheseBones = null, EditorVertex? onlyThisVertex = null) {
			var weights = attachment.Weights;
			if (weights.Count <= 0) return;

			attachment.SetupWorldTransform();
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

		// ============================================================================================== //
		// Selection modification
		// ============================================================================================== //

		public delegate void OnTransformed(IEditorType item, bool translation, bool rotation, bool scale, bool shear);
		public event OnTransformed? ItemTransformed;

		public void NotifyTransformed(IEditorType item) => ItemTransformed?.Invoke(item, true, true, true, true);

		public EditorResult RotateOne(IEditorType item, float rotation, bool localTo = true) {
			if (item == null) return new("Null item.");
			if (!item.CanRotate()) return new("Cannot rotate the item; it forbids it.");
			ModelEditor.Active.Actions.TrackTransform(item);
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
		public void MoveItemWorldspace(IEditorType item, Vector2F worldPos)
			=> DoSelectionTransformation(x => x.CanTranslate(), x => x.SetWorldPosition(worldPos), TRANSFORM_MODE.Location, [item]);
		public void RotateItem(IEditorType item, float degrees)
			=> DoSelectionTransformation(x => x.CanRotate(), x => x.EditRotation(x.GetRotation() + degrees), TRANSFORM_MODE.Rotation, [item]);

		private void DoSelectionTransformation(Func<IEditorType, bool> predicate, Action<IEditorType> onEachObject, TRANSFORM_MODE transform, IEnumerable<IEditorType>? items = null) {
			var selected = items ?? ModelEditor.Active.SelectedObjects;
			HashSet<IEditorType> transformed = [];

			var editor = ModelEditor.Active.Editor;
			bool compBones = editor.BoneCompensation;
			bool compImages = editor.ImageCompensation && !ModelEditor.Active.AnimationMode;
			HashSet<IEditorType> moving = [];
			if (compBones || compImages) {
				foreach (IEditorType obj in selected) {
					if (obj.DeferTransformationsTo() is IEditorType deferred && predicate(deferred))
						moving.Add(deferred);
				}
			}
			List<Compensation.Held> held = [];
			bool childRotates = (transform & ~TRANSFORM_MODE.Location) != 0;
			bool childScales = (transform & (TRANSFORM_MODE.Scale | TRANSFORM_MODE.Shear)) != 0;

			foreach (IEditorType obj in selected) {
				var transformable = obj.DeferTransformationsTo();
				if (transformable == null) continue;

				if (predicate(transformable) && !transformed.Contains(transformable)) {
					ModelEditor.Active.Actions.TrackTransform(transformable);

					held.Clear();
					if (transformable is EditorBone bone && (compBones || compImages)) {
						Compensation.Capture(bone, compBones, compImages, moving, held);
						foreach (var heldItem in held)
							ModelEditor.Active.Actions.TrackTransform(heldItem.Item);
					}

					onEachObject(transformable);

					foreach (var heldItem in held) {
						Compensation.Restore(heldItem);
						ItemTransformed?.Invoke(heldItem.Item, true, childRotates, childScales, childScales);
					}

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
		// Toolbar edits, in the toolbar's Local/Parent/World axes
		public EditorResult RotateSelectedAxes(float value, EditorTransformMode mode) {
			DoSelectionTransformation(x => x.CanRotate(), x => TransformAxes.SetRotation(x, value, mode), TRANSFORM_MODE.Rotation);
			return EditorResult.OK;
		}
		public EditorResult TranslateSelectedAxes(int axis, float value, EditorTransformMode mode) {
			DoSelectionTransformation(x => x.CanTranslate(), x => TransformAxes.SetTranslation(x, axis, value, mode), TRANSFORM_MODE.Location);
			return EditorResult.OK;
		}
		public EditorResult ScaleSelectedAxes(int axis, float value, EditorTransformMode mode) {
			DoSelectionTransformation(x => x.CanScale(), x => TransformAxes.SetScale(x, axis, value, mode), TRANSFORM_MODE.Scale);
			return EditorResult.OK;
		}

		public EditorResult SetSelectedScale(Func<IEditorType, Vector2F> scaleFor) {
			DoSelectionTransformation(x => x.CanScale(), item => {
				var scale = scaleFor(item);
				item.EditScaleX(scale.X);
				item.EditScaleY(scale.Y);
			}, TRANSFORM_MODE.Scale);
			return EditorResult.OK;
		}

		public EditorResult ScaleXSelected(float value, bool additive = false) {
			DoSelectionTransformation(x => x.CanScale(), x => x.EditScaleX(additive ? x.GetScaleX() + value : value), TRANSFORM_MODE.Scale);
			return EditorResult.OK;
		}
		public EditorResult ScaleYSelected(float value, bool additive = false) {
			DoSelectionTransformation(x => x.CanScale(), x => x.EditScaleY(additive ? x.GetScaleY() + value : value), TRANSFORM_MODE.Scale);
			return EditorResult.OK;
		}
		public EditorResult ShearXSelected(float value, bool additive = false) {
			DoSelectionTransformation(x => x.CanShear(), x => x.EditShearX(additive ? x.GetShearX() + value : value), TRANSFORM_MODE.Shear);
			return EditorResult.OK;
		}
		public EditorResult ShearYSelected(float value, bool additive = false) {
			DoSelectionTransformation(x => x.CanShear(), x => x.EditShearY(additive ? x.GetShearY() + value : value), TRANSFORM_MODE.Shear);
			return EditorResult.OK;
		}

		// ============================================================================================== //
		// General file operations
		// ============================================================================================== //

		public void Clear() {
			DeactivateOperator(true);
			DiskPath = null;
			Models.Clear();
			Export = new();
			Cleared?.Invoke(this);
		}

		public void NewFile() {
			Clear();
			FormatVersion = FORMAT_VERSION;
			AddModel("model");
			Actions.Clear();
		}

		public delegate void OnEditorItemVischange(EditorFile self, IEditorType editorItem, bool visible);
		public event OnEditorItemVischange? EditorItemHidden;
		public event OnEditorItemVischange? EditorItemShown;
		public event OnEditorItemVischange? EditorItemVisibilityChanged;

		public void ShowEditorItem(IEditorType item) {
			var before = CaptureVisibility(item);
			item.Hidden = false;
			item.OnShown();
			EditorItemShown?.Invoke(this, item, true);
			EditorItemVisibilityChanged?.Invoke(this, item, true);
			PushVisibility(item, before, $"Show {item.GetName()}");
			AttachmentPoseEdited(item, before);
		}

		public void HideEditorItem(IEditorType item) {
			var before = CaptureVisibility(item);
			item.Hidden = true;
			item.OnHidden();
			EditorItemHidden?.Invoke(this, item, false);
			EditorItemVisibilityChanged?.Invoke(this, item, false);
			PushVisibility(item, before, $"Hide {item.GetName()}");
			AttachmentPoseEdited(item, before);
		}

		// showing/hiding an attachment in animate mode changes the slot's keyable attachment
		private void AttachmentPoseEdited(IEditorType item, VisibilityState before) {
			if (item is EditorAttachment attachment && attachment.Slot.ActiveAttachment != before.Active)
				PoseEdited(attachment.Slot, KeyframeProperty.Slot_Attachment);
		}

		// Attachments also change which one the slot shows
		private record VisibilityState(bool Hidden, EditorAttachment? Setup, EditorAttachment? Active);

		private static VisibilityState CaptureVisibility(IEditorType item) => item is EditorAttachment attachment
				? new(item.Hidden, attachment.Slot.SetupActiveAttachment, attachment.Slot.ActiveAttachment) : new(item.Hidden, null, null);

		private void ApplyVisibility(IEditorType item, VisibilityState state) {
			item.Hidden = state.Hidden;
			if (item is EditorAttachment attachment) {
				attachment.Slot.SetupActiveAttachment = state.Setup;
				attachment.Slot.ActiveAttachment = state.Active;
			}
			(state.Hidden ? EditorItemHidden : EditorItemShown)?.Invoke(this, item, !state.Hidden);
			EditorItemVisibilityChanged?.Invoke(this, item, !state.Hidden);
		}

		private void PushVisibility(IEditorType item, VisibilityState before, string name) {
			var after = CaptureVisibility(item);
			if (before == after) return;
			Actions.Push(("visibility", item), name,
				() => ApplyVisibility(item, after), () => ApplyVisibility(item, before));
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

							meshTo.ShapeEdges.Add(EditorVertex.FromVector(quadpoints.TL, new(0, 0), meshTo));
							meshTo.ShapeEdges.Add(EditorVertex.FromVector(quadpoints.TR, new(1, 0), meshTo));
							meshTo.ShapeEdges.Add(EditorVertex.FromVector(quadpoints.BR, new(1, 1), meshTo));
							meshTo.ShapeEdges.Add(EditorVertex.FromVector(quadpoints.BL, new(0, 1), meshTo));

							break;
					}
					break;
			}

			if (newAttachment == null)
				throw new NotImplementedException($"No attachment conversion exists from {attachment.GetType().Name} -> {typeof(T).Name}.");

			// copy mutual properties
			newAttachment.Slot = attachment.Slot;
			newAttachment.Name = attachment.Name;

			SwapAttachment(attachment, newAttachment, selected);
			Actions.Push($"Convert {attachment.Name}",
				() => SwapAttachment(attachment, newAttachment, attachment.Selected),
				() => SwapAttachment(newAttachment, attachment, newAttachment.Selected));
		}

		private void SwapAttachment(EditorAttachment from, EditorAttachment to, bool select) {
			if (from.Selected)
				ModelEditor.Active.UnselectObject(from);

			var slot = from.Slot;
			var index = slot.Attachments.IndexOf(from);
			if (index == -1)
				throw new InvalidOperationException("Attachment wasn't a part of the slots attachments, yet was assigned to the slot...?");

			slot.Attachments[index] = to;
			if (slot.SetupActiveAttachment == from)
				slot.SetupActiveAttachment = to;
			if (slot.ActiveAttachment == from)
				slot.ActiveAttachment = to;

			AttachmentRemoved?.Invoke(this, slot, from);
			AttachmentAdded?.Invoke(this, slot, to);
			if (select)
				ModelEditor.Active.SelectObject(to);
		}

		public delegate void SeparatePropertyDelegate(EditorBone bone, KeyframeProperty property, bool separated);
		public event SeparatePropertyDelegate? PropertySeparatedOrCombined;
		public void SetDoesBoneHaveSeparatedProperty(EditorBone editorBone, KeyframeProperty property, bool separated) {
			if (ActiveAnimation == null) return;
			Actions.TrackAnimation(ActiveAnimation);

			if (separated) {
				// Find the root timeline if it exists
				var combinedTimeline = ActiveAnimation.SearchTimelineByProperty(editorBone, property, -1, false);
				if (combinedTimeline != null && combinedTimeline is CurveTimeline2 ct2) {
					SeparateTimelines(ct2);
				}
			}
			else {
				var x = ActiveAnimation.SearchTimelineByProperty(editorBone, property, 0, false);
				var y = ActiveAnimation.SearchTimelineByProperty(editorBone, property, 1, false);

				if (x is CurveTimeline1 ct1x && y is CurveTimeline1 ct1y) {
					CombineTimelines(ct1x, ct1y);
				}
			}

			ActiveAnimation.SetDoesBoneHaveSeparatedProperty(editorBone, property, separated);
			PropertySeparatedOrCombined?.Invoke(editorBone, property, separated);
		}

		public event Action<EditorSlot, bool>? SlotColorSeparatedOrCombined;
		internal void NotifyColorSeparated(EditorSlot slot, bool separated) => SlotColorSeparatedOrCombined?.Invoke(slot, separated);

		public void SetSlotColorSeparated(EditorSlot slot, bool separated) {
			var animation = ActiveAnimation;
			if (animation == null || animation.SeparatedColors.Contains(slot) == separated) return;
			Actions.TrackAnimation(animation);

			if (separated) {
				if (animation.SearchTimelineByProperty(slot, KeyframeProperty.Slot_Color, -1, false) is SlotColorTimeline color) {
					SlotColorRGBTimeline rgb = new() {
						Slot = slot, CurveR = color.CurveR, CurveG = color.CurveG, CurveB = color.CurveB
					};
					SlotColorAlphaTimeline alpha = new() { Slot = slot, CurveA = color.CurveA };
					animation.Timelines.Add(rgb);
					TimelineCreated?.Invoke(animation, rgb);
					animation.Timelines.Add(alpha);
					TimelineCreated?.Invoke(animation, alpha);
					RemoveTimeline(animation, color);
					TimelineRemoved?.Invoke(animation, color);
					TimelineSeparated?.Invoke(animation, color, rgb, alpha);
				}
				animation.SeparatedColors.Add(slot);
			}
			else {
				var rgb = animation.SearchTimelineByProperty(slot, KeyframeProperty.Slot_Color, 0, false)
					as SlotColorRGBTimeline;
				var alpha = animation.SearchTimelineByProperty(slot, KeyframeProperty.Slot_Color, 1, false)
					as SlotColorAlphaTimeline;
				if (rgb != null || alpha != null) {
					rgb ??= new() { Slot = slot };
					alpha ??= new() { Slot = slot };
					// Combined keys need all four channels, fill in times only one side has
					List<double> rgbTimes = [];
					foreach (var keyframe in rgb.CurveR.GetKeyframes())
						rgbTimes.Add(keyframe.Time);
					foreach (var time in rgbTimes) {
						if (!alpha.CurveA.TryFindKeyframe(time, out _))
							alpha.CurveA.AddKeyframe(new(time, alpha.CurveA.Count > 0 ? alpha.CurveA.DetermineValueAtTime(time) : slot.SetupColor.A / 255f));
					}
					List<double> alphaTimes = [];
					foreach (var keyframe in alpha.CurveA.GetKeyframes())
						alphaTimes.Add(keyframe.Time);
					foreach (var time in alphaTimes) {
						if (!rgb.CurveR.TryFindKeyframe(time, out _)) {
							bool empty = rgb.CurveR.Count == 0;
							rgb.CurveR.AddKeyframe(new(time, empty ? slot.SetupColor.R / 255f : rgb.CurveR.DetermineValueAtTime(time)));
							rgb.CurveG.AddKeyframe(new(time, empty ? slot.SetupColor.G / 255f : rgb.CurveG.DetermineValueAtTime(time)));
							rgb.CurveB.AddKeyframe(new(time, empty ? slot.SetupColor.B / 255f : rgb.CurveB.DetermineValueAtTime(time)));
						}
					}

					SlotColorTimeline color = new() {
						Slot = slot, CurveR = rgb.CurveR, CurveG = rgb.CurveG, CurveB = rgb.CurveB, CurveA = alpha.CurveA
					};
					animation.Timelines.Add(color);
					TimelineCreated?.Invoke(animation, color);
					foreach (var part in new EditorTimeline[] { rgb, alpha }) {
						if (animation.Timelines.Remove(part))
							TimelineRemoved?.Invoke(animation, part);
					}
					TimelineCombined?.Invoke(animation, rgb, alpha, color);
				}
				animation.SeparatedColors.Remove(slot);
			}
			SlotColorSeparatedOrCombined?.Invoke(slot, separated);
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

		public delegate void OnAnimationEdited(EditorAnimation animation);
		/// <summary>
		/// Timelines or keys swapped out wholesale (undo/redo)
		/// </summary>
		public event OnAnimationEdited? AnimationEdited;

		internal void NotifyAnimationEdited(EditorAnimation animation) => AnimationEdited?.Invoke(animation);
		internal void NotifySeparated(EditorBone bone, KeyframeProperty property, bool separated)
			=> PropertySeparatedOrCombined?.Invoke(bone, property, separated);

		public void InsertKeyframe(IEditorType? type, KeyframeProperty property, int arrayIndex = -1) {
			if (type == null) return;
			if (ActiveAnimation == null) return;
			Actions.TrackAnimation(ActiveAnimation);

			if (PoseKeys.Handles(property)) {
				var poseTimeline = PoseKeys.Insert(ActiveAnimation, type, property, Timeline.Frame, out bool poseCreated);
				if (poseTimeline == null) return;
				if (poseCreated)
					TimelineCreated?.Invoke(ActiveAnimation, poseTimeline);
				KeyframeCreated?.Invoke(ActiveAnimation, poseTimeline, Timeline.Frame);
				return;
			}

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

		public KeyframeState KeyState(IEditorType target, KeyframeProperty property, int arrayIndex) {
			var animation = ActiveAnimation;
			if (animation == null) return KeyframeState.NotKeyframed;
			if (PoseKeys.Handles(property)) return PoseKeys.State(animation, target, property, Timeline.Frame);
			return animation.SearchTimelineByProperty(target, property, arrayIndex, false)?.KeyframedAt(Timeline.Frame) ?? KeyframeState.NotKeyframed;
		}

		/// <summary>
		/// <see cref="PoseEdited"/> for a slot color change
		/// </summary>
		public void SlotColorEdited(EditorSlot slot, Color before, Color after) {
			if (ActiveAnimation?.SeparatedColors.Contains(slot) != true || !ModelEditor.Active.Editor.AutoKey || !ModelEditor.Active.CanInsertKeyframes()) {
				PoseEdited(slot, KeyframeProperty.Slot_Color);
				return;
			}
			if (!ModelEditor.Active.AnimationMode) return;
			if (before.R != after.R || before.G != after.G || before.B != after.B)
				InsertKeyframe(slot, KeyframeProperty.Slot_Color, 0);
			if (before.A != after.A)
				InsertKeyframe(slot, KeyframeProperty.Slot_Color, 1);
		}

		/// <summary>
		/// Call after a pose change in animate mode, keys it when auto key is on
		/// </summary>
		public void PoseEdited(IEditorType target, KeyframeProperty property) {
			if (!ModelEditor.Active.AnimationMode) return;
			if (ModelEditor.Active.Editor.AutoKey && ModelEditor.Active.CanInsertKeyframes())
				InsertPoseKey(target, property);
			else
				edited.Add((target, property));
		}

		// Changed in animate mode but not keyed yet
		readonly HashSet<(IEditorType Target, KeyframeProperty Property)> edited = [];

		/// <summary>
		/// K, keys everything changed since the last key or frame change
		/// </summary>
		public void KeyEdited() {
			if (!ModelEditor.Active.CanInsertKeyframes()) return;
			foreach (var item in edited.ToArray())
				InsertPoseKey(item.Target, item.Property);
			edited.Clear();
		}

		public void KeySelectedBones(params KeyframeProperty[] properties) {
			if (!ModelEditor.Active.CanInsertKeyframes()) return;
			List<EditorBone> bones = [];
			foreach (var selected in ModelEditor.Active.SelectedObjects) {
				if (selected is EditorBone selectedBone)
					bones.Add(selectedBone);
			}
			foreach (var bone in bones) {
				foreach (var property in properties) {
					InsertPoseKey(bone, property);
					edited.Remove((bone, property));
				}
			}
		}

		private void InsertPoseKey(IEditorType target, KeyframeProperty property) {
			bool twoAxes = property is KeyframeProperty.Bone_Translation or KeyframeProperty.Bone_Scale
				or KeyframeProperty.Bone_Shear;
			if (twoAxes && target is EditorBone bone && ActiveAnimation?.DoesBoneHaveSeparatedProperty(bone, property) == true) {
				InsertKeyframe(bone, property, 0);
				InsertKeyframe(bone, property, 1);
			}
			else if (property == KeyframeProperty.Slot_Color && target is EditorSlot slot && ActiveAnimation?.SeparatedColors.Contains(slot) == true) {
				InsertKeyframe(slot, property, 0);
				InsertKeyframe(slot, property, 1);
			}
			else
				InsertKeyframe(target, property, -1);
		}

		public void CombineTimelines(CurveTimeline1 x, CurveTimeline1 y) {
			var activeAnimation = ActiveAnimation;
			Debug.Assert(activeAnimation != null);

			CurveTimeline2? ct2 = null;
			switch (x) {
				case TranslateXTimeline translateX:
					if (y is not TranslateYTimeline translateY) throw new Exception("Mismatched X and Y types");
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
					if (y is not ShearYTimeline shearY)
						throw new Exception("Mismatched X and Y types");
					Debug.Assert(shearX.Bone == shearY.Bone);

					ShearTimeline shear = new() { Bone = shearX.Bone, CurveX = x.Curve, CurveY = y.Curve };
					activeAnimation.Timelines.Add(shear);
					TimelineCreated?.Invoke(activeAnimation, shear);
					ct2 = shear;
					break;

				default:
					throw new Exception("Cannot combine those timeline types");
			}

			// TODO: Evaluate both curves to determine what's missing in each curve, then create a dummy keyframe

			RemoveTimeline(activeAnimation, x);
			TimelineRemoved?.Invoke(activeAnimation, x);
			RemoveTimeline(activeAnimation, y);
			TimelineRemoved?.Invoke(activeAnimation, y);

			TimelineCombined?.Invoke(activeAnimation, x, y, ct2);
		}

		private static void RemoveTimeline(EditorAnimation animation, EditorTimeline timeline) {
			bool removed = animation.Timelines.Remove(timeline);
			Debug.Assert(removed);
		}

		public void SeparateTimelines(CurveTimeline2 timeline) {
			var activeAnimation = ActiveAnimation;
			Debug.Assert(activeAnimation != null);

			switch (timeline) {
				case TranslateTimeline translate:
					TranslateXTimeline xTimeline = new() { Bone = translate.Bone, Curve = translate.CurveX };
					TranslateYTimeline yTimeline = new() { Bone = translate.Bone, Curve = translate.CurveY };

					activeAnimation.Timelines.Add(xTimeline);
					TimelineCreated?.Invoke(activeAnimation, xTimeline);

					activeAnimation.Timelines.Add(yTimeline);
					TimelineCreated?.Invoke(activeAnimation, yTimeline);

					RemoveTimeline(activeAnimation, translate);

					TimelineRemoved?.Invoke(activeAnimation, translate);
					TimelineSeparated?.Invoke(activeAnimation, translate, xTimeline, yTimeline);
					break;
				case ScaleTimeline scale:
					ScaleXTimeline xScTimeline = new() { Bone = scale.Bone, Curve = scale.CurveX };
					ScaleYTimeline yScTimeline = new() { Bone = scale.Bone, Curve = scale.CurveY };

					activeAnimation.Timelines.Add(xScTimeline);
					TimelineCreated?.Invoke(activeAnimation, xScTimeline);

					activeAnimation.Timelines.Add(yScTimeline);
					TimelineCreated?.Invoke(activeAnimation, yScTimeline);

					RemoveTimeline(activeAnimation, scale);

					TimelineRemoved?.Invoke(activeAnimation, scale);
					TimelineSeparated?.Invoke(activeAnimation, scale, xScTimeline, yScTimeline);
					break;
				case ShearTimeline shear:
					ShearXTimeline xShTimeline = new() { Bone = shear.Bone, Curve = shear.CurveX };
					ShearYTimeline yShTimeline = new() { Bone = shear.Bone, Curve = shear.CurveY };

					activeAnimation.Timelines.Add(xShTimeline);
					TimelineCreated?.Invoke(activeAnimation, xShTimeline);

					activeAnimation.Timelines.Add(yShTimeline);
					TimelineCreated?.Invoke(activeAnimation, yShTimeline);

					RemoveTimeline(activeAnimation, shear);

					TimelineRemoved?.Invoke(activeAnimation, shear);
					TimelineSeparated?.Invoke(activeAnimation, shear, xShTimeline, yShTimeline);
					break;

				default:
					throw new Exception("Cannot split this timeline type");
			}
		}
	}
}
