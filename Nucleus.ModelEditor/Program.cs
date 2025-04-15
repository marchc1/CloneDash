using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Platform;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using Raylib_cs;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.ModelEditor
{
	public class OutlinerAndProperties : View
	{
		public override string Name => "Outliner";
		public OutlinerPanel Outliner;
		public PropertiesPanel Properties;

		protected override void Initialize() {
			base.Initialize();

			Add(out Properties);
			Properties.Size = new(64);
			Properties.Dock = Dock.Bottom;
			Properties.DrawPanelBackground = true;
			Properties.BackgroundColor = new Raylib_cs.Color(5, 7, 12, 200);

			Add(out Outliner);
			Outliner.Dock = Dock.Fill;
		}
	}
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
		private bool __queuedAnimationUpdate = false;
		public void QueueAnimationUpdate() => __queuedAnimationUpdate = true;
		public bool CheckQueuedAnimationUpdate() {
			if (__queuedAnimationUpdate) {
				__queuedAnimationUpdate = false;
				return true;
			}

			return false;
		}
		public bool CanInsertKeyframes() {
			if (!AnimationMode) return false; // block if in setup
			if (File.ActiveAnimation == null) return false; // block if no active animation
			if (File.Timeline.PlayDirection != 0) return false; // block if timeline playing

			return true;
		}

		private List<IKeyframe> __selectedKeyframes = [];
		private HashSet<IKeyframe> __selectedKeyframesHs = [];
		public IEnumerable<IKeyframe> SelectedKeyframes {
			get {
				foreach (var kfp in __selectedKeyframes)
					yield return kfp;
			}
		}
		public int SelectedKeyframesCount => __selectedKeyframes.Count;

		public delegate void SelectKeyframeDelegate(IKeyframe keyframe);
		public event SelectKeyframeDelegate? OnKeyframeSelected;
		public event SelectKeyframeDelegate? OnKeyframeUnselected;

		public bool KeyframesSelected => __selectedKeyframes.Count > 0;
		public bool IsKeyframeSelected(IKeyframe keyframe)
			=> __selectedKeyframesHs.Contains(keyframe);

		private bool selectKeyframe(IKeyframe keyframe) {
			if (__selectedKeyframesHs.Add(keyframe)) {
				__selectedKeyframes.Add(keyframe);
				return true;
			}
			return false;
		}

		private bool unselectKeyframe(IKeyframe keyframe) {
			if (__selectedKeyframesHs.Remove(keyframe)) {
				__selectedKeyframes.Remove(keyframe);
				return true;
			}
			return false;
		}

		public void SelectKeyframe(IKeyframe keyframe, bool multi = false) {
			if (!multi) UnselectAllKeyframes();
			if (selectKeyframe(keyframe))
				OnKeyframeSelected?.Invoke(keyframe);
		}

		public void UnselectKeyframe(IKeyframe keyframe) {
			if (unselectKeyframe(keyframe))
				OnKeyframeUnselected?.Invoke(keyframe);
		}

		public void SelectKeyframes(IEnumerable<IKeyframe> keyframes, bool multi = false) {
			if (!multi) UnselectAllKeyframes();

			foreach (var keyframe in keyframes)
				SelectKeyframe(keyframe);
		}

		public void UnselectKeyframes(IEnumerable<IKeyframe> keyframes) {
			foreach (var keyframe in keyframes)
				UnselectKeyframe(keyframe);
		}

		public void UnselectAllKeyframes() {
			foreach (var keyframe in __selectedKeyframes)
				OnKeyframeUnselected?.Invoke(keyframe);

			__selectedKeyframes.Clear();
			__selectedKeyframesHs.Clear();
		}



		public bool IsPropertyCurrentlyAnimatable(KeyframeProperty prop) {
			if (prop == KeyframeProperty.None) return false; // block if no property requested
			if (!CanInsertKeyframes()) return false; // see CanInsertKeyframes

			switch (prop) {
				case KeyframeProperty.Bone_Translation:
				case KeyframeProperty.Bone_Rotation:
				case KeyframeProperty.Bone_Scale:
				case KeyframeProperty.Bone_Shear:
				case KeyframeProperty.Bone_TransformMode:
					return LastSelectedObject is EditorBone;

				case KeyframeProperty.Slot_Attachment:
				case KeyframeProperty.Slot_Color:
					return LastSelectedObject is EditorSlot;

				default:
					return true;
			}
		}
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
					else {
						if (item != null)
							ActiveOperator.Selected(this, item);
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


		public ViewPanel View;
		public Panel SetupPanel;
		public EditorPanel Editor;
		public OutlinerPanel Outliner;
		public PropertiesPanel Properties;
		public OutlinerAndProperties OutlinerAndProperties;
		public WeightsPanel Weights;
		public AnimationsView Animations;
		public DopesheetView Dopesheet;
		public GraphView Graph;
		public PlaybackView Playback;
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
			// Cancel operator
			File.DeactivateOperator(true);

			AnimationMode = !AnimationMode;
			if (AnimationMode) {
				SwitchMode.Text = "Animate Mode";
				View.SetActiveWorkspaceByName("Animate");

				UpdateModelAnimations();
			}
			else {
				SwitchMode.Text = "Setup Mode";
				View.SetActiveWorkspaceByName("Setup");

				foreach (var model in File.Models) {
					model.ResetToSetupPose();
				}
			}



			SetupAnimateModeChanged?.Invoke(this, AnimationMode);
		}

		public override void PreThink(ref FrameState frameState) {
			base.Think(frameState);
			if (CheckQueuedAnimationUpdate() && AnimationMode)
				UpdateModelAnimations();
		}


		private Animation? activeAnimation;
		private double start;
		private void UpdateModel(Panel animationsButtons, EditorModel model, out ModelData modelData, out ModelInstance modelInstance) {
			modelData = new RuntimeViewer().LoadModelFromEditor(model);
			modelInstance = modelData.Instantiate();

			animationsButtons.ClearChildren();
			animationsButtons.Size = new(256);
			foreach (var animation in modelData.Animations) {
				var playThisOne = animationsButtons.Add<Button>();
				playThisOne.Size = new Vector2F(24);
				playThisOne.Dock = Dock.Top;
				playThisOne.Text = $"{animation.Name}";
				var instance = modelInstance;
				playThisOne.MouseReleaseEvent += (_, _, _) => {
					instance.SetToSetupPose();
					activeAnimation = animation;
					start = Curtime;
				};
			}
		}


		public void MakeRuntimeTest(EditorModel model) {
			var window = UI.Add<Window>();
			window.Size = new(1280, 720);
			window.Center();
			window.Title = "Runtime Test";
			//window.MakePopup();

			var refresh = window.Add<Button>();
			refresh.Dock = Dock.Top;
			refresh.Text = "Refresh Data";

			var animations = window.Add<Panel>();
			animations.Dock = Dock.Left;
			animations.Size = new(256);

			ModelData data;
			ModelInstance instance;
			UpdateModel(animations, model, out data, out instance);
			refresh.MouseReleaseEvent += (_, _, _) => UpdateModel(animations, model, out data, out instance);

			Stopwatch profiler = new();

			var renderingPanel = window.Add<Panel>();
			renderingPanel.Dock = Dock.Fill;
			renderingPanel.PaintOverride += (s, w, h) => {
				//Surface.SetViewport(s.GetGlobalPosition(), s.RenderBounds.Size);

				if (activeAnimation != null) {
					activeAnimation.Apply(instance, (Curtime - start) * File.Timeline.FPS);
				}

				Raylib.BeginMode2D(new() {
					Offset = s.GetGlobalPosition().ToNumerics() + (s.RenderBounds.Size / 2).ToNumerics(),
					Target = new(0, -82),
					Rotation = 0,
					Zoom = 2f
				});

				Raylib.DrawLineV(new(-w, 0), new(w, 0), Color.Red);
				Raylib.DrawLineV(new(0, -h), new(0, h), Color.Green);

				profiler.Reset();
				profiler.Start();
				instance.Render();
				profiler.Stop();

				Raylib.EndMode2D();

				string[] strings = [
					$"Model Name      : {instance.Data.Name}",
					$"    Bones       : {instance.Bones.Count}",
					$"    Slots       : {instance.Slots.Count}",
					$"    Skins       : {instance.Data.Skins.Count}",
					$"    Animations  : {instance.Data.Animations.Count}",
					$"    Render Time : {profiler.Elapsed.TotalMilliseconds:0.00}ms",
				];
				for (int i = 0; i < strings.Length; i++) {
					Graphics2D.DrawText(new(16, 16 + (i * 18)), strings[i], "Consolas", 16);
				}
				//Surface.ResetViewport();
			};
		}

		public override void Initialize(params object[] args) {

			Active = this;
			Menubar menubar = UI.Add<Menubar>();
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new ModelEditor()));
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Z], () => Actions.Undo());
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Y], () => Actions.Redo());

			var file = menubar.AddButton("File");
			file.AddButton("New", null, File_New);
			file.AddButton("Open...", null, File_Open);
			file.AddButton("Save...", null, File_Save);
			file.AddButton("Save as...", null, File_SaveAs);

			var tests = menubar.AddButton("Tests");
			tests.AddButton("Runtime Test", null, () => {
				MakeRuntimeTest(File.Models.First());
			});

			UI.Add(out View);
			View.Dock = Dock.Fill;

			{
				var setupWorkspace = View.AddWorkspace("Setup");

				Editor = setupWorkspace.AddView(View.Add<EditorPanel>());
				var split = setupWorkspace.SplitApart(Dock.Right);
				split.SizePercentage = 0.25f;

				OutlinerAndProperties = split.Division.AddView(split.Division.Add<OutlinerAndProperties>());

				Outliner = OutlinerAndProperties.Outliner;
				Properties = OutlinerAndProperties.Properties;
				Weights = split.Division.AddView(split.Division.Add<WeightsPanel>());

				var animateWorkspace = View.CopyWorkspace("Setup", "Animate");

				var animationTools = animateWorkspace.SplitApart(Dock.Bottom);

				animationTools.SizePercentage = 0.33f;
				Dopesheet = animationTools.Division.AddView<DopesheetView>();
				Graph = animationTools.Division.AddView<GraphView>();

				var playback = animationTools.Division.SplitApart(Dock.Right);
				playback.SizePercentage = 0.2f;
				Playback = playback.Division.AddView<PlaybackView>();

				var splitAn = animateWorkspace.SplitApart(Dock.Right);
				splitAn.SizePercentage = 0.17f;
				Animations = splitAn.Division.AddView<AnimationsView>();
			}

			Editor.Add(out SwitchMode);
			SwitchMode.Position = new Vector2F(8);
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
					if (LastSelectedObject?.OnUnselected() ?? false) {
						UnselectAllObjects();
					}

					if (KeyframesSelected) {
						UnselectAllKeyframes();
					}
				}

			});
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.S], () => SaveTest());
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.O], () => OpenTest());

			File.OperatorActivated += File_OperatorActivated;
			File.OperatorDeactivated += File_OperatorDeactivated;
			File.Cleared += File_Cleared;

			OpenTest();
			// ToggleModes();

			SetupHooks();
			// MakeRuntimeTest(File.Models.First());
		}

		private string createDefaultFolder() {
			var folderPath = Filesystem.Resolve("modelsrc", "game");
			Directory.CreateDirectory(folderPath);
			return folderPath;
		}

		private void titleUpdate() 
			=> EngineCore.SetWindowTitle($"Nucleus Model4 Editor - {(File.DiskPath == null ? "No active file" : $"Editing {Path.GetFileName(File.DiskPath)}")}");
		

		private void serializeTo(string? path, bool writePathToFile) {
			if (path == null) return;
			System.IO.File.WriteAllText(path, File.Serialize());
			if (writePathToFile) File.DiskPath = path;
			Logs.Debug("File write OK!");
			titleUpdate();
		}

		private void deserializeFrom(string? path) {
			if (path == null) return;
			File.Deserialize(System.IO.File.ReadAllText(path));
			Active.Editor.CameraX = File.CameraX;
			Active.Editor.CameraY = File.CameraY;
			Active.Editor.CameraZoom = File.CameraZoom;
			Logs.Info("File load OK!");
			titleUpdate();
		}

		private void File_SaveAs() {
			string folderPath = createDefaultFolder();
			var result = TinyFileDialogs.SaveFileDialog("Save Nucleus Model4 Project", folderPath, [NUCLEUS_MODEL4_SOURCE_EXT], "Nucleus Model4 Source File");
			if (!result.Cancelled) {
				serializeTo(result.Result, true);
			}
		}

		private void File_Save() {
			if(File.DiskPath == null) {
				File_SaveAs();
				return;
			}

			serializeTo(File.DiskPath, true);
		}

		private void File_Open() {
			string folderPath = createDefaultFolder();

			var result = TinyFileDialogs.OpenFileDialog("Open Nucleus Model4 Project", folderPath, [NUCLEUS_MODEL4_SOURCE_EXT], "Nucleus Model4 Source File", false);
			if (!result.Cancelled)
				deserializeFrom(result.Result);

			titleUpdate();
		}

		private void File_New() {
			File.Clear();
			
			if(AnimationMode)
				ToggleModes();
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
		private void SetupHooks() {
			Dopesheet.SetupHooks();
			File.Timeline.FrameChanged += (_, _) => UpdateModelAnimations();
			File.Timeline.FrameElapsed += (_, _) => UpdateModelAnimations();

			File.AnimationDeactivated += (_, _, _) => {
				foreach (var model in File.Models) {
					model.ResetToSetupPose();
				}
			};
		}

		private void UpdateModelAnimations() {
			if (!AnimationMode) return;

			foreach (var model in File.Models)
				if (model.ActiveAnimation != null)
					model.ActiveAnimation.Apply(File.Timeline.GetPlayhead());
		}

		public const string NUCLEUS_MODEL4_SOURCE_EXT = ".nm4src";
		private void SaveTest() {
			Logs.Info("SaveTest: executing");
			Logs.Info("SaveTest: success");
		}
		private void OpenTest() {
			Logs.Info("OpenTest: executing");

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
			EngineCore.Initialize(1800, 980, "Nucleus - Model v4 Editor", args);
			EngineCore.GameInfo = new() {
				GameName = "Nucleus - Model v4 Editor"
			};

			EngineCore.LoadLevel(new ModelEditor());
			EngineCore.Start();
		}
	}
}
