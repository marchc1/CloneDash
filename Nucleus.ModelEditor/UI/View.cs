using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Nucleus.ModelEditor.UI;

/// <summary>
/// A panel that lives in a tab of the view layout
/// </summary>
public class View : Panel
{
	public virtual string Name { get; set; } = "View";
	public virtual string? Icon { get; set; } = null;
	public View(Element parent) : base(parent) {
		SetVisible(false);
		BorderSize = 0;
	}
}

static class ViewColors
{
	public static readonly Color Body = TabView.SWITCHER_ACTIVE;
	public static readonly Color TabInactive = TabView.SWITCHER_INACTIVE;
	public static readonly Color Drop = new(225, 80, 10, 255);
	public static readonly Color Text = new(225, 228, 232, 255);
	public static readonly Color TextDim = new(140, 145, 155, 255);
	public const float TEXT_SIZE = 18;
	public const float ICON_SIZE = 18;
}

/// <summary>
/// Two layout nodes side by side (or stacked), with a draggable gap between them
/// </summary>
public class ViewSplit : Panel
{
	public const float GAP = 4;
	public const float MIN_SIZE = 60;

	public bool Vertical;
	/// <summary>
	/// How much of the space the first node gets
	/// </summary>
	public float Ratio = 0.5f;
	public Element First { get; private set; } = null!;
	public Element Second { get; private set; } = null!;
	readonly ViewSplitter splitter;
	readonly ViewHost host;

	public ViewSplit(ViewHost host, Element parent, bool vertical, float ratio) : base(parent) {
		this.host = host;
		Vertical = vertical;
		Ratio = ratio;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		DockPadding = RectangleF.Zero;
		splitter = new(this, host);
	}

	public void Set(Element first, Element second) {
		First = first;
		Second = second;
		first.SetParent(this);
		second.SetParent(this);
		first.Dock = Dock.None;
		second.Dock = Dock.None;
		InvalidateLayout();
	}

	public void Replace(Element old, Element with) {
		if (First == old)
			First = with;
		else if (Second == old)
			Second = with;
		else
			return;
		with.SetParent(this);
		with.Dock = Dock.None;
		InvalidateLayout();
	}

	public Element Other(Element node) => First == node ? Second : First;

	public float Length => Vertical ? GetRenderBounds().H : GetRenderBounds().W;

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		bool firstShown = !ViewHost.IsCollapsed(First);
		bool secondShown = !ViewHost.IsCollapsed(Second);
		First.SetVisible(firstShown);
		Second.SetVisible(secondShown);
		splitter.SetVisible(firstShown && secondShown);

		if (!firstShown || !secondShown) {
			var shown = firstShown ? First : Second;
			shown.Position = Vector2F.Zero;
			shown.Size = new(width, height);
			return;
		}

		float total = (Vertical ? height : width) - GAP;
		float firstSize = MathF.Round(total * Ratio);
		if (total >= MIN_SIZE * 2)
			firstSize = Math.Clamp(firstSize, MIN_SIZE, total - MIN_SIZE);
		float secondSize = total - firstSize;

		if (Vertical) {
			First.Position = Vector2F.Zero;
			First.Size = new(width, firstSize);
			splitter.Position = new(0, firstSize);
			splitter.Size = new(width, GAP);
			Second.Position = new(0, firstSize + GAP);
			Second.Size = new(width, secondSize);
		}
		else {
			First.Position = Vector2F.Zero;
			First.Size = new(firstSize, height);
			splitter.Position = new(firstSize, 0);
			splitter.Size = new(GAP, height);
			Second.Position = new(firstSize + GAP, 0);
			Second.Size = new(secondSize, height);
		}
	}

	internal void DragTo(Vector2F mouse) {
		var pos = GetGlobalPosition();
		float total = Length - GAP;
		if (total <= 0) return;
		float at = (Vertical ? mouse.Y - pos.Y : mouse.X - pos.X) - GAP / 2;
		Ratio = Math.Clamp(at / total, 0.02f, 0.98f);
		InvalidateLayout();
	}
}

public class ViewSplitter : Panel
{
	readonly ViewSplit split;
	readonly ViewHost host;
	bool dragging;

	public ViewSplitter(ViewSplit split, ViewHost host) : base(split) {
		this.split = split;
		this.host = host;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public bool Active => dragging || IsHovered();

	protected override void OnThink() {
		if (Active)
			EngineCore.SetMouseCursor(split.Vertical ? MouseCursor.MOUSE_CURSOR_RESIZE_NS : MouseCursor.MOUSE_CURSOR_RESIZE_EW);
	}

	public override void Paint(float width, float height) {
		if (!Active) return;
		Graphics2D.SetDrawColor(ViewColors.Drop);
		if (split.Vertical) {
			Graphics2D.DrawRectangle(0, 0, width, 1);
			Graphics2D.DrawRectangle(0, height - 1, width, 1);
		}
		else {
			Graphics2D.DrawRectangle(0, 0, 1, height);
			Graphics2D.DrawRectangle(width - 1, 0, 1, height);
		}
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		dragging = button == ButtonCode.MouseLeft;
		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		if (dragging)
			split.DragTo(state.Mouse.MousePos);
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (dragging) {
			dragging = false;
			host.Changed();
		}
		return true;
	}
}

/// <summary>
/// A tab group, one view at a time with a tab strip on top
/// </summary>
public class ViewGroup : Panel
{
	public readonly ViewHost Host;
	/// <summary>
	/// The viewport's group, no tabs and can't be emptied
	/// </summary>
	public readonly bool Fixed;
	public readonly List<View> Views = [];
	public readonly HashSet<View> Minimized = [];
	int active;

	public readonly ViewTabStrip Strip;
	public readonly Panel Body;

