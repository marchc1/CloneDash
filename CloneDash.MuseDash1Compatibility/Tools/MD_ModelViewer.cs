using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using Color = Nucleus.Common.Types.Color;

namespace CloneDash.Tools;

public static class MD_ModelViewerCommand
{
	[ConCommand(Name: "md_modelviewer", Help: "Opens the MuseDash Spine model browser.")]
	public static void OpenModelViewer() {
		if (EngineCore.Level == null) {
			Logs.Warn("md_modelviewer: No level loaded.");
			return;
		}

		var searchPath = MuseDash1Compatibility.StreamingAssets;
		if (searchPath == null) {
			Logs.Warn("md_modelviewer: StreamingAssets not loaded.");
			return;
		}

		var window = new MD_ModelViewerWindow(EngineCore.Level.UI);
		window.Populate(searchPath);
	}
}

public class SkeletonEntry
{
	public string Name;
	public string Path;
	public string AssetName;
	public MonoBehaviour? Asset;
	public ModelData? CachedModelData;
	public ModelInstance? CachedInstance;
	public AnimationHandler? CachedAnimHandler;
	public bool LoadAttempted;
	public bool LoadFailed;

	public SkeletonEntry(string name, string path, string assetName, MonoBehaviour? asset) {
		Name = name;
		Path = path;
		AssetName = assetName;
		Asset = asset;
	}

	public ModelData? EnsureModelData() {
		if (LoadAttempted) return CachedModelData;
		LoadAttempted = true;
		try {
			if (Asset == null) {
				Asset = MuseDash1Compatibility.StreamingAssets.FindAssetByName<MonoBehaviour>(AssetName);
				if (Asset == null) {
					LoadFailed = true;
					return null;
				}
			}
			CachedModelData = MuseDash1ModelConverter.MD_GetModelData(EngineCore.Level, Asset);
		}
		catch (Exception ex) {
			Logs.Warn($"MD_ModelViewer: Failed to load model '{Name}': {ex.Message}");
			LoadFailed = true;
		}
		return CachedModelData;
	}

	public ModelInstance? EnsureInstance() {
		if (CachedInstance != null) return CachedInstance;
		var data = EnsureModelData();
		if (data == null) return null;
		CachedInstance = data.Instantiate();
		return CachedInstance;
	}

	public AnimationHandler EnsureAnimHandler() {
		if (CachedAnimHandler != null) return CachedAnimHandler;
		CachedAnimHandler = new AnimationHandler();
		var instance = EnsureInstance();
		if (instance != null)
			CachedAnimHandler.SetModel(instance);
		return CachedAnimHandler;
	}
}

public class SkeletonFolder
{
	public string Name;
	public string FullPath;
	public Dictionary<string, SkeletonFolder> Subfolders = [];
	public List<SkeletonEntry> Entries = [];

	public SkeletonFolder(string name, string fullPath) {
		Name = name;
		FullPath = fullPath;
	}

	public SkeletonFolder GetOrCreateSubfolder(string name) {
		if (!Subfolders.TryGetValue(name, out var sub)) {
			sub = new SkeletonFolder(name, FullPath.Length > 0 ? $"{FullPath}/{name}" : name);
			Subfolders[name] = sub;
		}
		return sub;
	}

	public void CollectAllEntries(List<SkeletonEntry> into) {
		into.AddRange(Entries);
		foreach (var sub in Subfolders.Values)
			sub.CollectAllEntries(into);
	}

	public int TotalCount() {
		int count = Entries.Count;
		foreach (var sub in Subfolders.Values)
			count += sub.TotalCount();
		return count;
	}
}

public class MD_ModelViewerWindow : Window
{
	SkeletonFolder RootFolder = new("All Models", "");
	List<SkeletonEntry> AllEntries = [];
	List<SkeletonEntry> DisplayedEntries = [];
	Dictionary<string, SkeletonEntry> EntryLookup = [];
	HashSet<long> SeenPathIDs = [];

	Panel LeftPanel = null!;
	ScrollPanel RightPanel = null!;
	Panel GridContainer = null!;
	Label StatusLabel = null!;
	Textbox SearchBox = null!;

