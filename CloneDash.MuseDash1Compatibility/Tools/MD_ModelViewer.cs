using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Models.Runtime;
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

		var window = new MD_ModelViewerWindow(EngineCore.Level.RootPanel);
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
		SetSize(new(1200, 750));
		SetPos(new(50, 50));
		HideNonCloseButtons();
	}

	public void Populate(UnitySearchPathV2 searchPath) {
		DiscoverSkeletonAssets(searchPath);
		BuildUI();
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
		topBar.SetDock(Dock.Top);
		topBar.SetSize(new(0, 32));
		topBar.SetPaintBackgroundEnabled(false);

		var searchLabel = new Label(topBar);
		searchLabel.SetText("Filter:");
		searchLabel.SetTextSize(19);
		searchLabel.SetDock(Dock.Left);
		searchLabel.SetSize(new(50, 0));

		SearchBox = new Textbox(topBar);
		SearchBox.SetDock(Dock.Fill);
		SearchBox.SetText("");
		SearchBox.OnTextChanged += (_, _, _) => ApplyFilter();

		StatusLabel = new Label(this);
		StatusLabel.SetDock(Dock.Bottom);
		StatusLabel.SetSize(new(0, 24));
		StatusLabel.SetTextSize(17);
		StatusLabel.SetText($"Found {AllEntries.Count} skeleton assets");

		LeftPanel = new Panel(this);
		LeftPanel.SetDock(Dock.Left);
		LeftPanel.SetSize(new(220, 0));

		var treeScroll = new DirectionalLayoutPanel(LeftPanel);
		treeScroll.SetDock(Dock.Fill);

		BuildTreeNodes(treeScroll, RootFolder);

		RightPanel = new ScrollPanel(this);
		RightPanel.SetDock(Dock.Fill);
		RightPanel.HorizontalOverflow = false;

		GridContainer = new Panel(RightPanel);
		GridContainer.SetDock(Dock.Top);
		GridContainer.SetPaintBackgroundEnabled(false);
	}

	void BuildTreeNodes(DirectionalLayoutPanel parent, SkeletonFolder folder) {
		var rootBtn = new Button(parent);
		rootBtn.SetText($"{folder.Name} ({folder.TotalCount()})");
		rootBtn.SetDock(Dock.Top);
		rootBtn.SetSize(new(0, 24));
		rootBtn.SetTextSize(18);
		rootBtn.SetTextAlignment(Anchor.CenterLeft);
		rootBtn.SetTextPadding(new(6));
		rootBtn.OnButtonClick += (_, _) => SelectFolder(folder);

		foreach (var sub in folder.Subfolders.Values.OrderBy(f => f.Name)) {
			var btn = new Button(parent);
			btn.SetText($"  {sub.Name} ({sub.TotalCount()})");
			btn.SetDock(Dock.Top);
			btn.SetSize(new(0, 22));
			btn.SetTextSize(17);
			btn.SetTextAlignment(Anchor.CenterLeft);
			btn.SetTextPadding(new(6));
			btn.SetBgColor(new Color(0, 0, 0, 0));
			btn.SetFgColor(new Color(0, 0, 0, 0));
			var capturedSub = sub;
			btn.OnButtonClick += (_, _) => SelectFolder(capturedSub);
		}
	}

	void SelectFolder(SkeletonFolder folder) {
		DisplayedEntries.Clear();
		folder.CollectAllEntries(DisplayedEntries);
		DisplayedEntries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		ApplyFilter();
	}

	void ApplyFilter() {
		ReadOnlySpan<char> filter = SearchBox.GetText();
		RebuildGrid(filter);
	}

	void RebuildGrid(ReadOnlySpan<char> filter) {
		foreach (var child in GridContainer.GetChildren())
			child.Remove();

		var entries = DisplayedEntries;
		if (filter.Length != 0 && !filter.IsWhiteSpace()) {
			string filterStr = new(filter);
			entries = entries.Where(e => e.Name.Contains(filterStr, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		StatusLabel.SetText($"Showing {entries.Count} of {AllEntries.Count} models");

		float availableWidth = RightPanel.GetRenderBounds().Width - 20;
		int columns = Math.Max(1, (int)(availableWidth / (THUMB_SIZE + THUMB_PADDING)));
		int rows = (int)Math.Ceiling(entries.Count / (float)columns);
		GridContainer.SetSize(new(0, rows * (THUMB_SIZE + THUMB_PADDING + 20)));

		for (int i = 0; i < entries.Count; i++) {
			var entry = entries[i];
			int col = i % columns;
			int row = i / columns;

			var card = new MD_ModelThumbnailCard(GridContainer);
			card.SetDock(Dock.None);
			card.SetPos(new(
				col * (THUMB_SIZE + THUMB_PADDING) + THUMB_PADDING,
				row * (THUMB_SIZE + THUMB_PADDING + 20) + THUMB_PADDING
			));
			card.SetSize(new(THUMB_SIZE, THUMB_SIZE + 20));
			card.Setup(entry);
			card.OnButtonClick += (_, _) => OpenDetailWindow(entry);
		}
	}

	void OpenDetailWindow(SkeletonEntry entry) {
		var detail = new MD_ModelDetailWindow(EngineCore.Level.RootPanel);
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

	public MD_ModelThumbnailCard(Element? parent) : base(parent) {
		SetBorderSize(1);
		SetBgColor(new Color(25, 30, 38));
		SetFgColor(new Color(55, 62, 72));
		SetTextSize(11);
		SetTextAlignment(Anchor.BottomCenter);
	}

	public void Setup(SkeletonEntry entry) {
		Entry = entry;
		SetText(entry.Name);
	}

	protected override void OnThink() {
		base.OnThink();
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
		try {
			instance.Render(useDefaultShader: false);
		}
		catch (Exception ex) {
			errored = true;
			error = ex.Message;
		}

		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
		if (errored) {
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

	class ModelPreviewPanel(MD_ModelDetailWindow parent) : Panel(parent)
	{
		float CameraZoom => parent.CameraZoom;
		Vector2F CameraOffset => parent.CameraOffset;
		AnimationHandler AnimHandler => parent.AnimHandler;
		ModelInstance? Instance => parent.Instance;
		bool ShowWireframe => parent.ShowWireframe;
		bool ShowBones => parent.ShowBones;
		bool ShowSlotInfo => parent.ShowSlotInfo;
		bool ShowAttachmentNames => parent.ShowAttachmentNames;
		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(18, 22, 28);
			Graphics2D.DrawRectangle(0, 0, width, height);

			parent.DrawGrid(width, height);

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

		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			parent.CameraZoom *= (delta.Y > 0) ? 1.15f : 0.87f;
			parent.CameraZoom = Math.Clamp(CameraZoom, 0.01f, 50f);
			return true;
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			parent.CameraOffset -= delta / CameraZoom;
			return true;
		}
	}

	ModelPreviewPanel PreviewPanel = null!;
	TabView InfoTabs = null!;
	Label AnimInfoLabel = null!;
	DirectionalLayoutPanel AnimList = null!;
	DirectionalLayoutPanel BoneList = null!;
	DirectionalLayoutPanel SlotList = null!;
	Label ModelInfoLabel = null!;

	public MD_ModelDetailWindow(Element? parent) : base(parent) {
		Title = "Model Detail";
		SetSize(new(900, 650));
		SetPos(new(150, 80));
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
		PreviewPanel.SetDock(Dock.Fill);
		PreviewPanel.SetPaintBackgroundEnabled(false);
		var rightPanel = new Panel(this);
		rightPanel.SetDock(Dock.Right);
		rightPanel.SetSize(new(320, 0));

		InfoTabs = new TabView(rightPanel);
		InfoTabs.SetDock(Dock.Fill);

		BuildAnimationsTab();
		BuildInfoTab();
		BuildDebugTab();
	}

	void BuildAnimationsTab() {
		var tab = InfoTabs.AddTab("Animations");

		var controls = new Panel(tab.Panel);
		controls.SetDock(Dock.Top);
		controls.SetSize(new(0, 70));
		controls.SetPaintBackgroundEnabled(false);

		AnimInfoLabel = new Label(controls);
		AnimInfoLabel.SetDock(Dock.Top);
		AnimInfoLabel.SetSize(new(0, 18));
		AnimInfoLabel.SetTextSize(17);
		AnimInfoLabel.SetText("No animation playing");

		var channelBar = new Panel(controls);
		channelBar.SetDock(Dock.Top);
		channelBar.SetSize(new(0, 26));
		channelBar.SetPaintBackgroundEnabled(false);

		var chLabel = new Label(channelBar);
		chLabel.SetText("Channel:");
		chLabel.SetTextSize(17);
		chLabel.SetDock(Dock.Left);
		chLabel.SetSize(new(60, 0));

		for (int ch = 0; ch < 5; ch++) {
			int capturedCh = ch;
			var chBtn = new Button(channelBar);
			chBtn.SetDock(Dock.Left);
			chBtn.SetSize(new(28, 0));
			chBtn.SetText($"{ch}");
			chBtn.SetTextSize(16);
			chBtn.OnButtonClick += (_, _) => SelectedChannel = capturedCh;
		}

		var optBar = new Panel(controls);
		optBar.SetDock(Dock.Top);
		optBar.SetSize(new(0, 26));
		optBar.SetPaintBackgroundEnabled(false);

		var loopCb = new CheckboxButton(optBar);
		loopCb.SetDock(Dock.Left);
		loopCb.SetSize(new(70, 0));
		loopCb.SetText("Loop");
		loopCb.SetTextSize(17);
		loopCb.Checked = true;
		loopCb.OnCheckedChanged += (cb) => LoopAnimation = cb.Checked;

		var stopBtn = new Button(optBar);
		stopBtn.SetDock(Dock.Left);
		stopBtn.SetSize(new(50, 0));
		stopBtn.SetText("Stop");
		stopBtn.SetTextSize(16);
		stopBtn.OnButtonClick += (_, _) => {
			AnimHandler.StopAllAnimation();
			Instance?.SetToSetupPose();
		};

		var resetBtn = new Button(optBar);
		resetBtn.SetDock(Dock.Left);
		resetBtn.SetSize(new(60, 0));
		resetBtn.SetText("Reset");
		resetBtn.SetTextSize(16);
		resetBtn.OnButtonClick += (_, _) => {
			AnimHandler.ClearAllAnimation();
			Instance?.SetToSetupPose();
			CenterCamera();
		};

		AnimList = new DirectionalLayoutPanel(tab.Panel);
		AnimList.SetDock(Dock.Fill);

		if (Entry?.CachedModelData != null) {
			var anims = Entry.CachedModelData.Animations;
			for (int i = 0; i < anims.Count; i++) {
				var anim = anims[i];
				int capturedIdx = i;

				var btn = new Button(AnimList);
				btn.SetDock(Dock.Top);
				btn.SetSize(new(0, 22));
				btn.SetTextSize(17);
				btn.SetText($"{anim.Name} ({anim.Duration:F2}s)");
				btn.SetTextAlignment(Anchor.CenterLeft);
				btn.SetTextPadding(new(6));
				btn.SetBgColor(new Color(0, 0, 0, 0));
				btn.SetFgColor(new Color(55, 62, 72));

				btn.OnButtonClick += (_, _) => {
					SelectedAnimIndex = capturedIdx;
					PlayAnimation(anim.Name);
				};
			}
		}
	}

	void BuildInfoTab() {
		var tab = InfoTabs.AddTab("Info");

		var scroll = new DirectionalLayoutPanel(tab.Panel);
		scroll.SetDock(Dock.Fill);

		if (Entry?.CachedModelData == null) {
			var lbl = new Label(scroll);
			lbl.SetText("Model failed to load.");
			lbl.SetTextSize(18);
			lbl.SetDock(Dock.Top);
			lbl.SetSize(new(0, 24));
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
		lbl.SetText(text);
		lbl.SetTextSize(16);
		lbl.SetDock(Dock.Top);
		lbl.SetSize(new(0, 16));
		lbl.SetTextAlignment(Anchor.CenterLeft);
		lbl.SetTextPadding(new(4));
	}

	void BuildDebugTab() {
		var tab = InfoTabs.AddTab("Debug");

		var panel = new DirectionalLayoutPanel(tab.Panel);
		panel.SetDock(Dock.Fill);

		var bonesCb = new CheckboxButton(panel);
		bonesCb.SetDock(Dock.Top);
		bonesCb.SetSize(new(0, 26));
		bonesCb.SetText("Show Bones");
		bonesCb.SetTextSize(17);
		bonesCb.OnCheckedChanged += (cb) => ShowBones = cb.Checked;

		var wireCb = new CheckboxButton(panel);
		wireCb.SetDock(Dock.Top);
		wireCb.SetSize(new(0, 26));
		wireCb.SetText("Show Wireframe");
		wireCb.SetTextSize(17);
		wireCb.OnCheckedChanged += (cb) => ShowWireframe = cb.Checked;

		var slotCb = new CheckboxButton(panel);
		slotCb.SetDock(Dock.Top);
		slotCb.SetSize(new(0, 26));
		slotCb.SetText("Show Slot Info");
		slotCb.SetTextSize(17);
		slotCb.OnCheckedChanged += (cb) => ShowSlotInfo = cb.Checked;

		var attachCb = new CheckboxButton(panel);
		attachCb.SetDock(Dock.Top);
		attachCb.SetSize(new(0, 26));
		attachCb.SetText("Show Attachment Names");
		attachCb.SetTextSize(17);
		attachCb.OnCheckedChanged += (cb) => ShowAttachmentNames = cb.Checked;

		var setupBtn = new Button(panel);
		setupBtn.SetDock(Dock.Top);
		setupBtn.SetSize(new(0, 28));
		setupBtn.SetText("Reset to Setup Pose");
		setupBtn.SetTextSize(17);
		setupBtn.OnButtonClick += (_, _) => {
			AnimHandler.ClearAllAnimation();
			Instance?.SetToSetupPose();
		};

		var logBtn = new Button(panel);
		logBtn.SetDock(Dock.Top);
		logBtn.SetSize(new(0, 28));
		logBtn.SetText("Log Model Data to Console");
		logBtn.SetTextSize(17);
		logBtn.OnButtonClick += (_, _) => LogModelData();
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

	protected override void OnThink() {
		base.OnThink();

		if (Instance == null) return;
		AnimHandler.AddDeltaTime(globals.CurTimeDelta);

		if (AnimHandler.IsPlayingAnimation()) {
			var channel = AnimHandler.Channels[SelectedChannel];
			var entry = channel.CurrentEntry;
			if (entry != null)
				AnimInfoLabel.SetText($"Ch{SelectedChannel}: {entry.Animation.Name} @ {channel.Time:F2}s / {entry.Animation.Duration:F2}s");
			else
				AnimInfoLabel.SetText($"Ch{SelectedChannel}: idle");
		}
		else {
			AnimInfoLabel.SetText("No animation playing");
		}
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
}
