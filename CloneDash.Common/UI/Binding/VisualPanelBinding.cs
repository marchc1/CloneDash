using CloneDash.Game;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Common.UI.Binding
{
	public class VisualPanelBinding : Flow
	{
		private readonly List<Key> _keys = [];
		private readonly Label _label;

		public VisualPanelBinding(Element parent, PanelBinding bind) : base(parent) {
			AutoSize = Axis.Both;
			Spacing = 8;

			Flow buttons = new(this) {
				AutoSize = Axis.Both,
				Direction = FlowDirection.Horizontal,
				Spacing = 4
			};
			buttons.SetAnchor(Anchor.CenterLeft);
			buttons.SetOrigin(Anchor.CenterLeft);

			foreach ((ButtonCode[] button, Action _) in bind.Bindings) {
				foreach (ButtonCode code in button) {
					Key key = new(buttons, code);
					key.SetAnchor(Anchor.CenterLeft);
					key.SetOrigin(Anchor.CenterLeft);
					_keys.Add(key);
				}
			}

			_label = new Label(this);
			_label.SetTextSize(CloneDashUI.GetFontSize(20));
			_label.SetAnchor(Anchor.CenterLeft);
			_label.SetOrigin(Anchor.CenterLeft);
			_label.SetAutoSize(true);
			_label.SetText(bind.Label);
		}

		public override void SetBgColor(Color value) {
			_keys.ForEach(x => x.SetBgColor(value));
			_label.SetTextColor(value);
		}

		public override void SetFgColor(Color value) {
			_keys.ForEach(x => x.SetFgColor(value));
		}

		private class Key : Element
		{
			private const float Height = 24;

			private readonly Element _inner;

			public Key(Element parent, ButtonCode key) : base(parent) {
				SetPaintBackgroundEnabled(true);
				SetRoundness(4);
				SetSize(new Vector2F(Height));

				switch (key) {
					case ButtonCode.KeyUp:
					case ButtonCode.KeyLeft:
					case ButtonCode.KeyDown:
					case ButtonCode.KeyRight: {
						Image image = new(this);
						image.SetSize(new Vector2F(20));
						image.SetTexture(Level.Textures.LoadTextureFromFile(key switch {
							ButtonCode.KeyUp => "icons/caret-up.png",
							ButtonCode.KeyLeft => "icons/caret-left.png",
							ButtonCode.KeyDown => "icons/caret-down.png",
							ButtonCode.KeyRight => "icons/caret-right.png",
							_ => throw new ArgumentOutOfRangeException()
						}));
						_inner = image;
						break;
					}

					default: {
						Label label = new(this);
						label.SetFont(CloneDashUI.GetBoldFont(GetScheme()));
						label.SetText(key.ToString().Trim("Key"));
						_inner = label;
						break;
					}
				}

				_inner.SetAnchor(Anchor.Center);
				_inner.SetOrigin(Anchor.Center);
			}

			protected override void OnThink() {
				base.OnThink();

				float w = _inner.GetRenderBounds().W;
				if (w <= 20) w = 0; // if its small enough, let it be square
				SetSize(new Vector2F(Math.Max(Height, w + 16), Height));
			}

			public override void SetFgColor(Color value) {
				_inner.SetFgColor(value);
				(_inner as Label)?.SetTextColor(value);
				(_inner as Image)?.SetImageColor(value);
			}
		}
	}
}