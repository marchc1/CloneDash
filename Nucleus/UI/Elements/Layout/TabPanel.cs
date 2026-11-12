using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI.Elements;

public class Tab(Button switcher, Element panel)
{
	public string Name { get; internal set; } = "Tab";
	public string? Icon { get; internal set; }
	public Button Switcher => switcher;
	public Element Panel => panel;

	public void SetName(string newName) {
		Name = newName;
		Switcher.		Text = newName;
		Switcher.GetParent()?.InvalidateLayout();
		Switcher.InvalidateLayout();
	}
	public void SetIcon(string? newIcon) {
		Icon = newIcon; // unimplemented; but prob should invalidate parent etc here
	}
}
public class TabView : Panel
{
	public List<Tab> Tabs = [];

	private Tab? activeTab;
	public Tab? ActiveTab {
		get { return activeTab; }
		set {
			activeTab = value;

			foreach (var tab in Tabs)
				if (tab != activeTab)
					tab.Panel.SetVisible(false);

			if (activeTab != null) {
				activeTab.Panel.SetVisible(true);
				scrollToActive = true;
			}

			// Fire after visibility is updated, so handlers see the new state
			OnTabChanged?.Invoke(this, value);
		}
	}

	// A tab in the strip. Lighter and joined to the body when it's the active one.
	class TabSwitcher : Button
	{
		readonly TabView view;
		public Tab? Tab;

		public TabSwitcher(TabView view, Element parent) : base(parent) {
			this.view = view;
			SetAutoSize(false);
		}

		bool Active => Tab != null && view.activeTab == Tab;

		public override void PaintBackground(float width, float height) {
			Color back = Active ? view.switcherActive.Get()
				: MixColorBasedOnMouseState(this, view.switcherInactive.Get(), new(0, 1, 1.4f, 1), new(0, 1, 0.8f, 1));
			Graphics2D.SetDrawColor(back);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}

		public override void PaintBorder(float width, float height) {
			float thickness = view.Body.BorderSize;
			if (thickness <= 0) return;
			Graphics2D.SetDrawColor(IsKeyboardFocused() ? GetFocusColor() : view.Body.GetFgColor());
			Graphics2D.DrawRectangle(0, 0, width, thickness);
			Graphics2D.DrawRectangle(0, 0, thickness, height);
			Graphics2D.DrawRectangle(width - thickness, 0, thickness, height);
		}
	}

	// The box under the strip. Its top border opens up under the active tab.
	class TabBody : Panel
	{
		readonly TabView view;
		public TabBody(TabView view) : base(view) {
			this.view = view;
		}

		public override void PaintBorder(float width, float height) {
			float thickness = BorderSize;
			if (thickness <= 0) return;
			Graphics2D.SetDrawColor(GetFgColor());
			Graphics2D.DrawRectangle(0, height - thickness, width, thickness);
			Graphics2D.DrawRectangle(0, 0, thickness, height);
			Graphics2D.DrawRectangle(width - thickness, 0, thickness, height);

			var active = view.activeTab?.Switcher;
			if (active == null || !active.IsVisible()) {
				Graphics2D.DrawRectangle(0, 0, width, thickness);
				return;
			}

			// Where the active tab sits, clipped to the visible part of the strip
			var strip = view.TabSelectorContainer;
			float stripX = ToLocalSpace(strip.GetGlobalPosition()).X;
			float x0 = ToLocalSpace(active.GetGlobalPosition()).X;
			float x1 = x0 + active.GetRenderBounds().W;
			x0 = Math.Clamp(x0, stripX, stripX + strip.GetRenderBounds().W);
			x1 = Math.Clamp(x1, stripX, stripX + strip.GetRenderBounds().W);

			if (x1 - x0 <= thickness * 2) {
				Graphics2D.DrawRectangle(0, 0, width, thickness);
				return;
			}
			Graphics2D.DrawRectangle(0, 0, x0 + thickness, thickness);
			Graphics2D.DrawRectangle(x1 - thickness, 0, width - (x1 - thickness), thickness);
		}
	}