	public ViewGroup(ViewHost host, Element parent, bool isFixed = false) : base(parent) {
		Host = host;
		Fixed = isFixed;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		DockPadding = RectangleF.Zero;

		Strip = new(this);
		Strip.Dock = Dock.Top;
		Strip.Size = new(0, ViewTabStrip.HEIGHT);
		Strip.SetVisible(!isFixed);

		Body = new(this);
		Body.Dock = Dock.Fill;
		Body.BorderSize = 0;
		Body.DockPadding = RectangleF.Zero;
		Body.SetPaintBorderEnabled(false);
		Body.SetPaintBackgroundEnabled(!isFixed);
		Body.SetBgColor(ViewColors.Body);
	}

	public IEnumerable<View> ShownViews => Views.Where(x => !Minimized.Contains(x));

	public View? ActiveView {
		get {
			if (active >= 0 && active < Views.Count && !Minimized.Contains(Views[active]))
				return Views[active];
			return ShownViews.FirstOrDefault();
		}
	}

	public void Add(View view, int index = int.MaxValue) {
		Views.Insert(Math.Clamp(index, 0, Views.Count), view);
		view.SetParent(Body);
		view.Dock = Dock.Fill;
		Refresh();
	}

	public void Remove(View view) {
		var current = ActiveView;
		Views.Remove(view);
		Minimized.Remove(view);
		view.SetVisible(false);
		view.SetParent(Host.Stash);
		if (current != null && current != view)
			active = Views.IndexOf(current);
		Refresh();
	}

	public void SetActive(View view) {
		int index = Views.IndexOf(view);
		if (index < 0) return;
		active = index;
		Refresh();
	}

	internal void SetActiveQuiet(View view) => active = Math.Max(0, Views.IndexOf(view));

	/// <summary>
	/// Puts the views back under this group
	/// </summary>
	public void Attach() {
		foreach (var view in Views) {
			if (view.GetParent() != Body)
				view.SetParent(Body);
			view.Dock = Dock.Fill;
		}
		Refresh();
	}

	public void Refresh() {
		var shown = ActiveView;
		if (shown != null)
			active = Views.IndexOf(shown);
		foreach (var view in Views)
			view.SetVisible(view == shown);
		Strip.Rebuild();
		InvalidateLayout();
		Body.InvalidateLayout();
	}
}

public class ViewTabStrip : Panel
{
	public const float HEIGHT = 30;
	public readonly ViewGroup Group;
	readonly Panel tabs;
	readonly ViewMenuButton menu;

	public ViewTabStrip(ViewGroup group) : base(group) {
		Group = group;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		DockPadding = RectangleF.Zero;

		menu = new(this);
		menu.Dock = Dock.Right;
		menu.Size = new(HEIGHT);

		tabs = new(this);
		tabs.Dock = Dock.Fill;
		tabs.BorderSize = 0;
		tabs.DockPadding = RectangleF.Zero;
		tabs.SetPaintBackgroundEnabled(false);
		tabs.SetPaintBorderEnabled(false);
		tabs.Clipping = true;
	}

	public List<ViewTab> Tabs {
		get {
			List<ViewTab> list = [];
			using var children = tabs.GetChildren();
			foreach (var child in children) {
				if (child is ViewTab tab)
					list.Add(tab);
			}
			return list;
		}
	}

	public RectangleF TabsBounds => RectangleF.FromPosAndSize(tabs.GetGlobalPosition(), tabs.GetRenderBounds().Size);

	public void Rebuild() {
		tabs.ClearChildren();
		foreach (var view in Group.ShownViews) {
			var tab = new ViewTab(tabs, Group, view);
			tab.Dock = Dock.Left;
			tab.DockMargin = RectangleF.TLRB(0, 0, 2, 0);
		}
		tabs.InvalidateLayout();
	}
}

public class ViewTab : Button
{
	public readonly ViewGroup Group;
	public readonly View View;
	Vector2F pressedAt;
	bool pressed;
	bool dragging;

	public ViewTab(Element parent, ViewGroup group, View view) : base(parent) {
		Group = group;
		View = view;
		Text = view.Name;
		BorderSize = 0;
		SetAutoSize(false);
		float textW = Graphics2D.GetTextSize(view.Name, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE).X;
		Size = new(10 + (view.Icon != null ? ViewColors.ICON_SIZE + 6 : 0) + textW + 12, ViewTabStrip.HEIGHT);
	}

	bool IsActive => Group.ActiveView == View;

	public override void PaintBackground(float width, float height) {
		Color back = IsActive ? ViewColors.Body : MixColorBasedOnMouseState(this, ViewColors.TabInactive, new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1));
		Graphics2D.SetDrawColor(back);
		Graphics2D.DrawRectangle(0, 0, width, height);
	}

	public override void PaintBorder(float width, float height) { }

	public override void Paint(float width, float height) {
		float x = 10;
		if (View.Icon != null) {
			Graphics2D.SetDrawColor(255, 255, 255, IsActive ? 255 : 170);
			Graphics2D.SetTexture(textures.LoadTextureFromFile(View.Icon));
			Graphics2D.DrawTexturedRectangle(
				new Vector2F(x, MathF.Round((height - ViewColors.ICON_SIZE) / 2)),
				new Vector2F(ViewColors.ICON_SIZE));
			x += ViewColors.ICON_SIZE + 6;
		}
		Graphics2D.SetDrawColor(IsActive ? ViewColors.Text : ViewColors.TextDim);
		var size = Graphics2D.DrawText(x, height / 2, View.Name, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE, Anchor.CenterLeft);

		// focus underline
		if (Group.Host.FocusedView == View) {
			float y = MathF.Round(height / 2 + ViewColors.TEXT_SIZE / 2) + 1;
			Graphics2D.DrawRectangle(x, y, size.X, 1);
		}
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		if (button == ButtonCode.MouseLeft) {
			pressed = true;
			pressedAt = state.Mouse.MousePos;
		}
		return base.MouseClick(state, button);
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		if (!pressed) return true;
		if (!dragging && state.Mouse.MousePos.Distance(pressedAt) > 8) {
			dragging = true;
			Group.Host.BeginDrag(View, Group);
		}
		if (dragging)
			Group.Host.UpdateDrag(state.Mouse.MousePos);
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		bool wasDragging = dragging;
		pressed = dragging = false;
		if (wasDragging) {
			Group.Host.EndDrag(state.Mouse.MousePos);
			return true;
		}
		if (IsHovered() && button == ButtonCode.MouseLeft) {
			Group.SetActive(View);
			Group.Host.Focus(View);
			Group.Host.Changed();
		}
		return true;
	}
}

