using Nucleus.Commands;
using Nucleus.Common.Engine;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.NewEngine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.ModelEditor;

[Nucleus.MarkForStaticConstruction]
public static class ModelEditor_RecentFiles
{
	public struct ModelEditor_RecentFile
	{
		public string Path;
		public DateTime Opened;
	}
	public static ConVar modeleditor_maxrecentfiles = new("ModelEditor_maxrecentfiles", "24", FCvar.Saved, "How many recent files can be available before the least-recent file is forgotten about.", 0, null);
	private static ModelEditor_RecentFile[] recentfiles() {
		ModelEditor_RecentFile[] files = Host.GetDataStore<ModelEditor_RecentFile[]>("ModelEditor.RecentFiles") ?? [];
		return files;
	}
	private static void compilefiles(ModelEditor_RecentFile[] array) {
		Host.SetDataStore("ModelEditor.RecentFiles", array);
	}
	public static ModelEditor_RecentFile[] GetRecentFiles() {
		return recentfiles();
	}
	public static void PushRecentFile(string file) {
		List<ModelEditor_RecentFile> files = recentfiles().ToList();
		files.RemoveAll(x => x.Path == file);

		files.Insert(0, new() {
			Path = file,
			Opened = DateTime.Now
		});
		while (files.Count > modeleditor_maxrecentfiles.GetInt()) {
			files.RemoveAt(modeleditor_maxrecentfiles.GetInt() - 1);
		}
		compilefiles(files.ToArray());
	}
}
public class OutlinerAndProperties : View
{
	public override string Name => "Outliner";
	public OutlinerPanel Outliner;
	public PropertiesPanel Properties;

	public OutlinerAndProperties(Element parent) : base(parent) {
		Properties = new(this);
		Properties.SetSize(new(64));
		Properties.SetDock(Dock.Bottom);
		Properties.SetPaintBackgroundEnabled(true);
		Properties.SetBgColor(new Color(5, 7, 12, 200));

		Outliner = new(this);
		Outliner.SetDock(Dock.Fill);
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
			SwitchMode.SetText("Animate Mode");
			View.SetActiveWorkspaceByName("Animate");

			UpdateModelAnimations();
		}
		else {
			SwitchMode.SetText("Setup Mode");
			View.SetActiveWorkspaceByName("Setup");

			foreach (var model in File.Models) {
				model.ResetToSetupPose();
			}
		}



