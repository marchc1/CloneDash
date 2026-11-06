using Nucleus.Common.Audio;
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

	public class PopupWindow(Element? parent) : Window(parent)
	{
		public bool AutomateLayout {
			get => field;
			set {
				if (field == value)
					return;

				field = value;
				InvalidateLayout();
			}
		}

		public Vector2F MinimumInternalSize {
			get => field;
			set {
				if (field == value)
					return;

				field = value;
				InvalidateLayout();
			}
		} = new(320, 260);

		protected override void PerformLayout(float width, float height) {
			if (AutomateLayout) {
				Element content = GetAddParent();
				Vector2F size = new();
				foreach (var child in content.Children) {
					if (child.Dock != Dock.None || !child.IsVisible())
						continue;

					var rb = child.GetRenderBounds();
					size = new(
						MathF.Max(size.X, rb.X + rb.W),
						MathF.Max(size.Y, rb.Y + rb.H)
					);
				}
				RectangleF contentPadding = content.DockPadding;
				size += new Vector2F(contentPadding.Right, contentPadding.Bottom);

				RectangleF pad = DockPadding, cm = content.DockMargin;
				Vector2F chrome = new(
					pad.Left + pad.Right + cm.Left + cm.Right,
					pad.Top + pad.Bottom + Titlebar.Size.H + cm.Top + cm.Bottom);

				Size = new(
					MathF.Max(size.X + chrome.X, MinimumInternalSize.W),
					MathF.Max(size.Y + chrome.Y, MinimumInternalSize.H)
				);

				if (GetParent() is Element parent)
					Position = (parent.GetRenderBounds().Size / 2) - (Size / 2);
			}
		}
	}

    public static class Popups
    {
        public static PopupWindow DialogBase(this UserInterface UI, string title, bool automateLayout = true) {
			PopupWindow popup = new PopupWindow(UI);
			popup.			DockPadding = RectangleF.TLRB(2, 8, 8, 2);
            popup.Title = title;
            popup.Titlebar.MinimizeButton.SetVisible(false);
			popup.Titlebar.MaximizeButton.SetVisible(false);
            popup.MakePopup();
            popup.MakeModal();
            popup.AutomateLayout = automateLayout;

            return popup;
        }

        private static (PopupWindow popup, FlexPanel buttonContainer) SetupDialogCore(UserInterface UI, string title, string text) {
            PopupWindow popup = UI.DialogBase(title, automateLayout: false);

            FlexPanel containButtons = new FlexPanel(popup);
			containButtons.            Dock = Dock.Bottom;
			containButtons.            DockMargin = RectangleF.TLRB(0, 0, 0, 5);
			containButtons.            Size = new(0, 48);
            containButtons.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			containButtons.            DockPadding = RectangleF.TLRB(2, 2, 2, 2);

			Label lb = new Label(popup);
			lb.			TextSize = 17;
			lb.            Text = text.Replace("\r", "");
			lb.            Dock = Dock.Fill;

            var txtsize = Graphics2D.GetTextSize(lb.Text, lb.Font, lb.TextSize);
            var titlesize = Graphics2D.GetTextSize(title, popup.Titlebar.GetFont(), popup.Titlebar.GetTextSize());
            var finalsize = new Vector2F(MathF.Max(txtsize.X, titlesize.X + 64), txtsize.Y);
			popup.            Size = new Vector2F(100, 200) + finalsize;
            popup.Center();

            audiosystem.PlaySound("popup.wav", AudioPlaybackSettings.Unaltered);

            return (popup, containButtons);
        }

        public static void DialogOK(this UserInterface UI, string title, string text, Action? onOK = null, bool okHighlighted = true) {
            var (popup, containButtons) = SetupDialogCore(UI, title, text);

            Button ok = new Button(containButtons);
			ok.            Text = "OK";
            ok.OnButtonClick += (_, _) => {
                onOK?.Invoke();
                popup.Close();
            };
            if (okHighlighted) {
                ok.TriggeredWhenEnterPressed = true;
                ok.KeyboardFocus(); 
            }
        }

        public static void DialogOKCancel(this UserInterface UI, string title, string text, Action onOK, Action? onCancel = null, bool okHighlighted = true) {
            var (popup, containButtons) = SetupDialogCore(UI, title, text);

            Button close = new Button(containButtons);
			close.            Text = "Cancel";
            close.OnButtonClick += (_, _) => {
                onCancel?.Invoke();
                popup.Close();
            };

            Button ok = new Button(containButtons);
			ok.            Text = "OK";
            ok.OnButtonClick += (_, _) => {
                onOK?.Invoke();
                popup.Close();
            };

            Button highlighted = okHighlighted ? ok : close;
            highlighted.TriggeredWhenEnterPressed = true;
            highlighted.KeyboardFocus(); 
        }
    }
}
