using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements
{
	public class DropdownSelector<T> : Button
	{
		public T? Selected { get; set; } = default;
		public List<T> Items { get; } = [];
		public bool Editable { get; set; } = false;

		public static DropdownSelector<ET> FromEnum<ET>(ET v) where ET : Enum {
			DropdownSelector<ET> selector = new DropdownSelector<ET>();
			selector.Selected = v;
			foreach (var value in Enum.GetValuesAsUnderlyingType(typeof(ET))) {
				selector.Items.Add((ET)value);
			}

			return selector;
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			Menu m = UI.Menu();

			foreach (var i in Items) {
				m.AddButton(OnToString?.Invoke(i) ?? i?.ToString() ?? "<NULL>", null, new(() => {
					var old = Selected;
					Selected = i;
					if ((old != null && !old.Equals(Selected)) || (old == null && i != null) || (old != null && i == null))
						OnSelectionChanged?.Invoke(this, old, Selected);
				}));
			}

			if (Editable) {
				m.AddButton("Create New...", null, () => {
					T? ret = default(T);
					if (OnNew != null) {
						ret = OnNew.Invoke();
					}

					if (ret != null) {
						OnSelectionChanged?.Invoke(this, Selected, ret);
						Selected = ret;
					}
					else {

					}
				});
			}
			m.Open(EngineCore.MousePos);
		}
		protected override void OnThink(FrameState frameState) {
			this.Text = OnToString?.Invoke(this.Selected) ?? Selected?.ToString() ?? "<not-set>";
		}

		public delegate void OnSelectionChangedDelegate(DropdownSelector<T> self, T oldValue, T newValue);
		public event OnSelectionChangedDelegate? OnSelectionChanged;

		public delegate T? OnNewD();
		public event OnNewD? OnNew;

		public delegate string? ConvertToString(T? item);
		public event ConvertToString? OnToString;

	}
}
