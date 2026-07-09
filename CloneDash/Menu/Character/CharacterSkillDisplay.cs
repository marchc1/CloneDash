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
				_star.SetImageColor(value);
				_title.SetTextColor(value);
				_text.SetTextColor(value);
			}
		}

		public string Text {
			set => _text.SetText(value);
		}

		public float Scale {
			set {
				_top.SetSize(new Vector2F(CharacterIconLabel.Width, CharacterIconLabel.Height) * value);
				_title.SetTextSize(CloneDashUI.GetFontSize(CharacterIconLabel.FontSize) * value);
				_title.SetPos(new Vector2F(value * -32, 0));
				_star.SetSize(new Vector2F(32 * value));
				_text.SetTextSize(CloneDashUI.GetFontSize(20) * value);
			}
		}

		private readonly Element _top;
		private readonly Image _star;
		private readonly Label _title;
		private readonly Label _text;

		public CharacterSkillDisplay(CharacterSelector parent) : base(parent) {
			SetDockPadding(new RectangleF());
			SetClipping(false);

			_top = new Element(this);
			_top.SetDock(Dock.Top);
			_top.SetClipping(false);

			_star = new Image(_top);
			_star.SetImage(parent.Level.Textures.LoadTextureFromFile("icons/star.png"));
			_star.SetAnchor(Anchor.CenterRight);
			_star.SetOrigin(Anchor.CenterRight);
			_star.SetClipping(false);

			_title = new Label(_top);
			_title.SetAutoSize(true);
			_title.SetAnchor(Anchor.CenterRight);
			_title.SetOrigin(Anchor.CenterRight);
			_title.SetTextAlignment(Anchor.CenterRight);
			_title.SetFont(CloneDashUI.GetBoldFont(GetScheme()));
			_title.SetText("Skill");
			_title.SetClipping(false);

			_text = new Label(this) { TextOverflowMode = TextOverflowMode.WordWrap };
			_text.SetDock(Dock.Fill);
			_text.SetDockMargin(new RectangleF(16, 0, 0, 0));
			_text.SetTextAlignment(Anchor.TopRight);
		}

		protected override void OnThink() {
			base.OnThink();

			// TODO: THIS NEEDS TO BE FIXED!
			// elements don't get their size properly set when docked
			// the label thinks its 32x32 and the text wrapping breaks because of it
			_text.SetSize(_text.GetRenderBounds().Size);
		}
	}
}