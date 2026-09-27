using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI.Elements;

public interface IMenuItem
{
	public void Construct(Menu parent) { }
}
internal record MenuButton(string text, string? icon = null, Action? invoke = null) : IMenuItem;
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
			if (IsHovered()) {
				if (parent.lastHoveredPiece != this)
					parent.activeSubmenu?.Close();

				parent.lastHoveredPiece = this;
			}
		}
		public override void Paint(float width, float height) {
			float x = 0;
			var by = new Vector2F(x, 0);
			Graphics2D.OffsetDrawing(by);
			if (IsHovered()) {
				Graphics2D.SetDrawColor(parent.itemHoveredColor.Get());
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			base.Paint(width, height);
			Graphics2D.OffsetDrawing(-by);
		}
	}


	internal class MenuSubMenuButtonPanel(Menu parent, MenuSubmenu submenu) : Button(parent)
	{
		protected override void OnThink() {
			if (IsHovered()) {
				if (parent.lastHoveredPiece != this) {
					parent.activeSubmenu?.Close();
					parent.activeSubmenu = new Menu(this);
					var shouldUse = submenu.invoke?.Invoke(parent.activeSubmenu) ?? false;
					if (shouldUse) {
						parent.activeSubmenu.Open(new Vector2F(GetRenderBounds().W + 8, -7), false, parent);
					}
					else {
						parent.activeSubmenu.Close();
						parent.activeSubmenu = null;
					}
				}

				parent.lastHoveredPiece = this;
			}
		}
		public override void Paint(float width, float height) {
			float x = 0;
			var by = new Vector2F(x, 0);
			Graphics2D.OffsetDrawing(by);
			if (IsHovered()) {
				Graphics2D.SetDrawColor(parent.itemHoveredColor.Get());
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			base.Paint(width, height);
			Graphics2D.OffsetDrawing(-by);
		}
	}

	private List<IMenuItem> items = [];
	public void AddItem(IMenuItem item) {
		items.Add(item);
	}
	public void AddButton(string text, string? icon = null, Action? invoke = null) {
		items.Add(new MenuButton(text, icon, invoke));
	}
	public void AddSeparator() {
		items.Add(new MenuSeparator());
	}
	protected override void PostRenderBoundsFlush(ref RectangleF bounds) {

	}

	bool reverse = false;
	Menu? activeSubmenu = null;
	Element? lastHoveredPiece = null;

	public void Open(Vector2F pos, bool popup = true, Menu? parent = null) {
		this.Position = pos;
		this.BorderSize = 1;

		var i = 0;
		this.Clipping = false;
		reverse = false;
		activeSubmenu = null;
		lastHoveredPiece = null;

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
						var mic = MathF.Max(items.Count, 8);
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
		foreach (var child in Children) {
			if (child.Dock == Dock.Top)
				pY += child.Size.H;
			if (child is ITextElement textElement) {
				var textW = Graphics2D.GetTextSize(textElement.Text, textElement.Font, textElement.TextSize).X + 16;
				if (textW > pX) pX = textW;
			}
		}
		this.Size = new(pX + 12, pY);

		// Edge checks happen in UI space; for submenus, pos is local to the parent button
		Vector2F globalPos = pos;
		if (GetParent() is Element parentElement && parentElement != UI)
			globalPos += parentElement.GetGlobalPosition() + parentElement.ChildRenderOffset;
		var whereIsEnd = globalPos + this.Size + new Vector2F(4, 4);
		RectangleF uiBounds = UI.GetRenderBounds();

		TextAlignment lr = TextAlignment.Left;
		TextAlignment tb = TextAlignment.Top;

		if (whereIsEnd.X > uiBounds.W) {
			lr = TextAlignment.Right;
			reverse = true;
		}
		if (whereIsEnd.Y > uiBounds.H) tb = TextAlignment.Bottom;

		this.Origin = new TextAlignment2D(lr, tb).ToAnchor();
		if (popup) {
			this.MakeModal();
			this.MakePopup();
		}
		this.MoveToFront();
		this.Backdrop = true;
		this.TimeToBackdropAlpha = 0.15;

		UI.Input.OnClick += UI_OnElementClicked;
	}

	bool closing = false;
	public void Close() {
		closing = true;
		Backdrop = false;
	}

	protected override void OnThink() {
		base.OnThink();

		Opacity = (float)BackdropAlpha;

		if (closing) {
			if (BackdropAlpha <= 0)
				Remove();
		}
	}

	private void UI_OnElementClicked(Element? el) {
		if (this.Lifetime > 0.2f && (el == null || !el.IsIndirectChildOf(this))) {
			this.Close();
			UI.Input.OnClick -= UI_OnElementClicked;
		}
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		// Closing via a menu item never goes through UI_OnElementClicked; don't leave the handler holding a dead menu
		if (UI != null)
			UI.Input.OnClick -= UI_OnElementClicked;
	}
}