	const int THUMB_SIZE = 140;
	const int THUMB_PADDING = 6;
	public MD_ModelViewerWindow(Element? parent) : base(parent) {
		Title = "MuseDash Model Viewer";
		Size = new(1200, 750);
		Position = new(50, 50);
		HideNonCloseButtons();
	}

	public void Populate(UnitySearchPathV2 searchPath) {
		DiscoverSkeletonAssets(searchPath);
		BuildUI();
		SelectFolder(RootFolder);
	}

	void AddEntry(string name, string assetName, MonoBehaviour? mb) {
		string displayName = name.Replace("_SkeletonData", "");
		string folderName = DeriveFolderName(displayName);

		var entry = new SkeletonEntry(displayName, folderName, assetName, mb);
		AllEntries.Add(entry);
		EntryLookup[assetName] = entry;

		var folder = folderName.Length > 0 ? RootFolder.GetOrCreateSubfolder(folderName) : RootFolder;
		folder.Entries.Add(entry);
	}

	void DiscoverSkeletonAssets(UnitySearchPathV2 searchPath) {
		Interlude.Begin("Loading MD modelviewer...");
		int lookupIdx1 = 0;
		int lookupIdx2 = 0;
		foreach (var kvp in searchPath.Catalog.HashedAssetLookup) {
			lookupIdx1++;
			foreach (var searchBase in kvp.Value) {
				lookupIdx2++;
				Interlude.Spin("Loading MD modelviewer...", $"{lookupIdx1}/{searchPath.Catalog.HashedAssetLookup.Count} searchpaths - {lookupIdx2}/{kvp.Value.Count} entries");

				if (searchBase is not UnitySearchAsset asset) continue;
				string name = asset.Name;
				if (!name.Contains("SkeletonData", StringComparison.OrdinalIgnoreCase)) continue;

				if (EntryLookup.TryGetValue(name, out _)) continue;

				MonoBehaviour? mb = null;
				var cachedMb = searchPath.FindAssetByName<MonoBehaviour>(name);
				if (cachedMb != null) {
					mb = cachedMb;
					SeenPathIDs.Add(mb.m_PathID);
				}

				AddEntry(name, name, mb);
			}
			lookupIdx2 = 0;
		}

		foreach (var kvp in searchPath.CachedObjectPathIDLookup) {
			if (SeenPathIDs.Contains(kvp.Key)) continue;
			if (kvp.Value is MonoBehaviour mb) {
				string? name = mb.m_Name;
				if (name == null) continue;
				if (!name.Contains("SkeletonData", StringComparison.OrdinalIgnoreCase)) continue;
				if (EntryLookup.ContainsKey(name)) continue;

				SeenPathIDs.Add(mb.m_PathID);
				AddEntry(name, name, mb);
			}
		}

		AllEntries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		Logs.Info($"MD_ModelViewer: Discovered {AllEntries.Count} skeleton data assets.");
		Interlude.End();
	}

	static string DeriveFolderName(string displayName) {
		int underscoreIdx = displayName.IndexOf('_');
		if (underscoreIdx > 0 && underscoreIdx < displayName.Length - 1)
			return displayName[..underscoreIdx];
		return "misc";
	}

	void BuildUI() {
		var topBar = new Panel(this);
		topBar.Dock = Dock.Top;
		topBar.Size = new(0, 32);
		topBar.DrawPanelBackground = false;

		var searchLabel = new Label(topBar);
		searchLabel.Text = "Filter:";
		searchLabel.TextSize = 19;
		searchLabel.Dock = Dock.Left;
		searchLabel.Size = new(50, 0);

		SearchBox = new Textbox(topBar);
		SearchBox.Dock = Dock.Fill;
		SearchBox.Text = "";
		SearchBox.OnTextChanged += (_, _, _) => ApplyFilter();

		StatusLabel = new Label(this);
		StatusLabel.Dock = Dock.Bottom;
		StatusLabel.Size = new(0, 24);
		StatusLabel.TextSize = 17;
		StatusLabel.Text = $"Found {AllEntries.Count} skeleton assets";

		LeftPanel = new Panel(this);
		LeftPanel.Dock = Dock.Left;
		LeftPanel.Size = new(220, 0);

		var treeScroll = new DirectionalLayoutPanel(LeftPanel);
		treeScroll.Dock = Dock.Fill;

		BuildTreeNodes(treeScroll, RootFolder);

		RightPanel = new ScrollPanel(this);
		RightPanel.Dock = Dock.Fill;
		RightPanel.HorizontalOverflow = false;

		GridContainer = new Panel(RightPanel);
		GridContainer.Dock = Dock.Top;
		GridContainer.DrawPanelBackground = false;
	}

