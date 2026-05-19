using Nucleus.Commands;
using Nucleus.Common.Types;
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
		Switcher.Text = newName;
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
			OnTabChanged?.Invoke(this, value);

			foreach (var tab in Tabs) {
				if (tab != activeTab) {
					tab.Switcher.SetBgColor(SWITCHER_INACTIVE);
					tab.Panel.SetVisible(false);
				}
			}

			if (activeTab != null) {
				activeTab.Switcher.SetBgColor(SWITCHER_ACTIVE);
				activeTab.Panel.SetVisible(true);
			}
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
		TabGoLeft.BorderSize = 0;
		TabGoLeft.Text = "<";
		TabGoLeft.TextSize = 18;

		TabGoRight = new Button(TabSelector);
		TabGoRight.Size = new(28);
		TabGoRight.Dock = Dock.Right;
		TabGoRight.BorderSize = 0;
		TabGoRight.Text = ">";
		TabGoRight.TextSize = 18;

		TabSelectorContainer = new Panel(TabSelector);
		TabSelectorContainer.SetPaintBackgroundEnabled(false);
		TabSelectorContainer.Dock = Dock.Fill;

		TabContainer = new Panel(this);
		TabContainer.Dock = Dock.Fill;
		TabContainer.SetBgColor(SWITCHER_ACTIVE);
		TabContainer.BorderSize = 0;
		TabContainer.DockMargin = RectangleF.TLRB(-4, 8, 8, 8);
	}

	public delegate void OnTabChangedDelegate(TabView self, Tab? tab);
	public event OnTabChangedDelegate? OnTabChanged;

	public static readonly Color SWITCHER_INACTIVE = new(30, 35, 42, 200);
	public static readonly Color SWITCHER_ACTIVE = new(40, 44, 50, 245);

	public Tab AddTab(string name, string? icon = null, string? tooltip = null) {
		// We create the tab in TabContainer
		Panel panel = new Panel(TabContainer);
		panel.Dock = Dock.Fill;
		panel.SetPaintBackgroundEnabled(false);

		// The switcher in TabSelectorContainer
		Button switcher = new Button(TabSelectorContainer);
		switcher.Dock = Dock.Left;
		switcher.SetBgColor(SWITCHER_INACTIVE);
		switcher.TextPadding = new(4);
		switcher.AutoSize = true;
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
		foreach(var tab in Tabs) 
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
