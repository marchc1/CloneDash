using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements
{
	public class MenuContext(UserInterface UI) {
		private List<IMenuItem> MenuItems = [];

		public void AddMenuItem(IMenuItem item) => MenuItems.Add(item);
		public void AddButton(string text, string? icon = null, Action? callback = null) => AddMenuItem(new MenuButton(text, icon, callback));

		public void Show() {
			Menu menu = UI.Menu();

			foreach(var item in MenuItems) {
				menu.AddItem(item);
			}

			menu.Open(UI.Level.FrameState.MouseState.MousePos);
		}
	}
	public class Menubar : Panel
	{
		protected override void Initialize() {
			base.Initialize();
			this.Size = new(0, 32);
			this.Dock = Dock.Top;
		}
		public MenuContext AddButton(string text, string? icon = null) {
			MenuContext context = new MenuContext(this.UI);
			Button b = Add<Button>();
			b.TextPadding = new(8);
			b.Dock = Dock.Left;
			b.AutoSize = true;
			b.Text = text;
			b.BorderSize = 0;
			b.MouseReleaseEvent += (self, state, btn) => {
				context.Show();
			};

			return context;
		}
	}
}
