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
			w.SetSize(new(384, 128));
			w.Center();
			w.MakePopup();
			w.HideNonCloseButtons();
			w.Title = title;

			return w;
		}

		public static (Panel Panel, Checkbox Checkbox) CreateOptionPanel(Window dialog, bool isChecked, string label) {
			Panel p = new Panel(dialog);
			p.SetDock(Dock.Top);
			p.SetSize(new(32));
			p.SetPaintBackgroundEnabled(false);

			Checkbox c = new Checkbox(p);
			c.SetDock(Dock.Left);
			c.SetSize(new(28));
			c.Checked = isChecked;

			var l = new Label(p);
			l.SetAutoSize(true);
			l.SetText(label);
			l.SetDock(Dock.Left);

			l.PassMouseTo(c);

			return (p, c);
		}

		public static void SetupDescription(Window dialog, string text) {
			var lbl = new Label(dialog);
			lbl.SetText(text);
			lbl.SetAutoSize(true);
			lbl.SetDock(Dock.Top);
			lbl.SetDockMargin(Types.RectangleF.TLRB(4));
		}
		public static void SetupOKCancelButtons(Window dialog, bool preferOK, Action? confirmed, Action? denied) {
			var buttons = new CenteredObjectsPanel(dialog);
			buttons.SetDock(Dock.Bottom);
			buttons.SetSize(new(0, 42));
			buttons.XSeparation = 8;
			buttons.YSeparation = 16;

			var cancel = new Button(buttons);
			cancel.SetText("Cancel");
			cancel.TriggeredWhenEnterPressed = !preferOK;
			cancel.OnButtonClick += (_, _) => {
				dialog.Remove();
				denied?.Invoke();
			};
			cancel.SetSize(new(64));

			var ok = new Button(buttons);
			ok.SetText("OK");
			ok.TriggeredWhenEnterPressed = preferOK;
			ok.OnButtonClick += (_, _) => {
				dialog.Remove();
				confirmed?.Invoke();
			};
			ok.SetSize(new(64));
		}
		public static void ConfirmAction(string title, string description, bool preferOK = true, Action? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			SetupDescription(dialog, description);
			SetupOKCancelButtons(dialog, preferOK, onConfirmed, onDenied);
		}

		public static void TextInput(string title, string description, string? text = null, bool preferOK = true, Action<ReadOnlySpan<char>>? onConfirmed = null, Action? onDenied = null) {
			Window dialog = CreateDialogWindow(title);
			dialog.SetSize(dialog.GetSize() + new Types.Vector2F(0, 32));
			dialog.Center();
			SetupDescription(dialog, description);
			
			Textbox textbox = new Textbox(dialog);
			textbox.SetDock(Dock.Top);
			textbox.SetText(text ?? "");
			textbox.SetSize(new(28));
			textbox.SetDockMargin(Types.RectangleF.TLRB(0, 32, 32, 0));
			textbox.KeyboardFocus();

			SetupOKCancelButtons(dialog, preferOK, () => onConfirmed?.Invoke(textbox.GetText()), onDenied);
		}
	}
}
