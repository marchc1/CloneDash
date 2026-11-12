using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements;

public class DropdownSelector<T> : Button
{
	public T? Selected { get; set; } = default;
	public List<T> Items { get; } = [];
	public bool Editable { get; set; } = false;

	public DropdownSelector(Element? parent) : base(parent, text: "") {
		Setup();
	}

	public DropdownSelector(Element? parent, ReadOnlySpan<char> name) : base(parent, text: "", name: name) {
		Setup();
	}

	private void Setup() {
		SetTextAlignment(Anchor.CenterLeft);
		SetTextPadding(new(8, 0));
	}

	public static DropdownSelector<ET> FromEnum<ET>(Element parent, ET v) where ET : Enum {
		DropdownSelector<ET> selector = new DropdownSelector<ET>(parent);
		selector.Selected = v;
		foreach (var value in Enum.GetValuesAsUnderlyingType(typeof(ET))) {
			selector.Items.Add((ET)value);
		}

		return selector;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		Menu m = UI.Menu();

		foreach (var i in Items) {
			m.AddButton(OnToString?.Invoke(i) ?? i?.ToString() ?? "<NULL>", null, new(() => {
				var old = Selected;
				Selected = i;
				if ((old != null && !old.Equals(Selected)) || (old == null && i != null) || (old != null && i == null))
					OnSelectionChanged?.Invoke(this, old, Selected);
			}), EqualityComparer<T>.Default.Equals(i, Selected));
		}

		if (Editable) {
			m.AddButton("Create New...", null, () => {
				T? ret = default(T);
				if (OnNew != null) {
					ret = OnNew.Invoke();
				}

				if (ret != null) {
					var old = Selected;
					Selected = ret;
					OnSelectionChanged?.Invoke(this, old, Selected);
				}
				else {

				}
			});
		}
		m.MinimumWidth = GetRenderBounds().W * GetEffectiveScale().X;
		m.Open(ToScreenSpace(new Vector2F(0, GetRenderBounds().H)));
		return true;
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		// Arrow
		Graphics2D.SetDrawColor(GetTextColor());
		float arrowX = width - 16, arrowY = MathF.Round(height / 2) - 2;
		Graphics2D.DrawLine(new Vector2F(arrowX - 4, arrowY), new Vector2F(arrowX, arrowY + 4), 1.5f);
		Graphics2D.DrawLine(new Vector2F(arrowX, arrowY + 4), new Vector2F(arrowX + 4, arrowY), 1.5f);
	}

	bool displayedValid;
	T? displayedSelected;
	public void RefreshText() => displayedValid = false;
	protected override void OnThink() {
		if (displayedValid && EqualityComparer<T>.Default.Equals(displayedSelected, Selected))
			return;
		displayedValid = true;
		displayedSelected = Selected;
		this.Text = OnToString?.Invoke(this.Selected) ?? Selected?.ToString() ?? "<not-set>";
	}

	public delegate void OnSelectionChangedDelegate(DropdownSelector<T> self, T oldValue, T newValue);
	public event OnSelectionChangedDelegate? OnSelectionChanged;

	public delegate T? OnNewD();
	public event OnNewD? OnNew;

	public delegate string? ConvertToString(T? item);
	public event ConvertToString? OnToString;

}