public class ViewMenuButton : Button
{
	readonly ViewTabStrip strip;
	long lastClick;
	bool menuPending;

	private const double DOUBLE_CLICK_TIME = 0.3;

	public ViewMenuButton(ViewTabStrip strip) : base(strip) {
		this.strip = strip;
		Text = "";
		BorderSize = 0;
		SetTooltipText("Minimize: right click\nClose: double click");
	}

	public override void PaintBackground(float width, float height) {
		if (!IsHovered()) return;
		Graphics2D.SetDrawColor(MixColorBasedOnMouseState(this, ViewColors.TabInactive, new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1)));
		Graphics2D.DrawRectangle(0, 0, width, height);
	}

	public override void PaintBorder(float width, float height) { }

	public override void Paint(float width, float height) {
		Graphics2D.SetDrawColor(IsHovered() ? ViewColors.Text : ViewColors.TextDim);
		float barWidth = 14;
		float x = MathF.Round((width - barWidth) / 2);
		float y = MathF.Round(height / 2);
		Graphics2D.DrawRectangle(x, y - 5, barWidth, 2);
		Graphics2D.DrawRectangle(x, y - 1, barWidth, 2);
		Graphics2D.DrawRectangle(x, y + 3, barWidth, 2);
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		var host = strip.Group.Host;
		var view = strip.Group.ActiveView;
		if (view == null) return true;

		if (button == ButtonCode.MouseRight) {
			host.Minimize(view);
			return true;
		}
		if (button != ButtonCode.MouseLeft) return true;

		if (menuPending && Stopwatch.GetElapsedTime(lastClick).TotalSeconds < DOUBLE_CLICK_TIME) {
			menuPending = false;
			host.Close(view);
			return true;
		}
		lastClick = Stopwatch.GetTimestamp();
		menuPending = true;
		return true;
	}

	protected override void OnThink() {
		base.OnThink();
		if (!menuPending || Stopwatch.GetElapsedTime(lastClick).TotalSeconds < DOUBLE_CLICK_TIME) return;
		menuPending = false;

		var host = strip.Group.Host;
		var view = strip.Group.ActiveView;
		if (view == null) return;
		var menu = new Menu(UI);
		menu.AddItem(new ViewMenuRow("Minimize", null, "Right click", false, () => host.Minimize(view)));
		menu.AddItem(new ViewMenuRow("Close", null, "Double click", false, () => host.Close(view)));
		ViewMenuRow.Open(menu, GetGlobalPosition() + new Vector2F(GetRenderBounds().W, GetRenderBounds().H), true);
	}
}

public record ViewMenuRow(string Text, string? Icon, string? Hint, bool Dim, Action Invoke) : IMenuItem
{
	public const float ROW_HEIGHT = 30;

	class RowButton : Button
	{
		internal readonly ViewMenuRow row;
		readonly Menu menu;
		public RowButton(Menu menu, ViewMenuRow row) : base(menu) {
			this.menu = menu;
			this.row = row;
			Text = row.Text;
			BorderSize = 0;
			SetAutoSize(false);
		}

		public override void PaintBackground(float width, float height) {
			if (!IsHovered()) return;
			Graphics2D.SetDrawColor(70, 80, 90, 222);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		public override void PaintBorder(float width, float height) { }

		public override void Paint(float width, float height) {
			float x = 10;
			if (row.Icon != null) {
				Graphics2D.SetDrawColor(255, 255, 255, row.Dim ? 120 : 255);
				Graphics2D.SetTexture(textures.LoadTextureFromFile(row.Icon));
				Graphics2D.DrawTexturedRectangle(
					new Vector2F(x, MathF.Round((height - ViewColors.ICON_SIZE) / 2)),
					new Vector2F(ViewColors.ICON_SIZE));
			}
			if (HasIcons(menu))
				x += ViewColors.ICON_SIZE + 8;
			Graphics2D.SetDrawColor(row.Dim ? ViewColors.TextDim : ViewColors.Text);
			Graphics2D.DrawText(x, height / 2, row.Text, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE, Anchor.CenterLeft);
			if (row.Hint != null) {
				Graphics2D.SetDrawColor(ViewColors.TextDim);
				Graphics2D.DrawText(width - 10, height / 2, row.Hint, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE, Anchor.CenterRight);
			}
		}

		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			if (!IsHovered()) return true;
			Element? top = menu;
			while (top?.GetParent() is Menu parent)
				top = parent;
			(top as Menu)?.Close();
			row.Invoke();
			return true;
		}
	}

	private static bool HasIcons(Menu menu) {
		using var children = menu.GetChildren();
		foreach (var child in children) {
			if (child is RowButton { row.Icon: not null }) return true;
		}
		return false;
	}

	public void Construct(Menu parent) {
		var button = new RowButton(parent, this);
		button.Dock = Dock.Top;
		button.Size = new(0, ROW_HEIGHT);
	}

	public float Width(bool icons) {
		float width = 10 + Graphics2D.GetTextSize(Text, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE).X + 10;
		if (icons)
			width += ViewColors.ICON_SIZE + 8;
		if (Hint != null)
			width += 40 + Graphics2D.GetTextSize(Hint, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE).X;
		return width + 16;
	}

