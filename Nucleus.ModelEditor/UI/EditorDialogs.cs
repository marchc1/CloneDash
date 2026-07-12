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
			Window w = new Window(EngineCore.Level.RootPanel);
			w.			Size = new(384, 128);
			w.Center();
			w.MakePopup();
			w.HideNonCloseButtons();
			w.Title = title;

			return w;
		}

		public static (Panel Panel, Checkbox Checkbox) CreateOptionPanel(Window dialog, bool isChecked, string label) {
			Panel p = new Panel(dialog);
			p.			Dock = Dock.Top;
			p.			Size = new(32);
			p.SetPaintBackgroundEnabled(false);

			Checkbox c = new Checkbox(p);
			c.			Dock = Dock.Left;
			c.			Size = new(28);
			c.Checked = isChecked;

			var l = new Label(p);
			l.SetAutoSize(true);
			l.			Text = label;
			l.			Dock = Dock.Left;

			l.PassMouseTo(c);

			return (p, c);
		}

		public static void SetupDescription(Window dialog, string text) {
			var lbl = new Label(dialog);
			lbl.			Text = text;
			lbl.SetAutoSize(true);
			lbl.			Dock = Dock.Top;
			lbl.			DockMargin = Types.RectangleF.TLRB(4);
		}
		public static void SetupOKCancelButtons(Window dialog, bool preferOK, Action? confirmed, Action? denied) {
			var buttons = new CenteredObjectsPanel(dialog);
			buttons.			Dock = Dock.Bottom;
			buttons.			Size = new(0, 42);
			buttons.XSeparation = 8;
			buttons.YSeparation = 16;

			var cancel = new Button(buttons);
			cancel.			Text = "Cancel";
			cancel.TriggeredWhenEnterPressed = !preferOK;
			cancel.OnButtonClick += (_, _) => {
				dialog.Remove();
				denied?.Invoke();
			};
			cancel.			Size = new(64);

			var ok = new Button(buttons);
			ok.			Text = "OK";
			ok.TriggeredWhenEnterPressed = preferOK;
			ok.OnButtonClick += (_, _) => {
				dialog.Remove();
				confirmed?.Invoke();
			};
			ok.			Size = new(64);
		}
		public static void ConfirmAction(string title, string description, bool preferOK = true, Action? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			SetupDescription(dialog, description);
			SetupOKCancelButtons(dialog, preferOK, onConfirmed, onDenied);
		}

		public static void TextInput(string title, string description, string? text = null, bool preferOK = true, Action<ReadOnlySpan<char>>? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			dialog.			Size = dialog.Size + new Types.Vector2F(0, 32);
			dialog.Center();
			SetupDescription(dialog, description);
			
			Textbox textbox = new Textbox(dialog);
			textbox.			Dock = Dock.Top;
			textbox.			Text = text ?? "";
			textbox.			Size = new(28);
			textbox.			DockMargin = Types.RectangleF.TLRB(0, 32, 32, 0);
			textbox.KeyboardFocus();

			SetupOKCancelButtons(dialog, preferOK, () => onConfirmed?.Invoke(textbox.Text), onDenied);
		}
	}
}
