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
			BorderSize = 0;
			Anchor = Anchor.TopCenter;
			Origin = Anchor.TopCenter;

			Image icon = new(this);
			icon.Texture = texture;
			icon.ImageOrientation = ImageOrientation.Zoom;
			icon.Dock = Dock.Fill;
			icon.BorderSize = 0;
		}
	}
}