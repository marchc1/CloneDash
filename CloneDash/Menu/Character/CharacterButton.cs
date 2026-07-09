using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Character
{
	public class CharacterButton : Button
	{
		public CharacterButton(Element? parent, ITexture? texture) : base(parent) {
			SetPaintBackgroundEnabled(false);
			SetBgColor(Color.Blank);
			SetBorderSize(0);
			SetAnchor(Anchor.TopCenter);
			SetOrigin(Anchor.TopCenter);

			Image icon = new(this);
			icon.SetImage(texture);
			icon.SetImageOrientation(ImageOrientation.Zoom);
			icon.SetDock(Dock.Fill);
			icon.SetBorderSize(0);
		}
	}
}