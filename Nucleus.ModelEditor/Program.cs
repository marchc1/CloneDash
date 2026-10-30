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
		Properties.Size = new(0);
		Properties.		Dock = Dock.Bottom;
		Properties.SetPaintBackgroundEnabled(true);
		Properties.SetBgColor(new Color(5, 7, 12, 200));

		Outliner = new(this);
		Outliner.		Dock = Dock.Fill;
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
			SelectKeyframe(keyframe, true);
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

	// Actions.Top when the file was last saved or loaded
	private object? savedTop;
	private bool forceUnsaved;
	public bool HasUnsavedChanges => forceUnsaved || Actions.HasPending || Actions.Top != savedTop;
	private void MarkSaved() {
		savedTop = Actions.Top;
		forceUnsaved = false;
	}
	private Window? unsavedPrompt;
	private bool showedUnsaved;
	private Autosave autosave = null!;
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
					if (item == null) {
						if (!ActiveOperator.AcceptsEmptyClick)
							File.DeactivateOperator(true);
					}
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

	// Spacebar and escape deselect the Weights tool's vertices first.
	bool keysFocused => AnimationMode && (Dopesheet.Focused || Graph.Focused);
	bool dopesheetKeys => keysFocused && KeyframesSelected;

	private bool ClearWeightsSelection() {
		if (!Editor.InWeightsMode
			|| VertexWeightOperator.Target is not { SelectedVertices.Count: > 0 } target) return false;
		target.SelectedVertices.Clear();
		return true;
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


	public ViewHost View;
	public Panel SetupPanel;
	public EditorPanel Editor;
	public OutlinerPanel Outliner;
	public PropertiesPanel Properties;
	public OutlinerAndProperties OutlinerAndProperties;
	public WeightsPanel Weights;
	public AnimationsView Animations;
	public DopesheetView Dopesheet;
	public GraphView Graph;
	public GhostingView Ghosting;
	public PlaybackView Playback;
	public ModeLabel SwitchMode;

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
			SwitchMode.Text = "ANIMATE";
			View.SetMode("animate");

			// animations play over the setup pose
			foreach (var model in File.Models)
				model.ResetToSetupPose();
			UpdateModelAnimations();
		}
		else {
			SwitchMode.Text = "SETUP";
			View.SetMode("setup");

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

	// Selection history (page up/down) and groups (ctrl+1-9)
	readonly List<IEditorType[]> selectionHistory = [];
	int selectionHistoryAt = -1;
	readonly Dictionary<int, IEditorType[]> selectionGroups = [];

	/// <summary>
	/// Is this item still part of the file?
	/// </summary>
	public bool Exists(IEditorType item) => item switch {
		EditorBone bone => File.Models.Contains(bone.Model) && bone.Model.GetAllBones().Contains(bone),
		EditorSlot slot => Exists(slot.Bone) && slot.Bone.Model.Slots.Contains(slot),
		EditorAttachment attachment => Exists(attachment.Slot) && attachment.Slot.Attachments.Contains(attachment),
		EditorModel model => File.Models.Contains(model),
		EditorConstraint constraint =>
			File.Models.Contains(constraint.Model) && constraint.Model.GetAllConstraints().Contains(constraint),
		EditorEvent editorEvent =>
			File.Models.Contains(editorEvent.Model) && editorEvent.Model.Events.Contains(editorEvent),
		_ => false
	};

	private void RestoreSelection(IEditorType[] items) {
		UnselectAllObjects();
		List<IEditorType> alive = [];
		foreach (var item in items) {
			if (Exists(item))
				alive.Add(item);
		}
		if (alive.Count > 0)
			SelectObjects(alive.ToArray());
	}

	private void RecordSelection() {
		var now = __selectedObjectsL.ToArray();
		if (now.Length == 0) return;
		if (selectionHistoryAt >= 0 && selectionHistory[selectionHistoryAt].SequenceEqual(now)) return;

		selectionHistory.RemoveRange(selectionHistoryAt + 1, selectionHistory.Count - selectionHistoryAt - 1);
		selectionHistory.Add(now);
		if (selectionHistory.Count > 64)
			selectionHistory.RemoveAt(0);
		selectionHistoryAt = selectionHistory.Count - 1;
	}

	public void StepSelectionHistory(int by) {
		if (File.ActiveOperator != null || selectionHistory.Count == 0) return;
		selectionHistoryAt = Math.Clamp(selectionHistoryAt + by, 0, selectionHistory.Count - 1);
		RestoreSelection(selectionHistory[selectionHistoryAt]);
	}

	public void SetParentSelection() {
		switch (LastSelectedObject) {
			case ModelImage:
				File.InstantiateOperator<UI.Operators.ImageSetParentOperator>();
				break;
			case EditorBone { Parent: null }:
				break;
			case EditorBone or EditorSlot or EditorAttachment:
				File.InstantiateOperator<UI.Operators.SetParentOperator>();
				break;
		}
	}

	double frameNow => File.Timeline.Frame * File.Timeline.GetReferenceFPS();

	private List<double> KeyFrames() {
		if (File.ActiveAnimation is not EditorAnimation animation) return [];
		int fps = File.Timeline.GetReferenceFPS();
		HashSet<double> seen = [];
		List<double> frames = [];
		foreach (var timeline in animation.Timelines) {
			foreach (var time in timeline.GetKeyframeTimes()) {
				double frame = Math.Round(time * fps, 4);
				if (seen.Add(frame))
					frames.Add(frame);
			}
		}
		frames.Sort();
		return frames;
	}

	public void JumpToStart() {
		if (AnimationMode)
			File.Timeline.SetFrame(0);
	}

	public void JumpToEnd() {
		if (AnimationMode)
			File.Timeline.SetFrame(KeyFrames().LastOrDefault());
	}

	public void JumpKey(int dir) {
		if (!AnimationMode) return;
		double now = frameNow;
		var keys = KeyFrames();
		double? next = null;
		if (dir > 0) {
			for (int i = 0; i < keys.Count; i++) {
				if (keys[i] > now + 1e-4) {
					next = keys[i];
					break;
				}
			}
		}
		else {
			for (int i = keys.Count - 1; i >= 0; i--) {
				if (keys[i] < now - 1e-4) {
					next = keys[i];
					break;
				}
			}
		}
		if (next is double frame)
			File.Timeline.SetFrame(frame);
	}

	public void StepFrames(int by) {
		if (AnimationMode)
			File.Timeline.SetFrame(Math.Round(frameNow) + by);
	}

	public void ToggleSelectionVisibility() {
		List<IEditorType> items = [];
		foreach (var selected in SelectedObjects) {
			if (selected.CanHide() || selected is EditorAttachment)
				items.Add(selected);
		}
		if (items.Count == 0) return;
		bool hide = items.Any(x => x.GetVisible());
		foreach (var item in items) {
			if (item.GetVisible() == hide) {
				if (hide)
					File.HideEditorItem(item);
				else
					File.ShowEditorItem(item);
			}
		}
	}

	public void ShowAllHidden() {
		foreach (var model in File.Models) {
			foreach (var bone in model.GetAllBones()) {
				if (bone.Hidden)
					File.ShowEditorItem(bone);
			}
			foreach (var slot in model.Slots) {
				if (slot.Hidden)
					File.ShowEditorItem(slot);
			}
		}
	}

	public void MoveSelectedSlots(int by) {
		HashSet<EditorSlot> seen = [];
		List<EditorSlot> slots = [];
		foreach (var selected in SelectedObjects) {
			var selectedSlot = selected as EditorSlot ?? (selected as EditorAttachment)?.Slot;
			if (selectedSlot != null && seen.Add(selectedSlot))
				slots.Add(selectedSlot);
		}
		if (slots.Count == 0) return;
		// front-most first when moving up
		foreach (var slot in slots.OrderBy(s => IndexOf(s) * -Math.Sign(by)))
			File.SetSlotDrawOrder(slot, IndexOf(slot) + by);
	}

	private int IndexOf(EditorSlot slot) =>
		(AnimationMode && File.ActiveAnimation != null ? slot.Bone.Model.DrawOrder : slot.Bone.Model.Slots).IndexOf(slot);

	public void NudgeSelection(int x, int y, float step) {
		if (!SelectedObjects.Any()) return;
		switch (Editor.DefaultOperatorType) {
			case EditorDefaultOperator.RotateSelection:
				File.RotateSelected((x + y) * step, true);
				break;
			case EditorDefaultOperator.TranslateSelection:
				if (x != 0)
					File.TranslateXSelected(x * step, true);
				if (y != 0)
					File.TranslateYSelected(y * step, true);
				break;
			case EditorDefaultOperator.ScaleSelection:
				if (x != 0)
					File.ScaleXSelected(x * step * 0.01f, true);
				if (y != 0)
					File.ScaleYSelected(y * step * 0.01f, true);
				break;
			case EditorDefaultOperator.ShearSelection:
				if (x != 0)
					File.ShearXSelected(x * step, true);
				if (y != 0)
					File.ShearYSelected(y * step, true);
				break;
		}
	}

	public void SaveSelectionGroup(int group) {
		if (__selectedObjectsL.Count == 0)
			selectionGroups.Remove(group);
		else
			selectionGroups[group] = __selectedObjectsL.ToArray();
	}

	public void RecallSelectionGroup(int group) {
		if (File.ActiveOperator == null && selectionGroups.TryGetValue(group, out var items))
			RestoreSelection(items);
	}

	public override void PreThink(ref FrameState frameState) {
		base.Think(frameState);
		RecordSelection();
		Actions.Think(frameState.Mouse.MouseHeld);
		autosave.Think(Curtime, frameState.Mouse.MouseHeld);
		if (showedUnsaved != HasUnsavedChanges)
			TitleUpdate();
		foreach (var model in File.Models) {
			if (model.Images.TakeDiskChange(Curtime))
				File.RescanModelImages(model);
		}
		if (CheckQueuedAnimationUpdate() && AnimationMode)
			UpdateModelAnimations();
	}


	private Animation? activeAnimation;
	private double start;
	private void UpdateModel(Panel animationsButtons, EditorModel model, out ModelData modelData, out ModelInstance modelInstance) {
		modelData = new RuntimeConverter().LoadModelFromEditor(model);
		modelInstance = modelData.Instantiate();

		animationsButtons.ClearChildren();
		animationsButtons.		Size = new(256);
		foreach (var animation in modelData.Animations) {
			var playThisOne = new Button(animationsButtons);
			playThisOne.			Size = new Vector2F(24);
			playThisOne.			Dock = Dock.Top;
			playThisOne.			Text = $"{animation.Name}";
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
		window.		Size = new(1280, 720);
		window.Center();
		window.Title = "Runtime Test";
		//window.MakePopup();

		var refresh = new Button(window);
		refresh.		Dock = Dock.Top;
		refresh.		Text = "Refresh Data";

		var animations = new Panel(window);
		animations.		Dock = Dock.Left;
		animations.		Size = new(256);

		ModelData data;
		ModelInstance instance;
		UpdateModel(animations, model, out data, out instance);
		refresh.OnButtonClick += (_, _) => UpdateModel(animations, model, out data, out instance);

		var renderingPanel = new RenderingPanel(window, this, instance, data);
		renderingPanel.		Dock = Dock.Fill;
	}

	public override void Initialize(params object[] args) {
		Active = this;
		Menubar menubar = new(RootPanel);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyLeftShift, ButtonCode.KeyF5], () => EngineCore.LoadLevel(new ModelEditor()));
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyZ], () => Actions.Undo());
		Keybinds.AddKeybind([ButtonCode.KeyHome], () => Editor.FitToView());
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyY], () => Actions.Redo());
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyLeftShift, ButtonCode.KeyZ], () => Actions.Redo());

		var file = menubar.AddButton("File");
		file.AddButton("New", null, File_New);
		file.AddButton("Open...", null, File_Open);

		file.AddSubMenu("Open Recent", null, (m) => {
			var files = ModelEditor_RecentFiles.GetRecentFiles();
			if (files.Length == 0)
				return false;

			foreach (var file in files) {
				m.AddButton(Path.GetFileName(file.Path), null, () => {
					ConfirmDiscard("opening another file", () => deserializeFrom(file.Path));
				});
			}

			return true;
		});

		file.AddButton("Save...", null, File_Save);
		file.AddButton("Save as...", null, File_SaveAs);
		file.AddSeparator();
		file.AddButton("Export...", null, File_Export);
		file.AddSeparator();
		file.AddButton("New Model", null, File_AddModel);
		file.AddButton("Import Model from nm4src...", null, File_ImportModel);


		var tests = menubar.AddButton("Tests");
		tests.AddButton("Runtime Test", null, () => {
			MakeRuntimeTest(File.Models.First());
		});

		View = new(RootPanel);
		View.		Dock = Dock.Fill;

		{
			Editor = new EditorPanel(View.Stash);
			View.SetViewport(Editor);

			OutlinerAndProperties = View.Register("outliner", new OutlinerAndProperties(View.Stash), "models/tree.png",
				ButtonCode.KeyT, true, true);
			Outliner = OutlinerAndProperties.Outliner;
			Properties = OutlinerAndProperties.Properties;
			Weights = View.Register("weights", new WeightsPanel(View.Stash), "models/weights.png",
				ButtonCode.KeyW, true, true);
			Animations = View.Register("animations", new AnimationsView(View.Stash), "models/animation.png",
				ButtonCode.KeyA, false, true);
			Dopesheet = View.Register("dopesheet", new DopesheetView(View.Stash), "models/keyframe.png",
				ButtonCode.KeyD, false, true, Dock.Bottom);
			Graph = View.Register("graph", new GraphView(View.Stash), "models/curve_bezier.png",
				ButtonCode.KeyG, false, true, Dock.Bottom);
			Ghosting = View.Register("ghosting", new GhostingView(View.Stash), "models/viseye.png",
				null, false, true, Dock.Bottom);
			Playback = View.Register("playback", new PlaybackView(View.Stash), "models/playForward.png",
				ButtonCode.KeyP, false, true, Dock.Bottom);

			View.AddMode("setup", "h0.75([viewport],[outliner*,weights])");
			View.AddMode("animate",
				"h0.75(v0.67(h0.83([viewport],[animations*]),h0.8([dopesheet*,graph],[playback*])),[outliner*,weights])");
			View.Load();

			var viewsBar = new ViewsBar(menubar, View);
			viewsBar.Dock = Dock.Right;
			viewsBar.Size = new(400, 0);

			foreach (var info in View.Infos) {
				if (info.Key is ButtonCode key) {
					var id = info.Id;
					Keybinds.AddKeybind([ButtonCode.KeyLeftAlt, key], () => View.Open(id));
				}
			}
			Keybinds.AddKeybind([ButtonCode.KeyF9], () => View.ToggleMinimizeAll());
		}

		SwitchMode = new(Editor);
		SwitchMode.		Position = new Vector2F(8);
		SwitchMode.Size = new(200, 48);
		SwitchMode.Text = "SETUP";
		SwitchMode.OnButtonClick += (_, _) => ToggleModes();

		Outliner.NodeClicked += Outliner_NodeClicked;
		Outliner.NodeDoubleClicked += Outliner_NodeDoubleClicked;
		File.NewFile();

		Keybinds.AddKeybind([ButtonCode.KeyDelete], () => {
			if (dopesheetKeys)
				Dopesheet.DeleteSelected();
			else
				AttemptDelete();
		});
		Keybinds.AddKeybind([ButtonCode.KeyF2], () => AttemptRename());
		Keybinds.AddKeybind([ButtonCode.KeyEscape], () => {
			if (Menu.CloseOpen())
				return;
			if (File.ActiveOperator != null)
				File.DeactivateOperator(true);
			else if (!ClearWeightsSelection()) {
				if (LastSelectedObject?.OnUnselected() ?? false) {
					UnselectAllObjects();
				}

				if (KeyframesSelected) {
					UnselectAllKeyframes();
				}
			}

		});
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyS], File_Save);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyN], File_AddModel);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyO], File_Open);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyE], File_Export);
		Keybinds.AddKeybind([ButtonCode.KeyK], () => {
			if (AnimationMode && File.ActiveOperator == null)
				File.KeyEdited();
		}, true);
		Tool(ButtonCode.KeyB, EditorDefaultOperator.PoseBoneToTarget);
		Tool(ButtonCode.KeyN, EditorDefaultOperator.CreateNewBones);
		Tool(ButtonCode.KeyG, EditorDefaultOperator.ChangeMeshWeights);
		Tool(ButtonCode.KeyC, EditorDefaultOperator.RotateSelection);
		Tool(ButtonCode.KeyV, EditorDefaultOperator.TranslateSelection);
		Tool(ButtonCode.KeyX, EditorDefaultOperator.ScaleSelection);
		Tool(ButtonCode.KeyZ, EditorDefaultOperator.ShearSelection);
		Bind([ButtonCode.KeyP], () => {
			if (!AnimationMode)
				SetParentSelection();
		});

		// Playback
		Bind([ButtonCode.KeyD], () => {
			if (AnimationMode)
				File.Timeline.TogglePlayForwards();
		});
		Bind([ButtonCode.KeyA], () => {
			if (AnimationMode)
				File.Timeline.TogglePlayBackwards();
		});
		Bind([ButtonCode.KeyQ], JumpToStart);
		Bind([ButtonCode.KeyE], JumpToEnd);
		Bind([ButtonCode.KeyW], () => JumpKey(1));
		Bind([ButtonCode.KeyS], () => JumpKey(-1));
		Bind([ButtonCode.KeyR], () => StepFrames(1));
		Bind([ButtonCode.KeyF], () => StepFrames(-1));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyR], () => StepFrames(10));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyF], () => StepFrames(-10));
		Bind([ButtonCode.KeyLeftControl, ButtonCode.KeyR], () => File.Timeline.Repeat = !File.Timeline.Repeat);

		// Keying
		Bind([ButtonCode.KeyL], () => {
			KeyframeProperty? property = Editor.DefaultOperatorType switch {
				EditorDefaultOperator.RotateSelection => KeyframeProperty.Bone_Rotation,
				EditorDefaultOperator.TranslateSelection => KeyframeProperty.Bone_Translation,
				EditorDefaultOperator.ScaleSelection => KeyframeProperty.Bone_Scale,
				EditorDefaultOperator.ShearSelection => KeyframeProperty.Bone_Shear,
				_ => null
			};
			if (property is KeyframeProperty keyframeProperty)
				File.KeySelectedBones(keyframeProperty);
		});
		Bind([ButtonCode.KeyLeftControl, ButtonCode.KeyL], () => File.KeySelectedBones(KeyframeProperty.Bone_Rotation,
			KeyframeProperty.Bone_Translation, KeyframeProperty.Bone_Scale, KeyframeProperty.Bone_Shear));

		// Visibility
		Bind([ButtonCode.KeyH], ToggleSelectionVisibility);
		Bind([ButtonCode.KeyLeftControl, ButtonCode.KeyH], ShowAllHidden);

		// Draw order
		Bind([ButtonCode.KeyEqual], () => MoveSelectedSlots(1));
		Bind([ButtonCode.KeyPadPlus], () => MoveSelectedSlots(1));
		Bind([ButtonCode.KeyMinus], () => MoveSelectedSlots(-1));
		Bind([ButtonCode.KeyPadMinus], () => MoveSelectedSlots(-1));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyEqual], () => MoveSelectedSlots(5));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyPadPlus], () => MoveSelectedSlots(5));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyMinus], () => MoveSelectedSlots(-5));
		Bind([ButtonCode.KeyLeftShift, ButtonCode.KeyPadMinus], () => MoveSelectedSlots(-5));

		// Arrows change the active tool's value
		(ButtonCode Key, int X, int Y)[] arrows = [
			(ButtonCode.KeyLeft, -1, 0),
			(ButtonCode.KeyRight, 1, 0),
			(ButtonCode.KeyUp, 0, 1),
			(ButtonCode.KeyDown, 0, -1)
		];
		foreach (var arrow in arrows) {
			Bind([arrow.Key], () => NudgeSelection(arrow.X, arrow.Y, 1));
			Bind([ButtonCode.KeyLeftShift, arrow.Key], () => NudgeSelection(arrow.X, arrow.Y, 0.1f));
		}

		Keybinds.AddKeybind([ButtonCode.KeySpace], () => {
			if (File.ActiveOperator is EditMeshOperator or EditClippingOperator or BindOperator)
				File.DeactivateOperator(false);
			else if (File.ActiveOperator == null && !ClearWeightsSelection())
				UnselectAllObjects();
		}, true);

		// Weights tool
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyA], () => {
			if (dopesheetKeys) {
				if (Graph.Focused)
					Graph.SelectAllKeys();
				else
					Dopesheet.SelectAllKeys();
			}
			else if (Editor.InWeightsMode)
				VertexWeightOperator.SelectAll();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyC], () => {
			if (dopesheetKeys)
				Dopesheet.CopySelected();
			else if (Editor.InWeightsMode)
				VertexWeightOperator.Copy();
			else
				PoseClipboard.Copy();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyX], () => {
			if (dopesheetKeys)
				Dopesheet.CutSelected();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyV], () => {
			if (keysFocused && Dopesheet.Clipboard != null)
				Dopesheet.Paste();
			else if (Editor.InWeightsMode)
				VertexWeightOperator.Paste();
			else
				PoseClipboard.Paste();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyLeftShift, ButtonCode.KeyV], () => {
			if (!Editor.InWeightsMode)
				PoseClipboard.Paste(true);
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftShift, ButtonCode.KeyDelete], () => {
			if (dopesheetKeys)
				Dopesheet.CutSelected();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyInsert], () => {
			if (dopesheetKeys)
				Dopesheet.CopySelected();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyLeftShift, ButtonCode.KeyInsert], () => {
			if (keysFocused && Dopesheet.Clipboard != null)
				Dopesheet.Paste();
		}, true);
		Keybinds.AddKeybind([ButtonCode.KeyPageUp], () => StepSelectionHistory(-1), true);
		Keybinds.AddKeybind([ButtonCode.KeyPageDown], () => StepSelectionHistory(1), true);
		for (int i = 1; i <= 9; i++) {
			int group = i;
			var key = (ButtonCode)((int)ButtonCode.Key0 + i);
			Keybinds.AddKeybind([ButtonCode.KeyLeftControl, key], () => SaveSelectionGroup(group), true);
			Keybinds.AddKeybind([key], () => RecallSelectionGroup(group), true);
		}

		File.OperatorActivated += File_OperatorActivated;
		File.OperatorDeactivated += File_OperatorDeactivated;
		File.Cleared += File_Cleared;

		// ToggleModes();

		SetupHooks();
		autosave = new(this);
		// MakeRuntimeTest(File.Models.First());