	public static void Open(Menu menu, Vector2F at, bool rightAligned, IEnumerable<ViewMenuRow>? rows = null) {
		rows ??= [];
		bool icons = rows.Any(x => x.Icon != null);
		float width = 0;
		bool anyRow = false;
		foreach (var row in rows) {
			float rowWidth = row.Width(icons);
			width = anyRow ? MathF.Max(width, rowWidth) : rowWidth;
			anyRow = true;
		}
		if (!anyRow) width = 140;
		menu.MinimumWidth = MathF.Max(width, 140);
		menu.Open(rightAligned ? at - new Vector2F(menu.MinimumWidth + 12, 0) : at);
	}
}

/// <summary>
/// Paints the tab being dragged and where it will land.
/// </summary>
public class ViewDragOverlay : Panel
{
	public View Dragging = null!;
	public Vector2F Mouse;
	public ViewDropTarget Target;

	public ViewDragOverlay(Element parent) : base(parent) {
		SetPassthru(true);
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	protected override void OnThink() {
		Size = UI.GetRenderBounds().Size;
		MoveToFront();
	}

	public override void Paint(float width, float height) {
		var area = Target.Highlight;
		if (!Target.Failed) {
			if (Target.Tabs != null) {
				Graphics2D.SetDrawColor(ViewColors.Drop);
				Graphics2D.DrawRectangle(area.X - 1, area.Y, 3, area.H);
			}
			else {
				Graphics2D.SetDrawColor(ViewColors.Drop, 70);
				Graphics2D.DrawRectangle(area.X, area.Y, area.W, area.H);
				Graphics2D.SetDrawColor(ViewColors.Drop);
				Graphics2D.DrawRectangleOutline(area.X, area.Y, area.W, area.H, 2);
			}
		}

		// the tab chip
		float textW = Graphics2D.GetTextSize(Dragging.Name, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE).X;
		float chipW = 10 + (Dragging.Icon != null ? ViewColors.ICON_SIZE + 6 : 0) + textW + 12;
		var pos = Mouse + new Vector2F(14, 10);
		Graphics2D.SetDrawColor(ViewColors.Body);
		Graphics2D.DrawRectangle(pos.X, pos.Y, chipW, ViewTabStrip.HEIGHT);
		float x = pos.X + 10;
		if (Dragging.Icon != null) {
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(textures.LoadTextureFromFile(Dragging.Icon));
			Graphics2D.DrawTexturedRectangle(
				new Vector2F(x, pos.Y + MathF.Round((ViewTabStrip.HEIGHT - ViewColors.ICON_SIZE) / 2)),
				new Vector2F(ViewColors.ICON_SIZE));
			x += ViewColors.ICON_SIZE + 6;
		}
		Graphics2D.SetDrawColor(ViewColors.Text);
		Graphics2D.DrawText(x, pos.Y + ViewTabStrip.HEIGHT / 2, Dragging.Name, Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE, Anchor.CenterLeft);
	}
}

public struct ViewDropTarget
{
	/// <summary>
	/// If moving the view is impossible.
	/// </summary>
	public bool Failed;
	/// <summary>
	/// Set when dropping into a tab strip
	/// </summary>
	public ViewGroup? Tabs;
	public int TabIndex;
	/// <summary>
	/// Set when splitting a group
	/// </summary>
	public ViewGroup? Split;
	public Dock Side;
	public RectangleF Highlight;

	public static ViewDropTarget Fail => new() { Failed = true };
}

/// <summary>
/// Holds every view and one layout per editor mode.
/// </summary>
public class ViewHost : Panel
{
	public record ViewInfo(string Id, View View, string? Icon, ButtonCode? Key, bool Setup, bool Animate, Dock Side);

	public const string VIEWPORT_ID = "viewport";

	readonly Dictionary<string, ViewInfo> infos = [];
	readonly List<ViewInfo> order = [];
	readonly Dictionary<View, string> ids = [];
	readonly Dictionary<string, string> defaults = [];
	readonly Dictionary<string, Element> roots = [];
	readonly Dictionary<string, List<View>> minimizedByAll = [];
	readonly Dictionary<(string, View), ViewGroup> lastGroup = [];

	/// <summary>
	/// Closed views, and views the active layout doesn't use
	/// </summary>
	public readonly Panel Stash;
	public View? Viewport { get; private set; }
	public string Mode { get; private set; } = "";
	public View? FocusedView { get; private set; }

	public event Action? LayoutChanged;

	public IEnumerable<ViewInfo> Infos => order;

	public ViewHost(Element parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		BorderSize = 0;
		DockPadding = RectangleF.Zero;

		Stash = new(this);
		Stash.SetVisible(false);

		UI.Input.OnClick += Clicked;
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		if (UI != null)
			UI.Input.OnClick -= Clicked;
	}

	private void Clicked(Element? element) {
		while (element != null) {
			if (element is View view && (ids.ContainsKey(view) || view == Viewport)) {
				Focus(view);
				return;
			}
			element = element.GetParent();
		}
	}

	public void SetViewport(View viewport) {
		Viewport = viewport;
		ids[viewport] = VIEWPORT_ID;
	}

	public T Register<T>(string id, T view, string? icon, ButtonCode? key, bool setup, bool animate, Dock side = Dock.Right) where T : View {
		var info = new ViewInfo(id, view, icon, key, setup, animate, side);
		view.Icon = icon;
		infos[id] = info;
		order.Add(info);
		ids[view] = id;
		view.SetParent(Stash);
		return view;
	}

	public void AddMode(string mode, string defaultLayout) => defaults[mode] = defaultLayout;

	public bool AllowedIn(ViewInfo info, string mode) => mode == "animate" ? info.Animate : info.Setup;

	public Element? ActiveRoot => roots.GetValueOrDefault(Mode);