	public const float TAB_GAP = 2;
	public static Vector2F TabPadding = new(14, 0);

	Panel TabSelector;
	Button TabGoLeft;
	Button TabGoRight;
	Panel TabSelectorContainer;

	TabBody Body;
	Panel TabContainer => Body;

	float stripScroll;
	bool scrollToActive;

	public TabView(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);

		TabSelector = new Panel(this);
		TabSelector.SetPaintBackgroundEnabled(false);
		TabSelector.SetPaintBorderEnabled(false);
		TabSelector.Size = new(0, 32);
		TabSelector.Dock = Dock.Top;
		TabSelector.DockPadding = RectangleF.TLRB(0);

		TabGoLeft = new Button(TabSelector);
		TabGoLeft.Size = new(24);
		TabGoLeft.Dock = Dock.Left;
		TabGoLeft.SetPaintBorderEnabled(false);
		TabGoLeft.Text = "<";
		TabGoLeft.TextSize = 18;
		TabGoLeft.DrawBackgroundWhenMouseIdle = false;
		TabGoLeft.OnButtonClick += (_, _) => ScrollStrip(-1);

		TabGoRight = new Button(TabSelector);
		TabGoRight.Size = new(24);
		TabGoRight.Dock = Dock.Right;
		TabGoRight.SetPaintBorderEnabled(false);
		TabGoRight.Text = ">";
		TabGoRight.TextSize = 18;
		TabGoRight.DrawBackgroundWhenMouseIdle = false;
		TabGoRight.OnButtonClick += (_, _) => ScrollStrip(1);

		TabSelectorContainer = new Panel(TabSelector);
		TabSelectorContainer.SetPaintBackgroundEnabled(false);
		TabSelectorContainer.SetPaintBorderEnabled(false);
		TabSelectorContainer.DockPadding = RectangleF.TLRB(0);
		TabSelectorContainer.Dock = Dock.Fill;
		TabSelectorContainer.Clipping = true;

