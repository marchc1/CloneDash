using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI.Elements;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public enum FileDialogMode
	{
		Open,
		OpenFolder,
		Save
	}
	public static class Popups
	{
		public static void DialogOKCancel(this UserInterface UI, string title, string text, Action onOK, Action? onCancel = null, bool okHighlighted = true) {
			Window popup = UI.Add<Window>();
			popup.DockPadding = RectangleF.TLRB(2, 8, 8, 2);
			popup.Title = title;
			popup.Titlebar.MinimizeButton.Visible = false;
			popup.Titlebar.MaximizeButton.Visible = false;
			popup.MakePopup();
			popup.MakeModal();

			FlexPanel containButtons = popup.Add<FlexPanel>();
			containButtons.Dock = Dock.Bottom;
			containButtons.DockMargin = RectangleF.TLRB(0, 0, 0, 5);
			containButtons.Size = new(0, 48);
			containButtons.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			containButtons.DockPadding = RectangleF.TLRB(2, 2, 2, 2);

			Button close = containButtons.Add<Button>();
			close.Text = "Cancel";
			close.MouseReleaseEvent += (_, _, _) => {
				onCancel?.Invoke();
				popup.Remove();
			};
			Button ok = containButtons.Add<Button>();
			ok.Text = "OK";
			ok.MouseReleaseEvent += (_, _, _) => {
				onOK?.Invoke();
				popup.Remove();
			};
			if (okHighlighted)
				ok.TriggeredWhenEnterPressed = true;
			else
				close.TriggeredWhenEnterPressed = true;

			Label lb = popup.Add<Label>();
			lb.TextSize = 17;
			lb.Text = text.Replace("\r", "");
			lb.Dock = Dock.Fill;

			var txtsize = Graphics2D.GetTextSize(lb.Text, lb.Font, lb.TextSize);
			var titlesize = Graphics2D.GetTextSize(title, popup.Titlebar.Font, popup.Titlebar.TextSize);
			var finalsize = new Vector2F(MathF.Max(txtsize.X, titlesize.X + 64), txtsize.Y);
			popup.Size = new Vector2F(100, 200) + finalsize;
			popup.Center();

			EngineCore.Level.Sounds.PlaySound(EngineCore.Level.Sounds.LoadSoundFromFile("popup.wav"), 0.6f, 1, 0.5f);
		}
	}
}