	void BuildTreeNodes(DirectionalLayoutPanel parent, SkeletonFolder folder) {
		var rootBtn = new Button(parent);
		rootBtn.Text = $"{folder.Name} ({folder.TotalCount()})";
		rootBtn.Dock = Dock.Top;
		rootBtn.Size = new(0, 24);
		rootBtn.TextSize = 18;
		rootBtn.TextAlignment = Anchor.CenterLeft;
		rootBtn.TextPadding = new(6);
		rootBtn.MouseReleaseEvent += (_, _, _) => SelectFolder(folder);

		foreach (var sub in folder.Subfolders.Values.OrderBy(f => f.Name)) {
			var btn = new Button(parent);
			btn.Text = $"  {sub.Name} ({sub.TotalCount()})";
			btn.Dock = Dock.Top;
			btn.Size = new(0, 22);
			btn.TextSize = 17;
			btn.TextAlignment = Anchor.CenterLeft;
			btn.TextPadding = new(6);
			btn.BackgroundColor = new(0, 0, 0, 0);
			btn.ForegroundColor = new(0, 0, 0, 0);
			var capturedSub = sub;
			btn.MouseReleaseEvent += (_, _, _) => SelectFolder(capturedSub);
		}
	}

	void SelectFolder(SkeletonFolder folder) {
		DisplayedEntries.Clear();
		folder.CollectAllEntries(DisplayedEntries);
		DisplayedEntries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		ApplyFilter();
	}

	void ApplyFilter() {
		string? filter = SearchBox?.Text;
		RebuildGrid(filter);
	}

	void RebuildGrid(string? filter) {
		foreach (var child in GridContainer.GetChildren())
			child.Remove();

		var entries = DisplayedEntries;
		if (!string.IsNullOrWhiteSpace(filter))
			entries = entries.Where(e => e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

		StatusLabel.Text = $"Showing {entries.Count} of {AllEntries.Count} models";

		float availableWidth = RightPanel.RenderBounds.Width - 20;
		int columns = Math.Max(1, (int)(availableWidth / (THUMB_SIZE + THUMB_PADDING)));
		int rows = (int)Math.Ceiling(entries.Count / (float)columns);
		GridContainer.Size = new(0, rows * (THUMB_SIZE + THUMB_PADDING + 20));

		for (int i = 0; i < entries.Count; i++) {
			var entry = entries[i];
			int col = i % columns;
			int row = i / columns;

			var card = new MD_ModelThumbnailCard(GridContainer);
			card.Dock = Dock.None;
			card.Position = new(
				col * (THUMB_SIZE + THUMB_PADDING) + THUMB_PADDING,
				row * (THUMB_SIZE + THUMB_PADDING + 20) + THUMB_PADDING
			);
			card.Size = new(THUMB_SIZE, THUMB_SIZE + 20);
			card.Setup(entry);
			card.MouseReleaseEvent += (_, _, _) => OpenDetailWindow(entry);
		}
	}

	void OpenDetailWindow(SkeletonEntry entry) {
		var detail = new MD_ModelDetailWindow(EngineCore.Level.UI);
		detail.Setup(entry);
		this.AttachWindowAndLockInput(detail);
	}
}

public class MD_ModelThumbnailCard : Button
{
	SkeletonEntry? Entry;
	double AnimTime;
	int CurrentAnimIndex;
	bool animating;

	public MD_ModelThumbnailCard(Element? parent) : base(parent){ 
		BorderSize = 1;
		BackgroundColor = new(25, 30, 38);
		ForegroundColor = new(55, 62, 72);
		TextSize = 11;
		TextAlignment = Anchor.BottomCenter;
	}