	public static bool IsCollapsed(Element node) => node switch {
		ViewGroup group => !group.Fixed && !group.ShownViews.Any(),
		ViewSplit split => IsCollapsed(split.First) && IsCollapsed(split.Second),
		_ => false
	};

	public IEnumerable<ViewGroup> Groups(Element? node = null) {
		node ??= ActiveRoot;
		switch (node) {
			case ViewGroup group:
				yield return group;
				break;
			case ViewSplit split:
				foreach (var inner in Groups(split.First))
					yield return inner;
				foreach (var inner in Groups(split.Second))
					yield return inner;
				break;
		}
	}

	public ViewGroup? GroupOf(View view) => Groups().FirstOrDefault(x => x.Views.Contains(view));
	public bool IsOpen(View view) => GroupOf(view) != null;

	public IEnumerable<View> MinimizedViews() {
		foreach (var group in Groups()) {
			foreach (var view in group.Views) {
				if (group.Minimized.Contains(view))
					yield return view;
			}
		}
	}

	public View? Find(string id) => infos.TryGetValue(id, out var info) ? info.View : null;
	public ViewInfo? InfoOf(View view) => ids.TryGetValue(view, out var id) ? infos.GetValueOrDefault(id) : null;

	// Building

	private static string LayoutKey(string mode) => $"modeleditor_layout_{mode}";

	/// <summary>
	/// Builds every mode's layout from config, falling back to the defaults.
	/// </summary>
	public void Load() {
		foreach (var mode in defaults.Keys) {
			var saved = Host.GetDataStore<string>(LayoutKey(mode));
			roots[mode] = (saved == null ? null : Build(saved, mode)) ?? Build(defaults[mode], mode)
				?? throw new Exception($"Default layout for {mode} is broken");
			roots[mode].SetVisible(false);
		}
		SetMode(defaults.Keys.First());
	}

	public void ResetLayout() => ResetLayout(Mode);

	public void ResetLayout(string mode) {
		if (roots.TryGetValue(mode, out var old))
			DropTree(old);
		minimizedByAll.Remove(mode);
		roots[mode] = Build(defaults[mode], mode)!;
		roots[mode].SetVisible(false);
		if (mode == Mode) {
			Mode = "";
			SetMode(mode);
		}
		Save(mode);
	}

	private void DropTree(Element root) {
		foreach (var group in Groups(root)) {
			foreach (var view in group.Views.ToList()) {
				if (view.GetParent() == group.Body) {
					view.SetVisible(false);
					view.SetParent(Stash);
				}
			}
		}
		root.Remove();
	}

	public void SetMode(string mode) {
		if (Mode == mode) return;
		if (roots.TryGetValue(Mode, out var old))
			old.SetVisible(false);
		Mode = mode;
		var root = roots[mode];
		root.SetVisible(true);
		foreach (var group in Groups(root))
			group.Attach();
		if (FocusedView != null && !IsOpen(FocusedView) && FocusedView != Viewport)
			FocusedView = null;
		Relayout();
		LayoutChanged?.Invoke();
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		var root = ActiveRoot;
		if (root == null) return;
		root.Position = Vector2F.Zero;
		root.Size = new(width, height);
	}

	public void Relayout() {
		InvalidateLayout();
		var root = ActiveRoot;
		if (root == null) return;
		root.InvalidateLayout();
		Walk(root);
	}

	private static void Walk(Element node) {
		node.InvalidateLayout();
		if (node is ViewSplit split) {
			Walk(split.First);
			Walk(split.Second);
		}
	}

	internal void Changed() {
		Relayout();
		Save(Mode);
		LayoutChanged?.Invoke();
	}

	private void Save(string mode) {
		if (roots.TryGetValue(mode, out var root))
			Host.SetDataStore(LayoutKey(mode), Serialize(root));
	}

	// Layout strings: split = h|v ratio (a,b), group = [id, id*, id~], * = active, ~ = minimized

	public string Serialize(Element node) {
		StringBuilder builder = new();
		Write(node, builder);
		return builder.ToString();
	}

	private void Write(Element node, StringBuilder builder) {
		switch (node) {
			case ViewSplit split:
				builder.Append(split.Vertical ? 'v' : 'h');
				builder.Append(split.Ratio.ToString("0.####", CultureInfo.InvariantCulture));
				builder.Append('(');
				Write(split.First, builder);
				builder.Append(',');
				Write(split.Second, builder);
				builder.Append(')');
				break;
			case ViewGroup group:
				builder.Append('[');
				var active = group.ActiveView;
				for (int i = 0; i < group.Views.Count; i++) {
					var view = group.Views[i];
					if (i > 0)
						builder.Append(',');
					builder.Append(ids[view]);
					if (view == active && !group.Fixed)
						builder.Append('*');
					if (group.Minimized.Contains(view))
						builder.Append('~');
				}
				builder.Append(']');
				break;
		}
	}

	/// <summary>
	/// Builds a layout tree, null if the string is broken or has no viewport.
	/// </summary>
	public Element? Build(string layout, string mode) {
		int at = 0;
		HashSet<string> used = [];
		Element? result;
		try {
			result = Parse(layout, ref at, mode, used);
		}
		catch (FormatException) {
			result = null;
		}
		if (result == null || at != layout.Length || !used.Contains(VIEWPORT_ID)) {
			if (result != null)
				DropTree(result);
			return null;
		}
		return result;
	}

