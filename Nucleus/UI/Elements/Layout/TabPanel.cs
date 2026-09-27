using Nucleus.Commands;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
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

			foreach (var tab in Tabs) {
				if (tab != activeTab) {
					tab.Switcher.SetBgColor(switcherInactive.Get());
					tab.Panel.SetVisible(false);
				}
			}

			if (activeTab != null) {
				activeTab.Switcher.SetBgColor(switcherActive.Get());
				activeTab.Panel.SetVisible(true);
			}

			// Fire after visibility is updated, so handlers see the new state
			OnTabChanged?.Invoke(this, value);
		}
	}

	Panel TabSelector;
	Button TabGoLeft;
	Button TabGoRight;
	Panel TabSelectorContainer;

	Panel TabContainer;

	public TabView(Element? parent) : base(parent) {
		TabSelector = new Panel(this);
		TabSelector.SetPaintBackgroundEnabled(false);
		TabSelector.Size = new(0, 32);
		TabSelector.Dock = Dock.Top;

		TabGoLeft = new Button(TabSelector);
		TabGoLeft.Size = new(28);
		TabGoLeft.Dock = Dock.Left;
		TabGoLeft.SetPaintBorderEnabled(false);
		TabGoLeft.Text = "<";
		TabGoLeft.TextSize = 18;
		TabGoLeft.OnButtonClick += (_, _) => StepTab(-1);

		TabGoRight = new Button(TabSelector);
		TabGoRight.Size = new(28);
		TabGoRight.Dock = Dock.Right;
		TabGoRight.SetPaintBorderEnabled(false);
		TabGoRight.Text = ">";
		TabGoRight.TextSize = 18;
		TabGoRight.OnButtonClick += (_, _) => StepTab(1);

		TabSelectorContainer = new Panel(TabSelector);
		TabSelectorContainer.SetPaintBackgroundEnabled(false);
		TabSelectorContainer.SetPaintBorderEnabled(false);
		TabSelectorContainer.Dock = Dock.Fill;

		TabContainer = new Panel(this);
		TabContainer.Dock = Dock.Fill;
		TabContainer.SetBgColor(switcherActive.Get());
		TabContainer.BorderSize = 0;
		TabContainer.DockMargin = RectangleF.TLRB(-4, 8, 8, 8);
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

		TabContainer?.SetBgColor(switcherActive.Get());
		foreach (var tab in Tabs)
			tab.Switcher.SetBgColor(tab == activeTab ? switcherActive.Get() : switcherInactive.Get());
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
		panel.SetVisible(Tabs.Count == 0); 
		
		// The switcher in TabSelectorContainer
		Button switcher = new Button(TabSelectorContainer);
		switcher.		Dock = Dock.Left;
		switcher.SetBgColor(switcherInactive.Get());
		if (tooltip != null)
			switcher.TooltipText = tooltip;
		switcher.SetTextPadding(new(4));
		switcher.SetAutoSize(true);
		switcher.BorderSize = 0;

		// A new tab instance
		Tab newTab = new Tab(switcher, panel);
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
