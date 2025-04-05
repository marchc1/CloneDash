using Newtonsoft.Json;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucleus.ModelEditor
{
	public record ActionStackData(Action Redo, Action Undo);
	public class ActionStack
	{
		private MaxStack<ActionStackData> DidActions = new(256);
		private Stack<ActionStackData> UndidActions = [];
		public void Push(Action action, Action undo) {
			UndidActions.Clear();
			DidActions.Push(new(action, undo));
		}

		public void Undo() {
			if (DidActions.Count <= 0) return;
			ActionStackData data = DidActions.Pop();
			data.Undo();
			UndidActions.Push(data);
		}

		public void Redo() {
			if (UndidActions.Count <= 0) return;
			ActionStackData data = UndidActions.Pop();
			data.Redo();
			DidActions.Push(data);
		}
	}
	public class ModelEditor : Level
	{
		public ActionStack Actions { get; } = new();
		// Object selection management
		private List<IEditorType> __selectedObjectsL = [];
		private HashSet<IEditorType> __selectedObjects = [];
		public IEditorType? FirstSelectedObject => __selectedObjectsL.FirstOrDefault();
		public IEditorType? LastSelectedObject => __selectedObjectsL.LastOrDefault();
		public int SelectedObjectsCount => __selectedObjectsL.Count;

		public IEnumerable<IEditorType> SelectedObjects => __selectedObjectsL;

		public delegate void OnObjectSelected(IEditorType selected);
		public delegate void OnSelectedChanged();

		public event OnObjectSelected? ObjectSelected;
		public event OnObjectSelected? ObjectUnselected;
		public event OnSelectedChanged? SelectedChanged;

		private Operator? ActiveOperator => File.ActiveOperator;

		private bool OperatorSelectionBlocks(IEditorType? item) {
			if (ActiveOperator != null) {
				// it depends...
				if (ActiveOperator.OverrideSelection) {
					if (!ActiveOperator.SelectMultiple) {
						if (item == null)
							File.DeactivateOperator(true);
						else {
							ActiveOperator.Selected(this, item);
							File.DeactivateOperator(false);
						}
					}
					return true;
				}
				else {
					File.DeactivateOperator(true);
					return false;
				}
			}

			return false;
		}

		private void SELECTIONCHANGE() {
			SelectedChanged?.Invoke();
		}

		public void SelectObject(IEditorType o, bool additive = false) {
			if (OperatorSelectionBlocks(o)) return;

			// Check if selectable
			bool selectable = o.OnSelected();
			if (!selectable)
				return; // just cancel out

			if (!additive) {
				foreach (var obj in __selectedObjects.ToArray()) {
					__selectedObjects.Remove(obj);
					__selectedObjectsL.Remove(obj);
					ObjectUnselected?.Invoke(obj);

					obj.Selected = false;
					obj.OnUnselected();
				}
				// We don't call UnselectAllObjects because that calls SelectedChanged twice; we don't want that
			}

			SELECTIONCHANGE();

			o.Selected = true;
			__selectedObjects.Add(o);
			__selectedObjectsL.Add(o);

			SELECTIONCHANGE();
		}

		public void UnselectObject(IEditorType o) {
			__selectedObjects.Remove(o);
			__selectedObjectsL.Remove(o);
			ObjectUnselected?.Invoke(o);
			SELECTIONCHANGE();

			o.Selected = false;
			o.OnUnselected();
		}

		public void SelectObjects(params IEditorType[] os) {
			bool haltActualLogic = false;
			foreach (var obj in os) {
				if (OperatorSelectionBlocks(obj))
					haltActualLogic = true; // Selection is overriden; so don't actually process this stuff

				if (haltActualLogic) continue;

				obj.Selected = true;
				if (!obj.OnSelected()) {
					obj.Selected = false;
					continue;
				}

				__selectedObjects.Add(obj);
				__selectedObjectsL.Add(obj);
				ObjectSelected?.Invoke(obj);
			}
			if (haltActualLogic) return;
			SELECTIONCHANGE();
		}

		public void UnselectAllObjects() {
			// Copy made to avoid modifying enumerable during iteration
			foreach (var obj in __selectedObjects.ToArray()) {
				__selectedObjects.Remove(obj);
				__selectedObjectsL.Remove(obj);
				ObjectUnselected?.Invoke(obj);

				obj.Selected = false;
				obj.OnUnselected();
			}
			SELECTIONCHANGE();
		}

		public bool IsObjectSelected(IEditorType? editorObj)
			=> editorObj == null ? false : __selectedObjects.Contains(editorObj);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t">The type all objects share</param>
		/// <returns>If objects are selected, and if they all share the same type</returns>
		public bool AreAllSelectedObjectsTheSameType([NotNullWhen(true)] out Type? t) {
			t = null;

			if (__selectedObjectsL.Count <= 0)
				return false;

			foreach (var obj in __selectedObjectsL) {
				if (t == null)
					t = obj.GetType();
				else if (t != obj.GetType()) {
					t = null;
					return false;
				}
			}

			return t != null; // we could return true here but the compiler isn't recognizing that t would not be null.
		}


		public static ModelEditor Active;


		public static ConVar modeleditor_activetab = ConVar.Register("modeleditor_activetab", "", ConsoleFlags.Saved, "Active model editor tab");
		public Panel SetupPanel;
		public EditorPanel Editor;
		public OutlinerPanel Outliner;
		public WeightsPanel Weights;
		public PropertiesPanel Properties;
		public Button SwitchMode;

		public EditorFile File = new();

		public PreUIDeterminations GetDeterminations() {
			PreUIDeterminations determinations = new();

			var count = SelectedObjectsCount;
			var last = LastSelectedObject;
			if (AreAllSelectedObjectsTheSameType(out Type? type)) {
				determinations.OnlySelectedOne = count == 1;
				determinations.AllShareAType = true;
				determinations.SharedType = type;
			}

			determinations.First = FirstSelectedObject;
			determinations.Last = LastSelectedObject;
			determinations.Count = SelectedObjectsCount;
			determinations.Selected = __selectedObjectsL.ToArray();

			return determinations;
		}

		public bool TryGetFirstSelected([NotNullWhen(true)] out IEditorType? editorObj) {
			editorObj = FirstSelectedObject;
			return editorObj != null;
		}
		public bool TryGetLastSelected(out IEditorType? editorObj) {
			editorObj = LastSelectedObject;
			return editorObj != null;
		}

		public bool AnimationMode { get; private set; } = false;
		public delegate void ModelEditorChangedSetupAnimateModeD(ModelEditor editor, bool animationMode);
		public event ModelEditorChangedSetupAnimateModeD? SetupAnimateModeChanged;
		public void ToggleModes() {
			AnimationMode = !AnimationMode;
			if (AnimationMode) {
				SwitchMode.Text = "Animate Mode";
			}
			else {
				SwitchMode.Text = "Setup Mode";
			}

			foreach (var model in File.Models) {
				foreach (var bone in model.GetAllBones()) bone.ResetToSetupPose();
				foreach (var slot in model.Slots) slot.ResetToSetupPose();
			}

			SetupAnimateModeChanged?.Invoke(this, AnimationMode);
		}

		public override void Initialize(params object[] args) {
			//EngineCore.ShowDebuggingInfo = true;

			Active = this;
			Menubar menubar = UI.Add<Menubar>();
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new ModelEditor()));
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Z], () => Actions.Undo());
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Y], () => Actions.Redo());

			UI.Add(out SetupPanel);
			SetupPanel.Dock = Dock.Fill;
			SetupPanel.DrawPanelBackground = false;

			var rightSide = SetupPanel.Add<ResizablePanel>();
			rightSide.Dock = Dock.Right;
			rightSide.CanResizeTop = false;
			rightSide.CanResizeBottom = false;
			rightSide.CanResizeRight = false;
			rightSide.Size = new(464, 0);
			rightSide.DrawPanelBackground = false;

			var tabOptions = rightSide.Add<TabView>();
			tabOptions.Dock = Dock.Fill;

			var OutlinerTab = tabOptions.AddTab("Outliner");
			var StatisticsTab = tabOptions.AddTab("Statistics");
			var WeightsTab = tabOptions.AddTab("Weights");

			OutlinerTab.Panel.Add(out Properties);
			Properties.Size = new(64);
			Properties.Dock = Dock.Bottom;
			Properties.DrawPanelBackground = true;
			Properties.BackgroundColor = new Raylib_cs.Color(5, 7, 12, 200);

			OutlinerTab.Panel.Add(out Outliner);
			Outliner.Dock = Dock.Fill;

			WeightsTab.Panel.Add(out Weights);
			Weights.Dock = Dock.Fill;

			OutlinerTab.Panel.Add(out Outliner);
			Outliner.Dock = Dock.Fill;

			SetupPanel.Add(out Editor);
			Editor.Dock = Dock.Fill;

			UI.Add(out SwitchMode);
			SwitchMode.Position = new Vector2F(8) + new Vector2F(0, 36);
			SwitchMode.Size = new(128, 32);
			SwitchMode.TextSize = 20;
			SwitchMode.Text = "Setup Mode";
			SwitchMode.MouseReleaseEvent += (_, _, _) => ToggleModes();

			Outliner.NodeClicked += Outliner_NodeClicked;
			File.NewFile();

			Keybinds.AddKeybind([KeyboardLayout.USA.Delete], AttemptDelete);
			Keybinds.AddKeybind([KeyboardLayout.USA.F2], () => AttemptRename());
			Keybinds.AddKeybind([KeyboardLayout.USA.Escape], () => {
				if (File.ActiveOperator != null)
					File.DeactivateOperator(true);
				else {
					if (LastSelectedObject?.OnUnselected() ?? false)
						UnselectAllObjects();
				}

			});
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.S], () => SaveTest());
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.O], () => OpenTest());

			File.OperatorActivated += File_OperatorActivated;
			File.OperatorDeactivated += File_OperatorDeactivated;
			File.Cleared += File_Cleared;

			tabOptions.BindTabNameToConVar(modeleditor_activetab);
		}

		private void File_Cleared(EditorFile file) {
			UnselectAllObjects();
		}

		private void File_OperatorActivated(EditorFile self, Operator op) {
			op.ModifyEditor(this);
		}

		private void File_OperatorDeactivated(EditorFile self, Operator op, bool canceled) {
			op.RestoreEditor(this);
		}

		private string testPath => Filesystem.Resolve("test.bondsmodel", "game", false);
		private void SaveTest() {
			Logs.Info("SaveTest: executing");
			System.IO.File.WriteAllText(testPath, File.Serialize());
			Logs.Info("SaveTest: success");
		}
		private void OpenTest() {
			Logs.Info("OpenTest: executing");
			File.Deserialize(System.IO.File.ReadAllText(testPath));
			Active.Editor.CameraX = File.CameraX;
			Active.Editor.CameraY = File.CameraY;
			Active.Editor.CameraZoom = File.CameraZoom;

			Logs.Info("OpenTest: success");
		}

		private void AttemptRename(IEditorType? item = null) {
			var determinations = GetDeterminations();

			if (item == null && determinations.Count != 1)
				return;

			// This is... a weird way of doing it...
			// but I want to minimize how much code we write here...

			var foundItem = item ?? determinations.Last ?? throw new Exception();
			string typeName = foundItem.SingleName;
			string currentName = foundItem.GetName();

			// cannot rename
			if (!foundItem.CanRename())
				return;

			EditorDialogs.TextInput(
				$"Rename {typeName.CapitalizeFirstCharacter()}",
				$"Enter the new name for this {typeName}",
				currentName,
				true,
				(text) => {
					// No rename will be performed
					if (text == currentName)
						return;

					if (!foundItem.IsNameTaken(text)) {
						foundItem.Rename(text);
					}
					else {
						EditorDialogs.ConfirmAction(
							"Bad name",
							$"A {typeName} named '{text}' already exists. Choose a different name.",
							true,
							() => AttemptRename(item ?? determinations.Last)
						);
					}
				},
				null
			);
		}

		private bool CanDelete(IEditorType? item) {
			if (item == null)
				return false;

			return item.CanDelete();
		}
		private void AttemptDelete() {
			var determinations = GetDeterminations();

			if (determinations.Count == 0)
				return;

			var plural = determinations.Count > 1;
			if (!plural && !CanDelete(determinations.Last))
				return;

			var text = "";

			if (determinations.AllShareAType)
				text = plural ? determinations.Last?.PluralName : $"{determinations.Last?.SingleName} '{determinations.Last?.GetName() ?? "<NULL NAME>"}'";
			else
				text = "items";


			if (!string.IsNullOrWhiteSpace(text)) {
				EditorDialogs.ConfirmAction(
					$"Remove {text}",
					$"Are you sure you want to remove {(plural ? $"these {text}" : $"the {text}")}?",
					true,
					() => {
						foreach (var item in determinations.Selected) {
							item.Remove();
						}
					}
				);
			}
		}

		private void Outliner_NodeClicked(OutlinerPanel panel, OutlinerNode node, MouseButton btn) {
			var o = node.GetRepresentingObject();
			if (o == null) return;
			SelectObject(o);
		}
	}

	internal class Program
	{
		static void Main(string[] args) {
			EngineCore.Initialize(1600, 900, "Model v4 Editor", args);
			EngineCore.GameInfo = new() {
				GameName = "Model v4 Editor"
			};
			EngineCore.LimitFramerate(60);

			EngineCore.LoadLevel(new ModelEditor());
			EngineCore.Start();
		}
	}
}