	private Element? Parse(string layout, ref int at, string mode, HashSet<string> used) {
		if (at >= layout.Length)
			throw new FormatException();
		char symbol = layout[at];
		if (symbol == 'h' || symbol == 'v') {
			at++;
			int start = at;
			while (at < layout.Length && (char.IsDigit(layout[at]) || layout[at] == '.'))
				at++;
			float ratio = float.Parse(layout.AsSpan(start, at - start), CultureInfo.InvariantCulture);
			Expect(layout, ref at, '(');
			var first = Parse(layout, ref at, mode, used);
			Expect(layout, ref at, ',');
			var second = Parse(layout, ref at, mode, used);
			Expect(layout, ref at, ')');
			if (first == null) return second;
			if (second == null) return first;
			var split = new ViewSplit(this, this, symbol == 'v', Math.Clamp(ratio, 0.02f, 0.98f));
			split.Set(first, second);
			return split;
		}
		if (symbol == '[') {
			at++;
			List<(string Id, bool Active, bool Minimized)> items = [];
			while (at < layout.Length && layout[at] != ']') {
				int start = at;
				while (at < layout.Length && (char.IsLetterOrDigit(layout[at]) || layout[at] == '_'))
					at++;
				var id = layout[start..at];
				bool isActive = false;
				bool isMinimized = false;
				while (at < layout.Length && (layout[at] == '*' || layout[at] == '~')) {
					if (layout[at] == '*')
						isActive = true;
					else
						isMinimized = true;
					at++;
				}
				items.Add((id, isActive, isMinimized));
				if (at < layout.Length && layout[at] == ',')
					at++;
			}
			Expect(layout, ref at, ']');

			if (items.Any(x => x.Id == VIEWPORT_ID)) {
				if (items.Count != 1 || !used.Add(VIEWPORT_ID) || Viewport == null)
					throw new FormatException();
				var fixedGroup = new ViewGroup(this, this, true);
				fixedGroup.Views.Add(Viewport);
				return fixedGroup;
			}

			ViewGroup? group = null;
			View? activeView = null;
			foreach (var item in items) {
				if (!infos.TryGetValue(item.Id, out var info) || !AllowedIn(info, mode) || !used.Add(item.Id))
					continue;
				group ??= new ViewGroup(this, this);
				group.Views.Add(info.View);
				if (item.Minimized)
					group.Minimized.Add(info.View);
				if (item.Active)
					activeView = info.View;
			}
			if (group != null && activeView != null)
				group.SetActiveQuiet(activeView);
			return group;
		}
		throw new FormatException();
	}

	private static void Expect(string layout, ref int at, char expected) {
		if (at >= layout.Length || layout[at] != expected)
			throw new FormatException();
		at++;
	}

	// Tree edits

	private void ReplaceNode(Element old, Element with) {
		if (old.GetParent() is ViewSplit split)
			split.Replace(old, with);
		else {
			with.SetParent(this);
			with.Dock = Dock.None;
			roots[Mode] = with;
		}
	}

	private void RemoveGroup(ViewGroup group) {
		if (group.GetParent() is not ViewSplit split) return;
		var other = split.Other(group);
		ReplaceNode(split, other);
		split.Remove();
	}

	private void SplitNode(Element target, ViewGroup with, Dock side, float share) {
		bool vertical = side == Dock.Top || side == Dock.Bottom;
		bool first = side == Dock.Left || side == Dock.Top;
		var split = new ViewSplit(this, this, vertical, first ? share : 1 - share);
		ReplaceNode(target, split);
		if (first)
			split.Set(with, target);
		else
			split.Set(target, with);
	}

	private void TakeOut(View view, ViewGroup group) {
		lastGroup[(Mode, view)] = group;
		group.Remove(view);
	}

	private void CleanUp(ViewGroup group) {
		if (!group.Fixed && group.Views.Count == 0)
			RemoveGroup(group);
	}

	// Actions

	public void Focus(View? view) {
		FocusedView = view;
	}

	/// <summary>
	/// Opens a view in the active layout, or brings it forward when it's already open.
	/// </summary>
	public void Open(string id) {
		if (!infos.TryGetValue(id, out var info) || !AllowedIn(info, Mode)) return;
		var view = info.View;
		var group = GroupOf(view);
		if (group == null) {
			if (lastGroup.TryGetValue((Mode, view), out var last) && Groups().Contains(last) && !last.Fixed) {
				last.Add(view);
				group = last;
			}
			else {
				var viewportGroup = Groups().First(x => x.Fixed);
				group = new ViewGroup(this, this);
				group.Add(view);
				SplitNode(viewportGroup, group, info.Side, info.Side == Dock.Bottom ? 0.3f : 0.25f);
			}
		}
		group.Minimized.Remove(view);
		minimizedByAll.GetValueOrDefault(Mode)?.Remove(view);
		group.SetActive(view);
		Focus(view);
		Changed();
	}

	public void Close(View view) {
		var group = GroupOf(view);
		if (group == null || group.Fixed) return;
		TakeOut(view, group);
		CleanUp(group);
		if (FocusedView == view)
			FocusedView = null;
		Changed();
	}

	public void Minimize(View view) {
		var group = GroupOf(view);
		if (group == null || group.Fixed) return;
		group.Minimized.Add(view);
		group.Refresh();
		if (FocusedView == view)
			FocusedView = null;
		Changed();
	}

	public void Restore(View view) {
		var group = GroupOf(view);
		if (group == null) return;
		group.Minimized.Remove(view);
		minimizedByAll.GetValueOrDefault(Mode)?.Remove(view);
		group.SetActive(view);
		Focus(view);
		Changed();
	}

	/// <summary>
	/// Minimizes every view, or brings back the ones this minimized last time.
	/// </summary>
	public void ToggleMinimizeAll() {
		if (minimizedByAll.TryGetValue(Mode, out var list) && list.Count > 0) {
			foreach (var view in list) {
				var group = GroupOf(view);
				if (group == null) continue;
				group.Minimized.Remove(view);
				group.Refresh();
			}
			list.Clear();
		}
		else {
			list = [];
			foreach (var group in Groups().Where(x => !x.Fixed)) {
				foreach (var view in group.ShownViews.ToList()) {
					group.Minimized.Add(view);
					list.Add(view);
				}
			}
			foreach (var group in Groups())
				group.Refresh();
			minimizedByAll[Mode] = list;
			if (FocusedView != null && FocusedView != Viewport)
				FocusedView = null;
		}
		Changed();
	}

