using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements
{
	public class Tab(Button switcher, Element panel)
	{
		public string Name { get; internal set; } = "Tab";
		public string? Icon { get; internal set; }
		public Button Switcher => switcher;
		public Element Panel => panel;

		public void SetName(string newName) {
			Name = newName;
			Switcher.Text = newName;
			Switcher.InvalidateParentAndItsChildren();
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
						tab.Switcher.BackgroundColor = SWITCHER_INACTIVE;
						tab.Panel.Visible = true;
						tab.Panel.Enabled = true;
					}
				}

				if (activeTab != null) {
					activeTab.Switcher.BackgroundColor = SWITCHER_ACTIVE;
					activeTab.Panel.Visible = true;
					activeTab.Panel.Enabled = true;
				}
			}
		}

		Panel TabSelector;
		Button TabGoLeft;
		Button TabGoRight;
		Panel TabSelectorContainer;

		Panel TabContainer;

		protected override void Initialize() {
			base.Initialize();

			TabSelector = Add<Panel>();
			TabSelector.DrawPanelBackground = false;
			TabSelector.Size = new(0, 32);
			TabSelector.Dock = Dock.Top;

			TabGoLeft = TabSelector.Add<Button>();
			TabGoLeft.Size = new(28);
			TabGoLeft.Dock = Dock.Left;
			TabGoLeft.BorderSize = 0;
			TabGoLeft.Text = "<";
			TabGoLeft.TextSize = 18;

			TabGoRight = TabSelector.Add<Button>();
			TabGoRight.Size = new(28);
			TabGoRight.Dock = Dock.Right;
			TabGoRight.BorderSize = 0;
			TabGoRight.Text = ">";
			TabGoRight.TextSize = 18;

			TabSelectorContainer = TabSelector.Add<Panel>();
			TabSelectorContainer.DrawPanelBackground = false;
			TabSelectorContainer.Dock = Dock.Fill;

			TabContainer = Add<Panel>();
			TabContainer.Dock = Dock.Fill;
			TabContainer.BackgroundColor = SWITCHER_ACTIVE;
			TabContainer.BorderSize = 0;
			TabContainer.DockMargin = RectangleF.TLRB(-4, 8, 8, 8);
		}

		public delegate void OnTabChangedDelegate(TabView self, Tab? tab);
		public event OnTabChangedDelegate? OnTabChanged;

		public static readonly Color SWITCHER_INACTIVE = new(30, 35, 42, 200);
		public static readonly Color SWITCHER_ACTIVE = new(40, 44, 50, 245);

		public Tab AddTab(string name, string? icon = null, string? tooltip = null) {
			// We create the tab in TabContainer
			Panel panel = TabContainer.Add<Panel>();
			panel.Dock = Dock.Fill;
			panel.DrawPanelBackground = false;

			// The switcher in TabSelectorContainer
			Button switcher = TabSelectorContainer.Add<Button>();
			switcher.Dock = Dock.Left;
			switcher.BackgroundColor = SWITCHER_INACTIVE;
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
			if (tabCount <= 0)
				ActiveTab = newTab;

			switcher.MouseReleaseEvent += (_, _, _) => ActiveTab = newTab;

			return newTab;
		}
	}
}