		SetupAnimateModeChanged?.Invoke(this, AnimationMode);
	}

	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);
		ConsoleSystem.Draw(new() {
			Position = new(8, frameState.WindowHeight - 6),
			Anchor = Anchor.BottomLeft,
			DoNotRender = false,
			TextSize = 13,
			Parent = RootPanel
		});
	}

	public override void PreThink(ref FrameState frameState) {
		base.Think(frameState);
		if (CheckQueuedAnimationUpdate() && AnimationMode)
			UpdateModelAnimations();
	}


	private Animation? activeAnimation;
	private double start;
	private void UpdateModel(Panel animationsButtons, EditorModel model, out ModelData modelData, out ModelInstance modelInstance) {
		modelData = new RuntimeConverter().LoadModelFromEditor(model);
		modelInstance = modelData.Instantiate();

		animationsButtons.ClearChildren();
		animationsButtons.SetSize(new(256));
		foreach (var animation in modelData.Animations) {
			var playThisOne = new Button(animationsButtons);
			playThisOne.SetSize(new Vector2F(24));
			playThisOne.SetDock(Dock.Top);
			playThisOne.SetText($"{animation.Name}");
			var instance = modelInstance;
			playThisOne.OnButtonClick += (_, _) => {
				instance.SetToSetupPose();
				activeAnimation = animation;
				start = Curtime;
			};
		}
	}

	class RenderingPanel(Element parent, ModelEditor editor, ModelInstance instance, ModelData data) : Panel(parent)
	{
		Stopwatch profiler = new();
		public override void Paint(float w, float h) {
			//Surface.SetViewport(s.GetGlobalPosition(), s.RenderBounds.Size);

			if (editor.activeAnimation != null) {
				editor.activeAnimation.Apply(instance, (editor.Curtime - editor.start) * 0.3, 1f, MixBlendMode.First);
			}

			EngineCore.Window.BeginMode2D(new() {
				Offset = GetGlobalPosition().ToNumerics() + (GetRenderBounds().Size / 2).ToNumerics(),
				Target = new(-100, -355),
				Rotation = 0,
				Zoom = .55f
			});

			Raylib.DrawLineV(new(-w, 0), new(w, 0), Color.Red);
			Raylib.DrawLineV(new(0, -h), new(0, h), Color.Green);

			profiler.Reset();
			profiler.Start();
			instance.Render();
			profiler.Stop();

			EngineCore.Window.EndMode2D();

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
		}
	}

	public void MakeRuntimeTest(EditorModel model) {
		var window = new Window(RootPanel);
		window.SetSize(new(1280, 720));
		window.Center();
		window.Title = "Runtime Test";
		//window.MakePopup();

		var refresh = new Button(window);
		refresh.SetDock(Dock.Top);
		refresh.SetText("Refresh Data");

		var animations = new Panel(window);
		animations.SetDock(Dock.Left);
		animations.SetSize(new(256));

		ModelData data;
		ModelInstance instance;
		UpdateModel(animations, model, out data, out instance);
		refresh.OnButtonClick += (_, _) => UpdateModel(animations, model, out data, out instance);

		var renderingPanel = new RenderingPanel(window, this, instance, data);
		renderingPanel.SetDock(Dock.Fill);
	}

	public override void Initialize(params object[] args) {
		Active = this;
		Menubar menubar = new(RootPanel);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyR], () => EngineCore.LoadLevel(new ModelEditor()));
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyZ], () => Actions.Undo());
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyY], () => Actions.Redo());

		var file = menubar.AddButton("File");
		file.AddButton("New", null, File_New);
		file.AddButton("Open...", null, File_Open);

		file.AddSubMenu("Open Recent", null, (m) => {
			var files = ModelEditor_RecentFiles.GetRecentFiles();
			if (files.Length == 0)
				return false;

			foreach (var file in files) {
				m.AddButton(Path.GetFileName(file.Path), null, () => {
					deserializeFrom(file.Path);
				});
			}

			return true;
		});

		file.AddButton("Save...", null, File_Save);
		file.AddButton("Save as...", null, File_SaveAs);
		file.AddSeparator();
		file.AddButton("Export...", null, File_Export);
		file.AddSeparator();
		file.AddButton("Add a Model", null, File_AddModel);
		file.AddButton("Import Model from nm4src...", null, File_ImportModel);


		var tests = menubar.AddButton("Tests");
		tests.AddButton("Runtime Test", null, () => {
			MakeRuntimeTest(File.Models.First());
		});

		View = new(RootPanel);
		View.SetDock(Dock.Fill);

		{
			var setupWorkspace = View.AddWorkspace("Setup");

			Editor = setupWorkspace.AddView(new EditorPanel(View));
			var split = setupWorkspace.SplitApart(Dock.Right);
			split.SizePercentage = 0.25f;

			OutlinerAndProperties = split.Division.AddView(new OutlinerAndProperties(split.Division));

			Outliner = OutlinerAndProperties.Outliner;
			Properties = OutlinerAndProperties.Properties;
			Weights = split.Division.AddView(new WeightsPanel(split.Division));

			var animateWorkspace = View.CopyWorkspace("Setup", "Animate");

			var animationTools = animateWorkspace.SplitApart(Dock.Bottom);

			animationTools.SizePercentage = 0.33f;
			Dopesheet = animationTools.Division.AddView<DopesheetView>(p => new(p));
			Graph = animationTools.Division.AddView<GraphView>(p => new(p));

			var playback = animationTools.Division.SplitApart(Dock.Right);
			playback.SizePercentage = 0.2f;
			Playback = playback.Division.AddView<PlaybackView>(p => new(p));

			var splitAn = animateWorkspace.SplitApart(Dock.Right);
			splitAn.SizePercentage = 0.17f;
			Animations = splitAn.Division.AddView<AnimationsView>(p => new(p));
		}

		SwitchMode = new(Editor);
		SwitchMode.SetPos(new Vector2F(8));
		SwitchMode.SetSize(new(128, 32));
		SwitchMode.SetTextSize(20);
		SwitchMode.SetText("Setup Mode");
		SwitchMode.OnButtonClick += (_, _) => ToggleModes();

		Outliner.NodeClicked += Outliner_NodeClicked;
		File.NewFile();

		Keybinds.AddKeybind([ButtonCode.KeyDelete], AttemptDelete);
		Keybinds.AddKeybind([ButtonCode.KeyF2], () => AttemptRename());
		Keybinds.AddKeybind([ButtonCode.KeyEscape], () => {
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
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyS], File_Save);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyO], File_Open);

		File.OperatorActivated += File_OperatorActivated;
		File.OperatorDeactivated += File_OperatorDeactivated;
		File.Cleared += File_Cleared;

		// ToggleModes();

		SetupHooks();
		// MakeRuntimeTest(File.Models.First());
