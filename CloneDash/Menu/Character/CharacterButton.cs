using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Character
{
    public class CharacterButton : Button
    {
        public CharacterButton(Element? parent, ITexture? texture) : base(parent)
        {
            Image icon = new(this);
            icon.SetTexture(texture);
            icon.SetImageOrientation(ImageOrientation.Zoom);
            icon.SetDock(Dock.Fill);

            SetBgColor(new Color(0, 0, 0, 0));
            SetBorderSize(0);
            SetText("");
        }
    }
}