	// Dragging

	ViewDragOverlay? overlay;
	ViewGroup? dragFrom;

	internal void BeginDrag(View view, ViewGroup from) {
		overlay?.Remove();
		dragFrom = from;
		overlay = new ViewDragOverlay(UI);
		overlay.Dragging = view;
		overlay.Position = Vector2F.Zero;
		overlay.Size = UI.GetRenderBounds().Size;
		overlay.Target = ViewDropTarget.Fail;
	}

	internal void UpdateDrag(Vector2F mouse) {
		if (overlay == null || dragFrom == null) return;
		overlay.Mouse = mouse;
		overlay.Target = DropTargetAt(overlay.Dragging, dragFrom, mouse);
	}

	internal void EndDrag(Vector2F mouse) {
		if (overlay == null || dragFrom == null) return;
		var view = overlay.Dragging;
		var from = dragFrom;
		overlay.Remove();
		overlay = null;
		dragFrom = null;

		var target = DropTargetAt(view, from, mouse);
		if (!target.Failed)
			Drop(view, from, target);
	}

	public bool IsDragging => overlay != null;

	public ViewDropTarget DropTargetAt(View view, ViewGroup from, Vector2F mouse) {
		foreach (var group in Groups()) {
			if (!group.IsVisible()) continue;
			var bounds = RectangleF.FromPosAndSize(group.GetGlobalPosition(), group.GetRenderBounds().Size);
			if (!bounds.ContainsPoint(mouse)) continue;

			var strip = group.Strip;
			var stripBounds = RectangleF.FromPosAndSize(strip.GetGlobalPosition(), strip.GetRenderBounds().Size);
			if (!group.Fixed && stripBounds.ContainsPoint(mouse)) {
				var result = new ViewDropTarget() { Tabs = group, TabIndex = group.Views.Count };
				float x = -1;
				ViewTab? lastTab = null;
				foreach (var tab in strip.Tabs) {
					var pos = tab.GetGlobalPosition();
					var size = tab.GetRenderBounds().Size;
					if (mouse.X < pos.X + size.W / 2) {
						result.TabIndex = group.Views.IndexOf(tab.View);
						x = pos.X;
						break;
					}
					lastTab = tab;
				}
				if (x < 0)
					x = lastTab != null ? lastTab.GetGlobalPosition().X + lastTab.GetRenderBounds().W : stripBounds.X;
				result.Highlight = RectangleF.XYWH(x, stripBounds.Y, 0, stripBounds.H);
				return result;
			}

			if (group == from && group.Views.Count == 1)
				return ViewDropTarget.Fail;

			float fromLeft = (mouse.X - bounds.X) / bounds.W;
			float fromTop = (mouse.Y - bounds.Y) / bounds.H;
			float min = MathF.Min(MathF.Min(fromLeft, 1 - fromLeft), MathF.Min(fromTop, 1 - fromTop));
			Dock side;
			if (min == fromLeft)
				side = Dock.Left;
			else if (min == 1 - fromLeft)
				side = Dock.Right;
			else if (min == fromTop)
				side = Dock.Top;
			else
				side = Dock.Bottom;
			RectangleF area = side switch {
				Dock.Left => RectangleF.XYWH(bounds.X, bounds.Y, bounds.W / 3, bounds.H),
				Dock.Right => RectangleF.XYWH(bounds.X + bounds.W * 2 / 3, bounds.Y, bounds.W / 3, bounds.H),
				Dock.Top => RectangleF.XYWH(bounds.X, bounds.Y, bounds.W, bounds.H / 3),
				_ => RectangleF.XYWH(bounds.X, bounds.Y + bounds.H * 2 / 3, bounds.W, bounds.H / 3),
			};
			return new ViewDropTarget() { Split = group, Side = side, Highlight = area };
		}
		return ViewDropTarget.Fail;
	}

	public void Drop(View view, ViewGroup from, ViewDropTarget target) {
		if (target.Failed) return;
		if (target.Tabs != null) {
			var into = target.Tabs;
			int index = target.TabIndex;
			if (into == from) {
				int old = from.Views.IndexOf(view);
				if (old < index)
					index--;
				from.Views.Remove(view);
				from.Views.Insert(Math.Clamp(index, 0, from.Views.Count), view);
				from.SetActive(view);
			}
			else {
				bool wasMinimized = from.Minimized.Contains(view);
				TakeOut(view, from);
				into.Add(view, index);
				if (wasMinimized)
					into.Minimized.Add(view);
				into.SetActive(view);
				CleanUp(from);
			}
		}
		else if (target.Split != null) {
			TakeOut(view, from);
			var group = new ViewGroup(this, this);
			group.Add(view);
			SplitNode(target.Split, group, target.Side, 1f / 3f);
			CleanUp(from);
		}
		Focus(view);
		Changed();
	}
}

/// <summary>
/// The Views select box, minimized view icons and the minimize all button
/// </summary>
public class ViewsBar : Panel
{
	readonly ViewHost host;
	readonly Button views;
	readonly Button minimizeAll;
	readonly Panel icons;
	string shownIcons = "";

	public ViewsBar(Element parent, ViewHost host) : base(parent) {
		this.host = host;
		BorderSize = 0;
		DockPadding = RectangleF.Zero;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);

		views = new ViewsBoxButton(this);
		views.Dock = Dock.Right;
		views.Text = "Views";
		views.BorderSize = 0;
		views.SetAutoSize(false);
		float textWidth = Graphics2D.GetTextSize("Views", Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE).X;
		views.Size = new(30 + ViewColors.ICON_SIZE + textWidth + 20, 0);
		views.OnButtonClick += (_, _) => OpenMenu();