	public void Setup(SkeletonEntry entry) {
		Entry = entry;
		Text = entry.Name;
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		if (Entry == null) return;
		var instance = Entry.EnsureInstance();
		if (instance == null) return;

		var handler = Entry.EnsureAnimHandler();
		var data = Entry.CachedModelData;
		if (data == null || data.Animations.Count == 0) return;

		handler.AddDeltaTime(globals.CurTimeDelta);
		if (!handler.IsPlayingAnimation() && !handler.IsAnimationQueued()) {
			CurrentAnimIndex = (CurrentAnimIndex + 1) % data.Animations.Count;
			handler.SetAnimation(0, data.Animations[CurrentAnimIndex].Name, false);
			animating = true;
		}
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		if (Entry == null) return;
		var instance = Entry.CachedInstance;
		if (instance == null) {
			Graphics2D.SetDrawColor(100, 100, 100);
			if (Entry.LoadFailed)
				Graphics2D.DrawText(new(width / 2, height / 2 - 10), "ERROR", Graphics2D.UI_FONT_NAME, 12, Anchor.Center);
			else if (!Entry.LoadAttempted)
				Graphics2D.DrawText(new(width / 2, height / 2 - 10), "...", Graphics2D.UI_FONT_NAME, 12, Anchor.Center);
			return;
		}

		var handler = Entry.CachedAnimHandler;
		if (handler != null)
			handler.Apply(instance);

		float previewH = height - 22;
		float centerX = width / 2;
		float centerY = previewH / 2;

		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var bone in instance.Bones) {
			bone.UpdateWorldTransform();
			var pos = bone.WorldTransform.LocalToWorld(0, 0);
			minX = Math.Min(minX, pos.X);
			maxX = Math.Max(maxX, pos.X);
			minY = Math.Min(minY, -pos.Y);
			maxY = Math.Max(maxY, -pos.Y);
		}

		float boundsW = maxX - minX;
		float boundsH = maxY - minY;
		if (boundsW < 1) boundsW = 100;
		if (boundsH < 1) boundsH = 100;

		float fitScale = Math.Min((width - 10) / boundsW, (previewH - 10) / boundsH);
		fitScale = Math.Min(fitScale, 2f);

		float modelCX = (minX + maxX) / 2;
		float modelCY = (minY + maxY) / 2;

		var globalOffset = Graphics2D.Offset;

		Rlgl.PushMatrix();
		Rlgl.Translatef(globalOffset.X + centerX, globalOffset.Y + centerY, 0);
		Rlgl.Scalef(fitScale, fitScale, 1);
		Rlgl.Translatef(-modelCX, -modelCY, 0);

		instance.Scale = new(1);
		instance.Position = new(0, 0);
		bool errored = false;
		string? error = null;
		try{
			instance.Render(useDefaultShader: false);
		}
		catch(Exception ex){
			errored = true;
			error = ex.Message;
		}

		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
		if(errored){
			Graphics2D.SetDrawColor(155, 50, 50);
			Graphics2D.DrawRectangle(0, 0, width, height);
			Graphics2D.SetDrawColor(255, 150, 150);
			Graphics2D.DrawText(new(width / 2, height / 2), "Errored!!!", "Noto Sans", 20, Anchor.Center);
		}
	}
}

public class MD_ModelDetailWindow : Window
{
	SkeletonEntry? Entry;
	ModelInstance? Instance;
	AnimationHandler AnimHandler = new();

	float CameraZoom = 1f;
	Vector2F CameraOffset;
	bool CameraInitialized;

	int SelectedChannel = 0;
	bool LoopAnimation = true;
	int SelectedAnimIndex = -1;

	bool ShowBones;
	bool ShowWireframe;
	bool ShowSlotInfo;
	bool ShowAttachmentNames;

	Panel PreviewPanel = null!;
	TabView InfoTabs = null!;
	Label AnimInfoLabel = null!;
	DirectionalLayoutPanel AnimList = null!;
	DirectionalLayoutPanel BoneList = null!;
	DirectionalLayoutPanel SlotList = null!;
	Label ModelInfoLabel = null!;

	public MD_ModelDetailWindow(Element? parent) : base(parent) {
		Title = "Model Detail";
		Size = new(900, 650);
		Position = new(150, 80);
		HideNonCloseButtons();
	}

