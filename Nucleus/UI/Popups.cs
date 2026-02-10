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

	public class PopupWindow : Window
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

		protected override void PostLayoutChildren() {
			base.PostLayoutChildren();

			if (AutomateLayout) {
				Vector2F size = new();
				foreach (var child in Children) {
					if (child.Dock != Dock.None)
						continue;

					size = new(
						MathF.Max(size.X, child.RenderBounds.W),
						MathF.Max(size.Y, child.RenderBounds.H)
					);
				}

				Size = new(
					MathF.Max(size.X, MinimumInternalSize.W),
					MathF.Max(size.Y, MinimumInternalSize.H)
				);

				if (Parent != null)
					Position = (Parent.Size / 2) - (Size / 2);
			}
		}
	}

	public static class Popups
	{
		public static PopupWindow DialogBase(this UserInterface UI, string title, bool automateLayout = true) {
			PopupWindow popup = UI.Add<PopupWindow>();
			popup.DockPadding = RectangleF.TLRB(2, 8, 8, 2);
			popup.Title = title;
			popup.Titlebar.MinimizeButton.Visible = false;
			popup.Titlebar.MaximizeButton.Visible = false;
			popup.MakePopup();
			popup.MakeModal();
			popup.AutomateLayout = automateLayout;

			return popup;
		}
		public static void DialogOKCancel(this UserInterface UI, string title, string text, Action onOK, Action? onCancel = null, bool okHighlighted = true) {
			PopupWindow popup = UI.DialogBase(title, automateLayout: false);

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
				popup.Close();
			};
			Button ok = containButtons.Add<Button>();
			ok.Text = "OK";
			ok.MouseReleaseEvent += (_, _, _) => {
				onOK?.Invoke();
				popup.Close();
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
