using CloneDash.Common.UI;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using Image = Nucleus.UI.Elements.Image;

namespace CloneDash.Menu.Character
{
    public class CharacterIconLabel : Element
    {
        public const float FontSize = 32;
        public const float Width = 640;
        public const float Height = 20;

        public Color Color
        {
            set
            {
                _image.SetImageColor(value);
                _label.SetTextColor(value);
            }
        }

        public string Text
        {
            set => _label.SetText(value);
        }

        public float Scale
        {
            set
            {
                _label.SetTextSize(CloneDashUI.GetFontSize(FontSize) * value);
                _label.SetPos(new Vector2F(value * 40, 0));
                _image.SetSize(new Vector2F(32 * value));
                SetSize(new Vector2F(Width, value * Height));
            }
        }

        private readonly Image _image;
        private readonly Label _label;

        public CharacterIconLabel(CharacterSelector parent, string texture) : base(parent)
        {
            SetSize(new Vector2F(Width, Height));
            SetClipping(false);

            _image = new Image(this);
            _image.SetTexture(parent.Level.Textures.LoadTextureFromFile(texture));
            _image.SetAnchor(Anchor.CenterLeft);
            _image.SetOrigin(Anchor.CenterLeft);

            _label = new Label(this);
            _label.SetAnchor(Anchor.CenterLeft);
            _label.SetOrigin(Anchor.CenterLeft);
            _label.SetTextAlignment(Anchor.CenterLeft);
            _label.SetAutoSize(true);
        }
    }
}