#nullable disable
		Platform.RegisterFileAssociation(NUCLEUS_MODEL4_SOURCE_EXT, "March.Nucleus.Model4Editor", "Nucleus Model4 Source File");
#nullable enable

		titleUpdate();
	}

	private void File_AddModel() {
		File.AddModel($"New Model #{File.Models.Count + 1}");
	}
	private void File_ImportModel() {
		string folderPath = createDefaultFolder();

		var result = Platform.OpenFileDialog("Open Nucleus Model4 Project", folderPath, [$"*.{NUCLEUS_MODEL4_SOURCE_EXT}"], "Nucleus Model4 Source File", false);
		if (!result.Cancelled) {
			// Load the file into a separate object
			EditorFile temp = new EditorFile();
			temp.Deserialize(System.IO.File.ReadAllText(result.Result));
			if (temp.Models.Count == 0) return;
			else {
				var importWindow = new Window(RootPanel);
				importWindow.Title = $"Import from {Path.GetFileName(result)}";
				importWindow.SetSize(new(800, 600));
				importWindow.MakePopup();

				var lbl = new Label(importWindow);
				lbl.SetText("Which model do you want to import?");
				lbl.SetDock(Dock.Top);

				var lbl2 = new Label(importWindow);
				lbl2.SetText("Close the window when done. Clicking an item will append it to the file.");
				lbl2.SetDock(Dock.Top);

				var models = new ListView(importWindow);
				models.SetDock(Dock.Fill);

				foreach (var mdl in temp.Models) {
					var fileItem = new ListViewItem(models);
					fileItem.SetText( mdl.Name ?? "<unnamed model>");
					fileItem.OnButtonClick += (_, _) => {
						File.AppendModel(mdl);
						fileItem.Remove();
						if (mdl.Name == null)
							AttemptRename(mdl);
					};
				}
			}
		}

		titleUpdate();
	}

	private void File_Export() {
		var window = new Window(RootPanel);
		window.SetSize(new(1280, 720));
		window.Center();
		window.Title = $"Export [{Model4System.MODEL_FORMAT_VERSION}]";
		window.MakePopup();

		var btnJson = new Button(window);
		btnJson.SetText("Export JSON [.nm4rj]");
		btnJson.SetDock(Dock.Top);
		btnJson.OnButtonClick += (_, _) => {
			var save = Platform.SaveFileDialog("Save Model4 Data", AppContext.BaseDirectory, [$"*.{ModelRefJSON.EXTENSION}"], "Model4 as Ref'd JSON");
			if (!save.Cancelled) {
				new ModelRefJSON().SaveModelToFile(save.Result, new RuntimeConverter().LoadModelFromEditor(File.Models.First()));
			}
			Logs.Info("Saved.");
			window.Remove();
		};

		var btnBinary = new Button(window);
		btnBinary.SetText("Export Binary [.nm4b]");
		btnBinary.SetDock(Dock.Top);
		btnBinary.OnButtonClick += (_, _) => {
			var save = Platform.SaveFileDialog("Save Model4 Data", AppContext.BaseDirectory, [$"*.{ModelBinary.EXTENSION}"], "Model4 as Binary Data");
			if (!save.Cancelled) {
				new ModelBinary().SaveModelToFile(save.Result, new RuntimeConverter().LoadModelFromEditor(File.Models.First()));
			}
			Logs.Info("Saved.");
			window.Remove();
		};
	}

	private string createDefaultFolder() {
		var modelsrc = filesystem.GetSearchPathID("modelsrc");
		if (!modelsrc.Any())
			filesystem.AddSearchPath("modelsrc", new DiskSearchPath(Path.Combine(AppContext.BaseDirectory, "modelsrc")));

		return (modelsrc.First() as DiskSearchPath)!.RootDirectory;
	}

	private void titleUpdate()
		=> EngineCore.SetWindowTitle($"Nucleus Model4 Editor - {(File.DiskPath == null ? "No active file" : $"Editing {Path.GetFileName(File.DiskPath)}")}");


	private void serializeTo(string? path, bool writePathToFile) {
		if (path == null) return;
		System.IO.File.WriteAllText(path, File.Serialize());
		if (writePathToFile) File.DiskPath = path;
		Logs.Debug("File write OK!");
		titleUpdate();
		ModelEditor_RecentFiles.PushRecentFile(path);
	}

	private void deserializeFrom(string? path) {
		if (path == null) return;
		File.Deserialize(System.IO.File.ReadAllText(path));
		Active.Editor.CameraX = File.CameraX;
		Active.Editor.CameraY = File.CameraY;
		Active.Editor.CameraZoom = File.CameraZoom;
		File.DiskPath = path;
		Logs.Info("File load OK!");
		titleUpdate();
		ModelEditor_RecentFiles.PushRecentFile(path);
	}

	private void File_SaveAs() {
		string folderPath = createDefaultFolder();
		var result = Platform.SaveFileDialog("Save Nucleus Model4 Project", folderPath, [$"*.{NUCLEUS_MODEL4_SOURCE_EXT}"], "Nucleus Model4 Source File");
		if (!result.Cancelled) {
			serializeTo(result.Result, true);
		}
	}

	private void File_Save() {
		if (File.DiskPath == null) {
			File_SaveAs();
			return;
		}

		serializeTo(File.DiskPath, true);
	}

	private void File_Open() {
		string folderPath = createDefaultFolder();

		var result = Platform.OpenFileDialog("Open Nucleus Model4 Project", folderPath, [$"*{NUCLEUS_MODEL4_SOURCE_EXT}"], "Nucleus Model4 Source File", false);
		if (!result.Cancelled)
			deserializeFrom(result.Result);

		titleUpdate();
	}

	private void File_New() {
		File.Clear();

		if (AnimationMode)
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

	public const string NUCLEUS_MODEL4_SOURCE_EXT = "nm4src";

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

				if (!foundItem.IsNameTaken(new(text))) {
					foundItem.Rename(new(text));
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

	private void Outliner_NodeClicked(OutlinerPanel panel, OutlinerNode node, ButtonCode btn) {
		var o = node.GetRepresentingObject();
		if (o == null) return;
		SelectObject(o);
	}
}

internal class Program : IGameDLL
{
	static void Main(string[] args) {
		CommandLineParser commandLine = new();
		commandLine.CreateCmdLine(Environment.CommandLine);

		IEngineAPI engineAPI = new EngineBuilder(commandLine)
			.WithComponent<IGameDLL, Program>()
			.WithStandardComponents()
			.Build();

		engineAPI.SetStartupInfo(new() {
			AppName = "Nucleus - Model v4 Editor",
			AppIdentifier = "com.github.marchc1.NucleusModelEditor",
			AppCreator = "March (github/marchc1)",
			AppURL = "https://github.com/marchc1/CloneDash",
			AppType = Nucleus.Types.AppType.Application
		});

		using ServiceLocatorScope locatorScope = new(engineAPI);
		engineAPI.Run();
	}
	public void Init() {
		EngineCore.LoadLevel(new ModelEditor());
	}
}
