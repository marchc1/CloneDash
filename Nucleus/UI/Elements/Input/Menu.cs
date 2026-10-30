using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Types;

using System.Diagnostics;

namespace Nucleus.UI.Elements;

public interface IMenuItem
{
	public void Construct(Menu parent) { }
}
internal record MenuButton(string text, string? icon = null, Action? invoke = null, bool selected = false) : IMenuItem;
internal record MenuSubmenu(string text, string? icon = null, Func<Menu, bool>? invoke = null) : IMenuItem;
internal record MenuSeparator() : IMenuItem;
public class Menu(Element? parent) : Panel(parent)
{
	internal class MenuSeparatorPanel : Panel
	{
		readonly Menu menu;
		public MenuSeparatorPanel(Menu parent) : base(parent) {
			menu = parent;
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
		}
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) => !menu.Fading && base.HoverTest(bounds, mousePos);
		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(menu.separatorColor.Get());
			Graphics2D.DrawLine(8, height / 2, width - 8, height / 2);
		}
	}

	SchemeableSetting<Color> separatorColor = SchemeableSetting<Color>.Default(new(145, 145, 145, 255));
	SchemeableSetting<Color> itemHoveredColor = SchemeableSetting<Color>.Default(new(70, 80, 90, 222));

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetBgSchemeColor(scheme.GetColor("Nucleus.Menu.Background", new(20, 30, 45, 220)));
		SetFgSchemeColor(scheme.GetColor("Nucleus.Menu.Border", new(190, 195, 195, 114)));
		separatorColor.SetSchemeValue(scheme.GetColor("Nucleus.Menu.Separator", new(145, 145, 145, 255)));
		itemHoveredColor.SetSchemeValue(scheme.GetColor("Nucleus.Menu.ItemHovered", new(70, 80, 90, 222)));
	}

	internal class MenuButtonPanel(Menu parent, MenuButton btn) : Button(parent)
	{
		public bool Selected => btn.selected;
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) => !parent.Fading && base.HoverTest(bounds, mousePos);

		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			if (!IsHovered()) return true;
			btn.invoke?.Invoke();

			Menu ultimateMenu = parent;
			while (true) {
				var parent = ultimateMenu.GetParent();
				if (parent is not Menu parentMenu) break;
				ultimateMenu = parentMenu;
			}

			ultimateMenu?.Close();
			return true;
		}
		protected override void OnThink() {
			if (IsHovered())
				parent.PieceHovered(this);
		}
		public override void Paint(float width, float height) {
			float x = 0;
			var by = new Vector2F(x, 0);
			Graphics2D.OffsetDrawing(by);
			if (IsHovered()) {
				Graphics2D.SetDrawColor(parent.itemHoveredColor.Get());
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			else if (Selected) {
				var color = parent.itemHoveredColor.Get();
				Graphics2D.SetDrawColor(color.R, color.G, color.B, color.A / 2);
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			if (Selected) {
				Graphics2D.SetDrawColor(GetTextColor());
				Graphics2D.DrawRectangle(3, 6, 2, height - 12);
			}
			base.Paint(width, height);
			Graphics2D.OffsetDrawing(-by);
		}
	}


	internal class MenuSubMenuButtonPanel(Menu parent, MenuSubmenu submenu) : Button(parent)
	{
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) => !parent.Fading && base.HoverTest(bounds, mousePos);
		protected override void OnThink() {
			if (IsHovered())
				parent.PieceHovered(this);
		}

		internal void OpenSubmenu() {
			var menu = new Menu(this);
			if (submenu.invoke?.Invoke(menu) ?? false) {
				parent.activeSubmenu = menu;
				menu.Open(new Vector2F(GetRenderBounds().W + 8, -7), false, parent);
			}
			else
				menu.Remove();
		}

		bool OwnsOpenSubmenu => IValidatable.IsValid(parent.activeSubmenu) && !parent.activeSubmenu.Closing && parent.activeSubmenu.GetParent() == this;

		public override void Paint(float width, float height) {
			float x = 0;
			var by = new Vector2F(x, 0);
			Graphics2D.OffsetDrawing(by);
			if (IsHovered() || OwnsOpenSubmenu) {
				Graphics2D.SetDrawColor(parent.itemHoveredColor.Get());
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			base.Paint(width, height);

			// Arrow
			Graphics2D.SetDrawColor(GetTextColor());
			float arrowX = width - 14, arrowY = MathF.Round(height / 2);
			Graphics2D.DrawLine(new Vector2F(arrowX, arrowY - 4), new Vector2F(arrowX + 4, arrowY), 1.5f);
			Graphics2D.DrawLine(new Vector2F(arrowX + 4, arrowY), new Vector2F(arrowX, arrowY + 4), 1.5f);

			Graphics2D.OffsetDrawing(-by);
		}
	}

	private List<IMenuItem> items = [];
	public void AddItem(IMenuItem item) {
		items.Add(item);
	}
	public void AddButton(string text, string? icon = null, Action? invoke = null, bool selected = false) {
		items.Add(new MenuButton(text, icon, invoke, selected));
	}
	public void AddSeparator() {
		items.Add(new MenuSeparator());
	}
	protected override void PostRenderBoundsFlush(ref RectangleF bounds) {

	}

	/// <summary>
	/// The menu won't be thinner than this
	/// </summary>
	public float MinimumWidth { get; set; }

	Menu? activeSubmenu = null;
	Element? lastHoveredPiece = null;

	// Delay before switching away from an open submenu
	public static double SubmenuSwitchDelay = 0.3;
	Element? pendingPiece;
	long pendingStamp;

	internal void PieceHovered(Element piece) {
		if (piece == lastHoveredPiece) {
			pendingPiece = null;
			return;
		}

		if (IValidatable.IsValid(activeSubmenu) && !activeSubmenu.Closing) {
			if (pendingPiece != piece) {
				pendingPiece = piece;
				pendingStamp = Stopwatch.GetTimestamp();
			}
			if (Stopwatch.GetElapsedTime(pendingStamp).TotalSeconds < SubmenuSwitchDelay)
				return;
		}

		pendingPiece = null;
		activeSubmenu?.Close();
		activeSubmenu = null;
		lastHoveredPiece = piece;
		if (piece is MenuSubMenuButtonPanel submenuButton)
			submenuButton.OpenSubmenu();
	}

	private const float SCREEN_EDGE = 4;
	private const float SCROLLBAR_WIDTH = 6;
	bool scrollable;
	float contentHeight;
	float scroll;

	public void Open(Vector2F pos, bool popup = true, Menu? parent = null) {
		this.BorderSize = 1;

		var i = 0;
		this.Clipping = false;
		activeSubmenu = null;
		lastHoveredPiece = null;
		pendingPiece = null;
		scroll = 0;
		ChildRenderOffset = Vector2F.Zero;

		var first = items.FirstOrDefault();
		var last = items.LastOrDefault();

		foreach (var item in items) {
			switch (item) {
				case MenuSeparator sep:
					if (item == first || item == last)
						continue;

					var s = new MenuSeparatorPanel(this);
					s.Dock = Dock.Top;
					s.Size = new Types.Vector2F(0, 5);
					break;
				case MenuButton btn: {
						var b = new MenuButtonPanel(this, btn);
						b.Dock = Dock.Top;
						b.Size = new Types.Vector2F(0, 28);
						b.Text = btn.text;
						b.SetAutoSize(false);
						b.SetTextPadding(new(12, 12));
						b.TextSize = 18;
						b.SetTextAlignment(Anchor.CenterLeft);
						b.SetBgColor(new Color(0, 0, 0, 0));
						b.BorderSize = 0;
						b.Clipping = false;
					}
					break;
				case MenuSubmenu submenu: {
						var b = new MenuSubMenuButtonPanel(this, submenu);
						b.Dock = Dock.Top;
						b.Size = new Types.Vector2F(0, 28);
						b.Text = submenu.text;
						b.SetAutoSize(false);
						b.SetTextPadding(new(12, 12));
						b.TextSize = 18;
						b.SetTextAlignment(Anchor.CenterLeft);
						b.SetBgColor(new Color(0, 0, 0, 0));
						b.BorderSize = 0;

						b.
						Clipping = false;
					}
					break;
				default:
					item.Construct(this);
					break;
			}
			i++;
		}
		// Width from the widest item's text; height is just the items' heights stacked
		float pX = 0;
		float pY = 0;
		float selectedY = -1;
		float selectedH = 0;
		foreach (var child in Children) {
			if (child.Dock == Dock.Top) {
				if (child is MenuButtonPanel { Selected: true }) {
					selectedY = pY;
					selectedH = child.Size.H;
				}
				pY += child.Size.H;
			}
			if (child is ITextElement textElement) {
				var textW = Graphics2D.GetTextSize(textElement.Text, textElement.Font, textElement.TextSize).X + 16;
				if (textW > pX) pX = textW;
			}
		}
		// room for the arrows
		if (items.Any(x => x is MenuSubmenu))
			pX += 16;

		RectangleF pad = DockPadding;
		contentHeight = pY + pad.Top + pad.Bottom;

		RectangleF uiBounds = UI.GetRenderBounds();
		float height = MathF.Min(contentHeight, uiBounds.H - SCREEN_EDGE * 2);
		scrollable = height < contentHeight;
		float width = MathF.Max(pX + 12 + (scrollable ? SCROLLBAR_WIDTH : 0), MinimumWidth);
		this.Size = new(width, height);
		Clipping = scrollable;
		SetPostChildPaintEnabled(scrollable);

		// Edge checks happen in UI space, for submenus pos is local to the parent button
		Vector2F parentGlobal = Vector2F.Zero;
		Element? owner = GetParent();
		if (owner != null && owner != UI)
			parentGlobal = owner.GetGlobalPosition() + owner.ChildRenderOffset;
		Vector2F global = parentGlobal + pos;
		bool isSubmenu = parent != null && owner != null && owner != UI;

		if (global.X + width > uiBounds.W - SCREEN_EDGE) {
			// Submenus go on the other side of the parent menu.
			if (isSubmenu)
				global.X = parentGlobal.X - width - (pos.X - owner!.GetRenderBounds().W);
			else
				global.X -= width;
		}
		if (global.Y + height > uiBounds.H - SCREEN_EDGE) {
			if (isSubmenu)
				global.Y = parentGlobal.Y + owner!.GetRenderBounds().H - pos.Y - height;
			else
				global.Y -= height;
		}
		global.X = Math.Clamp(global.X, SCREEN_EDGE, MathF.Max(SCREEN_EDGE, uiBounds.W - width - SCREEN_EDGE));
		global.Y = Math.Clamp(global.Y, SCREEN_EDGE, MathF.Max(SCREEN_EDGE, uiBounds.H - height - SCREEN_EDGE));

		this.Origin = Anchor.TopLeft;
		this.Position = global - parentGlobal;

		if (scrollable && selectedY >= 0)
			SetScroll(-(selectedY + selectedH / 2 - height / 2));

		if (popup) {
			this.MakeModal(false);
			this.MakePopup();
			open.Add(this);
		}
		this.MoveToFront();
		Opacity = 0;
		openStamp = Stopwatch.GetTimestamp();

		// Opt in. Submenus would only dim their own row.
		if (Backdrop && !isSubmenu) {
			TimeToBackdropAlpha = FADE_TIME;
			Backdrop = true;
		}
		else
			Backdrop = false;

		UI.Input.OnClick += UI_OnElementClicked;
	}

	private void SetScroll(float value) {
		scroll = Math.Clamp(value, -(contentHeight - Size.H), 0);
		if (!scrollable || float.IsNaN(scroll))
			scroll = 0;
		ChildRenderOffset = new(0, scroll);
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (!scrollable)
			return false;

		// 3 items a notch
		SetScroll(scroll + delta.Y * 28 * 3);
		return true;
	}

	public override void PostChildPaint() {
		if (!scrollable)
			return;

		var bounds = GetRenderBounds();
		float height = bounds.H;
		float barHeight = MathF.Max(16, height * height / contentHeight);
		float t = -scroll / (contentHeight - height);
		Graphics2D.SetDrawColor(separatorColor.Get());
		Graphics2D.DrawRectangle(bounds.W - 5, MathF.Round(t * (height - barHeight)), 2, barHeight);
	}

	private const double FADE_TIME = 0.15;
	long openStamp;
	long closeStamp;

	bool closing = false;
	public bool Closing => closing;

	// This menu or one it's a submenu of is closing
	internal bool Fading {
		get {
			for (Element? element = this; element != null; element = element.GetParent()) {
				if (element is Menu { closing: true }) return true;
			}
			return false;
		}
	}

	public override bool HoverTest(RectangleF bounds, Vector2F mousePos) => !Fading && base.HoverTest(bounds, mousePos);

	private static readonly List<Menu> open = [];

	/// <summary>
	/// Closes every open popup menu, false if there weren't any.
	/// </summary>
	public static bool CloseOpen() {
		if (open.Count == 0) return false;
		foreach (var menu in open.ToArray())
			menu.Close();
		return true;
	}

	public void Close() {
		if (closing)
			return;
		closing = true;
		open.Remove(this);
		closeStamp = Stopwatch.GetTimestamp();
		Backdrop = false;
		// clicks go through while it fades out
		if (IsModal()) {
			UI.RemoveModal(this);
			RemoveFlag(ElementFlags.IsModal);
		}
	}

	protected override void OnThink() {
		base.OnThink();

		double t = Stopwatch.GetElapsedTime(closing ? closeStamp : openStamp).TotalSeconds / FADE_TIME;
		Opacity = (float)Math.Clamp(closing ? 1 - t : t, 0, 1);

		if (closing) {
			if (Opacity <= 0)
				Remove();
			return;
		}

		// Made it into the submenu, forget about switching
		if (pendingPiece != null && IValidatable.IsValid(activeSubmenu) && UI.GetHoveredElement() is Element hovered && (hovered == activeSubmenu || hovered.IsIndirectChildOf(activeSubmenu)))
			pendingPiece = null;

	}

	private void UI_OnElementClicked(Element? el) {
		if (this.Lifetime > 0.2f && (el == null || !el.IsIndirectChildOf(this))) {
			this.Close();
			UI.Input.OnClick -= UI_OnElementClicked;
		}
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		open.Remove(this);
		// Closing via a menu item never goes through UI_OnElementClicked; don't leave the handler holding a dead menu
		if (UI != null)
			UI.Input.OnClick -= UI_OnElementClicked;
	}
}
