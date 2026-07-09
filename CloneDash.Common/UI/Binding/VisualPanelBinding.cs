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
			buttons.			Anchor = Anchor.CenterLeft;
			buttons.			Origin = Anchor.CenterLeft;

			foreach ((ButtonCode[] button, Action _) in bind.Bindings) {
				foreach (ButtonCode code in button) {
					Key key = new(buttons, code);
					key.					Anchor = Anchor.CenterLeft;
					key.					Origin = Anchor.CenterLeft;
					_keys.Add(key);
				}
			}

			_label = new Label(this);
			_label.			TextSize = CloneDashUI.GetFontSize(20);
			_label.			Anchor = Anchor.CenterLeft;
			_label.			Origin = Anchor.CenterLeft;
			_label.SetAutoSize(true);
			_label.			Text = bind.Label;
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
				Roundness = 4;
				Size = new Vector2F(Height);

				switch (key) {
					case ButtonCode.KeyUp:
					case ButtonCode.KeyLeft:
					case ButtonCode.KeyDown:
					case ButtonCode.KeyRight: {
						Image image = new(this);
							image.						Size = new Vector2F(20);
							image.						Texture = Level.Textures.LoadTextureFromFile(key switch {
							ButtonCode.KeyUp => "icons/caret-up.png",
							ButtonCode.KeyLeft => "icons/caret-left.png",
							ButtonCode.KeyDown => "icons/caret-down.png",
							ButtonCode.KeyRight => "icons/caret-right.png",
							_ => throw new ArgumentOutOfRangeException()
						});
						_inner = image;
						break;
					}

					default: {
						Label label = new(this);
							label.						Font = CloneDashUI.GetBoldFont(GetScheme());
							label.						Text = key.ToString().Trim("Key");
						_inner = label;
						break;
					}
				}

				_inner.
				Anchor = Anchor.Center;
				_inner.				Origin = Anchor.Center;
			}

			protected override void OnThink() {
				base.OnThink();

				float w = _inner.GetRenderBounds().W;
				if (w <= 20) w = 0; // if its small enough, let it be square
				Size = new Vector2F(Math.Max(Height, w + 16), Height);
			}

			public override void SetFgColor(Color value) {
				_inner.SetFgColor(value);
				(_inner as Label)?.SetTextColor(value);
				(_inner as Image)?.ImageColor = value;
			}
		}
	}
}