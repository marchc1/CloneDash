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
				Vector2F size = new();
				foreach (var child in Children) {
					if (child.GetDock() != Dock.None)
						continue;

					size = new(
						MathF.Max(size.X, child.GetRenderBounds().W),
						MathF.Max(size.Y, child.GetRenderBounds().H)
					);
				}

				SetSize(new(
					MathF.Max(size.X, MinimumInternalSize.W),
					MathF.Max(size.Y, MinimumInternalSize.H)
				));

				if (GetParent() != null)
					SetPos((GetParent().GetSize() / 2) - (GetSize() / 2));
			}
		}
	}

    public static class Popups
    {
        public static PopupWindow DialogBase(this UserInterface UI, string title, bool automateLayout = true) {
			PopupWindow popup = new PopupWindow(UI);
			popup.SetDockPadding(RectangleF.TLRB(2, 8, 8, 2));
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
            containButtons.SetDock(Dock.Bottom);
            containButtons.SetDockMargin(RectangleF.TLRB(0, 0, 0, 5));
            containButtons.SetSize(new(0, 48));
            containButtons.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
            containButtons.SetDockPadding(RectangleF.TLRB(2, 2, 2, 2));

			Label lb = new Label(popup);
			lb.SetTextSize ( 17);
            lb.SetText(text.Replace("\r", ""));
            lb.SetDock(Dock.Fill);

            var txtsize = Graphics2D.GetTextSize(lb.GetText(), lb.GetFont(), lb.GetTextSize());
            var titlesize = Graphics2D.GetTextSize(title, popup.Titlebar.GetFont(), popup.Titlebar.GetTextSize());
            var finalsize = new Vector2F(MathF.Max(txtsize.X, titlesize.X + 64), txtsize.Y);
            popup.SetSize(new Vector2F(100, 200) + finalsize);
            popup.Center();

            audiosystem.PlaySound("popup.wav", AudioPlaybackSettings.Unaltered);

            return (popup, containButtons);
        }

        public static void DialogOK(this UserInterface UI, string title, string text, Action? onOK = null, bool okHighlighted = true) {
            var (popup, containButtons) = SetupDialogCore(UI, title, text);

            Button ok = new Button(containButtons);
            ok.SetText("OK");
            ok.OnButtonClick += (_, _) => {
                onOK?.Invoke();
                popup.Close();
            };
            if (okHighlighted) ok.TriggeredWhenEnterPressed = true;
        }

        public static void DialogOKCancel(this UserInterface UI, string title, string text, Action onOK, Action? onCancel = null, bool okHighlighted = true) {
            var (popup, containButtons) = SetupDialogCore(UI, title, text);

            Button close = new Button(containButtons);
            close.SetText("Cancel");
            close.OnButtonClick += (_, _) => {
                onCancel?.Invoke();
                popup.Close();
            };

            Button ok = new Button(containButtons);
            ok.SetText("OK");
            ok.OnButtonClick += (_, _) => {
                onOK?.Invoke();
                popup.Close();
            };

            if (okHighlighted)
                ok.TriggeredWhenEnterPressed = true;
            else
                close.TriggeredWhenEnterPressed = true;
        }
    }
}