#nullable disable
		Platform.RegisterFileAssociation(NUCLEUS_MODEL4_SOURCE_EXT, "March.Nucleus.Model4Editor", "Nucleus Model4 Source File");
#nullable enable

		TitleUpdate();
		autosave.OfferRecovery(RootPanel);
	}

	private void Bind(ButtonCode[] keys, Action action) => Keybinds.AddKeybind([.. keys], () => {
		if (File.ActiveOperator == null)
			action();
	}, true);

	private void Tool(ButtonCode key, EditorDefaultOperator tool) => Keybinds.AddKeybind([key], () => {
		if (File.ActiveOperator == null)
			Editor.SetEditorOperator(tool);
	}, true);

	public override void PreWindowClose() {
		if (!HasUnsavedChanges) {
			autosave.Delete(File.DiskPath);
			return;
		}
		EngineCore.BlockClosure();
		ConfirmDiscard("closing", () => Quit(false));
	}

	// closed on purpose
	public override void OnUnload() => autosave.Delete(File.DiskPath);

	public void Quit(bool askToSave = true) {
		if (askToSave) {
			EngineCore.Close();
			return;
		}
		autosave.Delete(File.DiskPath);
		EngineCore.Close(true);
	}

	/// <summary>
	/// Runs <paramref name="then"/> once unsaved changes are saved or dropped.
	/// </summary>
	public void ConfirmDiscard(string doing, Action then) {
		if (!HasUnsavedChanges) {
			then();
			return;
		}
		if (IValidatable.IsValid(unsavedPrompt)) return;

		var name = File.DiskPath == null ? "this file" : Path.GetFileName(File.DiskPath);
		var window = unsavedPrompt = new Window(RootPanel);
		window.Title = "Unsaved changes";
		window.Size = new(560, 150);
		window.Center();
		window.MakePopup();

		var info = new Label(window);
		info.Dock = Dock.Top;
		info.Size = new(56);
		info.TextSize = 20;
		info.Text = $"Save changes to {name} before {doing}?";

		var row = new Panel(window);
		row.Dock = Dock.Top;
		row.Size = new(40);
		row.DockMargin = RectangleF.TLRB(4, 8, 8, 0);
		row.DockPadding = RectangleF.TLRB(4);
		row.BorderSize = 0;
		row.SetPaintBackgroundEnabled(false);

		Button("Cancel", window.Remove, row);
		Button("Don't Save", () => {
			window.Remove();
			then();
		}, row);
		Button("Save", () => {
			File_Save();
			window.Remove();
			if (!HasUnsavedChanges)
				then();
		}, row);
	}

	private static Button Button(string text, Action click, Panel row) {
		var button = new Button(row, text);
		button.Dock = Dock.Right;
		button.Size = new(130);
		button.DockMargin = RectangleF.TLRB(0, 4, 0, 0);
		button.OnButtonClick += (_, _) => click();
		return button;
	}

	public void Recover(Autosave.Leftover leftover) {
		autosave.Delete(File.DiskPath);
		File.Deserialize(System.IO.File.ReadAllText(leftover.AutosavePath), leftover.SourcePath);
		File.DiskPath = leftover.SourcePath != null && System.IO.File.Exists(leftover.SourcePath) ? leftover.SourcePath : null;
		Active.Editor.CameraX = File.CameraX;
		Active.Editor.CameraY = File.CameraY;
		Active.Editor.CameraZoom = File.CameraZoom;

		MarkSaved();
		forceUnsaved = true;
		System.IO.File.Delete(leftover.AutosavePath);
		autosave.Write();
		Logs.Info($"Recovered {leftover.SourcePath ?? "untitled file"} from {leftover.Time:g}.");
		TitleUpdate();
	}

	private void File_AddModel() {
		var name = "model";
		for (int i = 2; File.Models.Any(x => x.Name == name); i++)
			name = $"model{i}";
		if (File.AddModel(name).Result is EditorModel model)
			SelectObject(model);
	}
	private void File_ImportModel() {
		string folderPath = createDefaultFolder();

		var result = Platform.OpenFileDialog("Open Nucleus Model4 Project", folderPath, [$"*.{NUCLEUS_MODEL4_SOURCE_EXT}"], "Nucleus Model4 Source File", false);
		if (!result.Cancelled) {
			// Load the file into a separate object
			EditorFile temp = new EditorFile();
			temp.Deserialize(System.IO.File.ReadAllText(result.Result), result.Result);
			if (temp.Models.Count == 0) return;
			else {
				var importWindow = new Window(RootPanel);
				importWindow.Title = $"Import from {Path.GetFileName(result)}";
				importWindow.				Size = new(800, 600);
				importWindow.MakePopup();

				var lbl = new Label(importWindow);
				lbl.				Text = "Which model do you want to import?";
				lbl.				Dock = Dock.Top;

				var lbl2 = new Label(importWindow);
				lbl2.				Text = "Close the window when done. Clicking an item will append it to the file.";
				lbl2.				Dock = Dock.Top;

				var models = new ListView(importWindow);
				models.				Dock = Dock.Fill;

				foreach (var mdl in temp.Models) {
					var fileItem = new ListViewItem(models);
					fileItem.					Text = mdl.Name ?? "<unnamed model>";
					fileItem.OnButtonClick += (_, _) => {
						File.AppendModel(mdl);
						fileItem.Remove();
						if (mdl.Name == null)
							AttemptRename(mdl);
					};
				}
			}
		}

		TitleUpdate();
	}

	private void File_Export() => ExportDialog.Show(this);

	private string createDefaultFolder() {
		var modelsrc = filesystem.GetSearchPathID("modelsrc");
		if (!modelsrc.Any())
			filesystem.AddSearchPath("modelsrc", new DiskSearchPath(Path.Combine(AppContext.BaseDirectory, "modelsrc")));

		return (modelsrc.First() as DiskSearchPath)!.RootDirectory;
	}

	private void TitleUpdate() {
		showedUnsaved = HasUnsavedChanges;
		var unsaved = showedUnsaved ? "*" : "";
		var state = File.DiskPath == null
			? $"No active file{unsaved}"
			: $"Editing {Path.GetFileName(File.DiskPath)}{unsaved}";
		EngineCore.SetWindowTitle($"Nucleus Model4 Editor - {state}");
	}


	private void serializeTo(string? path, bool writePathToFile) {
		if (path == null) return;
		System.IO.File.WriteAllText(path, File.Serialize());
		autosave.Delete(File.DiskPath);
		if (writePathToFile) File.DiskPath = path;
		autosave.Delete(path);
		MarkSaved();
		Logs.Debug("File write OK!");
		TitleUpdate();
		ModelEditor_RecentFiles.PushRecentFile(path);
	}

	private void deserializeFrom(string? path) {
		if (path == null) return;
		autosave.Delete(File.DiskPath);
		File.Deserialize(System.IO.File.ReadAllText(path), path);
		MarkSaved();
		Active.Editor.CameraX = File.CameraX;
		Active.Editor.CameraY = File.CameraY;
		Active.Editor.CameraZoom = File.CameraZoom;
		File.DiskPath = path;
		Logs.Info("File load OK!");
		TitleUpdate();
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

	private void File_Open() => ConfirmDiscard("opening another file", OpenFile);

	private void OpenFile() {
		string folderPath = createDefaultFolder();

		var result = Platform.OpenFileDialog("Open Nucleus Model4 Project", folderPath, [$"*{NUCLEUS_MODEL4_SOURCE_EXT}"], "Nucleus Model4 Source File", false);
		if (!result.Cancelled)
			deserializeFrom(result.Result);

		TitleUpdate();
	}

	private void File_New() => ConfirmDiscard("starting a new file", NewFile);

	private void NewFile() {
		autosave.Delete(File.DiskPath);
		File.Clear();
		MarkSaved();
		TitleUpdate();

		if (AnimationMode)
			ToggleModes();
	}

	private void File_Cleared(EditorFile file) {
		UnselectAllObjects();
		Actions.Clear();
	}

	private void File_OperatorActivated(EditorFile self, Operator op) {
		op.ModifyEditor(this);
	}

	private void File_OperatorDeactivated(EditorFile self, Operator op, bool canceled) {
		op.RestoreEditor(this);
	}
	private void SetupHooks() {
		Dopesheet.SetupHooks();
		Graph.SetupHooks();
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

		foreach (var model in File.Models) {
			// constraints and deforms back to setup first
			model.ResetAnimatedValues();
			if (model.ActiveAnimation == null) continue;
			KeyframeSettings.InterpolationOverride = File.Timeline.Stepped ? KeyframeInterpolation.Constant : null;
			model.ActiveAnimation.Apply(File.Timeline.GetPlayhead());
			KeyframeSettings.InterpolationOverride = null;
		}
	}

	public const string NUCLEUS_MODEL4_SOURCE_EXT = "nm4src";

	public void AttemptRename(IEditorType? item = null) {
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
	public void DropImage(ModelImage image, Vector2F windowPos) => DropImages([image], windowPos);
	public void DropImages(IEnumerable<ModelImage> images, Vector2F windowPos) {
		List<IEditorType> made = [];
		foreach (var image in images) {
			var model = File.Models.FirstOrDefault(m => m.Images.ImageLookup.ContainsValue(image)) ?? File.Models.FirstOrDefault();
			var bone = model?.Root;
			if (bone == null) continue;

			var result = File.CreateRegionFromImage(image, bone, Editor.ScreenToGrid(windowPos - Editor.GetGlobalPosition()));
			if (result.Failed)
				Logs.Warn(result.Reason ?? "Couldn't add the image.");
			else
				made.Add(result.ResultOrThrow);
		}
		if (made.Count > 0) {
			UnselectAllObjects();
			SelectObjects([.. made]);
		}
	}

	/// <summary>
	/// The dragged image, or every selected image when it's part of the selection
	/// </summary>
	public List<ModelImage> DraggedImages(ModelImage image) => image.Selected ? [.. SelectedObjects.OfType<ModelImage>()] : [image];

	public void AttemptDelete() {
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

	OutlinerNode? lastTreeClick;
	// ctrl+click adds or removes a node, shift+click selects everything between
	// double-click renames, or makes a new one on the Animations/Skins nodes
	private void Outliner_NodeDoubleClicked(OutlinerPanel panel, OutlinerNode node) {
		if (File.ActiveOperator != null) return;
		switch (node.GetRepresentingObject()) {
			case AnimationsList animations:
				PropertiesPanel.NewAnimationDialog(File, animations);
				break;
			case SkinsList skins:
				PropertiesPanel.NewSkinDialog(File, skins.Model);
				break;
			case IEditorType item when item.CanRename():
				AttemptRename(item);
				break;
		}
	}

	private void Outliner_NodeClicked(OutlinerPanel panel, OutlinerNode node, ButtonCode btn) {
		var o = node.GetRepresentingObject();
		if (o == null) return;
		var keys = EngineCore.Level.FrameState.Keyboard;
		if (File.ActiveOperator == null && keys.ShiftDown && lastTreeClick != null && IValidatable.IsValid(lastTreeClick)) {
			var range = panel.NodesBetween(lastTreeClick, node);
			if (!keys.ControlDown)
				UnselectAllObjects();
			foreach (var rangeNode in range) {
				if (rangeNode.GetRepresentingObject() is not IEditorType item) continue;
				if (!item.Selected)
					SelectObject(item, true);
			}
			return;
		}
		lastTreeClick = node;
		if (File.ActiveOperator == null && keys.ControlDown) {
			if (o.Selected)
				UnselectObject(o);
			else
				SelectObject(o, true);
			return;
		}
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