	public void Setup(SkeletonEntry entry) {
		Entry = entry;
		Title = $"Model: {entry.Name}";

		Instance = entry.EnsureInstance();
		if (Instance != null) {
			AnimHandler = new AnimationHandler();
			AnimHandler.SetModel(Instance);
		}

		BuildUI();
		CenterCamera();
	}

	void CenterCamera() {
		if (Instance == null) return;

		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var bone in Instance.Bones) {
			bone.UpdateWorldTransform();
			var pos = bone.WorldTransform.LocalToWorld(0, 0);
			minX = Math.Min(minX, pos.X);
			maxX = Math.Max(maxX, pos.X);
			minY = Math.Min(minY, -pos.Y);
			maxY = Math.Max(maxY, -pos.Y);
		}

		float boundsW = maxX - minX;
		float boundsH = maxY - minY;
		if (boundsW < 1) boundsW = 100;
		if (boundsH < 1) boundsH = 100;

		CameraOffset = new((minX + maxX) / 2, (minY + maxY) / 2);
		CameraZoom = Math.Min(500f / boundsW, 400f / boundsH);
		CameraZoom = Math.Clamp(CameraZoom, 0.05f, 5f);
		CameraInitialized = true;
	}

	void BuildUI() {
		PreviewPanel = new(this);
		PreviewPanel.Dock = Dock.Fill;
		PreviewPanel.DrawPanelBackground = false;
		PreviewPanel.PaintOverride += PaintPreview;
		PreviewPanel.MouseDragEvent += (self, state, delta) => {
			CameraOffset -= delta / CameraZoom;
		};
		PreviewPanel.MouseScrollEvent += (self, state, delta) => {
			CameraZoom *= (delta.Y > 0) ? 1.15f : 0.87f;
			CameraZoom = Math.Clamp(CameraZoom, 0.01f, 50f);
		};

		var rightPanel = new Panel(this);
		rightPanel.Dock = Dock.Right;
		rightPanel.Size = new(320, 0);

		InfoTabs = new TabView(rightPanel);
		InfoTabs.Dock = Dock.Fill;

		BuildAnimationsTab();
		BuildInfoTab();
		BuildDebugTab();
	}

	void BuildAnimationsTab() {
		var tab = InfoTabs.AddTab("Animations");

		var controls = new Panel(tab.Panel);
		controls.Dock = Dock.Top;
		controls.Size = new(0, 70);
		controls.DrawPanelBackground = false;

		AnimInfoLabel = new Label(controls);
		AnimInfoLabel.Dock = Dock.Top;
		AnimInfoLabel.Size = new(0, 18);
		AnimInfoLabel.TextSize = 17;
		AnimInfoLabel.Text = "No animation playing";

		var channelBar = new Panel(controls);
		channelBar.Dock = Dock.Top;
		channelBar.Size = new(0, 26);
		channelBar.DrawPanelBackground = false;

		var chLabel = new Label(channelBar);
		chLabel.Text = "Channel:";
		chLabel.TextSize = 17;
		chLabel.Dock = Dock.Left;
		chLabel.Size = new(60, 0);

		for (int ch = 0; ch < 5; ch++) {
			int capturedCh = ch;
			var chBtn = new Button(channelBar);
			chBtn.Dock = Dock.Left;
			chBtn.Size = new(28, 0);
			chBtn.Text = $"{ch}";
			chBtn.TextSize = 16;
			chBtn.MouseReleaseEvent += (_, _, _) => SelectedChannel = capturedCh;
		}

		var optBar = new Panel(controls);
		optBar.Dock = Dock.Top;
		optBar.Size = new(0, 26);
		optBar.DrawPanelBackground = false;

		var loopCb = new Checkbox(optBar);
		loopCb.Dock = Dock.Left;
		loopCb.Size = new(70, 0);
		loopCb.Text = "Loop";
		loopCb.TextSize = 17;
		loopCb.Checked = true;
		loopCb.OnCheckedChanged += (cb) => LoopAnimation = cb.Checked;

		var stopBtn = new Button(optBar);
		stopBtn.Dock = Dock.Left;
		stopBtn.Size = new(50, 0);
		stopBtn.Text = "Stop";
		stopBtn.TextSize = 16;
		stopBtn.MouseReleaseEvent += (_, _, _) => {
			AnimHandler.StopAllAnimation();
			Instance?.SetToSetupPose();
		};

		var resetBtn = new Button(optBar);
		resetBtn.Dock = Dock.Left;
		resetBtn.Size = new(60, 0);
		resetBtn.Text = "Reset";
		resetBtn.TextSize = 16;
		resetBtn.MouseReleaseEvent += (_, _, _) => {
			AnimHandler.ClearAllAnimation();
			Instance?.SetToSetupPose();
			CenterCamera();
		};

		AnimList = new DirectionalLayoutPanel(tab.Panel);
		AnimList.Dock = Dock.Fill;

		if (Entry?.CachedModelData != null) {
			var anims = Entry.CachedModelData.Animations;
			for (int i = 0; i < anims.Count; i++) {
				var anim = anims[i];
				int capturedIdx = i;

				var btn = new Button(AnimList);
				btn.Dock = Dock.Top;
				btn.Size = new(0, 22);
				btn.TextSize = 17;
				btn.Text = $"{anim.Name} ({anim.Duration:F2}s)";
				btn.TextAlignment = Anchor.CenterLeft;
				btn.TextPadding = new(6);
				btn.BackgroundColor = new(0, 0, 0, 0);
				btn.ForegroundColor = new(55, 62, 72);

				btn.MouseReleaseEvent += (_, _, _) => {
					SelectedAnimIndex = capturedIdx;
					PlayAnimation(anim.Name);
				};
			}
		}
	}

	void BuildInfoTab() {
		var tab = InfoTabs.AddTab("Info");

		var scroll = new DirectionalLayoutPanel(tab.Panel);
		scroll.Dock = Dock.Fill;

		if (Entry?.CachedModelData == null) {
			var lbl = new Label(scroll);
			lbl.Text = "Model failed to load.";
			lbl.TextSize = 18;
			lbl.Dock = Dock.Top;
			lbl.Size = new(0, 24);
			return;
		}

		var data = Entry.CachedModelData;

		AddInfoRow(scroll, $"Bones: {data.BoneDatas.Count}");
		AddInfoRow(scroll, $"Slots: {data.SlotDatas.Count}");
		AddInfoRow(scroll, $"Skins: {data.Skins.Count}");
		AddInfoRow(scroll, $"Animations: {data.Animations.Count}");
		AddInfoRow(scroll, $"Default Skin: {data.DefaultSkin?.Name ?? "none"}");
		AddInfoRow(scroll, "");

		AddInfoRow(scroll, "--- Bones ---");
		foreach (var bone in data.BoneDatas) {
			string parentName = bone.Parent?.Name ?? "root";
			AddInfoRow(scroll, $"  {bone.Name} (parent: {parentName})");
		}

		AddInfoRow(scroll, "");
		AddInfoRow(scroll, "--- Slots ---");
		foreach (var slot in data.SlotDatas) {
			AddInfoRow(scroll, $"  {slot.Name} (bone: {slot.BoneData.Name}, attach: {slot.Attachment ?? "none"})");
		}

		AddInfoRow(scroll, "");
		AddInfoRow(scroll, "--- Skins ---");
		foreach (var skin in data.Skins) {
			AddInfoRow(scroll, $"  {skin.Name}");
		}
	}

	void AddInfoRow(DirectionalLayoutPanel parent, string text) {
		var lbl = new Label(parent);
		lbl.Text = text;
		lbl.TextSize = 16;
		lbl.Dock = Dock.Top;
		lbl.Size = new(0, 16);
		lbl.TextAlignment = Anchor.CenterLeft;
		lbl.TextPadding = new(4);
	}

	void BuildDebugTab() {
		var tab = InfoTabs.AddTab("Debug");

		var panel = new DirectionalLayoutPanel(tab.Panel);
		panel.Dock = Dock.Fill;

		var bonesCb = new Checkbox(panel);
		bonesCb.Dock = Dock.Top;
		bonesCb.Size = new(0, 26);
		bonesCb.Text = "Show Bones";
		bonesCb.TextSize = 17;
		bonesCb.OnCheckedChanged += (cb) => ShowBones = cb.Checked;

		var wireCb = new Checkbox(panel);
		wireCb.Dock = Dock.Top;
		wireCb.Size = new(0, 26);
		wireCb.Text = "Show Wireframe";
		wireCb.TextSize = 17;
		wireCb.OnCheckedChanged += (cb) => ShowWireframe = cb.Checked;

		var slotCb = new Checkbox(panel);
		slotCb.Dock = Dock.Top;
		slotCb.Size = new(0, 26);
		slotCb.Text = "Show Slot Info";
		slotCb.TextSize = 17;
		slotCb.OnCheckedChanged += (cb) => ShowSlotInfo = cb.Checked;

		var attachCb = new Checkbox(panel);
		attachCb.Dock = Dock.Top;
		attachCb.Size = new(0, 26);
		attachCb.Text = "Show Attachment Names";
		attachCb.TextSize = 17;
		attachCb.OnCheckedChanged += (cb) => ShowAttachmentNames = cb.Checked;

		var setupBtn = new Button(panel);
		setupBtn.Dock = Dock.Top;
		setupBtn.Size = new(0, 28);
		setupBtn.Text = "Reset to Setup Pose";
		setupBtn.TextSize = 17;
		setupBtn.MouseReleaseEvent += (_, _, _) => {
			AnimHandler.ClearAllAnimation();
			Instance?.SetToSetupPose();
		};

		var logBtn = new Button(panel);
		logBtn.Dock = Dock.Top;
		logBtn.Size = new(0, 28);
		logBtn.Text = "Log Model Data to Console";
		logBtn.TextSize = 17;
		logBtn.MouseReleaseEvent += (_, _, _) => LogModelData();
	}

	void PlayAnimation(string name) {
		AnimHandler.SetAnimation(SelectedChannel, name, LoopAnimation);
	}

	void LogModelData() {
		if (Entry?.CachedModelData == null) return;
		var data = Entry.CachedModelData;

		Logs.Info($"=== Model: {Entry.Name} ===");
		Logs.Info($"  Bones: {data.BoneDatas.Count}");
		Logs.Info($"  Slots: {data.SlotDatas.Count}");
		Logs.Info($"  Skins: {data.Skins.Count}");
		Logs.Info($"  Animations: {data.Animations.Count}");

		foreach (var anim in data.Animations)
			Logs.Info($"    Animation: {anim.Name} ({anim.Duration:F3}s, {anim.Timelines.Count} timelines)");

		if (Instance != null) {
			Logs.Info($"  DrawOrder: {Instance.DrawOrder.Count} slots");
			foreach (var slot in Instance.DrawOrder)
				Logs.Info($"    Slot: {slot.Data.Name} attachment={slot.Attachment?.Name ?? "NULL"} color={slot.Color} blend={slot.BlendMode}");
		}
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);

		if (Instance == null) return;
		AnimHandler.AddDeltaTime(globals.CurTimeDelta);

		if (AnimHandler.IsPlayingAnimation()) {
			var channel = AnimHandler.Channels[SelectedChannel];
			var entry = channel.CurrentEntry;
			if (entry != null)
				AnimInfoLabel.Text = $"Ch{SelectedChannel}: {entry.Animation.Name} @ {channel.Time:F2}s / {entry.Animation.Duration:F2}s";
			else
				AnimInfoLabel.Text = $"Ch{SelectedChannel}: idle";
		}
		else {
			AnimInfoLabel.Text = "No animation playing";
		}
	}

	void PaintPreview(Element self, float width, float height) {
		Graphics2D.SetDrawColor(18, 22, 28);
		Graphics2D.DrawRectangle(0, 0, width, height);

		DrawGrid(width, height);

		if (Instance == null) {
			Graphics2D.SetDrawColor(120, 120, 120);
			Graphics2D.DrawText(new(width / 2, height / 2), "No model", Graphics2D.UI_FONT_NAME, 16, Anchor.Center);
			return;
		}

		AnimHandler.Apply(Instance);

		var globalOffset = Graphics2D.Offset;
		float cx = width / 2;
		float cy = height / 2;

		Rlgl.PushMatrix();
		Rlgl.Translatef(globalOffset.X + cx, globalOffset.Y + cy, 0);
		Rlgl.Scalef(CameraZoom, CameraZoom, 1);
		Rlgl.Translatef(-CameraOffset.X, -CameraOffset.Y, 0);

		int prevWireframe = Model4System.m4s_wireframe.GetInt();
		if (ShowWireframe)
			Model4System.m4s_wireframe.SetValue(1);

		Instance.Scale = new(1);
		Instance.Position = new(0, 0);
		Instance.Render(useDefaultShader: false);

		if (ShowWireframe)
			Model4System.m4s_wireframe.SetValue(prevWireframe);

		if (ShowBones)
			DrawBoneOverlay();

		if (ShowSlotInfo || ShowAttachmentNames)
			DrawSlotOverlay();

		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();

		Graphics2D.SetDrawColor(60, 70, 80);
		Graphics2D.DrawLine(new(cx - 8, cy), new(cx + 8, cy));
		Graphics2D.DrawLine(new(cx, cy - 8), new(cx, cy + 8));

		Graphics2D.SetDrawColor(80, 90, 100);
		Graphics2D.DrawText(4, 4, $"Zoom: {CameraZoom:F2}x  Offset: {CameraOffset.X:F0}, {CameraOffset.Y:F0}", Graphics2D.UI_FONT_NAME, 11);
	}

	void DrawGrid(float width, float height) {
		Graphics2D.SetDrawColor(28, 33, 40);
		float gridSize = 50 * CameraZoom;
		if (gridSize < 10) gridSize *= 5;
		if (gridSize < 10) return;

		float cx = width / 2 - (CameraOffset.X * CameraZoom) % gridSize;
		float cy = height / 2 - (CameraOffset.Y * CameraZoom) % gridSize;

		for (float x = cx % gridSize; x < width; x += gridSize)
			Graphics2D.DrawLine(new(x, 0), new(x, height));
		for (float y = cy % gridSize; y < height; y += gridSize)
			Graphics2D.DrawLine(new(0, y), new(width, y));

		float axisX = width / 2 - CameraOffset.X * CameraZoom;
		float axisY = height / 2 - CameraOffset.Y * CameraZoom;
		Graphics2D.SetDrawColor(80, 40, 40);
		Graphics2D.DrawLine(new(0, axisY), new(width, axisY));
		Graphics2D.SetDrawColor(40, 80, 40);
		Graphics2D.DrawLine(new(axisX, 0), new(axisX, height));
	}

	void DrawBoneOverlay() {
		if (Instance == null) return;

		foreach (var bone in Instance.Bones) {
			var pos = bone.WorldTransform.LocalToWorld(0, 0);
			float drawX = pos.X;
			float drawY = -pos.Y;

			Rlgl.DrawRenderBatchActive();
			Raylib.DrawCircleV(new(drawX, drawY), 3f / CameraZoom, new Color(255, 80, 80, 200));

			if (bone.Parent != null) {
				var parentPos = bone.Parent.WorldTransform.LocalToWorld(0, 0);
				Raylib.DrawLineV(
					new(drawX, drawY),
					new(parentPos.X, -parentPos.Y),
					new Color(255, 200, 80, 100));
			}
		}
	}

	void DrawSlotOverlay() {
		if (Instance == null) return;

		int idx = 0;
		foreach (var slot in Instance.DrawOrder) {
			if (slot.Attachment == null) continue;

			var bonePos = slot.Bone.WorldTransform.LocalToWorld(0, 0);
			float drawX = bonePos.X;
			float drawY = -bonePos.Y;

			Rlgl.DrawRenderBatchActive();

			if (ShowAttachmentNames) {
				float fontSize = 10f / CameraZoom;
				fontSize = Math.Clamp(fontSize, 8, 14);
				Graphics2D.SetDrawColor(200, 220, 255, 180);
				Graphics2D.DrawText(new(drawX, drawY), $"{slot.Data.Name}:{slot.Attachment.Name}", Graphics2D.UI_FONT_NAME, fontSize, Anchor.CenterLeft);
			}

			if (ShowSlotInfo) {
				Raylib.DrawCircleV(new(drawX, drawY), 2f / CameraZoom, new Color(80, 200, 255, 150));
			}

			idx++;
		}
	}
}
