using Newtonsoft.Json;
using Nucleus.ModelEditor.UI;
using Raylib_cs;
using System.Xml.Linq;
using static Nucleus.ModelEditor.EditorFile;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// The editor interface controller. All editor operations should go through this
	/// </summary>
	public class EditorFile
	{
		public List<EditorModel> Models = [];

		public static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings() {
			PreserveReferencesHandling = PreserveReferencesHandling.All
		};

		public string Serialize() {
			return JsonConvert.SerializeObject(this, SerializerSettings);
		}

		public void Deserialize(string data) {
			// Clear ourselves of old data (which tells the outliner to clear itself as well)
			Clear();

			JsonConvert.PopulateObject(data, this);

			foreach (var model in Models) {
				ModelAdded?.Invoke(this, model);
				model.Images.Scan();
				ModelImagesScanned?.Invoke(this, model);
				foreach (var bone in model.GetAllBones()) {
					BoneAdded?.Invoke(this, model, bone);
					BoneColorChanged?.Invoke(this, bone);
					foreach (var slot in bone.Slots) {
						SlotAdded?.Invoke(this, model, bone, slot);
					}
				}
			}
		}

		// Event callbacks

		/// <summary>
		/// Called when the file is cleared.
		/// </summary>
		public event FileClear? Cleared;
		public delegate void FileClear(EditorFile file);

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
		/// <summary>
		/// Called when a model's images have completed scanning.
		/// </summary>
		public event ModelImagesScannedD? ModelImagesScanned;
		public delegate void ModelAddRemove(EditorFile file, EditorModel model);
		public delegate void ModelRename(EditorFile file, EditorModel model, string oldName, string newName);

		public delegate void ModelImagesScannedD(EditorFile file, EditorModel model);

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

		// Adding models

		public EditorReturnResult<EditorModel> AddModel(string name) {
			if (Models.FirstOrDefault(x => x.Name == name) != null)
				return new(null, $"The model already contains a bone named '{name}'.");

			var model = new EditorModel();
			model.Name = name;
			Models.Add(model);

			model.Root = AddBone(model, null, "root").Result ?? throw new Exception("Wtf?");

			ModelAdded?.Invoke(this, model);
			return new(model);
		}
		public EditorResult RenameModel(EditorModel model, string newName) {
			if (Models.FirstOrDefault(x => x.Name == newName) != null)
				return new($"The model already contains a bone named '{newName}'.");

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

		// Adding bones

		public EditorReturnResult<EditorBone> AddBone(EditorModel model, EditorBone? parent, string? name) {
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
			return new(bone);
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

		// Renaming bones
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

		// Removing bones

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

				return new();
			}

			return new("Model was null.");
		}
		public EditorResult RemoveBone(EditorBone bone)
			=> RemoveBone(bone.Model, bone);
		public EditorResult RemoveBone(EditorModel model, string boneName)
			=> model.TryFindBone(boneName, out EditorBone? bone) ? RemoveBone(model, bone) : new($"Bone '{boneName}' not found.");

		// Adding slots

		public EditorReturnResult<EditorSlot> AddSlot(EditorModel model, EditorBone bone, string slotName) {
			if (model.TryFindSlot(slotName, out var _))
				return new(null, $"The model already contains a slot with the name '{slotName}'.");

			EditorSlot slot = new EditorSlot();
			slot.Name = slotName;
			slot.Bone = bone;
			model.Slots.Add(slot);
			bone.Slots.Add(slot);

			SlotAdded?.Invoke(this, model, bone, slot);

			return new(slot);
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

			return EditorResult.OK;
		}
		public EditorResult RemoveSlot(EditorSlot slot) => RemoveSlot(slot.Bone.Model, slot);

		public EditorResult SetModelImages(EditorModel model, string filepath) {
			model.Images.Filepath = filepath;
			return RescanModelImages(model);
		}

		public EditorResult RotateSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanRotate()) obj.EditRotation(value);
			return EditorResult.OK;
		}
		public EditorResult TranslateXSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanTranslate()) obj.EditTranslationX(value);
			return EditorResult.OK;
		}
		public EditorResult TranslateYSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanTranslate()) obj.EditTranslationY(value);
			return EditorResult.OK;
		}
		public EditorResult ScaleXSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanScale()) obj.EditScaleX(value);
			return EditorResult.OK;
		}
		public EditorResult ScaleYSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanScale()) obj.EditScaleY(value);
			return EditorResult.OK;
		}
		public EditorResult ShearXSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanShear()) obj.EditShearX(value);
			return EditorResult.OK;
		}
		public EditorResult ShearYSelected(float value) {
			var selected = ModelEditor.Active.SelectedObjects;
			foreach (IEditorType obj in selected) if (obj.CanShear()) obj.EditShearY(value);
			return EditorResult.OK;
		}

		// Clearing file

		public void Clear() {
			DeactivateOperator(true);
			Models.Clear();
			Cleared?.Invoke(this);
		}

		// Making a new file

		public void NewFile() {
			Clear();
			AddModel("model");
		}

		public EditorResult RescanModelImages(EditorModel model) {
			EditorResult scanResult = model.Images.Scan();

			if (scanResult.Succeeded) {
				ModelImagesScanned?.Invoke(this, model);
			}

			return scanResult;
		}

		// Operator support
		public delegate void OnOperatorActivated(EditorFile self, Operator op);
		public delegate void OnOperatorDeactivated(EditorFile self, Operator op, bool canceled);

		public event OnOperatorActivated? OperatorActivated;
		public event OnOperatorDeactivated? OperatorDeactivating;
		public event OnOperatorDeactivated? OperatorDeactivated;
		public Operator? ActiveOperator { get; private set; }
		public void ActivateOperator(Operator op) {
			if (ActiveOperator != null) DeactivateOperator(true);

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
	}
}
