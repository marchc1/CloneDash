using Nucleus.UI;
using Nucleus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor.UI
{
	public static class EditorDialogs
	{
		public static Window CreateDialogWindow(string title) {
			Window w = EngineCore.Level.UI.Add<Window>();
			w.Size = new(384, 128);
			w.Center();
			w.MakePopup();
			w.HideNonCloseButtons();
			w.Title = title;

			return w;
		}

		public static (Panel Panel, Checkbox Checkbox) CreateOptionPanel(Window dialog, bool isChecked, string label) {
			Panel p = dialog.Add<Panel>();
			p.Dock = Dock.Top;
			p.Size = new(32);
			p.DrawPanelBackground = false;

			Checkbox c = p.Add<Checkbox>();
			c.Dock = Dock.Left;
			c.Size = new(28);
			c.Checked = isChecked;

			var l = p.Add<Label>();
			l.AutoSize = true;
			l.Text = label;
			l.Dock = Dock.Left;

			return (p, c);
		}

		public static void SetupDescription(Window dialog, string text) {
			var lbl = dialog.Add<Label>();
			lbl.Text = text;
			lbl.AutoSize = true;
			lbl.Dock = Dock.Top;
			lbl.DockMargin = Types.RectangleF.TLRB(4);
		}
		public static void SetupOKCancelButtons(Window dialog, bool preferOK, Action? confirmed, Action? denied) {
			var buttons = dialog.Add<CenteredObjectsPanel>();
			buttons.Dock = Dock.Bottom;
			buttons.Size = new(0, 42);
			buttons.XSeparation = 8;
			buttons.YSeparation = 16;

			var cancel = buttons.Add<Button>();
			cancel.Text = "Cancel";
			cancel.TriggeredWhenEnterPressed = !preferOK;
			cancel.MouseReleaseEvent += (_, _, _) => {
				dialog.Remove();
				denied?.Invoke();
			};
			cancel.Size = new(64);

			var ok = buttons.Add<Button>();
			ok.Text = "OK";
			ok.TriggeredWhenEnterPressed = preferOK;
			ok.MouseReleaseEvent += (_, _, _) => {
				dialog.Remove();
				confirmed?.Invoke();
			};
			ok.Size = new(64);
		}
		public static void ConfirmAction(string title, string description, bool preferOK = true, Action? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			SetupDescription(dialog, description);
			SetupOKCancelButtons(dialog, preferOK, onConfirmed, onDenied);
		}

		public static void TextInput(string title, string description, string? text = null, bool preferOK = true, Action<string>? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			dialog.Size += new Types.Vector2F(0, 32);
			dialog.Center();
			SetupDescription(dialog, description);
			
			Textbox textbox = dialog.Add<Textbox>();
			textbox.Dock = Dock.Top;
			textbox.Text = text ?? "";
			textbox.Size = new(28);
			textbox.DockMargin = Types.RectangleF.TLRB(0, 32, 32, 0);

			SetupOKCancelButtons(dialog, preferOK, () => onConfirmed?.Invoke(textbox.Text), onDenied);
		}
	}
}
