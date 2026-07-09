using CloneDash.Common.UI;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Character
{
	public class CharacterSkillDisplay : Element
	{
		public Color Color {
			set {
				_star.ImageColor = value;
				_title.SetTextColor(value);
				_text.SetTextColor(value);
			}
		}

		public string Text {
			set => _text.Text = value;
		}

		public float Scale {
			set {
				_top.Size = new Vector2F(CharacterIconLabel.Width, CharacterIconLabel.Height) * value;
				_title.TextSize = CloneDashUI.GetFontSize(CharacterIconLabel.FontSize) * value;
				_title.Position = new Vector2F(value * -32, 0);
				_star.Size = new Vector2F(32 * value);
				_text.TextSize = CloneDashUI.GetFontSize(20) * value;
			}
		}

		private readonly Element _top;
		private readonly Image _star;
		private readonly Label _title;
		private readonly Label _text;

		public CharacterSkillDisplay(CharacterSelector parent) : base(parent) {
			DockPadding = new RectangleF();
			Clipping = false;

			_top = new Element(this);
			_top.Dock = Dock.Top;
			_top.Clipping = false;

			_star = new Image(_top);
			_star.Texture = parent.Level.Textures.LoadTextureFromFile("icons/star.png");
			_star.			Anchor = Anchor.CenterRight;
			_star.			Origin = Anchor.CenterRight;
			_star.Clipping = false;

			_title = new Label(_top);
			_title.SetAutoSize(true);
			_title.			Anchor = Anchor.CenterRight;
			_title.			Origin = Anchor.CenterRight;
			_title.SetTextAlignment(Anchor.CenterRight);
			_title.Font = CloneDashUI.GetBoldFont(GetScheme());
			_title.Text = "Skill";
			_title.Clipping = false;

			_text = new Label(this) { TextOverflowMode = TextOverflowMode.WordWrap };
			_text.Dock = Dock.Fill;
			_text.DockMargin = new RectangleF(16, 0, 0, 0);
			_text.SetTextAlignment(Anchor.TopRight);
		}

		protected override void OnThink() {
			base.OnThink();

			// TODO: THIS NEEDS TO BE FIXED!
			// elements don't get their size properly set when docked
			// the label thinks its 32x32 and the text wrapping breaks because of it
			_text.
			// TODO: THIS NEEDS TO BE FIXED!
			// elements don't get their size properly set when docked
			// the label thinks its 32x32 and the text wrapping breaks because of it
			Size = _text.GetRenderBounds().Size;
		}
	}
}