		Body = new TabBody(this);
		Body.Dock = Dock.Fill;
		Body.DockPadding = RectangleF.TLRB(4);
	}

	public delegate void OnTabChangedDelegate(TabView self, Tab? tab);
	public event OnTabChangedDelegate? OnTabChanged;

	public static readonly Color SWITCHER_INACTIVE = new(30, 35, 42, 200);
	public static readonly Color SWITCHER_ACTIVE = new(40, 44, 50, 245);

	SchemeableSetting<Color> switcherInactive = SchemeableSetting<Color>.Default(SWITCHER_INACTIVE);
	SchemeableSetting<Color> switcherActive = SchemeableSetting<Color>.Default(SWITCHER_ACTIVE);

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		switcherInactive.SetSchemeValue(scheme.GetColor("Nucleus.TabView.SwitcherInactive", SWITCHER_INACTIVE));
		switcherActive.SetSchemeValue(scheme.GetColor("Nucleus.TabView.SwitcherActive", SWITCHER_ACTIVE));
	}

	private float StripWidth() {
		float width = 0;
		foreach (var tab in Tabs)
			width += tab.Switcher.Size.W;
		return MathF.Max(0, width - TAB_GAP);
	}

	protected override void OnThink() {
		base.OnThink();

		Body.SetBgColor(switcherActive.Get());

		// tabs hug their labels
		foreach (var tab in Tabs) {
			var switcher = tab.Switcher;
			// dock margin comes out of the size
			float textWidth = Graphics2D.GetTextSize(switcher.Text, switcher.Font, switcher.GetRenderTextSize()).W;
			float width = MathF.Ceiling(textWidth + TabPadding.X * 2 + switcher.GetContentInset() * 2) + 1 + TAB_GAP;
			if (switcher.Size.W != width) {
				switcher.Size = new(width, 0);
				TabSelectorContainer.InvalidateLayout();
			}
		}

		float total = StripWidth();
		bool overflow = total > TabSelector.GetRenderBounds().W;
		if (TabGoLeft.IsVisible() != overflow) {
			TabGoLeft.SetVisible(overflow);
			TabGoRight.SetVisible(overflow);
			TabSelector.InvalidateLayout();
		}

		float visible = TabSelectorContainer.GetRenderBounds().W;
		float maxScroll = overflow ? MathF.Max(0, total - visible) : 0;

		if (scrollToActive && activeTab != null && visible > 0) {
			scrollToActive = false;
			float x0 = activeTab.Switcher.GetRenderBounds().X, x1 = x0 + activeTab.Switcher.GetRenderBounds().W;
			if (x0 < stripScroll)
				stripScroll = x0;
			else if (x1 > stripScroll + visible)
				stripScroll = x1 - visible;
		}

		stripScroll = Math.Clamp(stripScroll, 0, maxScroll);
		TabSelectorContainer.ChildRenderOffset = new Vector2F(-MathF.Round(stripScroll), 0);
		TabGoLeft.SetMouseInputEnabled(stripScroll > 0);
		TabGoRight.SetMouseInputEnabled(stripScroll < maxScroll);
	}

	public void ScrollStrip(int direction) {
		float visible = TabSelectorContainer.GetRenderBounds().W;
		foreach (var tab in direction < 0 ? Enumerable.Reverse(Tabs) : Tabs) {
			var bounds = tab.Switcher.GetRenderBounds();
			if (direction < 0 && bounds.X < stripScroll - 1) {
				stripScroll = bounds.X;
				return;
			}
			if (direction > 0 && bounds.X + bounds.W > stripScroll + visible + 1) {
				stripScroll = bounds.X + bounds.W - visible;
				return;
			}

		}
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (!TabGoLeft.IsVisible())
			return false;
		stripScroll -= (delta.X != 0 ? delta.X : delta.Y) * 40;
		return true;
	}

	public void StepTab(int direction) {
		if (Tabs.Count == 0)
			return;
		int index = activeTab == null ? 0 : Tabs.IndexOf(activeTab) + direction;
		ActiveTab = Tabs[Math.Clamp(index, 0, Tabs.Count - 1)];
	}

	public Tab AddTab(string name, string? icon = null, string? tooltip = null) {
		// We create the tab in TabContainer
		Panel panel = new Panel(TabContainer);
		panel.		Dock = Dock.Fill;
		panel.SetPaintBackgroundEnabled(false);
		panel.SetPaintBorderEnabled(false);
		panel.SetVisible(Tabs.Count == 0);

		// The switcher in TabSelectorContainer
		TabSwitcher switcher = new TabSwitcher(this, TabSelectorContainer);
		switcher.		Dock = Dock.Left;
		switcher.DockMargin = RectangleF.TLRB(0, 0, TAB_GAP, 0);
		if (tooltip != null)
			switcher.TooltipText = tooltip;
		switcher.SetTextPadding(TabPadding);

		// A new tab instance
		Tab newTab = new Tab(switcher, panel);
		switcher.Tab = newTab;
		newTab.SetName(name);
		newTab.SetIcon(icon);

		// No tabs? Set active tab
		int tabCount = Tabs.Count;
		Tabs.Add(newTab);
		if (tabCount <= 0) {
			ActiveTab = newTab;
		}

		switcher.OnButtonClick += (_, _) => ActiveTab = newTab;

		return newTab;
	}

	public void SetActiveTabByName(ReadOnlySpan<char> name) {
		foreach (var tab in Tabs)
			if (name.Equals(tab.Name, StringComparison.InvariantCultureIgnoreCase))
				ActiveTab = tab;
	}

	public void BindTabNameToConVar(ConVar convar) {
		SetActiveTabByName(convar.GetString());
		OnTabChanged += (self, tab) => {
			convar.SetValue(tab?.Name ?? "");
		};
	}
}