		minimizeAll = new MinimizeAllBox(this, host);
		minimizeAll.Dock = Dock.Right;
		minimizeAll.Size = new(32, 0);
		minimizeAll.BorderSize = 0;
		minimizeAll.SetTooltipText("Minimize all views (F9)");
		minimizeAll.OnButtonClick += (_, _) => host.ToggleMinimizeAll();

		icons = new(this);
		icons.Dock = Dock.Right;
		icons.BorderSize = 0;
		icons.DockPadding = RectangleF.Zero;
		icons.SetPaintBackgroundEnabled(false);
		icons.SetPaintBorderEnabled(false);
	}

	public Button ViewsButton => views;
	public Button MinimizeAllButton => minimizeAll;
	public List<Button> MinimizedIcons {
		get {
			List<Button> list = [];
			using var children = icons.GetChildren();
			foreach (var child in children) {
				if (child is Button button)
					list.Add(button);
			}
			return list;
		}
	}

	protected override void OnThink() {
		var minimized = host.MinimizedViews().ToList();
		var key = string.Join(",", minimized.Select(x => x.Name));
		if (key == shownIcons) return;
		shownIcons = key;

		icons.ClearChildren();
		foreach (var view in minimized) {
			var icon = new MinimizedIcon(icons, view);
			icon.Dock = Dock.Left;
			icon.Size = new(32, 0);
			icon.DockMargin = RectangleF.TLRB(0, 0, 2, 0);
			icon.SetTooltipText(view.Name);
			icon.OnButtonClick += (_, _) => host.Restore(view);
		}
		icons.Size = new(minimized.Count * 34, 0);
		InvalidateLayout();
	}

	public List<ViewMenuRow> MenuRows() {
		List<ViewMenuRow> rows = [];
		bool? lastAnimateOnly = null;
		foreach (var info in host.Infos) {
			if (!host.AllowedIn(info, host.Mode)) continue;
			bool animateOnly = !info.Setup;
			if (lastAnimateOnly != null && lastAnimateOnly != animateOnly)
				rows.Add(null!);
			lastAnimateOnly = animateOnly;
			string? hint = info.Key == null ? null : $"Alt+{info.Key.Value.ToString().Replace("Key", "")}";
			rows.Add(new ViewMenuRow(info.View.Name, info.Icon, hint, host.IsOpen(info.View), () => host.Open(info.Id)));
		}
		rows.Add(null!);
		rows.Add(new ViewMenuRow("Reset Layout", null, null, false, () => host.ResetLayout()));
		return rows;
	}

	private void OpenMenu() {
		var menu = new Menu(UI);
		var rows = MenuRows();
		foreach (var row in rows) {
			if (row == null)
				menu.AddSeparator();
			else
				menu.AddItem(row);
		}
		var at = views.GetGlobalPosition() + new Vector2F(views.GetRenderBounds().W, views.GetRenderBounds().H);
		ViewMenuRow.Open(menu, at, true, rows.Where(x => x != null));
	}

	class ViewsBoxButton(Element parent) : Button(parent)
	{
		public override void PaintBorder(float width, float height) { }
		public override void PaintBackground(float width, float height) {
			if (!IsHovered()) return;
			Graphics2D.SetDrawColor(MixColorBasedOnMouseState(this, ViewColors.TabInactive, new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1)));
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		public override void Paint(float width, float height) {
			float x = 10;
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(textures.LoadTextureFromFile("models/tree.png"));
			Graphics2D.DrawTexturedRectangle(
				new Vector2F(x, MathF.Round((height - ViewColors.ICON_SIZE) / 2)),
				new Vector2F(ViewColors.ICON_SIZE));
			x += ViewColors.ICON_SIZE + 8;
			Graphics2D.SetDrawColor(ViewColors.Text);
			Graphics2D.DrawText(x, height / 2, "Views",
				Graphics2D.UI_FONT_NAME, ViewColors.TEXT_SIZE, Anchor.CenterLeft);
			// select box corner
			Graphics2D.DrawTriangle(new(width - 6, height - 12), new(width - 6, height - 6), new(width - 12, height - 6));
		}
	}

	class MinimizeAllBox(Element parent, ViewHost host) : Button(parent)
	{
		public override void PaintBorder(float width, float height) { }
		public override void PaintBackground(float width, float height) {
			if (!IsHovered()) return;
			Graphics2D.SetDrawColor(MixColorBasedOnMouseState(this, ViewColors.TabInactive, new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1)));
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		public override void Paint(float width, float height) {
			bool anyHidden = host.MinimizedViews().Any();
			Graphics2D.SetDrawColor(IsHovered() || anyHidden ? ViewColors.Text : ViewColors.TextDim);
			float cx = MathF.Round(width / 2), cy = MathF.Round(height / 2);
			Graphics2D.DrawRectangleOutline(cx - 7, cy - 6, 10, 8, 1.5f);
			Graphics2D.DrawRectangleOutline(cx - 3, cy - 2, 10, 8, 1.5f);
		}
	}

	class MinimizedIcon(Element parent, View view) : Button(parent)
	{
		public override void PaintBorder(float width, float height) { }
		public override void PaintBackground(float width, float height) {
			Graphics2D.SetDrawColor(MixColorBasedOnMouseState(this, ViewColors.TabInactive, new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1)));
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		public override void Paint(float width, float height) {
			if (view.Icon == null) return;
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(textures.LoadTextureFromFile(view.Icon));
			float x = MathF.Round((width - ViewColors.ICON_SIZE) / 2);
			float y = MathF.Round((height - ViewColors.ICON_SIZE) / 2);
			Graphics2D.DrawTexturedRectangle(new Vector2F(x, y), new Vector2F(ViewColors.ICON_SIZE));
		}
	}
}
